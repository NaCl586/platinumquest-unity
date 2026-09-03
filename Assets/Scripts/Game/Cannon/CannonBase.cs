using UnityEngine;

public class CannonBase : MonoBehaviour
{
    public Cannon cannon;

    public void EnterTrigger(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble != null && cannon != null)
        {
            marble.EnterCannon(cannon);
        }
    }
}