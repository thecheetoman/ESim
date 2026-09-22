using UnityEngine;
using Robot.InputHandling;

public class ShooterIndexer : MonoBehaviour
{
    [Header("Indexer Settings")]
    public Transform targetPoint;
    public Transform inactiveTarget;
    public float indexerForce = 15f;
    public float reverseForce = 7.5f;

    [Header("Input Settings")]
    public KeyCode activationKey = KeyCode.Space;

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

    private void OnTriggerStay(Collider other)
    {
        if (!isEnabled) return;
        if (!other.CompareTag("GamePiece")) return;

        Rigidbody ballRb = other.attachedRigidbody;
        if (ballRb == null) return;

        bool activationPressed = Input.GetKey(activationKey)
            || (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.Shoot);

        if (activationPressed)
        {
            if (targetPoint != null)
            {
                Vector3 forceDirection = (targetPoint.position - transform.position).normalized;
                ballRb.AddForce(forceDirection * indexerForce, ForceMode.Force);
            }
        }
        else
        {
            if (inactiveTarget != null)
            {
                Vector3 forceDirection = (inactiveTarget.position - transform.position).normalized;
                ballRb.AddForce(forceDirection * reverseForce, ForceMode.Force);
            }
        }
    }
}