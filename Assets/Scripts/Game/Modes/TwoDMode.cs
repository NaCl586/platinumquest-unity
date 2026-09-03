using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Marble Blast 2D game mode.
///
/// The Haxe plane definitions are converted to the equivalent
/// Unity plane orientation:
///
/// Haxe XZ -> Unity YZ
/// Haxe YZ -> Unity XZ
///
/// The camera remains fixed in orientation while following
/// the marble normally.
/// </summary>
public class TwoDMode : NullMode
{
    private bool hasMissionPlane;

    private float missionYaw;
    private float missionCameraDistance = float.NaN;

    private bool missionChangesPitch;
    private float missionTargetPitch;

    // ============================================================
    // Current runtime 2D state
    // ============================================================

    private float currentYaw;
    private float currentCameraDistance = float.NaN;
    private float currentPitch;
    private bool currentChangesPitch;

    // ============================================================
    // 2D state saved at the active checkpoint
    // ============================================================

    private bool checkpointHas2DState;
    private float checkpointYaw;
    private float checkpointCameraDistance = float.NaN;
    private float checkpointPitch;
    private bool checkpointChangesPitch;

    // ============================================================
    // Runtime state
    // ============================================================

    private bool active;

    private float targetYaw;
    private float targetPitch;

    private bool changesPitch;

    private Coroutine pendingMissionActivation;

    public bool Active => active;

    /// <summary>
    /// False = left was most recently pressed.
    /// True  = right was most recently pressed.
    /// </summary>
    public bool LastPressedLR { get; private set; }

    public TwoDMode(GameManager gameManager)
        : base(gameManager)
    {
    }

    // ============================================================
    // Plane conversion
    // ============================================================

    public static float PlaneToYaw(
        string plane,
        bool invert)
    {
        if (string.IsNullOrWhiteSpace(plane))
            plane = "xz";

        float yaw;

        switch (plane.Trim().ToLowerInvariant())
        {
            // Haxe XZ -> Unity YZ
            case "xz":
                yaw = Mathf.PI * 0.5f;
                break;

            // Haxe YZ -> Unity XZ
            case "yz":
                yaw = 0f;
                break;

            default:
                if (!float.TryParse(
                    plane,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float degrees))
                {
                    Debug.LogWarning(
                        $"TwoDMode: Could not parse plane '{plane}'. " +
                        "Defaulting to XZ."
                    );

                    yaw = 0f;
                }
                else
                {
                    yaw =
                        degrees *
                        Mathf.Deg2Rad;
                }

                break;
        }

        if (invert)
            yaw += Mathf.PI;

        return yaw;
    }

    // ============================================================
    // Mission loading
    // ============================================================

    public override void OnMissionLoad()
    {
        base.OnMissionLoad();

        active = false;
        targetYaw = 0f;
        targetPitch = 0f;
        currentYaw = 0f;
        currentCameraDistance = float.NaN;
        currentPitch = 0f;
        currentChangesPitch = false;

        if (MissionInfo.instance == null)
        {
            hasMissionPlane = false;
            return;
        }

        string plane = MissionInfo.instance.cameraPlane;

        if (string.IsNullOrWhiteSpace(plane))
        {
            hasMissionPlane = false;
            return;
        }

        hasMissionPlane = true;

        missionYaw = PlaneToYaw(
            plane,
            MissionInfo.instance.invertCameraPlane
        );

        missionCameraDistance =
            MissionInfo.instance.hasInitialCameraDistance
                ? MissionInfo.instance.initialCameraDistance
                : float.NaN;

        if (MissionInfo.instance.hasCameraPitch)
        {
            missionChangesPitch = true;
            missionTargetPitch =
                MissionInfo.instance.cameraPitch * Mathf.Deg2Rad;
        }
        else
        {
            missionChangesPitch = false;
            missionTargetPitch = 0f;
        }

        checkpointHas2DState = false;
    }

    private IEnumerator ActivateMissionWhenCameraReady()
    {
        while (CameraController.instance == null)
            yield return null;

        while (Marble.instance == null)
            yield return null;

        if (!hasMissionPlane)
        {
            pendingMissionActivation = null;
            yield break;
        }

        CameraController camera =
            CameraController.instance;

        float pitch;

        if (missionChangesPitch)
        {
            pitch = missionTargetPitch;
        }
        else
        {
            pitch = camera.CameraPitch;
        }

        Activate(
            missionYaw,
            missionCameraDistance,
            missionChangesPitch,
            pitch
        );

        pendingMissionActivation = null;
    }

    // ============================================================
    // Checkpoint camera state
    // ============================================================

    /// <summary>
    /// Saves the current 2D camera state as the state belonging
    /// to the currently reached checkpoint.
    ///
    /// This is intentionally part of TwoDMode so other gameplay
    /// systems do not need to inspect or compare game modes.
    /// </summary>
    private void SaveCheckpointState()
    {
        if (!active)
        {
            checkpointHas2DState = false;
            return;
        }

        checkpointHas2DState = true;

        checkpointYaw =
            currentYaw;

        checkpointCameraDistance =
            currentCameraDistance;

        checkpointPitch =
            currentPitch;

        checkpointChangesPitch =
            currentChangesPitch;
    }

    /// <summary>
    /// Clears checkpoint-specific 2D state.
    ///
    /// Used when starting a completely new mission.
    /// </summary>
    public void ClearCheckpointState()
    {
        checkpointHas2DState = false;

        checkpointYaw =
            missionYaw;

        checkpointCameraDistance =
            missionCameraDistance;

        checkpointPitch =
            missionTargetPitch;

        checkpointChangesPitch =
            missionChangesPitch;
    }

    // ============================================================
    // Respawn
    // ============================================================

