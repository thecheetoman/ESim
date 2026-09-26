using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Robot.InputHandling;

public class Turret : MonoBehaviour
{
    public enum TurretMode { Manual, TrackingHub }

    [Header("Turret Transforms")]
    public Transform turretPivot; // the turrets like spinny thing
    public Vector3 turretRotationAxis = Vector3.up; // Standardized to Up for horizontal spinning
    public float offsetTurretAngle = 0f; // the angle offset for the turret, in degrees

    public Transform hoodPivot; // bro is not from da hood
    public Vector3 hoodRotationAxis = Vector3.right;
    public float offsetHoodAngle = 0f; // the angle offset for the hood, in degrees

    [Header("Turret Limits (Local Relative to Body)")]
    public float minTurretAngle = -90f; // Minimum limit for turret rotation
    public float maxTurretAngle = 90f;  // Maximum limit for turret rotation
    public float minHoodAngle = 0f;     // Minimum limit for hood pitch
    public float maxHoodAngle = 45f;    // Maximum limit for hood pitch

    [Header("Movement Settings(degs)")]
    public float turretRotateSpeed = 90f; // Degrees per second for turret
    public float hoodRotateSpeed = 90f;   // Degrees per second for hood

    [Header("Control Settings")]
    public bool enableKeyboardControl = true; // Toggle manual key controls
    [Tooltip("Mode while robot is enabled. K switches Manual -> TrackingHub (needs CV target).")]
    [SerializeField] public TurretMode mode = TurretMode.Manual;
    [SerializeField] private KeyCode trackingKey = KeyCode.K;
    [Tooltip("CV scanner used to gate tracking. Auto-found if empty.")]
    [SerializeField] private CV cv;
    [Tooltip("Yaw offset (deg) added to the hub aim while tracking. Positive = aim right.")]
    [SerializeField] private float hubAimAngleOffset = 0f;
    [Tooltip("Hood pitch offset (deg) added to the direct hub aim while tracking. Positive = aim up.")]
    [SerializeField] private float hoodAimAngleOffset = 0f;
    [Tooltip("How long to keep a lost hub lock before re-acquiring (seconds).")]
    [SerializeField] private float relockGrace = 0.5f;

    private float targetWorldTurretAngle = 0f; // Desired world angle for turret yaw
    private float targetHoodAngle = 0f;        // Desired local angle for hood pitch

    private float currentLocalTurretAngle = 0f;
    public float currentLocalHoodAngle = 0f;

    public bool enableGamepadControl = true;

    private bool isEnabled = false;

    private void OnEnable()
    {
        GameManager.OnRobotStateChanged += OnRobotStateChanged;
        isEnabled = GameManager.IsRobotEnabled;
    }

    private void OnDisable()
    {
        GameManager.OnRobotStateChanged -= OnRobotStateChanged;
    }

    private void OnRobotStateChanged(bool enabled)
    {
        isEnabled = enabled;
    }

    void Start()
    {
        // Initialize target turret angle based on starting world direction
        targetWorldTurretAngle = transform.eulerAngles.y;
        targetHoodAngle = 0f;

        if (cv == null)
            cv = GetComponentInParent<CV>();
        if (cv == null)
            cv = GetComponentInChildren<CV>();
    }

    void Update()
    {
        if (!isEnabled) return;

        // Any turret input while tracking hands control back to the player
        if (mode == TurretMode.TrackingHub && TurretInputApplied())
        {
            mode = TurretMode.Manual;
            lockedHub = null;
            lostTimer = 0f;
        }
        // Enter tracking only when CV can see at least one AprilTag
        bool trackingPressed = Input.GetKeyDown(trackingKey)
            || (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.ToggleTrackingPressedThisFrame());
        if (mode == TurretMode.Manual && trackingPressed && cv != null && cv.TargetVisible)
        {
            mode = TurretMode.TrackingHub;
        }

        // Handle input for manual control only
        if (mode == TurretMode.Manual)
        {
            if (enableKeyboardControl)
            {
                HandleKeyboardInput();
            }
            if (enableGamepadControl)
            {
                HandleGamepadInput();
            }
        }
        else if (mode == TurretMode.TrackingHub)
        {
            AimAtHubWhileVisible();
        }

        // 1. Calculate required local turret angle to maintain the desired world angle
        float robotWorldYaw = transform.eulerAngles.y;
        float desiredLocalTurretAngle = Mathf.DeltaAngle(robotWorldYaw, targetWorldTurretAngle);

        // Clamp local turret angle within physical limits relative to body
        desiredLocalTurretAngle = Mathf.Clamp(desiredLocalTurretAngle, minTurretAngle, maxTurretAngle);

        // Smoothly move local turret angle toward desired position
        currentLocalTurretAngle = Mathf.MoveTowards(currentLocalTurretAngle, desiredLocalTurretAngle, turretRotateSpeed * Time.deltaTime);

        if (turretPivot != null)
        {
            float totalTurretAngle = currentLocalTurretAngle + offsetTurretAngle;
            turretPivot.localRotation = Quaternion.AngleAxis(totalTurretAngle, turretRotationAxis);
        }

        // 2. Local hood pitch (does not lock to world rotation)
        currentLocalHoodAngle = Mathf.MoveTowards(currentLocalHoodAngle, targetHoodAngle, hoodRotateSpeed * Time.deltaTime);

        if (hoodPivot != null)
        {
            float totalHoodAngle = currentLocalHoodAngle + offsetHoodAngle;
            hoodPivot.localRotation = Quaternion.AngleAxis(totalHoodAngle, hoodRotationAxis);
        }
    }
    public float newWorldTurretAngle;

