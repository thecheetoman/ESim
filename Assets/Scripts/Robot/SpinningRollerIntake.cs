using UnityEngine;
using Robot.InputHandling;

public class SpinningRollerIntake : MonoBehaviour
{
    [Header("Roller Settings")]
    public float rotationSpeed = 1000f;
    public float surfaceVelocity = 8f;
    public Vector3 rotationAxis = Vector3.right;

    [Header("Target Destination")]
    public Transform targetPosition;

    [Header("Control")]
    [SerializeField] private KeyCode intakeKey = KeyCode.LeftShift;

    public AudioSource Audio;
    public float audioVolume = 0.5f;
    private bool started = false;

    private void Update()
    {
        bool intakePressed = Input.GetKeyDown(intakeKey)
            || (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.DeployPressedThisFrame());

        if (intakePressed)
        {
            started = !started; 
            if (started)
            {
                if (Audio != null)
                {
                    Audio.volume = audioVolume;
                    Audio.Play();
                }
            }
            else
            {
                if (Audio != null)
                {
                    Audio.Stop();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (started)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("GamePiece") && started)
        {
            Rigidbody ballRb = other.attachedRigidbody;
            if (ballRb != null && targetPosition != null)
            {
                Vector3 directionToTarget = (targetPosition.position - other.transform.position).normalized;
                ballRb.velocity = directionToTarget * surfaceVelocity;
            }
        }
    }
    public bool IsIntakeActive() => started;
}