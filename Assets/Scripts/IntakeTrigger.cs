using UnityEngine;

public class SpinningRollerIntake : MonoBehaviour
{
    [Header("Roller Settings")]
    public float rotationSpeed = 1000f;
    public float surfaceVelocity = 8f;
    public Vector3 rotationAxis = Vector3.right;

    [Header("Target Destination")]
    public Transform targetPosition;

    private void FixedUpdate()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.fixedDeltaTime, Space.Self);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("GamePiece"))
        {
            Rigidbody ballRb = other.attachedRigidbody;
            if (ballRb != null && targetPosition != null)
            {
                // mov towards target in world space
                Vector3 directionToTarget = (targetPosition.position - other.transform.position).normalized;
                ballRb.velocity = directionToTarget * surfaceVelocity;
            }
        }
    }
}