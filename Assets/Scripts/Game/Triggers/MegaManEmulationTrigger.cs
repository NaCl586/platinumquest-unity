using UnityEngine;

public class MegaManEmulationTrigger : MonoBehaviour
{
    private string startplatform;

    public string StartPlatformName
    {
        get => startplatform;
        set
        {
            startplatform = value;
            startPlatform = null;
        }
    }

    private MegaManPlatform startPlatform;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponentInParent<Marble>();

        if (marble == null)
            return;

        OnMarbleEnter(marble);
    }

    private void OnMarbleEnter(Marble marble)
    {
        if (string.IsNullOrEmpty(startplatform))
            return;

        MegaManPlatform platform = ResolveStartPlatform();

        if (platform == null)
            return;

        // First disable/reset every platform in this Mega Man sequence.
        ResetPlatformSequence(platform);

        // Now activate only the first platform.
        platform.RespondToCollision = true;
        platform.HasCollided = false;

        platform.Show(GetAttemptTime());
    }

    private MegaManPlatform ResolveStartPlatform()
    {
        if (startPlatform != null)
            return startPlatform;

        GameObject obj = GameObject.Find(startplatform);

        if (obj == null)
        {
            MegaManPlatform[] platforms =
                FindObjectsOfType<MegaManPlatform>(true);

            foreach (MegaManPlatform platform in platforms)
            {
                if (string.Equals(
                    platform.gameObject.name,
                    startplatform,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    obj = platform.gameObject;
                    break;
                }
            }
        }

        if (obj != null)
            startPlatform =
                obj.GetComponent<MegaManPlatform>();

        return startPlatform;
    }

    private void ResetPlatformSequence(MegaManPlatform firstPlatform)
    {
        MegaManPlatform current = firstPlatform;

        // Safety limit in case the mission data accidentally contains
        // a circular Next chain.
        const int maxPlatforms = 100;

        for (int i = 0; i < maxPlatforms; i++)
        {
            if (current == null)
                return;

            current.ResetPlatform();

            current = current.GetNextPlatform();
        }
    }

    private float GetAttemptTime()
    {
        if (GameManager.instance != null)
            return GameManager.instance.elapsedTime / 1000f;

        return 0f;
    }
}