using UnityEngine;

public class LapsCounterTrigger :
    MonoBehaviour,
    ILapsRespawnTrigger
{
    [Header("Respawning")]
    public bool enableRespawning = true;
    public bool customSpawnPoint = false;

    public string spawnPoint = "";
    public string forceGravity = "";

    [Header("Respawn Transforms")]
    [SerializeField] private Transform spawnTransform;
    [SerializeField] private Transform cameraPosTransform;

    private void Start()
    {
        if (GameManager.instance == null ||
            GameManager.instance.startPad == null)
            return;

        Transform startPad =
            GameManager.instance.startPad.transform;

        spawnTransform =
            startPad.Find("Spawn");

        cameraPosTransform =
            startPad.Find("CameraPos");
    }

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

        lapsMode.OnCounterTrigger(
            this,
            marble
        );
    }
}