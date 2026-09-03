using UnityEngine;

public class CancelVelocityTrigger : MonoBehaviour
{
    [Header("Cancel Velocity")]
    public bool cancelX;
    public bool cancelY;
    public bool cancelZ;

    private void OnTriggerEnter(Collider other)
    {
        Movement movement = other.GetComponent<Movement>();

        if (movement == null)
            return;

        Vector3 velocity = movement.marbleVelocity;

        if (cancelX)
            velocity.x = 0f;

        if (cancelY)
            velocity.y = 0f;

        if (cancelZ)
            velocity.z = 0f;

        movement.marbleVelocity = velocity;
    }
}