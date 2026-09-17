using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates a joint/arm around a chosen local axis toward a target angle,
/// using a user-defined keybind. If the arm collides with something
/// mid-rotation, it stops immediately at the current angle instead of
/// clipping through or snapping to the full extended angle.
///
/// Setup:
/// - Attach this to the rotating part (the arm/joint itself).
/// - Add a Collider (and Rigidbody, kinematic, with "Use Gravity" off)
///   somewhere on the arm — usually at the tip/business end — that can
///   detect the obstacle.
/// - Add the small "JointCollisionRelay" component (below, same file)
///   to that collider's GameObject, and drag this RotatingJoint into
///   its "Target Joint" field. This lets a collider on a child object
///   report hits back up to this script.
/// - The obstacle just needs any collider (does not need a Rigidbody).
/// </summary>
public class RotatingJoint : MonoBehaviour
{
    [Header("Hinge Settings")]
    [Tooltip("Local axis to rotate around (e.g. Vector3.right = local X).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.right;
    [SerializeField] private float retractedAngle = 0f;
    [SerializeField] private float extendedAngle = 90f;
    [SerializeField] private float rotationSpeed = 5f;
    [Tooltip("Constant Euler angle offset (X, Y, Z) applied on top of the axis rotation. Useful when the object's rest orientation isn't zero, or to fine-tune alignment without changing the two target angles.")]
    [SerializeField] private Vector3 angleOffset = Vector3.zero;

    [Header("Control Settings")]
    [Tooltip("Key that toggles deploy/retract.")]
    [SerializeField] private KeyCode deployKey = KeyCode.LeftShift;
    [Tooltip("If true, holding/pressing the key toggles state. If false, deploy only fires once and needs external reset.")]
    [SerializeField] private bool toggleable = true;

    [Header("Collision Stop")]
    [Tooltip("If true, the arm halts at its current angle when it detects a collision instead of continuing toward the target.")]
    [SerializeField] private bool stopOnCollision = true;
    [Tooltip("Once stopped by collision, does it require retracting first before it can deploy again?")]
    [SerializeField] private bool requireRetractToResetCollision = true;

    private float currentAngle;
    private bool isDeployed = false;
    private bool obstacleHit = false;

    void Start()
    {
        currentAngle = retractedAngle;
    }

    void Update()
    {
        if (Input.GetKeyDown(deployKey))
        {
            if (toggleable)
            {
                isDeployed = !isDeployed;
            }
            else
            {
                isDeployed = true;
            }

            // Allow re-deploying to clear a previous collision stop
            // once the arm has been retracted again.
            if (isDeployed && (!requireRetractToResetCollision || currentAngle == retractedAngle))
            {
                obstacleHit = false;
            }
        }

        float targetAngle = isDeployed ? extendedAngle : retractedAngle;

        // If we've hit something while deploying, freeze in place
        // instead of continuing to move toward the target.
        bool blocked = stopOnCollision && obstacleHit && isDeployed;

        if (!blocked)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, rotationSpeed * Time.deltaTime * 100f);
        }

        transform.localRotation = Quaternion.Euler(angleOffset) * Quaternion.AngleAxis(currentAngle, rotationAxis);
    }

    /// <summary>
    /// Called by JointCollisionRelay (or your own collision code) when
    /// the arm makes contact with something while deploying.
    /// </summary>
    public void ReportObstacleHit()
    {
        if (!isDeployed) return; // ignore hits while retracting/idle
        obstacleHit = true;
    }

    /// <summary>
    /// Manually clear the collision-stop flag (e.g. if you want a
    /// different reset condition than "must retract first").
    /// </summary>
    public void ClearObstacleFlag()
    {
        obstacleHit = false;
    }
}

/// <summary>
/// Put this on the same GameObject as the Collider that should detect
/// obstacles (often a child object at the tip of the arm, since the
/// pivot/root object's own collider may not be where contact happens).
/// Requires a Rigidbody on this object (or a parent) set to Kinematic
/// so trigger/collision callbacks fire reliably during transform-driven
/// (non-physics) rotation.
/// </summary>
[RequireComponent(typeof(Collider))]
public class JointCollisionRelay : MonoBehaviour
{
    [Tooltip("The RotatingJoint to notify when a collision/trigger happens.")]
    [SerializeField] private RotatingJoint targetJoint;

    [Tooltip("Only report hits from objects on these layers (leave as Everything to allow all).")]
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Tooltip("Use trigger events instead of collision events (requires the collider to be marked 'Is Trigger').")]
    [SerializeField] private bool useTriggerEvents = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (useTriggerEvents) return;
        if (IsInLayerMask(collision.gameObject.layer))
        {
            targetJoint?.ReportObstacleHit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerEvents) return;
        if (IsInLayerMask(other.gameObject.layer))
        {
            targetJoint?.ReportObstacleHit();
        }
    }

    private bool IsInLayerMask(int layer)
    {
        return (obstacleLayers.value & (1 << layer)) != 0;
    }
}