    public override void OnRestart()
    {
        base.OnRestart();

        checkpointHas2DState = false;

        checkpointYaw =
            missionYaw;

        checkpointCameraDistance =
            missionCameraDistance;

        checkpointPitch =
            missionTargetPitch;

        checkpointChangesPitch =
            missionChangesPitch;
    }

    public override void OnRespawn()
    {
        if (!hasMissionPlane)
            return;

        if (checkpointHas2DState)
        {
            Activate(
                checkpointYaw,
                checkpointCameraDistance,
                checkpointChangesPitch,
                checkpointPitch
            );
        }
        else
        {
            Activate(
                missionYaw,
                missionCameraDistance,
                missionChangesPitch,
                missionTargetPitch
            );
        }
    }

    // ============================================================
    // Activation
    // ============================================================

    public void Activate(
        float yaw,
        float cameraDistance,
        bool changesPitch,
        float pitch)
    {
        active = true;

        targetYaw =
            yaw;

        this.changesPitch =
            changesPitch;

        // Save the current runtime 2D side.
        currentYaw =
            yaw;

        currentChangesPitch =
            changesPitch;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.TwoDModeLocked =
            true;

        // ---------------------------------------------------------
        // Pitch
        // ---------------------------------------------------------

        if (changesPitch)
        {
            targetPitch =
                pitch;
        }
        else
        {
            // Preserve the current camera pitch.
            targetPitch =
                camera.CameraPitch;
        }

        currentPitch =
            targetPitch;

        // ---------------------------------------------------------
        // Camera distance
        // ---------------------------------------------------------

        if (!float.IsNaN(cameraDistance))
        {
            currentCameraDistance =
                cameraDistance;
        }
        else if (float.IsNaN(currentCameraDistance))
        {
            currentCameraDistance =
                camera.CameraDistance;
        }

        // ---------------------------------------------------------
        // Apply camera orientation
        // ---------------------------------------------------------

        float cameraYaw =
            yaw +
            Mathf.PI * 0.5f;

        camera.SetCameraAngles(
            cameraYaw,
            targetPitch,
            true,
            true
        );

        // ---------------------------------------------------------
        // Apply camera distance
        // ---------------------------------------------------------

        if (!float.IsNaN(currentCameraDistance))
        {
            camera.CameraDistance =
                currentCameraDistance;
        }

        // ---------------------------------------------------------
        // 2D FOV
        //
        // Default = PlayerPrefs.GetFloat("Graphics_FieldOfView", 70f);
        // If camerafov exists in the MCS, use that value.
        // ---------------------------------------------------------

        camera.SetFovX(
            GetBaseFov()
        );
    }

    // ============================================================
    // Deactivation
    // ============================================================

    public void Deactivate()
    {
        if (!active)
            return;

        active = false;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.TwoDModeLocked =
            false;

        camera.SetFovX(
            GetBaseFov()
        );
    }

    // ============================================================
    // Base FOV
    // ============================================================

    private float GetBaseFov()
    {
        if (MissionInfo.instance != null &&
            MissionInfo.instance.hasCameraFov)
        {
            return MissionInfo.instance.cameraFov;
        }

        return PlayerPrefs.GetFloat("Graphics_FieldOfView", 70f);
    }

    // ============================================================
    // Update
    // ============================================================

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (!active)
            return;

        UpdateLastPressedLR();

        // Let OOB / finish camera take control.
        if (GameManager.gameFinish)
            return;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.SetCameraAngles(
            targetYaw +
                Mathf.PI * 0.5f,
            targetPitch,
            true,
            true
        );
    }

    // ============================================================
    // Left / Right tracking
    // ============================================================

    private void UpdateLastPressedLR()
    {
        if (ControlBinding.instance == null)
            return;

        if (Input.GetKey(
            ControlBinding.instance.moveLeft))
        {
            LastPressedLR = false;
        }

        if (Input.GetKey(
            ControlBinding.instance.moveRight))
        {
            LastPressedLR = true;
        }
    }

    // ============================================================
    // Movement filtering
    // ============================================================

    public override Vector2 FilterMovementInput(
        Vector2 input)
    {
        if (!active)
            return input;

        // Unity:
        // X = left/right
        // Y = forward/backward
        //
        // 2D mode removes forward/backward movement.
        input.y = 0f;

        return input;
    }

    // ============================================================
    // Cannon restoration
    // ============================================================

    public void RestoreAfterCannon()
    {
        if (!active)
            return;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.TwoDModeLocked =
            true;

        camera.SetCameraAngles(
            targetYaw +
                Mathf.PI * 0.5f,
            targetPitch,
            true,
            true
        );

        camera.SetFovX(
            GetBaseFov()
        );
    }

    public override void OnCheckpointReached()
    {
        SaveCheckpointState();
    }

    public override void OnCameraReady()
    {
        if (!hasMissionPlane)
            return;

        CameraController camera = CameraController.instance;

        if (camera == null)
            return;

        float pitch = missionChangesPitch
            ? missionTargetPitch
            : camera.CameraPitch;

        Activate(
            missionYaw,
            missionCameraDistance,
            missionChangesPitch,
            pitch
        );
    }

    public override Vector3 GetSuperSpeedDirection(Vector3 defaultDirection)
    {
        Movement movement = Movement.instance;

        if (movement == null)
            return defaultDirection;

        movement.GetMarbleAxis(
            out Vector3 sideDir,
            out Vector3 motionDir,
            out Vector3 upDir
        );

        // Haxe:
        //
        // movementVector = marbleAxis[1]
        //                 * (lastPressedLR ? 1 : -1);
        //
        // In our Unity implementation marbleAxis[1]
        // corresponds to sideDir.

        Vector3 direction =
            sideDir * (LastPressedLR ? 1f : -1f);

        return direction;
    }
}