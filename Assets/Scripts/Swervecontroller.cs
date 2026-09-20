using UnityEngine;
using UnityEngine.Audio;

namespace RobotFramework.Controllers.Drivetrain
{
    /// <summary>
    /// Swerve drivetrain kinematics + per-module force application + drive audio,
    /// extracted from DriveController.cs. Handles: module discovery, chassis-to-module
    /// inverse kinematics (GenerateSwerveSetpoints), ground contact detection, the
    /// velocity-falloff-based propulsion force applied at each wheel's contact point,
    /// and tread/gear sound driven off rigidbody velocity.
    ///
    /// This class intentionally omits input handling (field/robot relative stick
    /// reading) and PlayerPrefs — those stay in the higher-level DriveController and
    /// should call into this class with normalized fwd/str/rotation values.
    /// </summary>
    public class SwerveController
    {
        // ---- Dependencies (wired up by the owning MonoBehaviour) ----
        private readonly Rigidbody _rb;
        private readonly Transform _wheelChild;

        // ---- Configuration ----
        private readonly float _wheelDiameter;
        private readonly float _maxSpeedFeetPerSec;
        private readonly float _accelerationForce;
        private readonly float _falloffPercent;
        private readonly int _falloffExponent;

        // ---- Derived / cached ----
        private float _maxSpeedMeters;
        private float[] _falloffLookup;
        private const int LookupTableSize = 2048;

        private float _length;
        private float _width;
        private float _radius;

        private int _fieldLayerMask;

        // ---- Module state ----
        private readonly SwerveWheel[] _swerveWheels = new SwerveWheel[4];
        private readonly SwerveSetpoint[] _swerveSetpoints = new SwerveSetpoint[4];

        public const int FL_MODULE = 0;
        public const int FR_MODULE = 1;
        public const int BL_MODULE = 2;
        public const int BR_MODULE = 3;

        // ---- Unit conversions ----
        private const float METERS_TO_FEET = 3.28084f;
        private const float FEET_TO_METERS = 0.3048f;
        private const float INCHES_TO_METERS = 0.0254f;
        private const float RAD_TO_DEG = 180f / Mathf.PI;

        // ---- Audio ----
        private readonly AudioSource _gearSource;
        private readonly AudioSource _treadSource;

        public bool IsTouchingGround { get; private set; }

        private struct SwerveSetpoint
        {
            public float Angle;
            public float Velocity;
        }

        /// <param name="audioHost">
        /// GameObject the two AudioSources (gear + tread) get attached to — pass the
        /// same object your DriveController lives on.
        /// </param>
        public SwerveController(
            Rigidbody rb,
            Transform wheelChild,
            float wheelDiameter,
            float maxSpeedFeetPerSec,
            float accelerationForce,
            float falloffPercent,
            int falloffExponent,
            GameObject audioHost,
            AudioMixerGroup swerveAudioMixerGroup,
            AudioClip gearAudioClip,
            AudioClip treadAudioClip)
        {
            _rb = rb;
            _wheelChild = wheelChild;
            _wheelDiameter = wheelDiameter;
            _maxSpeedFeetPerSec = maxSpeedFeetPerSec;
            _accelerationForce = accelerationForce;
            _falloffPercent = falloffPercent;
            _falloffExponent = falloffExponent;

            _fieldLayerMask = 1 << LayerMask.NameToLayer("Robot");

            BuildFalloffLookupTable();

            _treadSource = CreateAudioSource(audioHost, swerveAudioMixerGroup, treadAudioClip);
            _gearSource = CreateAudioSource(audioHost, swerveAudioMixerGroup, gearAudioClip);
        }

        private static AudioSource CreateAudioSource(GameObject host, AudioMixerGroup mixerGroup, AudioClip clip)
        {
            var source = host.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mixerGroup;
            source.spatialBlend = 0.4f;
            source.playOnAwake = false;
            source.clip = clip;
            source.loop = true;
            return source;
        }

        /// <summary>
        /// Locates the FL/FR/BL/BR SwerveWheel children and computes chassis
        /// geometry (length, width, turning radius) used by the kinematics.
        /// Returns false (and logs) if any module is missing.
        /// </summary>
        public bool AssignSwerveWheels()
        {
            var wheelNames = new[] { "FL", "FR", "BL", "BR" };
            var indices = new[] { FL_MODULE, FR_MODULE, BL_MODULE, BR_MODULE };

            for (int i = 0; i < wheelNames.Length; i++)
            {
                var wheelTransform = _wheelChild.Find(wheelNames[i]);
                if (wheelTransform == null)
                {
                    Debug.LogError($"No {wheelNames[i]} Wheel Object Found");
                    return false;
                }

                _swerveWheels[indices[i]] = wheelTransform.GetComponent<SwerveWheel>();
            }

            _length = Mathf.Abs(_swerveWheels[FL_MODULE].transform.localPosition.z -
                                 _swerveWheels[BL_MODULE].transform.localPosition.z);
            _width = Mathf.Abs(_swerveWheels[FL_MODULE].transform.localPosition.x -
                                _swerveWheels[FR_MODULE].transform.localPosition.x);
            _radius = Mathf.Sqrt(_length * _length + _width * _width);

            return true;
        }

        /// <summary>
        /// Runs one full swerve update: computes module setpoints from normalized
        /// fwd/str/rotation inputs (each expected in roughly [-1, 1]) and drives
        /// every module. Call once per FixedUpdate.
        /// </summary>
        public void Drive(float fwd, float str, float rotation)
        {
            _rb.maxLinearVelocity = _maxSpeedFeetPerSec * FEET_TO_METERS;

            GenerateSwerveSetpoints(
                Mathf.Clamp(fwd, -1, 1),
                Mathf.Clamp(str, -1, 1),
                Mathf.Clamp(rotation, -1, 1));

            IsTouchingGround = false;

            RunSwerveModuleSphere(FL_MODULE);
            RunSwerveModuleSphere(FR_MODULE);
            RunSwerveModuleSphere(BL_MODULE);
            RunSwerveModuleSphere(BR_MODULE);
        }

