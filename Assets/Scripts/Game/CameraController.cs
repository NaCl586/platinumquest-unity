using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    // ============================================================
    // Singleton & Events
    // ============================================================

    public class OnCameraFinish : UnityEvent { }

    public static CameraController instance;
    public static OnCameraFinish onCameraFinish = new OnCameraFinish();

    // Component References
    private Camera cam;
    private Transform marble;

    // ============================================================
    // Normal Camera State
    // ============================================================

    private bool positionLocked = true;
    private float mouseX;
    private float mouseY;

    private Vector3 offset;
    private Vector3 lastGravityDir;

    // Explicit camera angles (stored in radians)
    private float cameraYaw;
    private float cameraPitch;

    // Properties
    public float CameraYaw { get => cameraYaw; set => cameraYaw = value; }
    public float CameraPitch { get => cameraPitch; set => cameraPitch = value; }
    public float nextCameraYaw { get => cameraYaw; set => cameraYaw = value; }
    public float nextCameraPitch { get => cameraPitch; set => cameraPitch = value; }

    // ============================================================
    // Cannon Camera State
    // ============================================================

    private bool cameraInputLocked;
    private bool cannonCameraActive;

    private Vector3 preCannonOffset;
    private Vector3 preCannonPosition;
    private Quaternion preCannonRotation;
    private float preCannonFov;
    private Vector2 preCannonLensShift;
    private float preCannonPitch;
    private bool preCannonStateValid;

    public bool IsCannonCameraActive => cannonCameraActive;
    public bool CameraInputLocked => cameraInputLocked;

    private float GetCameraSpeedMultiplier()
    {
        if (Marble.instance == null)
            return 1f;

        return Mathf.Max(0f, Marble.instance.CameraSpeedMultiplier);
    }

    // ============================================================
    // 2D Camera State
    // ============================================================

    public bool TwoDModeLocked { get; set; }
    public bool TwoDOutOfBounds { get; private set; }

    // ============================================================
    // Unity Lifecycle
    // ============================================================

    private bool cameraDistanceOverride;
    private float overriddenCameraDistance;

    public bool CameraDistanceOverrideActive => cameraDistanceOverride;

    public void SetCameraDistanceOverride(float distance)
    {
        if (float.IsNaN(distance) || float.IsInfinity(distance))
            return;

        overriddenCameraDistance = Mathf.Max(0.001f, distance);
        cameraDistanceOverride = true;

        ApplyCameraDistanceOverride();
    }

    public void ClearCameraDistanceOverride()
    {
        cameraDistanceOverride = false;
    }

    private void ApplyCameraDistanceOverride()
    {
        if (!cameraDistanceOverride)
            return;

        if (offset.sqrMagnitude < 0.0001f)
            return;

        offset = offset.normalized * overriddenCameraDistance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        TryGetComponent(out cam);
    }

    private void Start()
    {
        SetFieldOfView((int)DefaultFovX);

        lastGravityDir = GravitySystem.GravityDir;

        onCameraFinish.AddListener(FinishCameraPan);
        GravityModifier.onGravityChanged.AddListener(OnGravityChanged);

        StartCoroutine(AssignReferences());
    }

    private IEnumerator AssignReferences()
    {
        while (Marble.instance == null)
            yield return null;

        marble = Marble.instance.transform;

        if (GameManager.instance == null || GameManager.instance.activeCheckpoint == null)
            yield break;

        try
        {
            transform.position = GameManager.instance.startPad.transform.Find("CameraPos").position;
        }
        catch
        {
            yield break;
        }

        foreach (IGameMode mode in GameManager.instance.GameModes)
            mode.OnCameraReady();
    }

    private void LateUpdate()
    {
        if (marble == null)
            return;

        bool marbleInCannon = Marble.instance != null &&
            (Marble.instance.IsInCannon || Marble.instance.CannonCameraLocked());

        if (marbleInCannon && Marble.instance.ActiveCannon != null)
        {
            SetCannonCamera(Marble.instance.ActiveCannon, false);
            return;
        }
        else if (cannonCameraActive && !marbleInCannon)
        {
            ExitCannonCamera(false);
        }

        // Keep 2D camera locked during gameplay, allow finish camera to take over.
        if (TwoDModeLocked && !GameManager.gameFinish)
        {
            if (TwoDOutOfBounds)
                UpdateTwoDOutOfBoundsCamera();
            else
                UpdateCameraPosition();
        }
        else
        {
            HandleLook();
        }

        // IMPORTANT:
        // Apply the distance override AFTER all normal camera processing.
        // This prevents camera movement/pan/reset logic from changing
        // the distance while the CameraDistanceTrigger is active.
        ApplyCameraDistanceOverride();
    }

    // ============================================================
    // 2D Controls
    // ============================================================

    public void EnterTwoDOutOfBounds() => TwoDOutOfBounds = true;

    public void ExitTwoDOutOfBounds()
    {
        TwoDOutOfBounds = false;

        if (marble != null && TwoDModeLocked && !GameManager.gameFinish)
        {
            UpdateCameraPosition();
        }
    }

    private void UpdateTwoDOutOfBoundsCamera()
    {
        if (marble == null) return;
        transform.LookAt(marble.position, -lastGravityDir.normalized);
    }

    // ============================================================
    // Gravity & Angles Synchronization
    // ============================================================

    public void OnGravityChanged(Vector3 oldDir, Vector3 newDir)
    {
        if (cannonCameraActive)
        {
            lastGravityDir = newDir;
            return;
        }

        Quaternion delta = Quaternion.FromToRotation(lastGravityDir, newDir);
        offset = delta * offset;
        lastGravityDir = newDir;

        UpdateAnglesFromOffset();
    }

    public void UpdateAnglesFromOffset()
    {
        if (marble == null || offset.sqrMagnitude < 0.0001f) return;

        Vector3 up = -lastGravityDir.normalized;
        Vector3 cameraDirection = -offset.normalized;
        Vector3 flatDirection = Vector3.ProjectOnPlane(cameraDirection, up);

        if (flatDirection.sqrMagnitude < 0.0001f) return;

        flatDirection.Normalize();
        float pitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(cameraDirection, up), -1f, 1f));

        Vector3 referenceForward = Vector3.ProjectOnPlane(Vector3.forward, up);
        if (referenceForward.sqrMagnitude < 0.0001f)
        {
            referenceForward = Vector3.ProjectOnPlane(Vector3.right, up);
        }
        referenceForward.Normalize();

        float yaw = Vector3.SignedAngle(referenceForward, flatDirection, up);

        cameraYaw = yaw * Mathf.Deg2Rad;
        cameraPitch = pitch;
    }

    private void UpdateOffsetFromAngles()
    {
        if (marble == null) return;

        Vector3 up = -lastGravityDir.normalized;
        float distance = offset.magnitude < 0.001f ? 5f : offset.magnitude;

        Vector3 referenceForward = Vector3.ProjectOnPlane(Vector3.forward, up);
        if (referenceForward.sqrMagnitude < 0.0001f)
        {
            referenceForward = Vector3.ProjectOnPlane(Vector3.right, up);
        }
        referenceForward.Normalize();

        Vector3 horizontal = (Quaternion.AngleAxis(cameraYaw * Mathf.Rad2Deg, up) * referenceForward).normalized;
        Vector3 right = Vector3.Cross(up, horizontal).normalized;
        Vector3 cameraDirection = (Quaternion.AngleAxis(cameraPitch * Mathf.Rad2Deg, right) * horizontal).normalized;

        offset = -cameraDirection * distance;
    }

    // ============================================================
    // Camera Motion & Collision
    // ============================================================

    private void UpdateCameraPosition()
    {
        if (marble == null) return;

        Vector3 diff = -offset;
        Vector3 marblePos = marble.position;
        Vector3 targetPos = marblePos + diff;

        float epsilon = 0.001f;
        int iter = 0;

        if (!MissionInfo.instance.gameModes.Contains(Mode.TwoD))
        {
            while (Physics.Raycast(marblePos, targetPos - marblePos, out RaycastHit hitInfo, Vector3.Distance(targetPos, marblePos)))
            {
                if (!hitInfo.collider.isTrigger)
                {
                    targetPos += Vector3.Project(hitInfo.point - targetPos, hitInfo.normal);
                    targetPos += hitInfo.normal * epsilon;
                    diff = targetPos - marblePos;
                }

                if (++iter > 100) break;
            }

            Vector3[] directions =
            {
            Vector3.down, Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back,
            new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(1, -1, 1), new Vector3(-1, -1, 1),
            new Vector3(1, 1, -1), new Vector3(-1, 1, -1), new Vector3(1, -1, -1), new Vector3(-1, -1, -1)
            };

            float castDistance = 0.05f;

            for (int i = 0; i < 5; i++)
            {
                bool hitSomething = false;

                foreach (Vector3 dir in directions)
                {
                    if (Physics.Raycast(marblePos + diff, dir, out var hitInfo, castDistance - epsilon))
                    {
                        if (!hitInfo.collider.isTrigger)
                        {
                            hitSomething = true;
                            Vector3 newPos = hitInfo.point + hitInfo.normal * castDistance;
                            diff = newPos - marblePos;
                        }
                    }
                }

                if (!hitSomething) break;
            }
        }
        
        if (positionLocked || GameManager.gameFinish || TwoDModeLocked)
        {
            transform.position = marble.position + diff;
        }

        transform.LookAt(marble.position, -lastGravityDir.normalized);
    }

    private void HandleLook()
    {
        ControlBinding controls = ControlBinding.instance;

        if (!cameraInputLocked && !GameManager.gameFinish && Time.timeScale > 0.01f && !ReplayRecorder.loadReplay)
        {
            int invert = controls.invertMouseYAxis ? -1 : 1;
            mouseX = Input.GetAxis("Mouse X") * controls.mouseSensitivity * GetCameraSpeedMultiplier();

            if (controls.alwaysFreeLook || Input.GetKey(controls.freelookKey))
            {
                mouseY = Input.GetAxis("Mouse Y") * controls.mouseSensitivity * invert * GetCameraSpeedMultiplier();
            }
            else
            {
                mouseY = 0f;
            }

            float keyRotationStep = Time.deltaTime * 0.25f * 90f * controls.keyboardSensitivity * GetCameraSpeedMultiplier();

            if (Input.GetKey(controls.rotateCameraRight) || Input.GetKeyDown(controls.rotateCameraRight))
                mouseX += keyRotationStep;

            if (Input.GetKey(controls.rotateCameraLeft) || Input.GetKeyDown(controls.rotateCameraLeft))
                mouseX -= keyRotationStep;

            if (Input.GetKey(controls.rotateCameraUp) || Input.GetKeyDown(controls.rotateCameraUp))
                mouseY += keyRotationStep * invert;

            if (Input.GetKey(controls.rotateCameraDown) || Input.GetKeyDown(controls.rotateCameraDown))
                mouseY -= keyRotationStep * invert;
        }
        else if (GameManager.gameFinish)
        {
            mouseX = Time.deltaTime * 10f;
        }
        else
        {
            mouseX = 0f;
            mouseY = 0f;
        }

        Vector3 up = -lastGravityDir.normalized;
        Vector3 forward = (marble.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        offset = Quaternion.AngleAxis(mouseX * 5f, up) * offset;

        float pitchAngle = Vector3.Angle(offset, up);
        bool canPitch = (pitchAngle > 10f && mouseY > 0f) || (pitchAngle < 170f && mouseY < 0f);

        if (canPitch)
        {
            offset = Quaternion.AngleAxis(-mouseY * 5f, right) * offset;
        }

        UpdateAnglesFromOffset();
        UpdateCameraPosition();
    }

    // ============================================================
    // State Modification API
    // ============================================================

    public void LockCamera(bool camCheck) => positionLocked = camCheck;

    public void ResetCam()
    {
        TwoDOutOfBounds = false;

        if (GameManager.instance == null || GameManager.instance.activeCheckpoint == null || marble == null)
            return;

        Transform startPad = GameManager.instance.activeCheckpoint.transform.parent;
        if (startPad == null) return;

        Transform cameraPos = startPad.Find("CameraPos");
        if (cameraPos == null) return;

        SetCameraPosition(marble.position, cameraPos.position);

        transform.position = cameraPos.position;
        transform.rotation = cameraPos.rotation;
    }

    public void SetCameraPosition(Vector3 marblePos, Vector3 cameraPos)
    {
        offset = marblePos - cameraPos;
        UpdateAnglesFromOffset();
    }

    public void RestoreCameraState(float yaw, float pitch, float distance)
    {
        cameraYaw = yaw;
        cameraPitch = pitch;

        UpdateOffsetFromAngles();

        if (!float.IsNaN(distance) && distance > 0.001f)
        {
            offset = offset.normalized * distance;
        }

        if (marble != null)
        {
            transform.position = marble.position + offset;
        }

        transform.LookAt(marble.position, -lastGravityDir.normalized);
    }

    public void SetCameraAngles(float yaw, float pitch, bool changeYaw, bool changePitch)
    {
        if (changeYaw) cameraYaw = yaw;
        if (changePitch) cameraPitch = pitch;

        UpdateOffsetFromAngles();
    }

    public Vector3 GetOffset() => offset;

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        UpdateAnglesFromOffset();
    }

    public float CameraDistance
    {
        get => offset.magnitude;
        set
        {
            if (offset.sqrMagnitude < 0.0001f)
            {
                offset = new Vector3(0f, 0f, -Mathf.Max(0.001f, value));
                return;
            }

            offset = offset.normalized * Mathf.Max(0.001f, value);
        }
    }

    public float DefaultFovX => PlayerPrefs.GetFloat("Graphics_FieldOfView", 70f);

    public void SetFovX(float fov) => SetFieldOfView(Mathf.RoundToInt(fov));

    // ============================================================
    // Cannon Mechanics
    // ============================================================

    public void SetCannonCamera(Cannon cannon, bool lockInput)
    {
        if (marble == null || cannon == null) return;

        if (!cannonCameraActive)
        {
            preCannonOffset = offset;
            preCannonPitch = cameraPitch;
            preCannonPosition = transform.position;
            preCannonRotation = transform.rotation;

            if (cam != null)
            {
                preCannonFov = cam.fieldOfView;
                preCannonLensShift = cam.lensShift;
            }

            preCannonStateValid = true;
        }

        cannonCameraActive = true;
        cameraInputLocked = lockInput;

        Vector3 up = -GravitySystem.GravityDir.normalized;
        Vector3 fireDirection = cannon.ComputeFireDirection().normalized;

        transform.position = cannon.cameraPos != null ? cannon.cameraPos.position : cannon.transform.position;
        transform.rotation = Quaternion.LookRotation(fireDirection, up);

        SetLensShift(-0.025f);
        SetFieldOfView(60);
    }

    public void ExitCannonCamera(bool lockInput)
    {
        Cannon cannon = Marble.instance != null ? Marble.instance.ActiveCannon : null;
        bool is2D = false;

        if (GameManager.instance != null)
        {
            foreach (IGameMode mode in GameManager.instance.GameModes)
            {
                if (mode is TwoDMode twoD && twoD.Active)
                {
                    is2D = true;
                    break;
                }
            }
        }

        if (!is2D && cannon != null && cannon.CPAS != null)
        {
            transform.position = cannon.CPAS.position;
            transform.rotation = cannon.CPAS.rotation;
        }

        cannonCameraActive = false;
        cameraInputLocked = lockInput;

        lastGravityDir = GravitySystem.GravityDir;

        SetLensShift(0.1f);
        SetFieldOfView((int)DefaultFovX);

        if (GameManager.instance != null)
        {
            foreach (IGameMode mode in GameManager.instance.GameModes)
            {
                if (mode is TwoDMode twoD && twoD.Active)
                {
                    twoD.RestoreAfterCannon();
                    break;
                }
            }
        }

        if (is2D && marble != null)
        {
            UpdateCameraPosition();
            transform.LookAt(marble.position, -lastGravityDir.normalized);
        }

        preCannonStateValid = false;
    }

    // ============================================================
    // Camera FX & Utilities
    // ============================================================

    public void FinishCameraPan() => StartCoroutine(PanCamera());

    private IEnumerator PanCamera()
    {
        float speed = 30f;
        float limit = 25f;
        float deadZone = 1.5f;

        while (true)
        {
            float pitch = GetGravityRelativePitch(GravitySystem.GravityDir);

            if (pitch < limit - deadZone)
                mouseY = -speed * Time.fixedDeltaTime;
            else if (pitch > limit + deadZone)
                mouseY = speed * Time.fixedDeltaTime;
            else
                break;

            yield return new WaitForFixedUpdate();
        }

        mouseY = 0f;
    }

    private float GetGravityRelativePitch(Vector3 gravity)
    {
        Vector3 up = -gravity.normalized;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        Vector3 flatForward = Vector3.ProjectOnPlane(forward, up).normalized;

        return Vector3.SignedAngle(flatForward, forward, right);
    }

    private void SetLensShift(float shiftY)
    {
        if (cam == null) return;
        Vector2 lensShift = cam.lensShift;
        lensShift.y = shiftY;
        cam.lensShift = lensShift;
    }

    private void SetFieldOfView(int fov)
    {
        if (cam != null)
            cam.fieldOfView = fov;
    }
}