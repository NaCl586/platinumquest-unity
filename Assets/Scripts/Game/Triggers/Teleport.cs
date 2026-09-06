using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    [Header("Destination")]
    public string destinationGameObjectName;
    public GameObject destination;

    [Header("Teleport")]
    public float time = 2f;

    [Header("Gem Requirements")]
    public int gemsToActivate = 0;
    public int gemsToDeactivate = 100000000;
    public bool displayGemsMessage = false;

    [Header("Teleport Options")]
    public bool centerDestinationPoint = false;
    public bool keepVelocity = false;
    public bool inverseVelocity = false;
    public bool keepAngular = false;
    public bool keepCamera = false;
    public float cameraYaw = 0f;

    [HideInInspector]
    public static bool teleporting;

    private GameObject player;
    private float teleportTime;
    private float initTime;

    private Color originalColor;
    private MeshRenderer marbleRenderer;
    private Material marbleMaterial;

    private bool originallyTransparent;

    private Coroutine teleportFadeCoroutine;

    public void InitTeleporter()
    {
        teleporting = false;

        if (Marble.instance != null)
            player = Marble.instance.gameObject;

        originalColor = Color.white;
        marbleRenderer = null;
        marbleMaterial = null;
        originallyTransparent = false;
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

    private void OnTriggerEnter(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (!CanTeleport())
            return;

        if (destination == null)
        {
            GameUIManager.instance.SetBottomText(
                "There's no destination specified! Please check the .mis file."
            );

            return;
        }

        if (teleporting)
            return;

        teleporting = true;

        teleportTime = time;
        initTime = time;

        if (marble == Marble.instance)
        {
            PlayTeleportSound();

            if (time >= 2f)
            {
                GameUIManager.instance.SetBottomText(
                    "Teleporter has been activated, please wait.",
                    time
                );
            }
            else
            {
                GameUIManager.instance.SetBottomText(
                    "Teleporter has been activated.",
                    time
                );
            }

            SetTransparent();

            if (teleportFadeCoroutine != null)
                StopCoroutine(teleportFadeCoroutine);

            teleportFadeCoroutine = StartCoroutine(TeleportFade());
        }

        CancelInvoke(nameof(TeleportMarble));
        Invoke(nameof(TeleportMarble), time);
    }

    private void OnTriggerExit(Collider other)
    {
        Marble marble = other.GetComponent<Marble>();

        if (marble == null)
            return;

        if (!CanTeleport())
            return;

        if (marble != Marble.instance)
            return;

        teleporting = false;

        // Clear Teleport Trigger timer.
        GameUIManager.instance?.SetTeleportTriggerTimer(0f, time);

        CancelInvoke(nameof(TeleportMarble));

        SetOpaque();

        if (teleportFadeCoroutine != null)
        {
            StopCoroutine(teleportFadeCoroutine);
            teleportFadeCoroutine = null;
        }

        if (Marble.instance.teleportSound != null)
            Marble.instance.teleportSound.DOFade(0f, 0.5f);

        GameUIManager.instance.TeleportFadeOutBottomText();
    }

    private bool CanTeleport()
    {
        if (GameManager.instance == null)
            return false;

        int gemCount = GameManager.instance.currentGems;

        if (gemCount < gemsToActivate)
        {
            if (displayGemsMessage)
            {
                GameUIManager.instance.SetBottomText(
                    "You need "
                        + gemsToActivate
                        + " gem"
                        + (gemsToActivate != 1 ? "s" : "")
                        + " to activate this Teleporter.",
                    2f
                );
            }

            return false;
        }

        if (gemCount >= gemsToDeactivate)
        {
            if (displayGemsMessage)
            {
                GameUIManager.instance.SetBottomText(
                    "You need "
                        + gemsToDeactivate
                        + " gem"
                        + (gemsToDeactivate != 1 ? "s" : "")
                        + " to deactivate this Teleporter.",
                    2f
                );
            }

            return false;
        }

        return true;
    }

    private void TeleportMarble()
    {
        if (Marble.instance == null)
            return;

        DestinationTrigger destinationTrigger =
            destination != null
                ? destination.GetComponent<DestinationTrigger>()
                : null;

        if (destinationTrigger == null)
        {
            Debug.LogError(
                $"Teleport destination '{destination?.name}' does not have a DestinationTrigger component."
            );

            teleporting = false;
            SetOpaque();

            GameUIManager.instance?.SetTeleportTriggerTimer(-1f, time);

            return;
        }

        if (destinationTrigger.spawn == null)
        {
            Debug.LogError(
                $"Teleport destination '{destination.name}' does not have a Spawn transform."
            );

            teleporting = false;
            SetOpaque();

            GameUIManager.instance?.SetTeleportTriggerTimer(-1f, time);

            return;
        }

        Movement movement =
            Marble.instance.GetComponent<Movement>();

        if (movement == null)
        {
            teleporting = false;
            SetOpaque();

            GameUIManager.instance?.SetTeleportTriggerTimer(-1, time);

            return;
        }

        if (Marble.instance.teleportSound != null)
            Marble.instance.teleportSound.Stop();

        GameManager.instance.PlaySpawnAudio();

        if (!ReplayRecorder.loadReplay)
            ReplayRecorder.Instance?.RecordTeleportFinished();

        teleporting = false;

        Vector3 oldVelocity = movement.marbleVelocity;
        Vector3 oldAngularVelocity = movement.marbleAngularVelocity;

        Vector3 targetPosition;

        if (centerDestinationPoint ||
            destinationTrigger.centerDestinationPoint)
        {
            Collider destinationCollider =
                destination.GetComponent<Collider>();

            if (destinationCollider != null)
                targetPosition = destinationCollider.bounds.center;
            else
                targetPosition = destinationTrigger.spawn.position;
        }
        else
        {
            targetPosition = destinationTrigger.spawn.position;
        }

        movement.SetPosition(targetPosition);

        bool finalKeepVelocity =
            keepVelocity || destinationTrigger.keepVelocity;

        bool finalInverseVelocity =
            inverseVelocity || destinationTrigger.inverseVelocity;

        if (finalKeepVelocity)
            movement.marbleVelocity = oldVelocity;
        else
            movement.marbleVelocity = Vector3.zero;

        if (finalInverseVelocity)
            movement.marbleVelocity *= -1f;

        bool finalKeepAngular =
            keepAngular || destinationTrigger.keepAngular;

        if (finalKeepAngular)
            movement.marbleAngularVelocity = oldAngularVelocity;
        else
            movement.marbleAngularVelocity = Vector3.zero;

        bool finalKeepCamera =
            keepCamera || destinationTrigger.keepCamera;

        if (!finalKeepCamera)
            ApplyDestinationCamera(destinationTrigger);

        SetOpaque();

        // Clear Teleport Trigger timer after teleporting.
        GameUIManager.instance?.SetTeleportTriggerTimer(-1f, time);
    }

    private void ApplyDestinationCamera(
        DestinationTrigger destinationTrigger)
    {
        CameraController cameraController =
            Camera.main != null
                ? Camera.main.GetComponent<CameraController>()
                : null;

        if (cameraController == null)
            return;

        cameraController.SetCameraPosition(
            destinationTrigger.spawn.position,
            destinationTrigger.cameraPos.position
        );
    }

    private void PlayTeleportSound()
    {
        if (Marble.instance == null ||
            Marble.instance.teleportSound == null)
        {
            return;
        }

        Marble.instance.teleportSound.volume =
            PlayerPrefs.GetFloat(
                "Audio_SoundVolume",
                0.5f
            );

        Marble.instance.teleportSound.Play();
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

        Color color = originalColor;
        marbleMaterial.color = color;

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

    private IEnumerator TeleportFade()
    {
        InitMeshMaterial();

        while (teleporting)
        {
            teleportTime -= Time.deltaTime;

            float alpha =
                time > 0f
                    ? Mathf.Clamp01(teleportTime / time)
                    : 0f;

            if (marbleMaterial != null)
            {
                Color color = originalColor;
                color.a = alpha;

                marbleMaterial.color = color;
            }

            // Update Teleport Trigger timer.
            GameUIManager.instance?.SetTeleportTriggerTimer(
                Mathf.Max(0f, teleportTime),
                time
            );

            yield return null;
        }

        GameUIManager.instance?.SetTeleportTriggerTimer(-1f, time);

        teleportFadeCoroutine = null;
    }

    public void ResetTeleporter()
    {
        CancelInvoke(nameof(TeleportMarble));

        teleporting = false;

        StopAllCoroutines();

        SetOpaque();

        // Clear Teleport Trigger timer on reset.
        GameUIManager.instance?.SetTeleportTriggerTimer(-1f, time);

        if (Marble.instance != null &&
            Marble.instance.teleportSound != null)
        {
            Marble.instance.teleportSound.Stop();

            Marble.instance.teleportSound.volume =
                PlayerPrefs.GetFloat(
                    "Audio_SoundVolume",
                    0.5f
                );
        }

        GameUIManager.instance?.TeleportFadeOutBottomText();
    }
}