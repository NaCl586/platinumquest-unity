using UnityEngine;

public class LockPowerupTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        marble.LockPowerupUse();
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        marble.UnlockPowerupUse();
    }
}