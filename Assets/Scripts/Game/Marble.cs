using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Movement))]
public class Marble : MonoBehaviour
{
    public static Marble instance { get; private set; }

    [Header("Sound Effects")]
    AudioSource audioSource;
    [SerializeField] AudioClip jumpSfx;
    [SerializeField] AudioClip[] bounceSfx;
    public AudioSource rollingSound;
    public AudioSource slidingSound;
    [SerializeField] AudioSource useShockAbsorberSound;
    [SerializeField] AudioSource useSuperBounceSound;
    [SerializeField] AudioSource gyroSound;
    [SerializeField] AudioSource TTActiveSound;

    [SerializeField] private AudioSource cannonChargeStartAudio;
    [SerializeField] private AudioSource cannonChargeLoopAudio;
    private bool cannonChargeAudioStarted;

    public AudioSource teleportSound;
    public AudioSource alarmSound;

    // Things that stick to the marble
    public GameObject gyrocopterBlades;
    public GameObject glowBounce;
    public GameObject bubble;
    public GameObject frozenIce;

    //particles
    public GameObject bounceParticle;
    public ParticleSystem trailParticle;
    public ParticleSystem bubbleParticle;
    public GameObject waterSplash;
    public GameObject fireballParticle;
    public GameObject fireballBlastParticle;

    public float trailSpeedThreshold = 10f;

    public Movement movement;
    public GameObject normalMesh;

    // Cannon State
    private Cannon activeCannon;
    private float cannonCharge;
    public float cannonBeforeGravity = 20f;
    private float cannonControlLockUntil = Mathf.NegativeInfinity;
    private bool cannonControlLockActive;
    private float cannonCameraLockUntil = Mathf.NegativeInfinity;
    private Cannon lastCannon;
    private float cannonReenableTime = Mathf.NegativeInfinity;
    private float instantCannonFireTime = Mathf.NegativeInfinity;
    private bool cannonPreviousCanMove;
    private bool cannonPreviousCanSpin;
    private bool cannonPreviousCanJump;

    // True while Marble has an active movement-trigger lock caused by
    // being inside a cannon or by the cannon's post-launch lockTime.
    private bool cannonMovementTriggerActive;

    bool canUsePowerupAfterCannon = true;
    public bool IsInCannon => activeCannon != null;
    public bool CanUsePowerupAfterCannon => canUsePowerupAfterCannon;

    public Cannon ActiveCannon => activeCannon;
    public bool isUsingBubble;

    // Water State
    private bool wasInWater;
    private WaterPhysicsTrigger lastWaterTrigger;
    private List<PhysicsAttributeOverride> waterPhysicsLayer;

    // Bubble State
    [Header("Bubble")]
    [SerializeField] private float bubbleTime;
    [SerializeField] private float bubbleTotalTime;
    [SerializeField] private bool bubbleInfinite;
    public bool BubbleInfinite => bubbleInfinite;
    private List<PhysicsAttributeOverride> bubblePhysicsLayer;

    [Header("Bubble Audio")]
    [SerializeField] private AudioSource bubbleUseAudio;
    [SerializeField] private AudioClip bubbleEndSound;

    [Header("Bubble Physics")]
    [SerializeField] private float bubbleMaxRollVelocity = 10f;
    [SerializeField] private float bubbleAngularAcceleration = 55f;
    [SerializeField] private float bubbleBrakingAcceleration = 30f;
    [SerializeField] private float bubbleGravity = -5f;
    [SerializeField] private float bubbleAirAcceleration = 7f;
    [SerializeField] private float bubbleStaticFriction = 1.1f;
    [SerializeField] private float bubbleKineticFriction = 0.7f;
    [SerializeField] private float bubbleBounceKineticFriction = 0.2f;
    [SerializeField] private float bubbleMaxDotSlide = 0.5f;
    [SerializeField] private float bubbleBounceRestitution = 0.7f;
    [SerializeField] private float bubbleJumpImpulse = 7.5f;
    [SerializeField] private float bubbleMinTrailVelocity = 1.2f;

    // Ice Shard State
    public bool isFrozen { get; private set; }
    private float lastFreezeTime = Mathf.NegativeInfinity;
    private IceShard iceShard;
    public float LastFreezeTime => lastFreezeTime;

    // Fireball State
    public bool fireball { get; private set; }
    private float fireballTime;
    private float fireballStartTime;
    float fireballTotalTime;
    public float nextFireBlastTime = Mathf.NegativeInfinity;
    [SerializeField] AudioClip fireExtinguishSound;

    public float GetFireballTime()
    {
        if (!fireball) return 0f;
        return Mathf.Max(0f, fireballTime - (Time.time - fireballStartTime));
    }

    // Fireball Bubble checkpoint behavior
    private float checkpointPowerupTime;
    private float checkpointPowerupTotalTime;
    private bool checkpointHadBubble;
    private bool checkpointHadFireball;
    private bool checkpointBubbleInfinite;

    // PhysMod State
    private readonly List<List<PhysicsAttributeOverride>> physicsLayers =
        new List<List<PhysicsAttributeOverride>>();

    private readonly Dictionary<string, float> physicsBaseValues =
        new Dictionary<string, float>();

    // Per-marble PhysMod time scale.
    // 1.0 = normal speed. Other values are supplied by the PhysMod.
    private float physicsTimeScale = 1f;
    private float cameraSpeedMultiplier = 1f;

    public float PhysicsTimeScale => physicsTimeScale;
    public float CameraSpeedMultiplier => cameraSpeedMultiplier;

    // Gyrocopter state guard/cooldown.
    // Prevents duplicate UseGyrocopter/CancelGyrocopter calls from
    // stacking gravity changes or repeatedly starting/stopping audio.
    [SerializeField] private float gyrocopterTransitionCooldown = 1f;
    private float gyrocopterNextTransitionTime = Mathf.NegativeInfinity;

    // LockPowerupTrigger state.
    private int powerupUseLockCount;

    // Respawn Event
    public class OnRespawn : UnityEvent { }
    public static OnRespawn onRespawn = new OnRespawn();

    public void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        movement = GetComponent<Movement>();
        CapturePhysicsAttributeBaseline();
        onRespawn.AddListener(Respawn);

        GetComponent<SphereCollider>().enabled = false;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        Transform sounds = transform.Find("Sounds");
        if (sounds != null)
        {
            Transform bubbleSound = sounds.Find("BubbleSound");
            if (bubbleSound != null)
            {
                bubbleUseAudio = bubbleSound.GetComponent<AudioSource>();
                if (bubbleUseAudio != null) bubbleUseAudio.loop = true;
            }
        }

