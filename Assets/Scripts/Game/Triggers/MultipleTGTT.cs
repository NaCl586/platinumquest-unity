using UnityEngine;

public class MultipleTGTT : MonoBehaviour
{
    [Header("Moving Platforms")]
    public MovingPlatform[] movingPlatforms = new MovingPlatform[9];

    [Header("Target")]
    public float targetTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        OnEnterTrigger();
    }

    private void OnEnterTrigger()
    {
        if (targetTime < 0f)
            return;

        if (movingPlatforms == null || movingPlatforms.Length == 0)
            return;

        foreach (MovingPlatform movingPlatform in movingPlatforms)
        {
            if (movingPlatform == null)
                continue;

            movingPlatform.GoToTime(targetTime);
        }
    }

    /// <summary>
    /// Assigns the nine platforms controlled by this trigger.
    /// Polymorphism uses plat1 through plat9.
    /// </summary>
    public void SetMovingPlatforms(MovingPlatform[] platforms)
    {
        if (platforms == null)
        {
            movingPlatforms = new MovingPlatform[9];
            return;
        }

        movingPlatforms = new MovingPlatform[platforms.Length];
        platforms.CopyTo(movingPlatforms, 0);
    }

    /// <summary>
    /// Convenience method for assigning the platforms individually.
    /// </summary>
    public void SetMovingPlatforms(
        MovingPlatform plat1,
        MovingPlatform plat2,
        MovingPlatform plat3,
        MovingPlatform plat4,
        MovingPlatform plat5,
        MovingPlatform plat6,
        MovingPlatform plat7,
        MovingPlatform plat8,
        MovingPlatform plat9)
    {
        movingPlatforms = new MovingPlatform[]
        {
            plat1,
            plat2,
            plat3,
            plat4,
            plat5,
            plat6,
            plat7,
            plat8,
            plat9
        };
    }
}