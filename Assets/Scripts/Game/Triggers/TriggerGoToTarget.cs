using System.Collections.Generic;
using UnityEngine;

public class TriggerGoToTarget : MonoBehaviour
{
    [Header("Moving Platforms")]

    public List<MovingPlatform> movingPlatforms =
        new List<MovingPlatform>();

    [Header("Target")]

    public float targetTime;

    public bool instantReturn = false;


    // =========================================================
    // TRIGGER
    // =========================================================

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


    // =========================================================
    // ACTIVATION
    // =========================================================

    public void Activate()
    {
        if (movingPlatforms == null ||
            movingPlatforms.Count == 0)
        {
            return;
        }

        for (int i = 0; i < movingPlatforms.Count; i++)
        {
            MovingPlatform movingPlatform =
                movingPlatforms[i];

            if (movingPlatform == null)
                continue;

            if (instantReturn)
            {
                movingPlatform.ResetMP();
            }
            else
            {
                movingPlatform.GoToTime(targetTime);
            }
        }
    }


    // =========================================================
    // PLATFORM REGISTRATION
    // =========================================================

    public void AddMovingPlatform(
        MovingPlatform movingPlatform)
    {
        if (movingPlatform == null)
            return;

        if (movingPlatforms.Contains(movingPlatform))
            return;

        movingPlatforms.Add(movingPlatform);
    }


    public void RemoveMovingPlatform(
        MovingPlatform movingPlatform)
    {
        if (movingPlatform == null)
            return;

        movingPlatforms.Remove(movingPlatform);
    }


    public void ClearMovingPlatforms()
    {
        movingPlatforms.Clear();
    }
}