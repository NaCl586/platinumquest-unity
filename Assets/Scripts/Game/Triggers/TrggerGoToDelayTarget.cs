using UnityEngine;

public class TriggerGotoDelayTarget : MonoBehaviour
{
    public MovingPlatform movingPlatform;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Marble"))
            return;

        Activate();
    }

    public void OnEnterTrigger()
    {
        Activate();
    }

    private void Activate()
    {
        if (movingPlatform == null)
            return;

        movingPlatform.GoToDelayTargetTime();
    }
}