    private void HandleKeyboardInput()
    {
        newWorldTurretAngle = targetWorldTurretAngle;
        float newHoodAngle = targetHoodAngle;

        // F = Turn Left in World Space, H = Turn Right in World Space
        if (Input.GetKey(KeyCode.F))
        {
            newWorldTurretAngle -= turretRotateSpeed / 2 * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.H))
        {
            newWorldTurretAngle += turretRotateSpeed / 2 * Time.deltaTime;
        }

        // G = Pitch Up, T = Pitch Down
        if (Input.GetKey(KeyCode.G))
        {
            newHoodAngle += hoodRotateSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.T))
        {
            newHoodAngle -= hoodRotateSpeed * Time.deltaTime;
        }

        setTurret(newWorldTurretAngle, newHoodAngle);
    }

    private void HandleGamepadInput()
    {
        if (PlayerInputHandler.Instance == null) return;
        if (!PlayerInputHandler.Instance.RightStickPressed) return;

        Vector2 stick = PlayerInputHandler.Instance.RightStickInput;

        float newWorldTurretAngle = targetWorldTurretAngle + stick.x * turretRotateSpeed / 3 * Time.deltaTime;
        float newHoodAngle = targetHoodAngle + stick.y * hoodRotateSpeed / -2 * Time.deltaTime;

        setTurret(newWorldTurretAngle, newHoodAngle);
    }

    private bool TurretInputApplied()
    {
        if (enableKeyboardControl &&
            (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.H) ||
             Input.GetKey(KeyCode.G) || Input.GetKey(KeyCode.T)))
        {
            return true;
        }

        if (enableGamepadControl &&
            PlayerInputHandler.Instance != null &&
            PlayerInputHandler.Instance.RightStickPressed)
        {
            return true;
        }

        return false;
    }

    private GameObject[] hubCache;
    private GameObject lockedHub;
    private float lostTimer;

    /// <summary>
    /// CV only gates tracking here. The aim target is always a "Hub"-tagged
    /// object (locked when a tag is first seen, dropped after relockGrace).
    /// </summary>
    private void AimAtHubWhileVisible()
    {
        if (cv == null) return;

        if (cv.TargetVisible)
        {
            lostTimer = relockGrace;

            // Lock onto the hub nearest to whatever tag CV currently sees so a
            // mid-field robot picks the correct one. Held until grace or exit.
            if (lockedHub == null)
                lockedHub = FindNearestHubTo(cv.VisibleTarget != null ? cv.VisibleTarget.position : transform.position);
        }
        else
        {
            lostTimer -= Time.deltaTime;
            if (lostTimer <= 0f)
            {
                lockedHub = null;
                return;
            }
        }

        if (lockedHub == null) return;

        // Yaw from the turret mount (stable along the yaw axis; the hood pivot
        // swings with the turret and would feed wobble back into the aim).
        Vector3 toHub = lockedHub.transform.position - transform.position;
        toHub.y = 0f;
        float horizontalSqr = toHub.x * toHub.x + toHub.z * toHub.z;
        if (horizontalSqr < 0.0001f) return;

        targetWorldTurretAngle = Mathf.Atan2(toHub.x, toHub.z) * Mathf.Rad2Deg + hubAimAngleOffset;

        // Direct line-of-sight pitch (deg). The pivot is rendered as
        // currentLocalHoodAngle + offsetHoodAngle, so invert that offset to get
        // the local control value, then clamp to physical limits.
        Vector3 aimOrigin = hoodPivot != null ? hoodPivot.position : transform.position;
        Vector3 toHubPitch = lockedHub.transform.position - aimOrigin;
        float elevationDeg = Mathf.Atan2(toHubPitch.y, Mathf.Sqrt(toHubPitch.x * toHubPitch.x + toHubPitch.z * toHubPitch.z))
            * Mathf.Rad2Deg + hoodAimAngleOffset;
        float hoodAngle = elevationDeg - offsetHoodAngle;
        targetHoodAngle = Mathf.Clamp(hoodAngle, minHoodAngle, maxHoodAngle);
    }

    private GameObject FindNearestHubTo(Vector3 worldPos)
    {
        if (hubCache == null)
            hubCache = GameObject.FindGameObjectsWithTag("Hub");

        GameObject best = null;
        float bestSqr = float.PositiveInfinity;
        foreach (GameObject hub in hubCache)
        {
            if (hub == null) continue;
            float sqr = (hub.transform.position - worldPos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = hub;
            }
        }
        return best;
    }

    /// <summary>
    /// Sets the target angles (Turret in World Space, Hood in Local Space).
    /// </summary>
    public void setTurret(float NworldTurretAngle, float NhoodAngle)
    {
        targetWorldTurretAngle = NworldTurretAngle;
        targetHoodAngle = Mathf.Clamp(NhoodAngle, minHoodAngle, maxHoodAngle);
    }
}