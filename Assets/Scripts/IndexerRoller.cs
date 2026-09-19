using UnityEngine;

public class IndexerRoller : MonoBehaviour
{
    [Header("Indexer Settings")]
    public Transform targetPoint;  
    public float indexerForce = 15f; 

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("GamePiece"))
        {
            Rigidbody ballRb = other.attachedRigidbody;
            if (ballRb != null && targetPoint != null)
            {
                Vector3 forceDirection = (targetPoint.position - transform.position).normalized;

                ballRb.AddForce(forceDirection * indexerForce, ForceMode.Force);
            }
        }
    }
}