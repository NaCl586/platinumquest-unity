using UnityEngine;

public class GravityTrigger : MonoBehaviour
{
    public bool onLeave;
    public GameObject from, to;

    [SerializeField]
    private Vector3 downVector = Vector3.down;

    private void Apply()
    {
        Vector3 direction = to.transform.position - from.transform.position;

        direction.Normalize();

        Vector3 oldGravity = GravitySystem.GravityDir.normalized;

        GravitySystem.GravityDir = direction;

        if (Marble.instance != null &&
            Marble.instance.gyrocopterBlades != null)
        {
            Marble.instance.gyrocopterBlades.transform.up = -direction;
        }

        GravityModifier.onGravityChanged?.Invoke(
            oldGravity,
            direction
        );
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (onLeave)
            return;

        Marble marble = collider.GetComponent<Marble>();

        Debug.Log(marble);

        if (marble != null)
            Apply();
    }

    public void OnTriggerExit(Collider collider)
    {
        if (!onLeave)
            return;

        Marble marble = collider.GetComponent<Marble>();

        if (marble != null)
            Apply();
    }
}