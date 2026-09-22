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
    private bool isEnabled = false;

    // NEW: remembers whether the intake was running at the moment we got disabled
    private bool wasStartedBeforeDisable = false;

    private void OnEnable()
    {
        GameManager.OnRobotStateChanged += OnRobotStateChanged;

        bool nowEnabled = GameManager.IsRobotEnabled;
        // Route through the same handler so enable/disable logic is in one place
        OnRobotStateChanged(nowEnabled);
    }

    private void OnDisable()
    {
        GameManager.OnRobotStateChanged -= OnRobotStateChanged;
    }

    private void OnRobotStateChanged(bool enabled)
    {
        // Transitioning from enabled -> disabled: remember state, then stop
        if (isEnabled && !enabled)
        {
            wasStartedBeforeDisable = started;
            started = false;
            if (Audio != null)
            {
                Audio.Stop();
            }
        }
        // Transitioning from disabled -> enabled: restore whatever it was doing before
        else if (!isEnabled && enabled)
        {
            started = wasStartedBeforeDisable;
            if (started && Audio != null)
            {
                Audio.volume = audioVolume;
                Audio.Play();
            }
        }

        isEnabled = enabled;
    }

    private void Update()
    {
        if (!isEnabled) return;

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
        if (isEnabled && started)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.fixedDeltaTime, Space.Self);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isEnabled && started && other.CompareTag("GamePiece"))
        {
            Rigidbody ballRb = other.attachedRigidbody;
            if (ballRb != null && targetPosition != null)
            {
                Vector3 directionToTarget = (targetPosition.position - other.transform.position).normalized;
                ballRb.velocity = directionToTarget * surfaceVelocity;
            }
        }
    }

    public bool IsIntakeActive() => isEnabled && started;
}