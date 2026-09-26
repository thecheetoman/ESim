using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CV : MonoBehaviour
{
    [Header("Cone Settings")]
    [Tooltip("Number of rays to cast.")]
    [SerializeField] private int rayCount = 30;
    [Tooltip("The maximum distance the rays will travel.")]
    [SerializeField] private float maxDistance = 20f;
    [Tooltip("The total opening angle of the cone in degrees.")]
    [SerializeField] private float coneAngle = 45f;
    [Tooltip("How often to scan in seconds (0 = every frame).")]
    [SerializeField] private float scanInterval = 0.1f;

    [Header("Target Settings")]
    [SerializeField] private string targetTag = "HubAprilTag";
    [Tooltip("Layers the cone rays can hit. Default = Everything.")]
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("Log every tagged hit to the console.")]
    [SerializeField] private bool logHits = false;

    [Header("UI Reference Settings")]
    [Tooltip("The Camera viewing the space (matching this transform's perspective).")]
    [SerializeField] private Camera visionCamera;
    [Tooltip("The prefab for the UI bounding box frame.")]
    [SerializeField] private RectTransform uiBoxPrefab;
    [Tooltip("Drag your 'CameraFeed' RawImage GameObject here.")]
    [SerializeField] private RectTransform canvasContainer;

    public bool TargetVisible { get; private set; }
    public Transform VisibleTarget { get; private set; }

    private Dictionary<Collider, RectTransform> activeBoxes = new Dictionary<Collider, RectTransform>();
    private HashSet<Collider> uniqueHitsThisFrame = new HashSet<Collider>();

    private void Start()
    {
        if (visionCamera == null) visionCamera = Camera.main;
        StartCoroutine(RoutineScan());
    }

    private IEnumerator RoutineScan()
    {
        WaitForSeconds wait = scanInterval > 0f ? new WaitForSeconds(scanInterval) : null;
        while (true)
        {
            ScanConeDeterministic();
            UpdateUIBoxes();

            if (wait == null)
                yield return null;
            else
                yield return wait;
        }
    }

    private void ScanConeDeterministic()
    {
        float maxRad = (coneAngle / 2f) * Mathf.Deg2Rad;
        Quaternion coneRotation = Quaternion.FromToRotation(Vector3.forward, transform.forward);
        Vector3 origin = transform.position;

        TargetVisible = false;
        VisibleTarget = null;
        uniqueHitsThisFrame.Clear();

        for (int i = 0; i < rayCount; i++)
        {
            float t = (float)i / rayCount;
            float theta = t * maxRad;
            float phi = i * 2.4f;

            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            Vector3 localDir = new Vector3(Mathf.Cos(phi) * sinTheta, Mathf.Sin(phi) * sinTheta, cosTheta);
            Vector3 rayDirection = coneRotation * localDir;

            if (Physics.Raycast(origin, rayDirection, out RaycastHit hit, maxDistance, hitMask))
            {
                if (hit.collider.CompareTag(targetTag))
                {
                    TargetVisible = true;
                    VisibleTarget = hit.collider.transform;
                    uniqueHitsThisFrame.Add(hit.collider);

                    if (logHits)
                        Debug.Log($"Found {targetTag} at: {hit.collider.transform.position}");

                    Debug.DrawLine(origin, hit.point, Color.green);
                    continue;
                }
            }
            Debug.DrawLine(origin, origin + rayDirection * maxDistance, Color.red);
        }
    }

    private void UpdateUIBoxes()
    {
        List<Collider> keysToRemove = new List<Collider>();
        foreach (var pair in activeBoxes)
        {
            if (!uniqueHitsThisFrame.Contains(pair.Key))
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
                keysToRemove.Add(pair.Key);
            }
        }
        foreach (Collider key in keysToRemove) activeBoxes.Remove(key);

        foreach (Collider targetCollider in uniqueHitsThisFrame)
        {
            if (targetCollider == null) continue;

            if (!activeBoxes.ContainsKey(targetCollider))
            {
                RectTransform newBox = Instantiate(uiBoxPrefab, canvasContainer);
                activeBoxes.Add(targetCollider, newBox);
            }

            PositionUIBox(targetCollider.bounds, activeBoxes[targetCollider]);
        }
    }

    private void PositionUIBox(Bounds bounds, RectTransform uiBox)
    {
        if (uiBox == null || canvasContainer == null) return;

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        Vector3[] corners = new Vector3[]
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        float minX = Mathf.Infinity, maxX = Mathf.NegativeInfinity;
        float minY = Mathf.Infinity, maxY = Mathf.NegativeInfinity;

        foreach (Vector3 corner in corners)
        {
            // Convert to Viewport Space (0 to 1 range across the camera view)
            Vector3 viewportPos = visionCamera.WorldToViewportPoint(corner);

            // Skip points behind camera view
            if (viewportPos.z < 0) continue;

            if (viewportPos.x < minX) minX = viewportPos.x;
            if (viewportPos.x > maxX) maxX = viewportPos.x;
            if (viewportPos.y < minY) minY = viewportPos.y;
            if (viewportPos.y > maxY) maxY = viewportPos.y;
        }

        if (minX == Mathf.Infinity)
        {
            uiBox.gameObject.SetActive(false);
            return;
        }

        uiBox.gameObject.SetActive(true);

        // Map viewport (0 to 1) directly onto the local raw texture width/height (854 x 480)
        float containerWidth = canvasContainer.rect.width;   // 854
        float containerHeight = canvasContainer.rect.height; // 480

        // Calculate positions in local 854x480 coordinate limits
        float boxMinX = minX * containerWidth;
        float boxMaxX = maxX * containerWidth;
        float boxMinY = minY * containerHeight;
        float boxMaxY = maxY * containerHeight;

        float finalWidth = boxMaxX - boxMinX;
        float finalHeight = boxMaxY - boxMinY;

        // Shift coordinates so they center correctly based on the RectTransform pivot structure (0,0 is center of 854x480)
        float localCenterX = boxMinX + (finalWidth / 2f) - (containerWidth / 2f);
        float localCenterY = boxMinY + (finalHeight / 2f) - (containerHeight / 2f);

        uiBox.anchoredPosition = new Vector2(localCenterX, localCenterY);
        uiBox.sizeDelta = new Vector2(finalWidth, finalHeight);
    }
}
