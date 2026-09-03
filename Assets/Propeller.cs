using UnityEngine;

public class Propeller : MonoBehaviour
{
    [Header("Force")]
    public float radius = 10f;
    public float arc = 0.7f;
    public float strength = 15f;

    private void FixedUpdate()
    {
        if (Movement.instance == null)
            return;

        Vector3 force = ComputeConicForce();
        Movement.instance.marbleVelocity += force * Time.fixedDeltaTime;
    }

    private Vector3 ComputeConicForce()
    {
        Vector3 marblePos = Movement.instance.transform.position;

        // Force originates slightly behind the propeller.
        Vector3 coneTip = transform.position - transform.up * 0.7f;

        Vector3 toMarble = marblePos - coneTip;
        float distance = toMarble.magnitude;

        // Outside the force radius.
        if (distance <= Mathf.Epsilon || distance > radius)
            return Vector3.zero;

        // Linear distance falloff.
        float distanceFactor = 1f - (distance / radius);
        float finalStrength = distanceFactor * strength;

        // Normalize direction to the marble.
        toMarble /= distance;

        // Propeller force direction.
        Vector3 axis = transform.forward;

        float dot = Vector3.Dot(axis, toMarble);

        // Outside the cone.
        if (dot <= arc)
            return Vector3.zero;

        // Cone falloff.
        float coneFactor = (dot - arc) / (1f - arc);

        return toMarble * finalStrength * coneFactor;
    }
}
