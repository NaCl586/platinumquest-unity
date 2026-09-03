using UnityEngine;

public class MustChangeTrigger : MonoBehaviour
{
    [Header("Moving Platform")]
    public MovingPlatform movingPlatform;

    [Header("Target Time")]
    [Tooltip("Target position/time in seconds.")]
    public float targetTime = 0f;

    [Header("Delay")]
    [Tooltip("Delay target time in seconds.")]
    public float delayTargetTime = 0f;

    [Header("Instant")]
    public bool instant = false;

    [Tooltip(
        "Equivalent to the Haxe icontinuetottime field. " +
        "Used only when Instant is enabled."
    )]
    public float iContinueToTime = 0f;


    // ============================================================
    // Trigger
    // ============================================================

    private void OnTriggerEnter(Collider other)
    {
        Marble marble =
            other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (movingPlatform == null)
            return;


        // --------------------------------------------------------
        // delaytargettime
        // --------------------------------------------------------

        if (delayTargetTime > 0f)
        {
            movingPlatform.delayTargetTime =
                delayTargetTime;
        }


        // --------------------------------------------------------
        // targettime
        // --------------------------------------------------------

        float target = targetTime;

        if (target > 0f)
            target /= 1f;

        movingPlatform.GoToTime(target);


        // --------------------------------------------------------
        // instant
        // --------------------------------------------------------

        if (!instant)
            return;


        // --------------------------------------------------------
        // icontinuetottime
        // --------------------------------------------------------

        if (iContinueToTime > 0f)
        {
            /*
             * Haxe:
             *
             * interior.currentTime =
             *     interior.targetTime;
             *
             * interior.targetTime =
             *     parseNumber(icontinuetottime) / 1000;
             */

            movingPlatform.SetCurrentTimeToTarget();

            movingPlatform.GoToTime(
                iContinueToTime
            );
        }
    }
}