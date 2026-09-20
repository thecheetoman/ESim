using UnityEngine;

public class Flywheel : MonoBehaviour
{
    [Header("Motor")]
    [Tooltip("Max rotations per second at full power (100%).")]
    public float maxRPS = 40f;

    [Tooltip("Seconds to spin from 0 to maxRPS. Real motors aren't instant.")]
    public float spinUpTime = 0.25f;

    [Tooltip("Seconds to coast from maxRPS down to 0 once power is released.")]
    public float spinDownTime = 0.6f;

    [Tooltip("Optional non-linear torque curve (0-1 time -> 0-1 RPS). " +
             "Leave a straight line for linear spin-up, or shape it for " +
             "a motor that surges early and tapers near max RPS.")]
    public AnimationCurve spinUpCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("Wheel radius in meters. Used to convert RPS to a real " +
             "tangential surface speed (v = ω * r).")]
    public float radius = 0.05f;

    [Header("Visual")]
    [Tooltip("The mesh/transform that actually spins. Keep this separate " +
             "from any parent that holds hood angle, so rotations don't fight.")]
    public Transform flywheelMesh;
    public bool spinVisual = true;

    [Header("Audio")]
    [Tooltip("Looping motor whir/hum. Pitch and volume track CurrentRPS.")]
    public AudioSource motorAudio;
    public float minPitch = 0.6f;
    public float maxPitch = 2.2f;
    public float minVolume = 0.05f;
    public float maxVolume = 1f;

    public float CurrentRPS { get; private set; }

    private float targetRPS;
    private float spinT;              // 0-1 progress along spinUpCurve
    private float visualRotationZ;

    public void SetPowerLevel(float normalized01)
    {
        targetRPS = Mathf.Clamp01(normalized01) * maxRPS;
    }
    public void ApplyLoadKick(float rpsDrop)
    {
        CurrentRPS = Mathf.Max(0f, CurrentRPS - rpsDrop);
        spinT = maxRPS > 0f ? CurrentRPS / maxRPS : 0f;
    }

    public float GetSurfaceSpeed()
    {
        return CurrentRPS * 2f * Mathf.PI * radius;
    }

    private void Update()
    {
        StepRPS();
        UpdateVisual();
        UpdateAudio();
    }

    private void StepRPS()
    {
        bool spinningUp = targetRPS > CurrentRPS;
        float duration = spinningUp ? spinUpTime : spinDownTime;
        duration = Mathf.Max(duration, 0.0001f);

        spinT = Mathf.MoveTowards(spinT, spinningUp ? 1f : 0f, Time.deltaTime / duration);

        // Use the curve for spin-up character; spin-down stays linear (coasting/braking).
        float shaped = spinningUp ? spinUpCurve.Evaluate(spinT) : spinT;
        CurrentRPS = Mathf.Lerp(0f, maxRPS, shaped);
    }

    private void UpdateVisual()
    {
        if (!spinVisual || flywheelMesh == null) return;

        visualRotationZ = (visualRotationZ + CurrentRPS * 360f * Time.deltaTime) % 360f;
        Vector3 e = flywheelMesh.localEulerAngles;
        flywheelMesh.localRotation = Quaternion.Euler(e.x, e.y, visualRotationZ);
    }

    private void UpdateAudio()
    {
        if (motorAudio == null) return;

        float t = maxRPS > 0f ? CurrentRPS / maxRPS : 0f;
        motorAudio.pitch = Mathf.Lerp(minPitch, maxPitch, t);
        motorAudio.volume = Mathf.Lerp(minVolume, maxVolume, t);

        if (t > 0.01f && !motorAudio.isPlaying) motorAudio.Play();
        else if (t <= 0.01f && motorAudio.isPlaying) motorAudio.Stop();
    }
}