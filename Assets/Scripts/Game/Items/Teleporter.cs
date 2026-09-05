using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Teleporter : Powerups
{
    public static Teleporter activeTeleporter;

    [Header("Teleporter")]
    public float teleportDelay = 2f;

    [SerializeField]
    private bool keepVelocity = false;

    [Header("Marker")]
    [SerializeField]
    private GameObject teleportMarkerPrefab;

    [SerializeField]
    private GameObject teleportMarkerYellowPrefab;

    private GameObject teleportMarker;

    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;

    private Vector3 savedGravityDir;
    private float savedGravityStrength;

    private Coroutine teleportCoroutine;

    private bool locationSet;
    private bool teleporting;

    private Movement movement;
    private bool waitingForRelease;

    private MeshRenderer marbleRenderer;
    private Material marbleMaterial;

    private Color originalColor;
    private bool originallyTransparent;

    protected override void Start()
    {
        base.Start();

        powerupType = PowerupType.Teleporter;

        powerupName = keepVelocity
            ? "Transporter PowerUp!"
            : "Teleporter PowerUp!";

        movement = Marble.instance.GetComponent<Movement>();
    }

    public void InitMeshMaterial()
    {
        if (Marble.instance.normalMesh != null)
        {
            marbleRenderer =
                Marble.instance.normalMesh.GetComponent<MeshRenderer>();

            if (marbleRenderer != null)
            {
                marbleMaterial = marbleRenderer.material;
                originalColor = marbleMaterial.color;
            }
        }
    }

    public override void PickupItem()
    {
        activeTeleporter = this;

        base.PickupItem();
    }

    protected override void UsePowerup()
    {
        UseTeleporter();
    }

    private void Update()
    {
        if (waitingForRelease &&
            !Input.GetKey(ControlBinding.instance.usePowerup))
        {
            waitingForRelease = false;
        }
    }

    public void UseTeleporter()
    {
        if (teleporting || waitingForRelease)
            return;

        if (!locationSet)
        {
            SetLocation();
        }
        else
        {
            ActivateTeleport();
        }

        waitingForRelease = true;
    }

    private void SetLocation()
    {
        if (Marble.instance == null || movement == null)
            return;

        savedPosition = Marble.instance.transform.position;
        savedRotation = Marble.instance.transform.rotation;

        savedVelocity = movement.marbleVelocity;
        savedAngularVelocity = movement.marbleAngularVelocity;

        savedGravityDir = GravitySystem.GravityDir;
        savedGravityStrength = GravitySystem.GravityStrength;

        locationSet = true;

        CreateTeleportMarker();
    }

    private void ActivateTeleport()
    {
        if (teleportCoroutine != null)
            StopCoroutine(teleportCoroutine);

        InitMeshMaterial();

        GameManager.instance.ConsumePowerup();

        teleportCoroutine = StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        teleporting = true;

        SetTransparent();

        if (Marble.instance.teleportSound != null)
        {
            Marble.instance.teleportSound.volume =
                PlayerPrefs.GetFloat(
                    "Audio_SoundVolume",
                    0.5f
                );

            Marble.instance.teleportSound.Play();
        }

        float teleportTime = teleportDelay;
        float initTime = teleportDelay;

        while (teleportTime > 0f)
        {
            teleportTime -= Time.deltaTime;

            float alpha =
                initTime > 0f
                    ? Mathf.Clamp01(teleportTime / initTime)
                    : 0f;

            if (marbleMaterial != null)
            {
                Color color = originalColor;
                color.a = alpha;

                marbleMaterial.color = color;
            }

            yield return null;
        }

        RemoveTeleportMarker();

        FinishTeleport();
    }

    private void FinishTeleport()
    {
        if (Marble.instance == null || movement == null)
            return;

        if (Marble.instance.teleportSound != null)
            Marble.instance.teleportSound.Stop();

        if (!ReplayRecorder.loadReplay)
            ReplayRecorder.Instance?.RecordTeleportFinished();

        movement.SetPosition(savedPosition);

        Marble.instance.transform.rotation = savedRotation;

        GravitySystem.GravityDir = savedGravityDir;
        GravitySystem.GravityStrength = savedGravityStrength;

        if (keepVelocity)
        {
            movement.marbleVelocity = savedVelocity;
            movement.marbleAngularVelocity = savedAngularVelocity;
        }
        else
        {
            movement.marbleVelocity = Vector3.zero;
            movement.marbleAngularVelocity = Vector3.zero;
        }

        SetOpaque();

        teleporting = false;
        locationSet = false;
        teleportCoroutine = null;

        if (activeTeleporter == this)
            activeTeleporter = null;
    }

    private void CreateTeleportMarker()
    {
        RemoveTeleportMarker();

        GameObject markerPrefab = keepVelocity
            ? teleportMarkerYellowPrefab
            : teleportMarkerPrefab;

        if (markerPrefab == null)
            return;

        teleportMarker = Instantiate(
            markerPrefab,
            savedPosition,
            Quaternion.identity
        );
    }

    private void RemoveTeleportMarker()
    {
        if (teleportMarker != null)
        {
            Destroy(teleportMarker);
            teleportMarker = null;
        }
    }

    private void SetTransparent()
    {
        if (marbleMaterial == null)
            return;

        originallyTransparent = IsSurfaceTransparent();

        if (!originallyTransparent)
        {
            SetSurfaceType(true);
        }

        Color color = originalColor;
        color.a = 0f;

        marbleMaterial.color = color;
    }

    private void SetOpaque()
    {
        if (marbleMaterial == null)
            return;

        marbleMaterial.color = originalColor;

        if (!originallyTransparent)
        {
            SetSurfaceType(false);
        }
    }

    private bool IsSurfaceTransparent()
    {
        if (marbleMaterial == null)
            return false;

        if (!marbleMaterial.HasProperty("_Surface"))
            return false;

        return marbleMaterial.GetFloat("_Surface") >= 0.5f;
    }

    private void SetSurfaceType(bool transparent)
    {
        if (marbleMaterial == null)
            return;

        if (transparent)
        {
            marbleMaterial.SetFloat("_Surface", 1f);

            marbleMaterial.SetFloat(
                "_Blend",
                (float)UnityEngine.Rendering.BlendMode.SrcAlpha
            );

            marbleMaterial.SetFloat(
                "_SrcBlend",
                (float)UnityEngine.Rendering.BlendMode.SrcAlpha
            );

            marbleMaterial.SetFloat(
                "_DstBlend",
                (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            );

            marbleMaterial.SetFloat("_ZWrite", 0f);

            marbleMaterial.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            marbleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            marbleMaterial.renderQueue =
                (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            marbleMaterial.SetFloat("_Surface", 0f);

            marbleMaterial.SetFloat(
                "_Blend",
                (float)UnityEngine.Rendering.BlendMode.One
            );

            marbleMaterial.SetFloat(
                "_SrcBlend",
                (float)UnityEngine.Rendering.BlendMode.One
            );

            marbleMaterial.SetFloat(
                "_DstBlend",
                (float)UnityEngine.Rendering.BlendMode.Zero
            );

            marbleMaterial.SetFloat("_ZWrite", 1f);

            marbleMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            marbleMaterial.EnableKeyword("_SURFACE_TYPE_OPAQUE");

            marbleMaterial.renderQueue =
                (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }

        // Make sure the material gets re-evaluated.
        marbleMaterial.globalIlluminationFlags =
            MaterialGlobalIlluminationFlags.None;
    }

    public void ResetTeleporter()
    {
        if (teleportCoroutine != null)
        {
            StopCoroutine(teleportCoroutine);
            teleportCoroutine = null;
        }

        RemoveTeleportMarker();

        locationSet = false;
        teleporting = false;
        waitingForRelease = false;

        if (activeTeleporter == this)
            activeTeleporter = null;

        SetOpaque();
    }

    private void OnDestroy()
    {
        if (teleportCoroutine != null)
            StopCoroutine(teleportCoroutine);

        RemoveTeleportMarker();

        if (activeTeleporter == this)
            activeTeleporter = null;

        if (Marble.instance != null)
            SetOpaque();
    }
}