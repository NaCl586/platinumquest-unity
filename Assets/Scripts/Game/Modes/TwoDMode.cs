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

    private float currentYaw;
    private float currentCameraDistance = float.NaN;
    private float currentPitch;
    private bool currentChangesPitch;

    private bool cameraDistanceOverride;
    private float overriddenCameraDistance = float.NaN;

    private bool checkpointHas2DState;
    private float checkpointYaw;
    private float checkpointCameraDistance = float.NaN;
    private float checkpointPitch;
    private bool checkpointChangesPitch;

    private bool active;

    private float targetYaw;
    private float targetPitch;

    private bool changesPitch;

    private Coroutine pendingMissionActivation;

    public bool Active => active;

    public bool CameraDistanceOverrideActive =>
        cameraDistanceOverride;

    public bool LastPressedLR { get; private set; }

    public TwoDMode(GameManager gameManager)
        : base(gameManager)
    {
    }

    public static float PlaneToYaw(
        string plane,
        bool invert)
    {
        if (string.IsNullOrWhiteSpace(plane))
            plane = "xz";

        float yaw;

        switch (plane.Trim().ToLowerInvariant())
        {
            case "xz":
                yaw = Mathf.PI * 0.5f;
                break;

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
                    yaw = degrees * Mathf.Deg2Rad;
                }

                break;
        }

        if (invert)
            yaw += Mathf.PI;

        return yaw;
    }

    public override void OnMissionLoad()
    {
        base.OnMissionLoad();

        if (pendingMissionActivation != null &&
            GameManager.instance != null)
        {
            GameManager.instance.StopCoroutine(
                pendingMissionActivation
            );

            pendingMissionActivation = null;
        }

        active = false;

        targetYaw = 0f;
        targetPitch = 0f;

        currentYaw = 0f;
        currentCameraDistance = float.NaN;
        currentPitch = 0f;
        currentChangesPitch = false;

        hasMissionPlane = false;

        missionYaw = 0f;
        missionCameraDistance = float.NaN;

        missionChangesPitch = false;
        missionTargetPitch = 0f;

        checkpointHas2DState = false;

        cameraDistanceOverride = false;
        overriddenCameraDistance = float.NaN;

        if (MissionInfo.instance == null)
            return;

        string plane =
            MissionInfo.instance.cameraPlane;

        if (string.IsNullOrWhiteSpace(plane))
            return;

        hasMissionPlane = true;

        missionYaw =
            PlaneToYaw(
                plane,
                MissionInfo.instance.invertCameraPlane
            );

        if (MissionInfo.instance.hasInitialCameraDistance)
        {
            missionCameraDistance =
                MissionInfo.instance.initialCameraDistance;
        }
        else
        {
            missionCameraDistance =
                float.NaN;
        }

        if (MissionInfo.instance.hasCameraPitch)
        {
            missionChangesPitch = true;

            missionTargetPitch =
                MissionInfo.instance.cameraPitch *
                Mathf.Deg2Rad;
        }
        else
        {
            missionChangesPitch = false;
            missionTargetPitch = 0f;
        }

        if (GameManager.instance != null)
        {
            pendingMissionActivation =
                GameManager.instance.StartCoroutine(
                    ActivateMissionWhenCameraReady()
                );
        }
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

        if (GameManager.instance != null)
        {
            while (
                GameManager.instance.activeCheckpoint == null)
            {
                yield return null;
            }
        }

        yield return null;

        if (!active)
        {
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
        }

        pendingMissionActivation = null;
    }

    private void SaveCheckpointState()
    {
        if (!active)
        {
            checkpointHas2DState = false;
            return;
        }

        checkpointHas2DState = true;

        checkpointYaw = currentYaw;
        checkpointCameraDistance = currentCameraDistance;
        checkpointPitch = currentPitch;
        checkpointChangesPitch = currentChangesPitch;
    }

    public void ClearCheckpointState()
    {
        checkpointHas2DState = false;

        checkpointYaw = missionYaw;
        checkpointCameraDistance = missionCameraDistance;
        checkpointPitch = missionTargetPitch;
        checkpointChangesPitch = missionChangesPitch;
    }

    public override void OnRestart()
    {
        base.OnRestart();

        RestoreMissionPlane();

        checkpointHas2DState = false;

        checkpointYaw = missionYaw;
        checkpointCameraDistance = missionCameraDistance;
        checkpointPitch = missionTargetPitch;
        checkpointChangesPitch = missionChangesPitch;

        currentYaw = missionYaw;
        currentCameraDistance = missionCameraDistance;
        currentPitch = missionTargetPitch;
        currentChangesPitch = missionChangesPitch;

        targetYaw = missionYaw;
        targetPitch = missionTargetPitch;
        changesPitch = missionChangesPitch;

        cameraDistanceOverride = false;
        overriddenCameraDistance = float.NaN;
    }

    public override void OnCheckpointReached()
    {
        if (!active)
            return;

        checkpointHas2DState = true;
        checkpointYaw = currentYaw;
        checkpointCameraDistance = currentCameraDistance;
        checkpointPitch = currentPitch;
        checkpointChangesPitch = currentChangesPitch;
    }

    public void SetCameraDistanceOverride(float distance)
    {
        if (float.IsNaN(distance) ||
            float.IsInfinity(distance))
        {
            return;
        }

        cameraDistanceOverride = true;
        overriddenCameraDistance =
            Mathf.Max(0.001f, distance);
    }

    public void ClearCameraDistanceOverride()
    {
        cameraDistanceOverride = false;
        overriddenCameraDistance = float.NaN;
    }

    private float GetEffectiveCameraDistance()
    {
        if (cameraDistanceOverride &&
            !float.IsNaN(overriddenCameraDistance))
        {
            return overriddenCameraDistance;
        }

        return currentCameraDistance;
    }

    public void Activate(
        float yaw,
        float cameraDistance,
        bool changesPitch,
        float pitch)
    {
        active = true;

        targetYaw = yaw;
        this.changesPitch = changesPitch;

        cameraDistanceOverride = false;
        overriddenCameraDistance = float.NaN;

        currentYaw = yaw;
        currentChangesPitch = changesPitch;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.TwoDModeLocked = true;

        if (changesPitch)
        {
            targetPitch = pitch;
        }
        else
        {
            targetPitch = camera.CameraPitch;
        }

        currentPitch = targetPitch;

        if (!float.IsNaN(cameraDistance))
        {
            currentCameraDistance = cameraDistance;
        }
        else if (float.IsNaN(currentCameraDistance))
        {
            currentCameraDistance = camera.CameraDistance;
        }

        float cameraYaw =
            yaw + Mathf.PI * 0.5f;

        camera.SetCameraAngles(
            cameraYaw,
            targetPitch,
            true,
            true
        );

        if (!float.IsNaN(currentCameraDistance))
        {
            camera.CameraDistance =
                currentCameraDistance;
        }

        camera.SetFovX(
            GetBaseFov()
        );
    }

    public void Deactivate()
    {
        if (!active)
            return;

        active = false;

        cameraDistanceOverride = false;
        overriddenCameraDistance = float.NaN;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.TwoDModeLocked = false;

        camera.SetFovX(
            GetBaseFov()
        );
    }

    private float GetBaseFov()
    {
        float savedFov =
            PlayerPrefs.GetFloat(
                "Graphics_FieldOfView",
                70f
            );

        if (MissionInfo.instance != null &&
            MissionInfo.instance.hasCameraFov)
        {
            return
                MissionInfo.instance.cameraFov *
                savedFov /
                70f;
        }

        return savedFov;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (!active)
            return;

        UpdateLastPressedLR();

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

        float effectiveDistance =
            GetEffectiveCameraDistance();

        if (!float.IsNaN(effectiveDistance))
        {
            camera.CameraDistance =
                effectiveDistance;
        }
    }

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

    public override Vector2 FilterMovementInput(
        Vector2 input)
    {
        if (!active)
            return input;

        input.y = 0f;

        return input;
    }

    public void RestoreAfterCannon()
    {
        if (!active)
            return;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.TwoDModeLocked = true;

        camera.SetCameraAngles(
            targetYaw +
                Mathf.PI * 0.5f,
            targetPitch,
            true,
            true
        );

        float effectiveDistance =
            GetEffectiveCameraDistance();

        if (!float.IsNaN(effectiveDistance))
        {
            camera.CameraDistance =
                effectiveDistance;
        }

        camera.SetFovX(
            GetBaseFov()
        );
    }

    public override void OnCameraReady()
    {
        if (!hasMissionPlane)
            return;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

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
    }

    public override Vector3 GetSuperSpeedDirection(
        Vector3 defaultDirection)
    {
        Movement movement =
            Movement.instance;

        if (movement == null)
            return defaultDirection;

        movement.GetMarbleAxis(
            out Vector3 sideDir,
            out Vector3 motionDir,
            out Vector3 upDir
        );

        Vector3 direction =
            sideDir *
            (LastPressedLR ? 1f : -1f);

        return direction;
    }

    private void RestoreMissionPlane()
    {
        if (MissionInfo.instance == null)
            return;

        string plane =
            MissionInfo.instance.cameraPlane;

        if (string.IsNullOrWhiteSpace(plane))
            return;

        missionYaw =
            PlaneToYaw(
                plane,
                MissionInfo.instance.invertCameraPlane
            );

        targetYaw = missionYaw;
        currentYaw = missionYaw;

        CameraController camera =
            CameraController.instance;

        if (camera == null)
            return;

        camera.TwoDModeLocked = true;

        camera.SetCameraAngles(
            missionYaw +
                Mathf.PI * 0.5f,
            targetPitch,
            true,
            true
        );

        float effectiveDistance =
            GetEffectiveCameraDistance();

        if (!float.IsNaN(effectiveDistance))
            camera.CameraDistance =
                effectiveDistance;

        camera.SetFovX(GetBaseFov());
    }
}
