using UnityEngine;

public class DuctFan : MonoBehaviour
{
    public float radius = 10f;
    public float arc = 0.7f;
    public float strength = 50f;

    public bool powered = true;

    public void SetPowered(bool value)
    {
        powered = value;
    }

    private void FixedUpdate()
    {
        if (!powered)
            return;

        Vector3 force = ComputeConicForce();
        Movement.instance.marbleVelocity += force * Time.fixedDeltaTime;
    }

    private Vector3 ComputeConicForce()
    {
        Vector3 marblePos = Movement.instance.transform.position;
        Vector3 coneTip = transform.position - transform.up * 0.7f;

        Vector3 toMarble = marblePos - coneTip;
        float distance = toMarble.magnitude;

        if (distance <= Mathf.Epsilon || distance > radius)
            return Vector3.zero;

        float distanceFactor = 1f - distance / radius;
        float finalStrength = distanceFactor * strength;

        toMarble /= distance;

        Vector3 axis = transform.up;
        float dot = Vector3.Dot(axis, toMarble);

        if (dot <= arc)
            return Vector3.zero;

        float coneFactor = (dot - arc) / (1f - arc);

        return toMarble * finalStrength * coneFactor;
    }
}