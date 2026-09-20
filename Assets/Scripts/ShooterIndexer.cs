using UnityEngine;

public class ShooterIndexer : MonoBehaviour
{
    [Header("Indexer Settings")]
    public Transform targetPoint;
    public Transform inactiveTarget;
    public float indexerForce = 15f;
    public float reverseForce = 7.5f;

    [Header("Input Settings")]
    public KeyCode activationKey = KeyCode.Space;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("GamePiece")) return;

        Rigidbody ballRb = other.attachedRigidbody;
        if (ballRb == null) return;

        if (Input.GetKey(activationKey))
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