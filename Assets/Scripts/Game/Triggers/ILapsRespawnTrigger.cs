using UnityEngine;

public interface ILapsRespawnTrigger
{
    bool EnableRespawning { get; }

    Transform spawn { get; }
    Transform cameraPos { get; }

    string ForceGravity { get; }
}