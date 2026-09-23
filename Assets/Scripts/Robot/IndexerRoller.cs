using UnityEngine;
using Robot.InputHandling;

public class IndexerRoller : MonoBehaviour
{
    [Header("Indexer Settings")]
    public Transform targetPoint;
    public float indexerForce = 15f;
    private float shootingMult = 1f;

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

        if (!isEnabled)
        {
            shootingMult = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isEnabled) return;

        if (Input.GetKey(KeyCode.U) || (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.reverseIndexer))
        {
            shootingMult = -20f;
        }
        else if (Input.GetKey(KeyCode.Space) || (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.Shoot))
        {
            shootingMult = 2.5f;
        }
        else
        {
            shootingMult = 1f;
        }

        if (other.CompareTag("GamePiece"))
        {
            Rigidbody ballRb = other.attachedRigidbody;
            if (ballRb != null && targetPoint != null)
            {
                Vector3 forceDirection = (targetPoint.position - transform.position).normalized;

                ballRb.AddForce(forceDirection * (indexerForce * shootingMult), ForceMode.Force);
            }
        }
    }
}