using System.Collections.Generic;
using UnityEngine;

public class NullMode : IGameMode
{
    protected readonly GameManager gameManager;

    // Null mode does not track collected gems.
    private static readonly List<Gem> emptyCollectedGems =
        new List<Gem>();

    public List<Gem> collectedGems =>
        emptyCollectedGems;

    public NullMode(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public virtual void OnMissionLoad()
    {
        if (GameManager.instance.ContainsMode(Mode.Quota))
            return;

        GameUIManager.instance.SetTargetGem(
            gameManager.TotalGems
        );

        GameUIManager.instance.SetCurrentGem(
            gameManager.CurrentGems
        );
    }

    public virtual void OnRestart()
    {
    }

    public virtual void OnRespawn()
    {
        if (!GameManager.instance.ContainsMode(Mode.TwoD))
            CameraController.instance?.ResetCam();
    }

    public virtual void OnCheckpointReached()
    {
        // Default modes have no checkpoint-specific state.
    }

    public virtual void OnGemCollected(
        int newGemCount)
    {
    }

    public virtual bool CanFinish()
    {
        return gameManager.CheckForAllGems();
    }

    public virtual string GetFinishMessage()
    {
        return "You may not finish without all gems!";
    }

    public virtual string GetGemPickupMessage()
    {
        int current =
            gameManager.CurrentGems;

        int total =
            gameManager.TotalGems;

        int remaining =
            total - current;

        if (remaining == 1)
            return "You picked up a gem! Only one more gem to go!";

        if (remaining == 0)
            return "You picked up all the gems! Head for the finish!";

        return $"You picked up a gem! {remaining} gems to go!";
    }

    public virtual int GetGemTarget()
    {
        return gameManager.TotalGems;
    }

    public virtual bool ShouldPlayCollectAllGemsSound(
        int newGemCount)
    {
        return newGemCount ==
               gameManager.TotalGems;
    }

    public virtual void OnGemCollected(
        Gem gem,
        int newGemCount)
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual Vector2 FilterMovementInput(
        Vector2 input)
    {
        return input;
    }

    public virtual void OnCameraReady()
    {
        if (!GameManager.instance.ContainsMode(Mode.TwoD))
        {
            CameraController.instance.UpdateAnglesFromOffset();
            CameraController.instance.ResetCam();
        }
    }

    public virtual Vector3 GetSuperSpeedDirection(Vector3 defaultDirection)
    {
        return defaultDirection;
    }
}