        /// <summary>
        /// Inverse kinematics: converts chassis-space fwd/str/rotation into a
        /// per-module (angle, speed) setpoint using the classic swerve formulas.
        /// </summary>
        private void GenerateSwerveSetpoints(float fwd, float str, float rotation)
        {
            var a = str - rotation * (_length / _radius);
            var b = str + rotation * (_length / _radius);
            var c = fwd - rotation * (_width / _radius);
            var d = fwd + rotation * (_width / _radius);

            CalculateModuleSetpoint(FR_MODULE, b, c);
            CalculateModuleSetpoint(FL_MODULE, b, d);
            CalculateModuleSetpoint(BL_MODULE, a, d);
            CalculateModuleSetpoint(BR_MODULE, a, c);
        }

        private void CalculateModuleSetpoint(int moduleIndex, float x, float y)
        {
            var speed = Mathf.Sqrt(x * x + y * y);
            _swerveSetpoints[moduleIndex].Velocity = speed;

            // Only update angle if there's movement, so modules hold heading
            // when speed drops to zero rather than snapping to 0 deg.
            if (speed > 0f)
            {
                _swerveSetpoints[moduleIndex].Angle = Mathf.Atan2(x, y) * RAD_TO_DEG;
            }
        }

        /// <summary>
        /// Ground-contact check for a module via a short spherecast down the
        /// wheel's local up axis, then applies propulsion force at the contact point.
        /// </summary>
        private void RunSwerveModuleSphere(int moduleIndex)
        {
            var module = _swerveWheels[moduleIndex];
            var wheelRadius = (_wheelDiameter / 2f) * INCHES_TO_METERS;

            if (Physics.SphereCast(
                    module.transform.position,
                    wheelRadius * 0.3f,
                    -module.transform.up,
                    out RaycastHit hit,
                    wheelRadius * 1.1f,
                    ~_fieldLayerMask))
            {
                ApplyWheelForceAtContact(moduleIndex, hit);
                IsTouchingGround = true;
            }
        }

        private void ApplyWheelForceAtContact(int moduleIndex, RaycastHit hit)
        {
            var module = _swerveWheels[moduleIndex];
            var setpoint = _swerveSetpoints[moduleIndex];

            // Ground speed along the module's forward axis.
            var realGroundSpeed = module.transform.InverseTransformVector(
                _rb.GetPointVelocity(module.transform.position)).z;

            var speedRatio = Mathf.Clamp01(Mathf.Abs(realGroundSpeed) / _maxSpeedMeters);
            var falloff = GetFalloffFromLookup(speedRatio);

            var forceMag = _accelerationForce * FEET_TO_METERS * setpoint.Velocity * falloff;

            module.transform.localEulerAngles = new Vector3(0f, setpoint.Angle, 0f);

            Vector3 propulsionForce = module.transform.forward * forceMag;
            _rb.AddForceAtPosition(propulsionForce, hit.point, ForceMode.Impulse);

            module.wheelAngle = module.transform.localRotation.eulerAngles.y;
        }

        // ---- Velocity falloff lookup table ----
        // Precomputes Pow(1 - speedRatio * falloffPercent, falloffExponent) across
        // a fixed resolution so per-module force application avoids a Pow() call
        // every physics step.

        private void BuildFalloffLookupTable()
        {
            _maxSpeedMeters = _maxSpeedFeetPerSec * FEET_TO_METERS;
            _falloffLookup = new float[LookupTableSize];

            for (int i = 0; i < LookupTableSize; i++)
            {
                float speedRatio = (float)i / (LookupTableSize - 1);
                _falloffLookup[i] = Mathf.Pow(1f - speedRatio * _falloffPercent, _falloffExponent);
            }
        }

        private float GetFalloffFromLookup(float speedRatio)
        {
            int index = Mathf.Clamp(
                Mathf.RoundToInt(speedRatio * (LookupTableSize - 1)),
                0,
                LookupTableSize - 1);

            return _falloffLookup[index];
        }

        // ---- Drive audio ----
        // Call once per Update() (not FixedUpdate) — this is presentation, not
        // physics, and doesn't need to run at a fixed timestep.

        public void UpdateAudio()
        {
            if (_rb.velocity.magnitude > 0f || Mathf.Abs(_rb.angularVelocity.magnitude) > 0f)
            {
                PlaySwerveSounds();
            }
            else
            {
                StopSwerveSounds();
            }
        }

        private void PlaySwerveSounds()
        {
            var velocityFactor = Mathf.Clamp01(_rb.velocity.magnitude / _rb.maxLinearVelocity);
            var rotationFactor = Mathf.Clamp01(Mathf.Abs(_rb.angularVelocity.magnitude) / 6);
            var accelerationFactor = Mathf.Clamp(1f + velocityFactor, 1f, 2f);

            var volume = velocityFactor + rotationFactor * 0.25f;
            var pitch = Mathf.Max(accelerationFactor, rotationFactor);

            _treadSource.volume = volume * 0.5f;
            _treadSource.pitch = pitch * 0.6f;
            _gearSource.volume = volume * 0.2f;

            if (!_treadSource.isPlaying)
            {
                _treadSource.Play();
                _gearSource.Play();
            }
        }

        private void StopSwerveSounds()
        {
            if (_treadSource.isPlaying)
            {
                _treadSource.Stop();
                _gearSource.Stop();
            }
        }
    }
}