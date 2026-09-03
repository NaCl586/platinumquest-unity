using UnityEngine;

public class RepetitiveTriggerGotoTarget : MonoBehaviour
{
    [Header("Target")]
    public MovingPlatform movingPlatform;

    [Header("Trigger")]
    public int numTimesToTrigger = 0;
    public bool triggerOnce = true;
    public int numTimesToRepeat = 0;

    [Header("Target Time")]
    [Tooltip("Target time in milliseconds, matching the PQ mission field.")]
    public float targetTime = 999999f;

    private int enterCount;
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        int count = ++enterCount;

        if (count < numTimesToTrigger)
            return;

        if (triggerOnce && count > numTimesToTrigger)
            return;

        if (
            triggered
            && numTimesToRepeat != 0
            && (count - numTimesToTrigger) % numTimesToRepeat != 0
        )
        {
            return;
        }

        if (movingPlatform == null)
            return;

        float targetSeconds = targetTime;

        if (targetSeconds > 0f)
            targetSeconds /= 1000f;

        movingPlatform.GoToTime(targetSeconds);

        triggered = true;
    }

    public void ResetTrigger()
    {
        triggered = false;
        enterCount = 0;
    }
}