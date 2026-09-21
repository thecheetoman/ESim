using UnityEngine;
using UnityEngine.Audio;
using Robot.InputHandling;

namespace RobotFramework.Controllers.Drivetrain
{
    public class DriveController : MonoBehaviour
    {
        // Core references
        private Rigidbody _rb;
        private Transform _wheelChild;
        private SwerveController _swerve;

        // Configuration
        [SerializeField] private GameObject drivetrainParent;
        [SerializeField] private float wheelDiameter = 4f;
        [SerializeField] private float maxSpeed = 17f;
        [SerializeField] private float accelerationForce = 8f;
        [SerializeField] private float falloffPercent = 0.075f;
        [SerializeField] private int falloffExponent = 10;
        [SerializeField] private float steerMultiplier = 1f;
        [SerializeField] private float speedDebug;

        // Field-relative driving
        [SerializeField] private bool fieldRelative = false;
        [SerializeField] private WorldAxis fieldForwardAxis = WorldAxis.PositiveZ;

        // World axis to treat as "downfield" (the fixed forward direction W drives
        // toward, regardless of the robot's own heading) when fieldRelative is on.
        public enum WorldAxis
        {
            PositiveZ,
            PositiveX,
            NegativeZ,
            NegativeX,
        }

        // Audio
        [SerializeField] private AudioMixerGroup swerveAudioMixerGroup;
        [SerializeField] private AudioClip gearAudioClip;
        [SerializeField] private AudioClip treadAudioClip;

        // State
        public bool IsTouchingGround => _swerve != null && _swerve.IsTouchingGround;
        [HideInInspector] public float moveSpeedMultiplier;
        [HideInInspector] public float rotationSpeedMultiplier;

        // Standalone enable flag, replacing the old BaseGameManager.RobotState gate.
        // Flip this off (e.g. from a match-state manager, a pause menu, etc.) to
        // zero out drive input without touching anything else in this script.
        public bool isEnabled = true;

        private bool _drivetrainAssigned;
        private bool _drivetrainError;
        private bool _setupError;

        // Constants
        private const float METERS_TO_FEET = 3.28084f;

        private bool overideActive;
        private float str;
        private float fwd;
        private float rotation;
        private float softSteer;
        private float driveMP;

        private void Start()
        {
            InitializeComponents();
            LoadPlayerPreferences();
            overideActive = false;
            driveMP = 1;
        }

        private void Update()
        {
            _swerve?.UpdateAudio();
        }

        private void FixedUpdate()
        {
            if (_drivetrainError) return;
            if (!_drivetrainAssigned && !RuntimeCheck()) return;

            RunSwerve();
        }

        private void InitializeComponents()
        {
            _rb = GetComponent<Rigidbody>();
            ValidateSetup();

            if (!_setupError)
            {
                _swerve = new SwerveController(
                    _rb,
                    _wheelChild,
                    wheelDiameter,
                    maxSpeed,
                    accelerationForce,
                    falloffPercent,
                    falloffExponent,
                    gameObject,
                    swerveAudioMixerGroup,
                    gearAudioClip,
                    treadAudioClip);
            }
        }

        private void LoadPlayerPreferences()
        {
            moveSpeedMultiplier = Mathf.Clamp01(PlayerPrefs.GetFloat("MoveSpeed", 1f));
            rotationSpeedMultiplier = Mathf.Clamp01(PlayerPrefs.GetFloat("RotationSpeed", 1f));
        }

        private void ValidateSetup()
        {
            _setupError = false;

            if (drivetrainParent == null)
            {
                Debug.LogError("Please add a Drivetrain Parent");
                _setupError = true;
            }

            if (drivetrainParent != null && drivetrainParent.transform.Find("Wheels") != null)
            {
                _wheelChild = drivetrainParent.transform.Find("Wheels");
            }
            else
            {
                Debug.LogError("Please add a Wheels Child to the Drivetrain Parent");
                _setupError = true;
            }

            if (_rb == null)
            {
                Debug.LogError($"Rigidbody not found on {gameObject.name}. Adding temporary Rigidbody");
                _rb = gameObject.AddComponent<Rigidbody>();
                _rb.mass = 20f;
                _rb.drag = 3f;
                _rb.angularDrag = 3f;
                _setupError = true;
            }
        }

        private bool RuntimeCheck()
        {
            if (_setupError || !isEnabled)
                return false;

            if (!_drivetrainAssigned)
            {
                _drivetrainError = !_swerve.AssignSwerveWheels();
                _drivetrainAssigned = true;
            }

            return !_drivetrainError;
        }

        private void RunSwerve()
        {
            if (!isEnabled)
            {
                fwd = 0;
                str = 0;
                rotation = 0;
            }
            else if (overideActive)
            {
                overideActive = false;
            }
            else
            {
                GetKeyboardInput();
            }

            var mag = new Vector2(fwd * driveMP, str * driveMP);
            if (mag.magnitude > 1)
            {
                mag = mag.normalized;
            }

            _swerve.Drive(
                Mathf.Clamp(mag.x, -1, 1),
                Mathf.Clamp(mag.y, -1, 1),
                Mathf.Clamp(-rotation + softSteer, -1, 1));

            softSteer = 0;

            speedDebug = _rb.velocity.magnitude * METERS_TO_FEET;
        }

        // Temporary direct keyboard binding: WASD for translation, J/L for rotation.
        private void GetKeyboardInput()
        {
            Vector2 move = PlayerInputHandler.Instance.MoveInput;

            if (fieldRelative)
            {
                var driveInput = new Vector3(move.x, 0f, move.y);
                var angle = GetFieldForwardAngle() - transform.eulerAngles.y;
                var rotated = Quaternion.AngleAxis(angle, Vector3.up) * driveInput;

                fwd = rotated.z * moveSpeedMultiplier;
                str = rotated.x * moveSpeedMultiplier;
            }
            else
            {
                fwd = move.y * moveSpeedMultiplier;
                str = move.x * moveSpeedMultiplier;
            }

            rotation = PlayerInputHandler.Instance.RightStickPressed ? 0f : PlayerInputHandler.Instance.RotationInput * steerMultiplier * rotationSpeedMultiplier;
        }

        private float GetFieldForwardAngle()
        {
            switch (fieldForwardAxis)
            {
                case WorldAxis.PositiveZ: return 0f;
                case WorldAxis.PositiveX: return 90f;
                case WorldAxis.NegativeZ: return 180f;
                case WorldAxis.NegativeX: return 270f;
                default: return 0f;
            }
        }

        public void overideInput(Vector2 input, float rotation, DriveMode mode)
        {
            overideActive = true;

            if (mode == DriveMode.RobotRelative)
            {
                fwd = input.x;
                str = input.y;
            }
            else
            {
                Vector3 driveInput = new Vector3(input.x, 0, input.y);
                var angle = GetFieldForwardAngle() - transform.eulerAngles.y;
                var fieldRelativeInput = Quaternion.AngleAxis(angle, Vector3.up) * driveInput;
                fwd = fieldRelativeInput.z;
                str = fieldRelativeInput.x;
            }

            this.rotation = rotation;
        }

        public void SetDriveMp(float value)
        {
            driveMP = value;
        }

        public void SoftSteer(float input)
        {
            if (isEnabled)
            {
                softSteer = input;
            }
        }

        public enum DriveMode
        {
            FieldOriented,
            RobotRelative,
        }
    }
}