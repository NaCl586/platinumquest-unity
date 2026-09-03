using UnityEngine;

public class HelpBubble : MonoBehaviour
{
    public float triggerRadius = 3f;
    public bool displayOnce = false;
    public bool disabled = false;

    [Header("References")]
    public HelpTrigger helpTrigger;

    private bool wasWithin = false;
    private bool hasBeenInOnce = false;

    private void Update()
    {
        if (disabled)
            return;

        if (GameManager.instance == null)
            return;

        if (globalMarble == null)
            return;

        Vector3 bubblePosition = transform.position;
        Vector3 marblePosition = globalMarble.transform.position;

        bool within = Vector3.Distance(marblePosition, bubblePosition) < triggerRadius;

        // Equivalent to:
        //
        // if (within && !wasWithin)
        //
        // This means the help only triggers when
        // entering the radius, not every frame.
        if (within && !wasWithin)
        {
            if ((!displayOnce || !hasBeenInOnce) && !string.IsNullOrEmpty(helpTrigger.helpText))
            {
                hasBeenInOnce = true;

                if (helpTrigger != null)
                {
                    helpTrigger.TriggerEnter();
                }
            }
        }

        wasWithin = within;
    }

    public void ResetBubble()
    {
        wasWithin = false;
        hasBeenInOnce = false;
    }

    private GameObject globalMarble
    {
        get { return GameManager.instance != null ? Marble.instance.gameObject : null; }
    }
}
