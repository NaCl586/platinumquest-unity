using UnityEngine;

public class LapsCheckpoint :
    MonoBehaviour,
    ILapsRespawnTrigger
{
    [Header("Checkpoint")]
    public int checkpointNumber = 1;

    [Header("Respawning")]
    public bool enableRespawning = true;
    public bool customSpawnPoint = false;

    public string spawnPoint = "";
    public string forceGravity = "";

    [Header("Respawn Transforms")]
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private Transform cameraPosTransform;

    public bool EnableRespawning =>
        enableRespawning;

    public Transform spawn =>
        spawnTransform;

    public Transform cameraPos =>
        cameraPosTransform;

    public string ForceGravity =>
        forceGravity;

    private void OnTriggerEnter(Collider other)
    {
        Marble marble =
            other.GetComponentInParent<Marble>();

        if (marble == null ||
            marble != Marble.instance)
            return;

        LapsMode lapsMode = null;

        foreach (IGameMode mode in GameManager.instance.GameModes)
        {
            if (mode is LapsMode laps)
            {
                lapsMode = laps;
                break;
            }
        }

        if (lapsMode == null)
            return;

        lapsMode.OnCheckpointTrigger(
            this,
            marble
        );
    }
}