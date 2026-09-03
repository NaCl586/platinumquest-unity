using UnityEngine;

public class NoMovementKeysTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        marble.movement.EnterMovementTrigger();
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        marble.movement.ExitMovementTrigger();
    }
}