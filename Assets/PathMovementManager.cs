using System.Collections.Generic;
using UnityEngine;

public class PathMovementManager : MonoBehaviour
{
    private readonly List<PathMover> movingObjects = new List<PathMover>();

    public void RegisterMovingObject(PathMover mover)
    {
        if (mover == null)
            return;

        if (!movingObjects.Contains(mover))
        {
            movingObjects.Add(mover);
        }
    }

    public void ComputeNextPathStep(float deltaTime)
    {
        for (int i = 0; i < movingObjects.Count; i++)
        {
            PathMover mover = movingObjects[i];

            if (mover == null)
                continue;

            mover.ComputeNextPathStep(deltaTime);
        }
    }

    public void AdvancePath(float timeStep)
    {
        for (int i = 0; i < movingObjects.Count; i++)
        {
            PathMover mover = movingObjects[i];

            if (mover == null)
                continue;

            mover.AdvancePath(timeStep);
        }
    }

    public void ResetMovement()
    {
        for (int i = 0; i < movingObjects.Count; i++)
        {
            PathMover mover = movingObjects[i];

            if (mover == null)
                continue;

            mover.ResetMover();
        }
    }

    public void Clear()
    {
        movingObjects.Clear();
    }

    private void FixedUpdate()
    {
        float timeStep = Time.fixedDeltaTime;

        // Phase 1:
        // Calculate the new path state.
        ComputeNextPathStep(timeStep);

        // Phase 2:
        // Apply/interpolate the calculated state.
        AdvancePath(timeStep);
    }
}
