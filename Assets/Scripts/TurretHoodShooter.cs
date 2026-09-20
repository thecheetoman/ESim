using System.Collections;
using UnityEngine;

public class TurretHoodShooter : MonoBehaviour
{
    [Header("Path & Exit Setup")]
    [Tooltip("Point where the ball enters the hood from the feeder")]
    public Transform hoodEntryPoint;

    [Tooltip("Point at the end of the hood that defines exit angle and velocity vector")]
    public Transform hoodExitPoint;

    [Header("Shooter Physics")]
    public float exitVelocity = 18f;        // Base launch speed
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

            // Curved path evaluation (eased motion up the hood)
            Vector3 currentPos = Vector3.Lerp(startPos, hoodExitPoint.position, t);
            ball.transform.position = currentPos;

            yield return null;
        }

        // Ensure exact snap to exit point on final frame
        ball.transform.position = hoodExitPoint.position;

        // 3. Restore physics and apply exit vector with random deviation
        ballRb.isKinematic = false;
        if (ballCollider != null) ballCollider.enabled = true;

        // Reset any residual velocities
        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        // Calculate a randomized direction vector within the specified cone angle
        Quaternion randomRotation = Quaternion.Euler(
            Random.Range(-maxDeviationAngle, maxDeviationAngle),
            Random.Range(-maxDeviationAngle, maxDeviationAngle),
            0f
        );

        Vector3 launchDirection = randomRotation * hoodExitPoint.forward;

        // Safely map the negative angle range (-30° to 0°) to a 0.0 to 1.0 ratio
        float rawAngle = (Turret != null) ? Turret.currentLocalHoodAngle : minHoodAngle;
        float normalizedHood = Mathf.InverseLerp(minHoodAngle, maxHoodAngle, rawAngle);

        // Calculate multiplier: 1.0x at 0°, scaling up to (1 + hoodVMult)x at -30°
        float velocityMultiplier = 1f + (normalizedHood * hoodVMult);

        ballRb.velocity = launchDirection * (exitVelocity * velocityMultiplier);

        isReadyToShoot = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Auto-fire trigger when a ball hits the entry zone
        if (other.CompareTag("GamePiece") && isReadyToShoot)
        {
            FeedBallIntoTurret(other.gameObject);
        }
    }
}