        canUsePowerupAfterCannon = true;
    }

    private void Update()
    {
        if (GameUIManager.instance != null && GameUIManager.instance.IsChatInputOpen) return;

        if (GameUIManager.instance != null && GameUIManager.instance.isInitialized &&
            (GameUIManager.instance.oobInsultMenu.activeSelf || GameUIManager.instance.saveReplayMenu.activeSelf))
        {
            return;
        }

        if (activeCannon != null) 
            UpdateCannonFiring();

        UpdateCannonLocks();

        float speed = movement.marbleVelocity.magnitude;
        if (speed > trailSpeedThreshold)
        {
            if (!trailParticle.isPlaying) trailParticle.Play();
        }
        else
        {
            if (trailParticle.isPlaying) trailParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (Input.GetKeyDown(ControlBinding.instance.respawn) &&
            !GameManager.gameFinish &&
            !ReplayRecorder.loadReplay)
        {
            if (GameManager.instance.GetGameMode<HuntMode>() != null)
            {
                // Hunt: R always fully restarts the level.
                GameManager.instance.RestartLevel();
            }
            else if (!GameManager.isPaused)
            {
                GameManager.instance.IncrementOutOfBoundsCount();
                onRespawn?.Invoke();
            }
            else
            {
                GameManager.instance.RestartLevel();
            }
        }

        if (GameManager.isPaused && Input.GetKeyDown(KeyCode.Return))
        {
            if (ReplayRecorder.recordReplay)
            {
                GameManager.instance.pauseMenu.SetActive(false);
                GameManager.instance.finishMenu.SetActive(false);
                GameUIManager.instance.saveReplayMenu.SetActive(true);
            }
            else
            {
                JukeboxManager.instance.PlayMusic("Pianoforte", true);
                SceneManager.LoadScene("PlayMission");
            }
        }

        if (Input.GetKeyDown(ControlBinding.instance.blast))
            FireBlast();

        UpdateFireball();
        UpdateBubble();
        UpdateFreeze();

        if (Input.GetKey(ControlBinding.instance.usePowerup) && !GameManager.isPaused &&
            !GameManager.gameFinish && movement.canMove && !ReplayRecorder.loadReplay &&
            !IsInCannon && canUsePowerupAfterCannon && !isFrozen && !IsPowerupUseLocked &&
            GameManager.instance.activePowerup != PowerupType.Bubble)
        {

            if (GameManager.instance != null && GameManager.instance.IsOutOfBounds)
            {
                if (GameManager.instance.GetGameMode<HuntMode>() != null)
                    GameManager.instance.HuntRestartRequested = false;

                GameManager.instance.CancelInvoke(nameof(GameManager.InvokeRespawn));
                GameManager.instance.InvokeRespawn();
                return;
            }

            UsePowerup();
        }
    }

    private void UpdateFreeze()
    {
        if (!isFrozen)
            return;

        if (Time.time >= lastFreezeTime + IceShard.FREEZE_TIME)
            Unfreeze(false);
    }

    private void FixedUpdate()
    {
        if (movement == null) return;

        if (isUsingBubble && !IsInWater())
        {
            DeactivateBubble();
            return;
        }

        if (IsInWater())
        {
            WaterPhysicsTrigger trigger =
                WaterPhysicsTrigger.GetClosestTrigger(this);

            // Bubble particles are visible only when the entire
            // marble is below the water surface.
            SetBubbleParticle(IsFullySubmerged(trigger));

            UpdateWaterPhysics();
        }
        else
        {
            SetBubbleParticle(false);
        }
    }

    public void LateUpdate()
    {
        if (gyrocopterBlades != null) gyrocopterBlades.transform.position = transform.position;
    }

    // Bubble
    public void SetBubbleTime(float time, bool infinite)
    {
        bubbleTotalTime = bubbleTime = Mathf.Max(0f, time);
        bubbleInfinite = infinite;
        isUsingBubble = false;
    }

    private void UpdateBubble()
    {
        if (movement == null) return;

        bool use = Input.GetKey(ControlBinding.instance.usePowerup);
        bool inWater = IsInWater();

        if (isUsingBubble)
        {
            if (bubbleInfinite)
                GameUIManager.instance.SetBubbleTimer(Mathf.Infinity, bubbleTotalTime);
            else if (bubbleTime > 0)
                GameUIManager.instance.SetBubbleTimer(bubbleTime, bubbleTotalTime);

            if (!use || !inWater || GameManager.isPaused || GameManager.gameFinish ||
                ReplayRecorder.loadReplay || IsInCannon)
            {
                DeactivateBubble();
                return;
            }

            if (!bubbleInfinite)
            {
                bubbleTime -= Time.deltaTime;
                GameUIManager.instance.SetBubbleTimer(bubbleTime, bubbleTotalTime);

                if (bubbleTime <= 0f)
                {
                    bubbleTime = 0f;
                    GameUIManager.instance.SetBubbleTimer(-1, bubbleTotalTime);
                    DeactivateBubble();
                    PlayBubbleEndSound();
                    GameManager.instance.activePowerup = PowerupType.None;
                    return;
                }
            }
            return;
        }

        if (use && inWater && movement.canMove && !GameManager.isPaused &&
            !GameManager.gameFinish && !ReplayRecorder.loadReplay && !IsInCannon &&
            (bubbleInfinite || bubbleTime > 0f))
        {
            ActivateBubble();
        }
    }

    private void ActivateBubble()
    {
        if (isUsingBubble || !IsInWater() || movement == null) return;

        isUsingBubble = true;

        bubblePhysicsLayer = PushPhysicsLayer(
            BuildBubblePhysicsLayer()
        );

        if (bubble != null) bubble.SetActive(true);

        if (bubbleUseAudio != null && !bubbleUseAudio.isPlaying) bubbleUseAudio.Play();

        if (fireball)
        {
            DeactivateFireball();
            GameUIManager.instance.SetFireballTimer(-1, fireballTotalTime);
        }
    }

    private List<PhysicsAttributeOverride> BuildBubblePhysicsLayer()
    {
        return new List<PhysicsAttributeOverride>
        {
            new PhysicsAttributeOverride("maxrollvelocity", bubbleMaxRollVelocity),
            new PhysicsAttributeOverride("angularacceleration", bubbleAngularAcceleration),
            new PhysicsAttributeOverride("brakingacceleration", bubbleBrakingAcceleration),
            new PhysicsAttributeOverride("airacceleration", bubbleAirAcceleration),
            new PhysicsAttributeOverride("gravity", bubbleGravity),
            new PhysicsAttributeOverride("staticfriction", bubbleStaticFriction),
            new PhysicsAttributeOverride("kineticfriction", bubbleKineticFriction),
            new PhysicsAttributeOverride("bouncekineticfriction", bubbleBounceKineticFriction),
            new PhysicsAttributeOverride("maxdotslide", bubbleMaxDotSlide),
            new PhysicsAttributeOverride("bouncerestitution", bubbleBounceRestitution),
            new PhysicsAttributeOverride("jumpimpulse", bubbleJumpImpulse),
            new PhysicsAttributeOverride("trailspeedthreshold", bubbleMinTrailVelocity)
        };
    }

    private void DeactivateBubble()
    {
        if (!isUsingBubble) return;

        isUsingBubble = false;

        if (bubble != null) bubble.SetActive(false);
        if (bubbleUseAudio != null && bubbleUseAudio.isPlaying) bubbleUseAudio.Stop();

        if (bubblePhysicsLayer != null)
        {
            PopPhysicsLayer(bubblePhysicsLayer);
            bubblePhysicsLayer = null;
        }
    }

    // Called when the marble's overall water-trigger state changes.
    public void OnWaterTriggerChanged()
    {
        bool inWater = IsInWater();

        if (inWater && !wasInWater)
        {
            WaterPhysicsTrigger trigger =
                WaterPhysicsTrigger.GetClosestTrigger(this);

            lastWaterTrigger = trigger;

            SetBubbleParticle(IsFullySubmerged(trigger));

            EnterWater();

            if (trigger != null && movement != null)
            {
                movement.marbleVelocity *=
                    1f - trigger.GetVelocityMultiplier();
            }

            waterPhysicsLayer = PushPhysicsLayer(
                BuildWaterPhysicsLayer()
            );
        }

        if (!inWater && wasInWater)
        {
            SetBubbleParticle(false);

            if (isUsingBubble)
                DeactivateBubble();

            if (waterPhysicsLayer != null)
            {
                PopPhysicsLayer(waterPhysicsLayer);
                waterPhysicsLayer = null;
            }

            lastWaterTrigger = null;
        }

        wasInWater = inWater;
    }

    // Called directly by WaterPhysicsTrigger only for a real physical
    // trigger entry. Teleports/state refreshes never call this method.
    public void OnWaterTriggerEntered(WaterPhysicsTrigger trigger)
    {
        if (trigger == null)
            return;

        lastWaterTrigger = trigger;

        // Play the splash before OnWaterTriggerChanged() modifies velocity.
        PlayWaterSplash(trigger, "Enter");
    }

    // Called directly by WaterPhysicsTrigger only for a real physical
    // trigger exit. Teleports/state refreshes never call this method.
    public void OnWaterTriggerExited(WaterPhysicsTrigger trigger)
    {
        if (trigger == null)
            return;

        // Play the splash while the exact trigger is still known.
        PlayWaterSplash(trigger, "Exit");
    }

    private bool IsFullySubmerged(WaterPhysicsTrigger trigger = null)
    {
        if (trigger == null)
            trigger = WaterPhysicsTrigger.GetClosestTrigger(this);

        if (trigger == null)
            return false;

        Collider waterCollider = trigger.GetComponent<Collider>();

        if (waterCollider == null)
            return false;

        SphereCollider sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null)
            return false;

        float maxScale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z)
        );

        float radius = sphereCollider.radius * maxScale;

        float waterSurfaceY = waterCollider.bounds.max.y;
        float marbleTopY = transform.position.y + radius;

        return marbleTopY <= waterSurfaceY;
    }

    private void SetBubbleParticle(bool active)
    {
        if (bubbleParticle == null)
            return;

        if (active)
        {
            if (!bubbleParticle.gameObject.activeSelf)
                bubbleParticle.gameObject.SetActive(true);

            if (!bubbleParticle.isPlaying)
                bubbleParticle.Play();
        }
        else
        {
            if (bubbleParticle.isPlaying)
            {
                bubbleParticle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }

            if (bubbleParticle.gameObject.activeSelf)
                bubbleParticle.gameObject.SetActive(false);
        }
    }

    private void PlayWaterSplash(WaterPhysicsTrigger trigger, string debug)
    {
        // Consume the respawn suppression flag immediately.
        // This prevents isRespawn from becoming permanently stuck if
        // the splash cannot be played for another reason.
        bool suppressSplash = isRespawn;
        isRespawn = false;

        if (suppressSplash)
            return;

        if (waterSplash == null || trigger == null)
            return;

        Collider waterCollider = trigger.GetComponent<Collider>();

        if (waterCollider == null)
            return;

        Bounds bounds = waterCollider.bounds;

        Vector3 position = transform.position;

        // Find the distance from the marble to each face of the cube.
        float distanceLeft = Mathf.Abs(position.x - bounds.min.x);
        float distanceRight = Mathf.Abs(bounds.max.x - position.x);
        float distanceBottom = Mathf.Abs(position.y - bounds.min.y);
        float distanceTop = Mathf.Abs(bounds.max.y - position.y);
        float distanceBack = Mathf.Abs(position.z - bounds.min.z);
        float distanceFront = Mathf.Abs(bounds.max.z - position.z);

        float minDistance = distanceLeft;
        Vector3 normal = Vector3.left;

        if (distanceRight < minDistance)
        {
            minDistance = distanceRight;
            normal = Vector3.right;
        }

        if (distanceBottom < minDistance)
        {
            minDistance = distanceBottom;
            normal = Vector3.down;
        }

        if (distanceTop < minDistance)
        {
            minDistance = distanceTop;
            normal = Vector3.up;
        }

        if (distanceBack < minDistance)
        {
            minDistance = distanceBack;
            normal = Vector3.back;
        }

        if (distanceFront < minDistance)
        {
            minDistance = distanceFront;
            normal = Vector3.forward;
        }

        GameObject splash = Instantiate(
            waterSplash,
            transform.position,
            Quaternion.FromToRotation(Vector3.up, normal) *
            Quaternion.Euler(-90f, 0f, 0f)
        );

        splash.name = "Water Splash " + debug;

        ParticleSystem particleSystem =
            splash.GetComponent<ParticleSystem>();

        if (particleSystem != null)
        {
            Destroy(
                splash,
                particleSystem.main.duration +
                particleSystem.main.startLifetime.constantMax
            );
        }
        else
        {
            Destroy(splash, 2f);
        }
    }

    private List<PhysicsAttributeOverride> BuildWaterPhysicsLayer()
    {
        return new List<PhysicsAttributeOverride>
        {
            new PhysicsAttributeOverride("maxrollvelocity", 5f),
            new PhysicsAttributeOverride("angularacceleration", 35f),
            new PhysicsAttributeOverride("gravity", 10f),
            new PhysicsAttributeOverride("staticfriction", 1.1f),
            new PhysicsAttributeOverride("kineticfriction", 0.7f),
            new PhysicsAttributeOverride("bouncekineticfriction", 0.2f),
            new PhysicsAttributeOverride("maxdotslide", 0.5f),
            new PhysicsAttributeOverride("bouncerestitution", 0.2f),
            new PhysicsAttributeOverride("jumpimpulse", 7.5f)
        };
    }

    private void UpdateWaterPhysics()
    {
        if (!IsInWater() || waterPhysicsLayer == null)
            return;

        WaterPhysicsTrigger trigger =
            WaterPhysicsTrigger.GetClosestTrigger(this);

        if (trigger == null)
            return;

        float depth01 = trigger.GetWaterDepth01(this);

        // PQ updates these two directly while submerged.
        SetPhysicsAttribute(
            "maxrollvelocity",
            depth01 * 10f + 5f
        );

        SetPhysicsAttribute(
            "angularacceleration",
            depth01 * 40f + 35f
        );
    }

    private bool firstRespawn = true;
    private bool isRespawn = false;
    private bool IsInWater() => WaterPhysicsTrigger.IsMarbleInWater(this);

    private void PlayBubbleEndSound()
    {
        if (bubbleEndSound == null) return;
        GameManager.instance.PlayAudioClip(bubbleEndSound);
    }

    // Cannon - Enter
    public void EnterCannon(Cannon cannon)
    {
        if (cannon == null || activeCannon != null) return;

        cannonCharge = 0f;
        activeCannon = cannon;
        canUsePowerupAfterCannon = false;
        cannon.ResetCharge();

        cannonPreviousCanMove = movement.canMove;
        cannonPreviousCanSpin = movement.canSpin;
        cannonPreviousCanJump = movement.canJump;
        cannonBeforeGravity = GravitySystem.GravityStrength;

        movement.SetPosition(cannon.GetBasePosition());
        movement.marbleVelocity = Vector3.zero;
        movement.marbleAngularVelocity = Vector3.zero;

        // Use the same movement-trigger mechanism as NoMovementTrigger.
        // This blocks player input without changing the underlying
        // canMove/canSpin/canJump state.
        if (!cannonMovementTriggerActive)
        {
            movement.EnterMovementTrigger();
            cannonMovementTriggerActive = true;
        }

        cannon.UpdateAim(cannon.yaw * Mathf.Deg2Rad, cannon.pitch * Mathf.Deg2Rad);
        cannon.HideAimVisualization();

        if (CameraController.instance != null)
        {
            CameraController.instance.SetCannonCamera(cannon, cannon.lockCam);
        }

        GameUIManager.instance.ShowCannonMenu(true);
        GameUIManager.instance.SetPowerupLocked(true);
    }

    // Cannon - Firing
    private void UpdateCannonFiring()
    {
        Cannon cannon = activeCannon;
        if (cannon == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            cannon.HideAimVisualization();
            CancelCannon();

            return;
        }

        movement.SetPosition(cannon.GetMarblePosition());

        if (cannon.instant)
        {
            if (Time.time >= instantCannonFireTime)
            {
                FireCannon(cannon, cannon.ComputeFireDirection(), 1f);
                instantCannonFireTime = Mathf.NegativeInfinity;
            }
            return;
        }

        if (cannon.useCharge)
        {
            if (IsCannonFireHeld())
            {
                StartCannonChargeAudio();
                UpdateCannonChargeAudio();

                cannonCharge = Mathf.Min(cannonCharge + Time.deltaTime, cannon.chargeTime);
                float fraction = cannon.chargeTime > 0f ? cannonCharge / cannon.chargeTime : 1f;

                GameUIManager.instance.CannonCharge(Mathf.Clamp(Mathf.RoundToInt(10f * fraction), 1, 10));
                cannon.UpdateAimVisualization(cannon.lastYaw, cannon.lastPitch, fraction);
            }
            else
            {
                StopCannonChargeAudio();
                cannon.HideAimVisualization();

                if (cannonCharge < cannon.minimumChargeTime)
                {
                    cannonCharge = 0f;
                    GameUIManager.instance.CannonCharge(0);
                    return;
                }

                float forceFraction = cannon.chargeTime > 0f ? cannonCharge / cannon.chargeTime : 1f;
                FireCannon(cannon, cannon.ComputeFireDirection(), forceFraction);
            }
            return;
        }

        if (IsCannonFireHeld())
        {
            FireCannon(cannon, cannon.ComputeFireDirection(), 1f);
        }
    }

    private bool IsCannonFireHeld()
    {
        return !GameManager.isPaused && !GameManager.gameFinish &&
               !ReplayRecorder.loadReplay && Input.GetKey(ControlBinding.instance.usePowerup);
    }

    private void FireCannon(Cannon cannon, Vector3 fireDirection, float forceFraction)
    {
        if (cannon == null) 
            return;

        cannonCharge = 0f;
        float launchForce = cannon.force * (cannon.useCharge ? Mathf.Clamp01(forceFraction) : 1f);
        Vector3 launchPosition = cannon.GetMarblePosition();

        LeaveCannonInternal();

        movement.SetPosition(launchPosition);
        movement.marbleVelocity = fireDirection.normalized * launchForce;

        // Keep player movement locked after launch for lockTime.
        // The same movement-trigger lock used while inside the cannon
        // remains active until this coroutine releases it.
        StartCannonMovementLock(cannon);
        movement.marbleAngularVelocity = Vector3.zero;

        foreach (Collider c in cannon.GetComponentsInChildren<Collider>()) c.enabled = false;

        cannon.Explode();

        DG.Tweening.DOVirtual.DelayedCall(1f, () =>
        {
            cannon.ResetCannon();
            foreach (Collider c in cannon.GetComponentsInChildren<Collider>()) 
                c.enabled = true;

            canUsePowerupAfterCannon = true;
        }, false);

        GameUIManager.instance.SetPowerupLocked(false);
        GameUIManager.instance.ShowCannonMenu(false);
    }

    // Cannon - Controls & Locks
    private void LockCannonControls(Cannon cannon)
    {
        if (cannon == null)
            return;

        float lockTime =
            cannon.lockTime > 0f
                ? cannon.lockTime
                : 0.3f;

        cannonControlLockUntil =
            Time.time + lockTime;

        cannonControlLockActive = true;

        if (cannon.lockCam)
        {
            cannonCameraLockUntil =
                Time.time + lockTime;
        }

        if (!cannonMovementTriggerActive)
        {
            movement.EnterMovementTrigger();
            cannonMovementTriggerActive = true;
        }
    }

    private void StartCannonMovementLock(Cannon cannon)
    {
        if (cannon == null)
            return;

        // The movement trigger is already active from EnterCannon().
        // If this was not the case, create it here.
        if (!cannonMovementTriggerActive)
        {
            movement.EnterMovementTrigger();
            cannonMovementTriggerActive = true;
        }

        float lockTime =
            cannon.lockTime > 0f
                ? cannon.lockTime
                : 0.3f;

        cannonControlLockUntil =
            Time.time + lockTime;

        cannonControlLockActive = true;

        if (cannon.lockCam)
        {
            cannonCameraLockUntil =
                Time.time + lockTime;
        }
    }

    private void UpdateCannonLocks()
    {
        if (
            cannonControlLockActive &&
            Time.time >= cannonControlLockUntil
        )
        {
            cannonControlLockActive = false;
            cannonControlLockUntil =
                Mathf.NegativeInfinity;

            if (
                activeCannon == null &&
                cannonMovementTriggerActive
            )
            {
                movement.ExitMovementTrigger();
                cannonMovementTriggerActive = false;
            }
        }

        if (Time.time >= cannonCameraLockUntil)
        {
            cannonCameraLockUntil =
                Mathf.NegativeInfinity;
        }
    }

    public bool CannonCameraLocked() =>
        Time.time < cannonCameraLockUntil;

    // Cannon - Leave / Cancel
    private void LeaveCannonInternal()
    {
        Cannon cannon = activeCannon;

        if (cannon == null) 
            return;

        cannon.HideAimVisualization();

        activeCannon = null;
        lastCannon = cannon;
        cannonReenableTime = Time.time + 0.2f;

        CameraController.instance?.ExitCannonCamera(false);
    }

    public void CancelCannon()
    {
        Cannon cannon = activeCannon;
        if (cannon == null) return;

        StopCannonChargeAudio();
        Vector3 exitPosition = cannon.GetExitPosition();

        canUsePowerupAfterCannon = true;
        LeaveCannonInternal();

        movement.SetPosition(exitPosition);
        movement.marbleVelocity = Vector3.zero;
        movement.marbleAngularVelocity = Vector3.zero;

        if (cannonMovementTriggerActive)
        {
            movement.ExitMovementTrigger();
            cannonMovementTriggerActive = false;
        }

        cannonCharge = 0f;
        instantCannonFireTime = Mathf.NegativeInfinity;
        cannonControlLockActive = false;
        cannonControlLockUntil = Mathf.NegativeInfinity;
        cannonCameraLockUntil = Mathf.NegativeInfinity;

        GameUIManager.instance.SetPowerupLocked(false);
        GameUIManager.instance.ShowCannonMenu(false);
    }

    private void ResetCannonState()
    {
        if (cannonMovementTriggerActive)
        {
            movement.ExitMovementTrigger();
            cannonMovementTriggerActive = false;
        }

        activeCannon = null;
        lastCannon = null;
        cannonCharge = 0f;
        cannonControlLockUntil = Mathf.NegativeInfinity;
        cannonControlLockActive = false;
        cannonCameraLockUntil = Mathf.NegativeInfinity;
        cannonReenableTime = Mathf.NegativeInfinity;
        instantCannonFireTime = Mathf.NegativeInfinity;
    }

    // ============================================================
    // Ice Shard
    // ============================================================

    public void Freeze(IceShard shard)
    {
        if (isFrozen)
            return;

        isFrozen = true;
        iceShard = shard;
        lastFreezeTime = Time.time;

        movement.StopAllMovement();
        movement.StopMoving();

        if (frozenIce != null)
            frozenIce.SetActive(true);

        if (GameUIManager.instance != null)
            GameUIManager.instance.SetPowerupLocked(true);

        if (iceShard != null && GameManager.gameStart)
            iceShard.PlayFreezeSound(this);
    }

    public void Unfreeze(bool cancel = false)
    {
        if (!isFrozen)
            return;

        isFrozen = false;

        if (cancel)
        {
            iceShard = null;
            return;
        }

        movement.StartMoving();

        Vector3 away =
            transform.position -
            (iceShard != null
                ? iceShard.transform.position
                : transform.position);

        if (away.sqrMagnitude > 0.0001f)
            away.Normalize();
        else
            away = Vector3.right;

        movement.marbleVelocity +=
            away * 3f +
            transform.up * 5f;

        if (frozenIce != null)
            frozenIce.SetActive(false);

        if (GameUIManager.instance != null)
            GameUIManager.instance.SetPowerupLocked(false);

        if (iceShard != null && GameManager.gameStart)
            iceShard.PlayCrackSound(this);

        iceShard = null;
    }

    /// <summary>
    /// Completely clears the Ice Shard freeze state without performing
    /// the normal unfreeze launch/velocity effect.
    /// Used when the marble respawns.
    /// </summary>
    private IEnumerator ResetFreezeState()
    {
        for (int i = 0; i < 200; i ++)
        {
            isFrozen = false;
            iceShard = null;
            lastFreezeTime = Mathf.NegativeInfinity;
            frozenIce.SetActive(false);
            GameUIManager.instance.SetPowerupLocked(false);

            if (GameManager.instance.useCheckpoint)
                movement.StartMoving();
            else if (!GameManager.gameStart)
                movement.StopAllbutJumping();
            else
                movement.StartMoving();

            yield return null;
        }

        if (GameManager.instance.useCheckpoint)
            movement.StartMoving();
        else if (!GameManager.gameStart)
            movement.StopAllbutJumping();
        else
            movement.StartMoving();
    }

    // Powerup-use trigger locking
    public void LockPowerupUse()
    {
        powerupUseLockCount++;

        if (GameUIManager.instance != null)
            GameUIManager.instance.SetPowerupLocked(true);
    }

    public void UnlockPowerupUse()
    {
        powerupUseLockCount =
            Mathf.Max(0, powerupUseLockCount - 1);

        if (powerupUseLockCount == 0 &&
            GameUIManager.instance != null)
        {
            GameUIManager.instance.SetPowerupLocked(false);
        }
    }

    public bool IsPowerupUseLocked =>
        powerupUseLockCount > 0;

    // Powerups & Utilities
    public void UsePowerup()
    {
        PowerupType powerUp = GameManager.instance.activePowerup;

        if (powerUp == PowerupType.Teleporter || powerUp == PowerupType.Transporter)
        {
            if (Teleporter.activeTeleporter != null)
                Teleporter.activeTeleporter.UseTeleporter();

            return;
        }

        GameManager.instance.ConsumePowerup();

        if (powerUp == PowerupType.SuperJump)
            SuperJump.onUseSuperJump?.Invoke();

        if (powerUp == PowerupType.Anvil)
            Anvil.onUseAnvil?.Invoke();

        if (powerUp == PowerupType.SuperSpeed)
            SuperSpeed.onUseSuperSpeed?.Invoke();

        if (powerUp == PowerupType.ShockAbsorber)
            ShockAbsorber.onUseShockAbsorber?.Invoke();

        if (powerUp == PowerupType.SuperBounce)
            SuperBounce.onUseSuperBounce?.Invoke();

        if (powerUp == PowerupType.Gyrocopter)
            Gyrocopter.onUseGyrocopter?.Invoke();
    }

    public void Respawn()
    {
        // Completely cancel any existing Ice Shard freeze.
        // This must happen before anything else in the respawn process.
        StartCoroutine(ResetFreezeState());

        // The first initialization/respawn is not considered a normal
        // player-triggered respawn for water-splash suppression.
        isRespawn = !firstRespawn;
        firstRespawn = false;

        SetBubbleParticle(false);
        lastWaterTrigger = null;

        DeactivateBubble();
        DeactivateFireball();

        if (!GameManager.instance.useCheckpoint)
        {
            bubbleTime = 0f;
            bubbleInfinite = false;
            GameUIManager.instance.SetBubbleTimer(
                -1,
                bubbleTotalTime
            );

            fireballTime = 0f;
            GameUIManager.instance.SetFireballTimer(
                -1,
                fireballTotalTime
            );

            GameManager.instance.activePowerup =
                PowerupType.None;
        }

        ResetCannonState();

        gyrocopterNextTransitionTime =
            Mathf.NegativeInfinity;

        powerupUseLockCount = 0;

        GameUIManager.instance.SetPowerupLocked(false);
        GameUIManager.instance.ShowCannonMenu(false);

        ClearPhysicsLayers();
        PhysModTrigger.ForgetAllMarbleLayers(this);

        wasInWater = false;
        lastWaterTrigger = null;
        waterPhysicsLayer = null;
        bubblePhysicsLayer = null;

        movement.SetPosition(
            GameManager.instance.activeCheckpoint.position
        );

        CameraController.instance?.ResetCam();

        PhysModTrigger.RefreshAllTriggers(this);
        WaterPhysicsTrigger.RefreshMarbleWaterState(this);

        // Trigger refresh must not be allowed to leave the marble frozen.
        StartCoroutine(ResetFreezeState());
    }

    public void PlaySound(PowerupType _powerup)
    {
        if (_powerup == PowerupType.ShockAbsorber) useShockAbsorberSound.Play();
        else if (_powerup == PowerupType.SuperBounce) useSuperBounceSound.Play();
        else if (_powerup == PowerupType.Gyrocopter) gyroSound.Play();
        else if (_powerup == PowerupType.TimeTravel) TTActiveSound.Play();
    }

    public void StopSound(PowerupType _powerup)
    {
        if (_powerup == PowerupType.ShockAbsorber) useShockAbsorberSound.Stop();
        else if (_powerup == PowerupType.SuperBounce) useSuperBounceSound.Stop();
        else if (_powerup == PowerupType.Gyrocopter) gyroSound.Stop();
        else if (_powerup == PowerupType.TimeTravel) TTActiveSound.Stop();
    }

    public void PlayBounceSound(float volume)
    {
        if (GameManager.gameFinish) return;
        audioSource.volume = volume * PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);
        audioSource.PlayOneShot(bounceSfx[Random.Range(0, bounceSfx.Length)]);
    }

    public void ToggleGlowBounce(bool _toggle) => glowBounce.SetActive(_toggle);

    public void ToggleGyrocopterBlades(bool _toggle) => gyrocopterBlades.SetActive(_toggle);

    public void RevertMaterial()
    {
        ToggleGlowBounce(false);

        if (GameManager.instance.superBounceIsActive) StopSound(PowerupType.SuperBounce);
        else if (GameManager.instance.shockAbsorberIsActive) StopSound(PowerupType.ShockAbsorber);

        GameManager.instance.superBounceIsActive = false;
        GameManager.instance.shockAbsorberIsActive = false;

        if (GameManager.instance.shockAbsorberIsActive) movement.bounceRestitution = 0.01f;
        else if (GameManager.instance.superBounceIsActive) movement.bounceRestitution = 0.9f;
        else movement.bounceRestitution = 0.5f;
    }

    public void UseSuperBounce()
    {
        if (GameManager.instance.shockAbsorberIsActive) GameManager.instance.shockAbsorberIsActive = false;
        ToggleGlowBounce(true);

        if (!GameManager.instance.superBounceIsActive)
        {
            GameManager.instance.superBounceIsActive = true;
            movement.bounceRestitution = 0.9f;
        }
    }

    public void UseShockAbsorber()
    {
        if (GameManager.instance.superBounceIsActive) GameManager.instance.superBounceIsActive = false;
        ToggleGlowBounce(true);

        if (!GameManager.instance.shockAbsorberIsActive)
        {
            GameManager.instance.shockAbsorberIsActive = true;
            movement.bounceRestitution = 0.01f;
        }
    }

    public void UseGyrocopter()
    {
        if (movement == null || GameManager.instance == null)
            return;

        // Already active: duplicate calls do nothing.
        if (GameManager.instance.gyrocopterIsActive)
            return;

        if (Time.time < gyrocopterNextTransitionTime)
            return;

        GameManager.instance.gyrocopterIsActive = true;
        gyrocopterNextTransitionTime =
            Time.time + Mathf.Max(0f, gyrocopterTransitionCooldown);

        ToggleGyrocopterBlades(true);
        PlaySound(PowerupType.Gyrocopter);

        movement.gravity = GravitySystem.GravityStrength / 4f;
    }

    public void CancelGyrocopter()
    {
        if (movement == null || GameManager.instance == null)
            return;

        // Already inactive: duplicate calls do nothing.
        if (!GameManager.instance.gyrocopterIsActive)
            return;

        if (Time.time < gyrocopterNextTransitionTime)
            return;

        GameManager.instance.gyrocopterIsActive = false;
        gyrocopterNextTransitionTime =
            Time.time + Mathf.Max(0f, gyrocopterTransitionCooldown);

        ToggleGyrocopterBlades(false);
        StopSound(PowerupType.Gyrocopter);

        movement.gravity = GravitySystem.GravityStrength;
    }

    public void ActivateTimeTravel(float _timeBonus)
    {
        if (_timeBonus > 0)
        {
            PlaySound(PowerupType.TimeTravel);

            if (!GameManager.instance.timeTravelActive)
            {
                GameManager.instance.timeTravelStartTime = Time.time;
                GameManager.instance.timeTravelActive = true;
            }
            GameManager.instance.timeTravelBonus += _timeBonus;
        }
        else
        {
            if (GameManager.instance.timeTravelActive)
            {
                float elapsed = Time.time - GameManager.instance.timeTravelStartTime;
                float remainingTime = GameManager.instance.timeTravelBonus - elapsed;
                float penalty = -_timeBonus;

                if (penalty >= remainingTime)
                {
                    float leftoverPenalty = penalty - remainingTime;
                    GameManager.instance.elapsedTime += leftoverPenalty * 1000f;
                    GameManager.instance.timeTravelBonus = 0;
                    GameManager.instance.timeTravelActive = false;

                    if (!GameManager.gameFinish) GameUIManager.instance.SetTimeTravelTimer(-1);
                    InactivateTimeTravel();
                }
                else
                {
                    GameManager.instance.timeTravelBonus -= penalty;
                }
            }
            else
            {
                GameManager.instance.elapsedTime += (-_timeBonus) * 1000f;
                InactivateTimeTravel();
            }
        }
    }

    public void InactivateTimeTravel()
    {
        if (GameManager.gameFinish)
        {
            float elapsed = Time.time - GameManager.instance.timeTravelStartTime;
            float remainingTime = GameManager.instance.timeTravelBonus - elapsed;
            GameUIManager.instance.SetTimeTravelTimer(remainingTime * 1000f, true);
            Marble.instance.alarmSound.Stop();
        }

        GameManager.instance.timeTravelBonus = 0f;
        GameManager.instance.timeTravelActive = false;
        StopSound(PowerupType.TimeTravel);
    }

    public void BounceEmitter(float _speed, CollisionInfo _collisionInfo)
    {
        if (GameManager.gameFinish) return;

        if (_speed > 3)
        {
            var effect = Instantiate(bounceParticle);
            effect.transform.position = transform.position;
            effect.transform.up = _collisionInfo.normal.normalized;
            effect.transform.parent = _collisionInfo.collider.transform;
            effect.transform.localScale = Vector3.one;

            Destroy(effect.gameObject, effect.GetComponent<ParticleSystem>().main.duration + 1f);
        }
    }

    public void BounceEmitter(float speed, Vector3 point, Vector3 normal)
    {
        if (GameManager.gameFinish) return;

        if (speed > 3)
        {
            var effect = Instantiate(bounceParticle);
            effect.transform.position = point;
            effect.transform.up = normal.normalized;
            effect.transform.localScale = Vector3.one;

            Destroy(effect.gameObject, effect.GetComponent<ParticleSystem>().main.duration + 1f);
        }
    }

    private void StartCannonChargeAudio()
    {
        if (cannonChargeAudioStarted) return;
        cannonChargeAudioStarted = true;
        cannonChargeStartAudio.Play();
    }

    private void UpdateCannonChargeAudio()
    {
        if (!cannonChargeAudioStarted) return;

        if (!cannonChargeStartAudio.isPlaying && !cannonChargeLoopAudio.isPlaying)
        {
            cannonChargeLoopAudio.Play();
        }
    }

    private void StopCannonChargeAudio()
    {
        cannonChargeStartAudio.Stop();
        cannonChargeLoopAudio.Stop();
        cannonChargeAudioStarted = false;
    }

    // Fireball
    public void ActivateFireball(float time)
    {
        fireball = true;
        fireballTime = time;
        fireballStartTime = Time.time;
        fireballTotalTime = time;

        if (fireballParticle != null)
            fireballParticle.SetActive(true);

        if (isUsingBubble)
        {
            GameUIManager.instance.SetBubbleTimer(-1, bubbleTotalTime);
            DeactivateBubble();
        }
    }

    public void DeactivateFireball()
    {
        if (blastParticle)
        {
            Destroy(blastParticle.gameObject);
            blastParticle = null;
        }

        fireball = false;
        fireballTime = 0f;
        fireballStartTime = 0f;

        if (fireballParticle != null)
            fireballParticle.SetActive(false);
    }

    private void UpdateFireball()
    {
        if (!fireball) return;

        if (blastParticle != null)
            blastParticle.transform.position = transform.position;

        GameUIManager.instance.SetFireballTimer(GetFireballTime(), fireballTotalTime);

        if (GetFireballTime() <= 0f)
        {
            GameUIManager.instance.SetFireballTimer(-1, fireballTotalTime);
            DeactivateFireball();
        }
    }

    public void HitIceShardWithFireball(IceShard shard)
    {
        if (!fireball) return;

        shard.DestroyByFireball();
        fireballTime -= 0.5f;

        if (GetFireballTime() <= 0f)
        {
            GameUIManager.instance.SetFireballTimer(-1, fireballTotalTime);
            DeactivateFireball();
        }
    }

    public bool canBlast => (Time.time > nextFireBlastTime && GetFireballTime() > 1f);
    GameObject blastParticle;
    public void FireBlast()
    {
        if (!fireball) return;

        float remaining = GetFireballTime();
        if (remaining < 1f || Time.time < nextFireBlastTime) return;

        nextFireBlastTime = Time.time + 2f;

        float blastAmount = remaining / fireballTime;
        float impulseStrength = (blastAmount > 1f ? blastAmount : Mathf.Sqrt(blastAmount)) * 10f;

        movement.marbleVelocity += -GravitySystem.GravityDir.normalized * impulseStrength;

        blastParticle = Instantiate(fireballBlastParticle, transform.position, Quaternion.identity);
        GameManager.instance.PlayFireballBlastSfx();
        Destroy(blastParticle.gameObject, 1f);

        float radius = blastAmount * 1.5f + 1.5f;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hits)
        {
            IceShard shard = hit.GetComponentInParent<IceShard>();
            if (shard == null) continue;
            shard.DestroyByFireball();
        }
    }

    public void EnterWater()
    {
        if (fireball)
        {
            GameUIManager.instance.SetFireballTimer(-1, fireballTotalTime);
            GameManager.instance.PlayAudioClip(fireExtinguishSound);
            DeactivateFireball();
        }
    }

    public void SavePowerupCheckpoint()
    {
        if (isUsingBubble)
        {
            checkpointHadBubble = true;
            checkpointHadFireball = false;
            checkpointBubbleInfinite = bubbleInfinite;
            checkpointPowerupTime = bubbleTime;
            checkpointPowerupTotalTime = bubbleTotalTime;
        }
        else if (fireball)
        {
            checkpointHadBubble = false;
            checkpointHadFireball = true;
            checkpointBubbleInfinite = false;
            checkpointPowerupTime = GetFireballTime();
            checkpointPowerupTotalTime = fireballTotalTime;
        }
        else
        {
            checkpointHadBubble = false;
            checkpointHadFireball = false;
            checkpointBubbleInfinite = false;
            checkpointPowerupTime = 0f;
            checkpointPowerupTotalTime = 0f;
        }
    }

    public void RestorePowerupCheckpoint()
    {
        if (checkpointHadBubble)
        {
            DeactivateFireball();
            bubbleTime = checkpointPowerupTime;
            bubbleTotalTime = checkpointPowerupTotalTime;
            bubbleInfinite = checkpointBubbleInfinite;
            isUsingBubble = false;
        }
        else if (checkpointHadFireball)
        {
            DeactivateBubble();
            ActivateFireball(checkpointPowerupTime);
        }
        else
        {
            DeactivateBubble();
            DeactivateFireball();
            bubbleTime = 0f;
            fireballTime = 0f;
        }
    }

    public void CapturePhysicsBaseline()
    {
        CapturePhysicsAttributeBaseline();
    }

    public List<PhysicsAttributeOverride> PushPhysicsLayer(
        List<PhysicsAttributeOverride> overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return null;

        physicsLayers.Add(overrides);

        foreach (var overrideValue in overrides)
        {
            if (overrideValue == null ||
                string.IsNullOrEmpty(overrideValue.attribute))
            {
                continue;
            }

            SetPhysicsAttribute(
                overrideValue.attribute.ToLowerInvariant(),
                overrideValue.value
            );
        }

        return overrides;
    }

    public void PopPhysicsLayer(List<PhysicsAttributeOverride> layer)
    {
        if (layer == null)
            return;

        if (!physicsLayers.Remove(layer))
            return;

        RecalculatePhysicsLayers();
    }

    private void RecalculatePhysicsLayers()
    {
        // Start from the original values.
        foreach (var pair in physicsBaseValues)
            SetPhysicsAttribute(pair.Key, pair.Value);

        // Apply layers in order.
        //
        // This means the most recently entered PhysMod wins when
        // multiple PhysMods modify the same attribute.
        foreach (var layer in physicsLayers)
        {
            if (layer == null)
                continue;

            foreach (var overrideValue in layer)
            {
                if (overrideValue == null ||
                    string.IsNullOrEmpty(overrideValue.attribute))
                    continue;

                SetPhysicsAttribute(
                    overrideValue.attribute.ToLowerInvariant(),
                    overrideValue.value
                );
            }
        }
    }

    private bool TryGetPhysicsAttribute(
        string attribute,
        out float value)
    {
        value = 0f;

        if (movement == null)
            return false;

        switch (attribute)
        {
            case "maxrollvelocity":
                value = movement.maxRollVelocity;
                return true;

            case "angularacceleration":
                value = movement.angularAcceleration;
                return true;

            case "brakingacceleration":
                value = movement.brakingAcceleration;
                return true;

            case "airacceleration":
                value = movement.airAcceleration;
                return true;

            case "gravity":
                value = movement.gravity;
                return true;

            case "staticfriction":
                value = movement.staticFriction;
                return true;

            case "kineticfriction":
                value = movement.kineticFriction;
                return true;

            case "bouncekineticfriction":
                value = movement.bounceKineticFriction;
                return true;

            case "maxdotslide":
                value = movement.maxDotSlide;
                return true;

            case "jumpimpulse":
                value = movement.jumpImpulse;
                return true;

            case "maxforceradius":
                value = movement.maxForceRadius;
                return true;

            case "minbouncevel":
                value = movement.minBounceVel;
                return true;

            case "bouncerestitution":
                value = movement.bounceRestitution;
                return true;

            case "bounce":
                value = movement.bounce;
                return true;

            case "trailspeedthreshold":
                value = trailSpeedThreshold;
                return true;

            case "timescale":
                value = physicsTimeScale;
                return true;
            case "cameraspeedmultiplier":
                value = cameraSpeedMultiplier;
                return true;
        }

        return false;
    }

    private bool SetPhysicsAttribute(
        string attribute,
        float value)
    {
        if (movement == null)
            return false;

        switch (attribute)
        {
            case "maxrollvelocity":
                movement.maxRollVelocity = value;
                return true;

            case "angularacceleration":
                movement.angularAcceleration = value;
                return true;

            case "brakingacceleration":
                movement.brakingAcceleration = value;
                return true;

            case "airacceleration":
                movement.airAcceleration = value;
                return true;

            case "gravity":
                movement.gravity = value;
                return true;

            case "staticfriction":
                movement.staticFriction = value;
                return true;

            case "kineticfriction":
                movement.kineticFriction = value;
                return true;

            case "bouncekineticfriction":
                movement.bounceKineticFriction = value;
                return true;

            case "maxdotslide":
                movement.maxDotSlide = value;
                return true;

            case "jumpimpulse":
                movement.jumpImpulse = value;
                return true;

            case "maxforceradius":
                movement.maxForceRadius = value;
                return true;

            case "minbouncevel":
                movement.minBounceVel = value;
                return true;

            case "bouncerestitution":
                movement.bounceRestitution = value;
                return true;

            case "bounce":
                movement.bounce = value;
                return true;

            case "trailspeedthreshold":
                trailSpeedThreshold = value;
                return true;

            case "timescale":
                physicsTimeScale = Mathf.Max(0f, value);
                return true;
            case "cameraspeedmultiplier":
                cameraSpeedMultiplier = Mathf.Max(0f, value);
                return true;
        }

        Debug.LogWarning(
            $"Unknown PhysMod attribute '{attribute}'."
        );

        return false;
    }

    public void ClearPhysicsLayers()
    {
        physicsLayers.Clear();

        foreach (var pair in physicsBaseValues)
            SetPhysicsAttribute(pair.Key, pair.Value);

        waterPhysicsLayer = null;
        bubblePhysicsLayer = null;
    }

    private void CapturePhysicsAttributeBaseline()
    {
        physicsBaseValues.Clear();

        string[] attributes =
        {
            "maxrollvelocity",
            "angularacceleration",
            "brakingacceleration",
            "airacceleration",
            "gravity",
            "staticfriction",
            "kineticfriction",
            "bouncekineticfriction",
            "maxdotslide",
            "jumpimpulse",
            "maxforceradius",
            "minbouncevel",
            "bouncerestitution",
            "bounce",
            "trailspeedthreshold",
            "timescale"
        };

        foreach (string attribute in attributes)
        {
            if (TryGetPhysicsAttribute(attribute, out float value))
                physicsBaseValues[attribute] = value;
        }
    }

}