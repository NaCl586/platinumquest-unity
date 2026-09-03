using UnityEngine;

public class SetVelocityTrigger : MonoBehaviour
{
    [Header("Velocity")]
    public Vector3 velocity;

    [Header("Ignore Axes")]
    public bool ignoreX;
    public bool ignoreY;
    public bool ignoreZ;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        Vector3 newVelocity = marble.movement.marbleVelocity;

        if (!ignoreX)
            newVelocity.x = -velocity.x;

        if (!ignoreY)
            newVelocity.y = velocity.y;

        if (!ignoreZ)
            newVelocity.z = velocity.z;

        marble.movement.marbleVelocity = newVelocity;
    }
}