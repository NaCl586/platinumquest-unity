using UnityEngine;

public class Tornado : MonoBehaviour
{
    public bool powered = true;

    public void SetPowered(bool value)
    {
        powered = value;
    }

    private void FixedUpdate()
    {
        if (!powered || Movement.instance == null)
            return;

        Vector3 origin = transform.position;
        Vector3 totalForce = Vector3.zero;

        totalForce += ApplySphericalForce(origin, 8f, -60f);
        totalForce += ApplySphericalForce(origin, 3f, 60f);
        totalForce += ApplyFieldForce(
            origin,
            3f,
            new Vector3(0f, 150f, 0f)
        );

        Movement.instance.marbleVelocity +=
            totalForce * Time.fixedDeltaTime;
    }

    private Vector3 ApplySphericalForce(
        Vector3 origin,
        float radius,
        float strength)
    {
        Vector3 marblePos = Movement.instance.transform.position;
        Vector3 vec = marblePos - origin;

        float dist = vec.magnitude;

        if (dist <= Mathf.Epsilon || dist >= radius)
            return Vector3.zero;

        float strengthFac =
            1f - Mathf.Pow(Mathf.Clamp01(dist / radius), 2f);

        return vec.normalized * strength * strengthFac;
    }

    private Vector3 ApplyFieldForce(
        Vector3 origin,
        float radius,
        Vector3 forceVector)
    {
        Vector3 marblePos = Movement.instance.transform.position;

        if (Vector3.Distance(marblePos, origin) >= radius)
            return Vector3.zero;

        return forceVector;
    }
}