using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Ball : MonoBehaviour
{
    private Rigidbody rb;
    private Collider ballCollider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
    }

    // Moves ball to child target while turning off collisions
    public IEnumerator TravelToTargetRoutine(Transform target, float speed)
    {
        rb.isKinematic = true;
        ballCollider.enabled = false;

        while (Vector3.Distance(transform.position, target.position) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = target.position;
    }

    // Re-enables collisions and applies launch velocity
    public void ExitWithVelocity(Vector3 direction, float speed)
    {
        rb.isKinematic = false;
        ballCollider.enabled = true;
        rb.velocity = direction.normalized * speed;
    }
}