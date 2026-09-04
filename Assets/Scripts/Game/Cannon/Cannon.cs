using System;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [HideInInspector]
    public string radarCannonType;
    // ============================================================
    // Cannon Properties
    // ============================================================

    [Header("Cannon Properties")]
    public bool useCharge = false;
    public bool useBase = true;
    public float chargeTime = 2.0f;
    public float force = 30f;
    public float minimumChargeTime = 0.25f;

    // ============================================================
    // Aim Constraints
    // ============================================================

    [Header("Aim Constraints (Degrees)")]
    public float pitch = 0f;
    public float yaw = 0f;

    public float pitchBoundHigh = 80f;
    public float pitchBoundLow = -30f;

    public float yawBoundLeft = 70f;
    public float yawBoundRight = 70f;

    public bool yawLimit = true;

    // ============================================================
    // Behavior Settings
    // ============================================================

    [Header("Behavior Settings")]
    public bool instant = false;
    public float instantDelayTime = 0f;

    [Tooltip("Movement control lock after firing. 0 = 0.3 seconds.")]
    public float lockTime = 0f;

    public bool lockCam = false;

    // ============================================================
    // Camera References
    // ============================================================

    [Header("Camera References")]
    public Transform cameraPos;
    public Transform CPAS;

    // ============================================================
    // Cannon Base
    // ============================================================

    [Header("Cannon Base")]
    [SerializeField]
    private Transform cannonBase;

    // ============================================================
    // Aim Visualization
    // ============================================================

    [Header("Aim Visualization")]
    public bool showAim = true;
    public float aimSize = 0.25f;
    public float aimLineWidth = 0.02f;

    private Material aimMaterial;

    private const int AimStepCount = 125;
    private const int AimCircleCount = 12;
    private const int AimCircleSegments = 20;

    private readonly List<LineRenderer> aimRings =
        new List<LineRenderer>();

    private LineRenderer collisionRing;

    // ============================================================
    // Audio & FX
    // ============================================================

    [Header("Audio & FX References")]
    public AudioClip explodeSound;
    public AudioClip explodeForceSound;

    public ParticleSystem smokeEmitterPrefab;
    public ParticleSystem sparkEmitterPrefab;
    public ParticleSystem volumeSmokeEmitterPrefab;

    // ============================================================
    // Internal State
    // ============================================================

    [HideInInspector]
    public float lastYaw = 0f;

    [HideInInspector]
    public float lastPitch = 0f;

    private Collider cannonCollider;

    private const int AIM_STEP_COUNT = 125;

    private float lastAimAppliedYaw =
        float.MaxValue;

    private float lastAimAppliedPitch =
        float.MaxValue;

    private float lastAimAppliedForceFraction =
        float.MaxValue;

    private bool aimVisualizationValid = false;

    private float explodeReenableTime =
        -1e8f;

    private Quaternion initRotation;

    private float currentYaw;
    private float currentPitch;

    private bool skipNextLook;

    private bool charging;
    private float chargeStartTime;

    // ============================================================
    // Charge
    // ============================================================

    private float GetChargeFraction()
    {
        if (!useCharge)
            return 1f;

        if (!charging)
            return 0f;

        if (chargeTime <= 0f)
            return 1f;

        return Mathf.Clamp01(
            (Time.time - chargeStartTime) /
            chargeTime
        );
    }

    public void ResetCharge()
    {
        charging = false;
        chargeStartTime = Time.time;

        HideAimVisualization();
    }

    // ============================================================
    // Unity
    // ============================================================

    private void Awake()
    {
        cannonCollider =
            GetComponent<Collider>();

        // Cannon is the "cannon" child.
        // The "base" object is its sibling.
        if (cannonBase == null)
        {
            Transform root =
                transform.parent;

            if (root != null)
            {
                Transform baseTransform =
                    root.Find("base");

                if (baseTransform != null)
                    cannonBase =
                        baseTransform;
            }
        }

        if (Marble.onRespawn != null)
        {
            Marble.onRespawn.AddListener(
                ResetCannon
            );
        }
    }

    private void Start()
    {
        if (!useBase)
            transform.parent.Find("base").Find("Col").GetComponent<Renderer>().enabled = false;

        ResetCannon();
    }

    private void Update()
    {
        float currentTime =
            Time.time;

        if (cannonCollider != null)
        {
            cannonCollider.enabled =
                currentTime >=
                explodeReenableTime;
        }

        if (Marble.instance == null)
            return;

        if (!Marble.instance.IsInCannon)
            return;

        if (Marble.instance.ActiveCannon != this)
            return;

        HandleLook();

        // ========================================================
        // Charge State
        // ========================================================

        if (useCharge)
        {
            if (
                Input.GetKeyDown(
                    ControlBinding.instance.usePowerup
                )
            )
            {
                charging = true;
                chargeStartTime =
                    Time.time;
            }

            if (
                Input.GetKeyUp(
                    ControlBinding.instance.usePowerup
                )
            )
            {
                charging = false;
            }
        }

        // ========================================================
        // Trajectory
        // ========================================================

        UpdateAimVisualization(
            currentYaw,
            currentPitch,
            GetChargeFraction()
        );
    }

    // ============================================================
    // Cannon Look
    // ============================================================

    public void HandleLook()
    {
        if (CameraController.instance == null)
            return;

        if (
            CameraController.instance.CameraInputLocked
            || GameManager.gameFinish
            || Time.timeScale <= 0.01f
            || ReplayRecorder.loadReplay
        )
        {
            return;
        }

        if (skipNextLook)
        {
            // Consume mouse delta so the first frame after
            // entering the cannon does not jump.
            Input.GetAxis("Mouse X");
            Input.GetAxis("Mouse Y");

            skipNextLook = false;

            return;
        }

        int invert =
            ControlBinding.instance
                .invertMouseYAxis
                ? 1
                : -1;

        float mouseX =
            Input.GetAxis("Mouse X") *
            ControlBinding.instance
                .mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            ControlBinding.instance
                .mouseSensitivity *
            invert;

        // ========================================================
        // Keyboard Rotation
        // ========================================================

        if (
            Input.GetKey(
                ControlBinding.instance
                    .rotateCameraRight
            )
            ||
            Input.GetKeyDown(
                ControlBinding.instance
                    .rotateCameraRight
            )
        )
        {
            mouseX +=
                Time.deltaTime *
                0.25f *
                90f *
                ControlBinding.instance
                    .mouseSensitivity;
        }

        if (
            Input.GetKey(
                ControlBinding.instance
                    .rotateCameraLeft
            )
            ||
            Input.GetKeyDown(
                ControlBinding.instance
                    .rotateCameraLeft
            )
        )
        {
            mouseX -=
                Time.deltaTime *
                0.25f *
                90f *
                ControlBinding.instance
                    .mouseSensitivity;
        }

        if (
            Input.GetKey(
                ControlBinding.instance
                    .rotateCameraUp
            )
            ||
            Input.GetKeyDown(
                ControlBinding.instance
                    .rotateCameraUp
            )
        )
        {
            mouseY +=
                Time.deltaTime *
                0.25f *
                90f *
                invert *
                ControlBinding.instance
                    .mouseSensitivity;
        }

        if (
            Input.GetKey(
                ControlBinding.instance
                    .rotateCameraDown
            )
            ||
            Input.GetKeyDown(
                ControlBinding.instance
                    .rotateCameraDown
            )
        )
        {
            mouseY -=
                Time.deltaTime *
                0.25f *
                90f *
                invert *
                ControlBinding.instance
                    .mouseSensitivity;
        }

        // ========================================================
        // Runtime Aim
        // ========================================================

        currentYaw += mouseX;
        currentPitch += mouseY;

        // Pitch is absolute.
        currentPitch =
            Mathf.Clamp(
                currentPitch,
                pitchBoundLow,
                pitchBoundHigh
            );

        // Yaw is relative to starting yaw.
        if (yawLimit)
        {
            currentYaw =
                Mathf.Clamp(
                    currentYaw,
                    yaw - yawBoundLeft,
                    yaw + yawBoundRight
                );
        }

        ApplyAimRotation(
            currentYaw * Mathf.Deg2Rad,
            currentPitch * Mathf.Deg2Rad
        );
    }

    // ============================================================
    // Apply Aim
    // ============================================================

    public void ApplyAimRotation(
        float yawRad,
        float pitchRad
    )
    {
        Vector3 up =
            -GravitySystem.GravityDir.normalized;

        // --------------------------------------------------------
        // Base
        // --------------------------------------------------------

        if (cannonBase != null)
        {
            Vector3 baseDir =
                new Vector3(
                    Mathf.Sin(yawRad),
                    0f,
                    Mathf.Cos(yawRad)
                );

            baseDir.Normalize();

            if (
                Mathf.Abs(
                    Vector3.Dot(
                        baseDir,
                        up
                    )
                ) > 0.9999f
            )
            {
                up = Vector3.right;
            }

            cannonBase.localRotation =
                Quaternion.LookRotation(
                    baseDir,
                    up
                );
        }

        // --------------------------------------------------------
        // Cannon
        // --------------------------------------------------------

        Vector3 bodyDir =
            new Vector3(
                Mathf.Sin(yawRad) *
                Mathf.Cos(pitchRad),

                Mathf.Sin(pitchRad),

                Mathf.Cos(yawRad) *
                Mathf.Cos(pitchRad)
            );

        bodyDir.Normalize();

        if (
            Mathf.Abs(
                Vector3.Dot(
                    bodyDir,
                    up
                )
            ) > 0.9999f
        )
        {
            up = Vector3.right;
        }

        transform.localRotation =
            Quaternion.LookRotation(
                bodyDir,
                up
            );

        lastYaw =
            yawRad;

        lastPitch =
            pitchRad;
    }

    // ============================================================
    // Base Yaw
    // ============================================================

    private void ApplyBaseYaw(
        float yawRad,
        Vector3 up
    )
    {
        if (cannonBase == null)
            return;

        Vector3 baseDir =
            new Vector3(
                Mathf.Sin(yawRad),
                0f,
                Mathf.Cos(yawRad)
            );

        if (
            baseDir.sqrMagnitude <=
            0.0001f
        )
        {
            return;
        }

        baseDir.Normalize();

        if (
            Mathf.Abs(
                Vector3.Dot(
                    baseDir,
                    up
                )
            ) > 0.9999f
        )
        {
            up = Vector3.right;
        }

        cannonBase.rotation =
            Quaternion.LookRotation(
                baseDir,
                up
            );
    }

    // ============================================================
    // Reset Runtime Aim
    // ============================================================

    public void ResetRuntimeAim()
    {
        currentYaw = yaw;
        currentPitch = pitch;

        ApplyAimRotation(
            currentYaw * Mathf.Deg2Rad,
            currentPitch * Mathf.Deg2Rad
        );

        skipNextLook = true;
    }

    // ============================================================
    // Fire Direction
    // ============================================================

    public Vector3 ComputeFireDirection()
    {
        // Cannon visually points in -forward.
        return -transform.forward.normalized;
    }

    // ============================================================
    // Aim API
    // ============================================================

    public void UpdateAim(
        float yawRad,
        float pitchRad
    )
    {
        Vector3 bodyDir =
            new Vector3(
                Mathf.Sin(yawRad) *
                Mathf.Cos(pitchRad),

                Mathf.Sin(pitchRad),

                Mathf.Cos(yawRad) *
                Mathf.Cos(pitchRad)
            );

        if (
            bodyDir.sqrMagnitude >
            0.0001f
        )
        {
            Vector3 up =
                -GravitySystem.GravityDir.normalized;

            transform.rotation =
                Quaternion.LookRotation(
                    bodyDir,
                    up
                );

            ApplyBaseYaw(
                yawRad,
                up
            );
        }

        lastYaw =
            yawRad;

        lastPitch =
            pitchRad;
    }

    public void UpdateAimFromCamera(
        float cameraYaw,
        float cameraPitch
    )
    {
        UpdateAim(
            cameraYaw,
            cameraPitch
        );
    }

    public void ResetCannon()
    {
        currentYaw = yaw;
        currentPitch = pitch;

        UpdateAim(
            yaw * Mathf.Deg2Rad,
            pitch * Mathf.Deg2Rad
        );
    }

    // ============================================================
    // Explosion
    // ============================================================

    public void Explode()
    {
        Vector3 pos =
            transform.position;

        AudioClip clipToPlay =
            force >= 60f
                ? explodeForceSound
                : explodeSound;

        if (clipToPlay != null)
        {
            GameManager.instance.PlayAudioClip(clipToPlay);
        }

        SpawnExplosionBurst(pos);

        for (int i = 0; i < 2; i++)
        {
            Vector3 randVec =
                new Vector3(
                    UnityEngine.Random.Range(
                        -1f,
                        1f
                    ),
                    UnityEngine.Random.Range(
                        0f,
                        1f
                    ),
                    UnityEngine.Random.Range(
                        -1f,
                        1f
                    )
                );

            if (
                randVec.sqrMagnitude >
                0.0001f
            )
            {
                randVec.Normalize();
            }

            SpawnExplosionBurst(
                pos + randVec
            );
        }

        if (
            volumeSmokeEmitterPrefab != null
        )
        {
            Instantiate(
                volumeSmokeEmitterPrefab,
                pos,
                Quaternion.identity
            );
        }

        if (cannonCollider != null)
        {
            cannonCollider.enabled =
                false;
        }

        explodeReenableTime =
            Time.time + 1.0f;
    }

    private void SpawnExplosionBurst(
        Vector3 pos
    )
    {
        if (smokeEmitterPrefab != null)
        {
            Instantiate(
                smokeEmitterPrefab,
                pos,
                Quaternion.identity
            );
        }

        if (sparkEmitterPrefab != null)
        {
            Instantiate(
                sparkEmitterPrefab,
                pos,
                Quaternion.identity
            );
        }
    }

    // ============================================================
    // Marble / Camera Interaction
    // ============================================================

    public bool CanEnter =>
        Time.time >=
        explodeReenableTime;

    public Vector3 GetMarblePosition()
    {
        if (
            CameraController.instance != null &&
            CPAS != null
        )
        {
            CameraController.instance.SetOffset(
                transform.position -
                CPAS.position
            );
        }

        return transform.position;
    }

    public Quaternion GetMarbleRotation()
    {
        return transform.rotation;
    }

    public Vector3 GetBasePosition()
    {
        return transform.position;
    }

    public Vector3 GetExitPosition()
    {
        ResetCannon();

        return
            transform.position +
            transform.forward * 1.5f;
    }

    // ============================================================
    // Aim Visualization
    // ============================================================

    public void HideAimVisualization()
    {
        foreach (
            LineRenderer ring
            in aimRings
        )
        {
            if (ring != null)
                ring.enabled = false;
        }

        if (collisionRing != null)
            collisionRing.enabled = false;

        aimVisualizationValid = false;
    }

    public void UpdateAimVisualization(
        float cameraYaw,
        float cameraPitch,
        float forceFraction
    )
    {
        if (!showAim)
        {
            HideAimVisualization();
            return;
        }

        float forceValue =
            force *
            (
                useCharge
                    ? forceFraction
                    : 1f
            );

        Vector3 initPos =
            GetBasePosition();

        Vector3 vel =
            ComputeFireDirection().normalized *
            forceValue;

        Vector3 currentUp =
            -GravitySystem.GravityDir.normalized;

        Vector3 gravity =
            currentUp *
            -Marble.instance
                .cannonBeforeGravity;

        float timeStep =
            Mathf.Clamp(
                forceValue * 0.001f,
                0.02f,
                0.2f
            );

        Vector3[] aimPositions =
            new Vector3[AimStepCount];

        Vector3[] aimDirs =
            new Vector3[AimStepCount];

        Vector3 hitPos =
            Vector3.zero;

        Vector3 hitNormal =
            Vector3.zero;

        bool hasHit = false;

        int stepCount =
            AimStepCount;

        for (
            int i = 0;
            i < AimStepCount;
            i++
        )
        {
            float startTime =
                i * timeStep;

            float endTime =
                (i + 1) * timeStep;

            Vector3 start =
                initPos +
                vel * startTime +
                gravity *
                (
                    0.5f *
                    startTime *
                    startTime
                );

            Vector3 end =
                initPos +
                vel * endTime +
                gravity *
                (
                    0.5f *
                    endTime *
                    endTime
                );

            Vector3 segment =
                end - start;

            float segmentLength =
                segment.magnitude;

            if (
                segmentLength >
                0.0001f
            )
            {
                RaycastHit[] hits =
                    Physics.RaycastAll(
                        start,
                        segment /
                        segmentLength,
                        segmentLength
                    );

                RaycastHit? closestHit =
                    null;

                float closestDistance =
                    float.MaxValue;

                foreach (
                    RaycastHit hit
                    in hits
                )
                {
                    if (
                        hit.collider.isTrigger
                    )
                    {
                        continue;
                    }

                    float distance =
                        Vector3.Distance(
                            start,
                            hit.point
                        );

                    if (
                        distance <
                        closestDistance
                    )
                    {
                        closestDistance =
                            distance;

                        closestHit =
                            hit;
                    }
                }

                if (closestHit.HasValue)
                {
                    RaycastHit hit =
                        closestHit.Value;

                    end =
                        hit.point;

                    hitPos =
                        hit.point;

                    hitNormal =
                        hit.normal;

                    hasHit = true;
                }
            }

            aimPositions[i] =
                end;

            aimDirs[i] =
                end - start;

            if (hasHit)
            {
                stepCount =
                    i + 1;

                break;
            }
        }

        DrawAimVisualization(
            aimPositions,
            aimDirs,
            stepCount,
            hasHit,
            hitPos,
            hitNormal
        );
    }

    private void DrawAimVisualization(
        Vector3[] aimPositions,
        Vector3[] aimDirs,
        int stepCount,
        bool hit,
        Vector3 hitPos,
        Vector3 hitNormal
    )
    {
        EnsureAimRings();

        for (
            int i = 0;
            i < AimCircleCount;
            i++
        )
        {
            int index =
                Mathf.FloorToInt(
                    i *
                    stepCount /
                    (float)AimCircleCount
                );

            if (
                index >=
                AimStepCount
            )
            {
                index =
                    AimStepCount - 1;
            }

            if (index < 0)
            {
                aimRings[i].enabled =
                    false;

                continue;
            }

            float progress =
                Mathf.Clamp01(
                    i /
                    (float)AimCircleCount
                );

            Color color =
                new Color(
                    1f,
                    progress,
                    0f,
                    1f
                );

            LineRenderer ring =
                aimRings[i];

            ring.startColor =
                color;

            ring.endColor =
                color;

            ring.enabled =
                true;

            DrawAimRing(
                ring,
                aimPositions[index],
                aimDirs[index],
                aimSize
            );
        }

        if (hit)
        {
            collisionRing.startColor =
                Color.green;

            collisionRing.endColor =
                Color.green;

            collisionRing.enabled =
                true;

            DrawAimRing(
                collisionRing,
                hitPos,
                hitNormal,
                aimSize
            );
        }
        else
        {
            collisionRing.enabled =
                false;
        }

        aimVisualizationValid = true;
    }

    private void DrawAimRing(
        LineRenderer line,
        Vector3 position,
        Vector3 normal,
        float radius
    )
    {
        Vector3 n =
            normal.sqrMagnitude >
            0.0001f
                ? normal.normalized
                : Vector3.up;

        Vector3 reference =
            Mathf.Abs(
                Vector3.Dot(
                    n,
                    Vector3.up
                )
            ) > 0.999f
                ? Vector3.right
                : Vector3.up;

        Vector3 axisX =
            Vector3.Cross(
                n,
                reference
            ).normalized;

        Vector3 axisY =
            Vector3.Cross(
                axisX,
                n
            ).normalized;

        for (
            int i = 0;
            i <= AimCircleSegments;
            i++
        )
        {
            float theta =
                i /
                (float)AimCircleSegments *
                Mathf.PI *
                2f;

            Vector3 point =
                position +
                axisX *
                Mathf.Cos(theta) *
                radius +
                axisY *
                Mathf.Sin(theta) *
                radius;

            line.SetPosition(
                i,
                point
            );
        }
    }

    private LineRenderer CreateAimRing(
        string name
    )
    {
        GameObject obj =
            new GameObject(name);

        obj.transform.SetParent(
            transform,
            false
        );

        LineRenderer line =
            obj.AddComponent<
                LineRenderer
            >();

        line.useWorldSpace =
            true;

        line.positionCount =
            AimCircleSegments + 1;

        line.loop =
            false;

        line.widthMultiplier =
            aimLineWidth;

        if (aimMaterial == null)
        {
            Shader shader =
                Shader.Find(
                    "Sprites/Default"
                );

            if (shader != null)
            {
                aimMaterial =
                    new Material(shader);
            }
        }

        if (aimMaterial != null)
            line.material =
                aimMaterial;

        line.shadowCastingMode =
            UnityEngine.Rendering
                .ShadowCastingMode.Off;

        line.receiveShadows =
            false;

        line.enabled = false;

        return line;
    }

    private void EnsureAimRings()
    {
        while (
            aimRings.Count <
            AimCircleCount
        )
        {
            aimRings.Add(
                CreateAimRing(
                    $"AimRing_{aimRings.Count}"
                )
            );
        }

        if (collisionRing == null)
        {
            collisionRing =
                CreateAimRing(
                    "AimCollisionRing"
                );
        }
    }

    // ============================================================
    // Cleanup
    // ============================================================

    private void OnDestroy()
    {
        if (Marble.onRespawn != null)
        {
            Marble.onRespawn.RemoveListener(
                ResetCannon
            );
        }
    }
}