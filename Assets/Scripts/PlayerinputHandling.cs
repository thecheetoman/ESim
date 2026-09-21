using UnityEngine;
using UnityEngine.InputSystem;

namespace Robot.InputHandling
{
    public class PlayerInputHandler : MonoBehaviour
    {
        public static PlayerInputHandler Instance { get; private set; }

        // Consumed by gameplay scripts. Move: x = strafe, y = forward.
        public Vector2 MoveInput { get; private set; }
        public float RotationInput { get; private set; }
        public Vector2 RightStickInput { get; private set; }
        public bool RightStickPressed { get; private set; }

        public bool Shoot { get; private set; }
        
        public bool IntakeOut { get; private set; }

        public bool reverseIndexer { get; private set; }

        private InputAction _moveAction;
        private InputAction _rotateAction;

        private InputAction _rightStickAction;
        private InputAction _rightStickPressAction;

        private InputAction _shoot;

        private InputAction _intakeOut;

        private InputAction _reversingIndexer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildActions();
        }

        private void BuildActions()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _rotateAction = new InputAction("Rotate", InputActionType.Value, expectedControlType: "Axis");
            _rotateAction.AddCompositeBinding("1DAxis")
                .With("Positive", "<Keyboard>/j")
                .With("Negative", "<Keyboard>/l");
            _rotateAction.AddBinding("<Gamepad>/rightStick/x").WithProcessor("invert");

            _rightStickAction = new InputAction("RightStick", InputActionType.Value, expectedControlType: "Vector2");
            _rightStickAction.AddBinding("<Gamepad>/rightStick");

            _rightStickPressAction = new InputAction("RightStickPress", InputActionType.Button);
            _rightStickPressAction.AddBinding("<Gamepad>/rightStickPress");

            _shoot = new InputAction("Shoot", InputActionType.Button);
            _shoot.AddBinding("<Keyboard>/space");
            _shoot.AddBinding("<Gamepad>/rightTrigger");

            _intakeOut = new InputAction("IntakeOut", InputActionType.Button);
            _intakeOut.AddBinding("<Keyboard>/LeftShift");
            _intakeOut.AddBinding("<Gamepad>/buttonSouth");

            _reversingIndexer = new InputAction("ReverseIndexer", InputActionType.Button);
            _reversingIndexer.AddBinding("<Keyboard>/u");
            _reversingIndexer.AddBinding("<Gamepad>/rightShoulder");
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _rotateAction.Enable();
            _rightStickAction.Enable();
            _rightStickPressAction.Enable();
            _shoot.Enable();
            _intakeOut.Enable();
            _reversingIndexer.Enable();
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _rotateAction.Disable();
            _rightStickAction.Disable();
            _rightStickPressAction.Disable();
            _shoot.Disable();
            _intakeOut.Disable();
            _reversingIndexer.Disable();
        }

        private void Update()
        {
            MoveInput = _moveAction.ReadValue<Vector2>();
            RotationInput = _rotateAction.ReadValue<float>();
            RightStickInput = _rightStickAction.ReadValue<Vector2>();
            RightStickPressed = _rightStickPressAction.IsPressed();
            Shoot = _shoot.IsPressed();
            IntakeOut = _intakeOut.IsPressed();
            reverseIndexer = _reversingIndexer.IsPressed();
        }
        public bool ShootPressedThisFrame()
        {
            return _shoot != null && _shoot.WasPressedThisFrame();
        }
        public bool DeployPressedThisFrame()
        {
            return _intakeOut != null && _intakeOut.WasPressedThisFrame();

        }
    }
}