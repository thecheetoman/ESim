using UnityEngine;
using System.Collections;

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

    /// <summary>True if the last scan saw at least one target-tagged collider.</summary>
    public bool TargetVisible { get; private set; }

    /// <summary>Transform of a target the last scan hit (null if none).</summary>
    public Transform VisibleTarget { get; private set; }

    private void Start()
    {
        StartCoroutine(RoutineScan());
    }

    private IEnumerator RoutineScan()
    {
        WaitForSeconds wait = scanInterval > 0f ? new WaitForSeconds(scanInterval) : null;
        while (true)
        {
            ScanConeDeterministic();
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

        for (int i = 0; i < rayCount; i++)
        {
            // Use Golden Ratio spiral distribution for even spread
            float t = (float)i / rayCount;
            float theta = t * maxRad;
            float phi = i * 2.4f; // Golden angle approximation in radians

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
                    if (logHits)
                        Debug.Log($"Found {targetTag} at: {hit.collider.transform.position}");
                    Debug.DrawLine(origin, hit.point, Color.green);
                    continue;
                }
            }

            Debug.DrawLine(origin, origin + rayDirection * maxDistance, Color.red);
        }
    }
}
