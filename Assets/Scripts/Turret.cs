using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
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

    private float targetWorldTurretAngle = 0f; // Desired world angle for turret yaw
    private float targetHoodAngle = 0f;        // Desired local angle for hood pitch

    private float currentLocalTurretAngle = 0f;
    public float currentLocalHoodAngle = 0f;

    void Start()
    {
        // Initialize target turret angle based on starting world direction
        targetWorldTurretAngle = transform.eulerAngles.y;
        targetHoodAngle = 0f;
    }

    void Update()
    {
        // Handle keyboard input for manual control
        if (enableKeyboardControl)
        {
            HandleKeyboardInput();
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

    private void HandleKeyboardInput()
    {
        float newWorldTurretAngle = targetWorldTurretAngle;
        float newHoodAngle = targetHoodAngle;

        // F = Turn Left in World Space, H = Turn Right in World Space
        if (Input.GetKey(KeyCode.F))
        {
            newWorldTurretAngle -= turretRotateSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.H))
        {
            newWorldTurretAngle += turretRotateSpeed * Time.deltaTime;
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

    /// <summary>
    /// Sets the target angles (Turret in World Space, Hood in Local Space).
    /// </summary>
    public void setTurret(float NworldTurretAngle, float NhoodAngle)
    {
        targetWorldTurretAngle = NworldTurretAngle;
        targetHoodAngle = Mathf.Clamp(NhoodAngle, minHoodAngle, maxHoodAngle);
    }
}