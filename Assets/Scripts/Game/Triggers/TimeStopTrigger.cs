using UnityEngine;

public class TimeStopTrigger : MonoBehaviour
{
    private bool marbleInside;

    private void FixedUpdate()
    {
        Marble marble = Marble.instance;

        if (marble == null)
            return;

        Collider triggerCollider = GetComponent<Collider>();
        Collider marbleCollider = marble.GetComponent<Collider>();

        if (triggerCollider == null || marbleCollider == null)
            return;

        bool isInside = triggerCollider.bounds.Intersects(marbleCollider.bounds);

        if (isInside && !marbleInside)
        {
            marbleInside = true;
            GameManager.instance.timeStopTriggerCount++;
        }
        else if (!isInside && marbleInside)
        {
            marbleInside = false;
            GameManager.instance.timeStopTriggerCount--;

            if (GameManager.instance.timeStopTriggerCount < 0)
                GameManager.instance.timeStopTriggerCount = 0;
        }
    }
}