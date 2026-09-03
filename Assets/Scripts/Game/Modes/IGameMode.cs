using System.Collections.Generic;
using UnityEngine;

public interface IGameMode
{
    void OnMissionLoad();

    void OnRestart();

    void OnRespawn();

    void OnCheckpointReached();

    void OnGemCollected(
        Gem gem,
        int newGemCount
    );

    bool CanFinish();

    string GetFinishMessage();

    string GetGemPickupMessage();

    int GetGemTarget();

    bool ShouldPlayCollectAllGemsSound(
        int newGemCount
    );

    Vector2 FilterMovementInput(Vector2 input);

    void OnUpdate();

    void OnCameraReady();

    Vector3 GetSuperSpeedDirection(Vector3 defaultDirection);
}