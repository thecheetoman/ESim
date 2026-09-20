using System.Collections;
using UnityEngine;

public class TurretHoodShooter : MonoBehaviour
{
    [Header("Path & Exit Setup")]
    [Tooltip("Point where the ball enters the hood from the feeder")]
    public Transform hoodEntryPoint;

    [Tooltip("Point at the end of the hood that defines exit angle and velocity vector")]
    public Transform hoodExitPoint;

    [Header("Flywheel")]
    [Tooltip("The flywheel that actually powers this shot. If null, falls back to exitVelocity.")]
    public Flywheel flywheel;

    [Tooltip("Fraction of the flywheel's rim speed that actually transfers to the ball " +
             "(friction/slip losses). 1.0 = perfect transfer, unrealistic. 0.7-0.9 is typical.")]
    [Range(0f, 1f)]
    public float transferEfficiency = 0.85f;

    [Tooltip("Minimum RPS required for a clean launch. Below this the ball still " +
             "gets shoved out, but weakly - simulates an underpowered motor.")]
    public float minRPSForCleanShot = 8f;

    [Tooltip("How much RPS the flywheel loses when a ball takes energy from it. " +
             "Feeds into Flywheel.ApplyLoadKick so back-to-back shots aren't identical.")]
    public float rpsLoadKick = 6f;

    [Header("Fallback (used only if flywheel is null)")]
    public float exitVelocity = 18f;

    [Header("Timing")]
    public float travelTimeThroughHood = 0.15f; // Time (in seconds) the ball takes to travel up the hood

    [Tooltip("Maximum angle of random deviation in degrees applied to the exit trajectory")]
    public float maxDeviationAngle = 2.0f;

    [Header("Status")]
    public bool isReadyToShoot = true;

    [Header("Turret Reference")]
    public Turret Turret;

    [Tooltip("Angle where hood is at default position (0)")]
    public float minHoodAngle = 0f;

    [Tooltip("Angle where hood is at max extension (-30)")]
    public float maxHoodAngle = -30f;

    [Tooltip("Additional velocity multiplier added at full hood extension (e.g. 0.5 = 1.5x total speed)")]
    public float hoodVMult = 0.5f;

    private void Update()
    {
        // Hold Space to spin the flywheel up; release to let it coast back down.
        if (flywheel != null)
        {
            flywheel.SetPowerLevel(Input.GetKey(KeyCode.Space) ? 1f : 0f);
        }
    }

    public void FeedBallIntoTurret(GameObject ball)
    {
        if (!isReadyToShoot) return;

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        Collider ballCollider = ball.GetComponent<Collider>();

        if (ballRb != null)
        {
            StartCoroutine(PropelThroughHood(ball, ballRb, ballCollider));
        }
    }

    private IEnumerator PropelThroughHood(GameObject ball, Rigidbody ballRb, Collider ballCollider)
    {
        isReadyToShoot = false;

        // 1. Momentarily take over physics to ensure smooth travel up the hood
        ballRb.isKinematic = true;
        if (ballCollider != null) ballCollider.enabled = false;

        float elapsedTime = 0f;
        Vector3 startPos = hoodEntryPoint.position;

        // 2. Interpolate ball position along the hood arc toward the exit point
        while (elapsedTime < travelTimeThroughHood)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTimeThroughHood;

            Vector3 currentPos = Vector3.Lerp(startPos, hoodExitPoint.position, t);
            ball.transform.position = currentPos;

            yield return null;
        }

        ball.transform.position = hoodExitPoint.position;

        // 3. Restore physics and apply exit vector with random deviation
        ballRb.isKinematic = false;
        if (ballCollider != null) ballCollider.enabled = true;

        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-maxDeviationAngle, maxDeviationAngle),
            Random.Range(-maxDeviationAngle, maxDeviationAngle),
            0f
        );
        Vector3 launchDirection = randomRotation * hoodExitPoint.forward;

        // --- Base speed: driven by flywheel surface speed (v = ω * r), not a flat constant ---
        float baseSpeed;
        if (flywheel != null)
        {
            float surfaceSpeed = flywheel.GetSurfaceSpeed();

            // Weak/underpowered shots: below threshold, scale down further instead of
            // hard-cutting, so it reads as "sputtering out" rather than snapping to zero.
            float rpsRatio = flywheel.maxRPS > 0f ? flywheel.CurrentRPS / flywheel.maxRPS : 0f;
            float thresholdRatio = flywheel.maxRPS > 0f ? minRPSForCleanShot / flywheel.maxRPS : 0f;
            float weakShotPenalty = flywheel.CurrentRPS < minRPSForCleanShot
                ? Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0f, thresholdRatio, rpsRatio))
                : 1f;

            baseSpeed = surfaceSpeed * transferEfficiency * weakShotPenalty;
        }
        else
        {
            baseSpeed = exitVelocity;
        }

        // Hood angle still adds its own multiplier on top (mechanical advantage of hood extension)
        float rawAngle = (Turret != null) ? Turret.currentLocalHoodAngle : minHoodAngle;
        float normalizedHood = Mathf.InverseLerp(minHoodAngle, maxHoodAngle, rawAngle);
        float hoodMultiplier = 1f + (normalizedHood * hoodVMult);

        ballRb.velocity = launchDirection * (baseSpeed * hoodMultiplier);

        // Feedback: the wheel just gave up energy to the ball and needs to spin back up
        if (flywheel != null)
        {
            flywheel.ApplyLoadKick(rpsLoadKick);
        }

        isReadyToShoot = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GamePiece") && isReadyToShoot)
        {
            FeedBallIntoTurret(other.gameObject);
        }
    }
}