using UnityEngine;

public class RelativeTPTrigger : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Name of the DestinationTrigger in the scene.")]
    public string destinationTriggerName;

    // Resolved automatically by MissionImporter.
    [HideInInspector]
    public GameObject destination;

    [Header("Teleport")]
    public bool silent = false;

    public Vector3 tpScale = Vector3.one;
    public Vector3 tpOffset = Vector3.zero;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (destination == null)
        {
            Debug.LogWarning(
                $"RelativeTPTrigger '{gameObject.name}' could not find " +
                $"destination trigger '{destinationTriggerName}'."
            );
            return;
        }

        Collider triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null)
            return;

        Vector3 destinationCenter = GetDestinationCenter();

        // Haxe:
        //
        // diff = triggerCenter * -tpScale
        //
        Vector3 triggerCenter = triggerCollider.bounds.center;

        Vector3 diff = new Vector3(
            triggerCenter.x * -tpScale.x,
            triggerCenter.y * -tpScale.y,
            triggerCenter.z * -tpScale.z
        );

        // Haxe:
        //
        // pos = destCenter + diff + tpOffset
        //
        Vector3 offset =
            destinationCenter +
            diff +
            tpOffset;

        // Haxe:
        //
        // marblePos += pos
        //
        Vector3 newPosition =
            marble.transform.position +
            offset;

        Movement movement =
            marble.GetComponent<Movement>();

        if (movement != null)
        {
            movement.SetPosition(newPosition, silent);
        }
        else
        {
            marble.transform.position = newPosition;
        }

        // Keep the marble's physics position synchronized.
        Rigidbody rb =
            marble.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.position = newPosition;
        }

        // Sound is optional for this trigger.
        if (!silent && marble == Marble.instance)
        {
            if (Marble.instance.teleportSound != null)
            {
                Marble.instance.teleportSound.Play();
            }
        }
    }

    private Vector3 GetDestinationCenter()
    {
        Collider destinationCollider =
            destination.GetComponent<Collider>();

        if (destinationCollider != null)
            return destinationCollider.bounds.center;

        return destination.transform.position;
    }

    public void SetDestination(GameObject destinationObject)
    {
        destination = destinationObject;
    }

    public void ResetTrigger()
    {
        // No persistent state in the Haxe trigger.
    }
}
