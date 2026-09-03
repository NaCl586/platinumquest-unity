using UnityEngine;

public class AccelerationTrigger : MonoBehaviour
{
    [Header("Acceleration")]
    public float xForce;
    public float yForce;
    public float zForce;

    private void OnTriggerStay(Collider other)
    {
        Movement marble = other.GetComponent<Movement>();

        if (marble == null)
            return;

        Vector3 velocity = marble.marbleVelocity;

        velocity.x += -xForce * Time.fixedDeltaTime;
        velocity.y += yForce * Time.fixedDeltaTime;
        velocity.z += zForce * Time.fixedDeltaTime;

        marble.marbleVelocity = velocity;
    }
}