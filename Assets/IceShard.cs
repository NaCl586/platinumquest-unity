using PlatinumQuestScripts;
using System.Collections;
using UnityEngine;

public class IceShard : MonoBehaviour
{
    public const float FREEZE_TIME = 2f;
    public const float INVULN_TIME = 1f;

    [Header("Ice Shard")]
    [SerializeField]
    private string skin = "";

    [Header("Effects")]
    [SerializeField]
    private ParticleSystem mistParticle;

    [SerializeField]
    private ParticleSystem shineParticle;

    [Header("Audio")]
    [SerializeField]
    private AudioClip freezeSound;

    [SerializeField]
    private AudioClip crackSound;

    [SerializeField]
    private AudioClip destroyedSound;

    public int Points { get; private set; }

    public Color messageColor;
    public string message;

    // Controls whether the ambient particles are allowed to show.
    public bool noParticles = false;

    private bool initialized;

    // ============================================================
    // Initialization
    // ============================================================

    public void SetSkin(string value)
    {
        Debug.Log(value);

        skin = value;
        Points = 0;
        message = "";
        messageColor = Color.white;

        if (string.IsNullOrEmpty(value))
        {
            UpdateAmbientEffects();
            initialized = true;
            return;
        }

        switch (value.ToLowerInvariant())
        {
            case "red":
                Points = 1;
                message = "+1";
                messageColor =
                    new Color32(
                        255,
                        102,
                        102,
                        255
                    );
                break;

            case "yellow":
                Points = 2;
                message = "+2";
                messageColor =
                    new Color32(
                        255,
                        255,
                        102,
                        255
                    );
                break;

            case "blue":
                Points = 5;
                message = "+5";
                messageColor =
                    new Color32(
                        102,
                        102,
                        255,
                        255
                    );
                break;

            case "platinum":
                Points = 10;
                message = "+10";
                messageColor =
                    new Color32(
                        221,
                        221,
                        221,
                        255
                    );
                break;
        }

        UpdateAmbientEffects();
        initialized = true;
    }

    private void Start()
    {
        UpdateAmbientEffects();
    }

    // ============================================================
    // Collision
    // ============================================================

    public void HandleCollision(Collider collider)
    {
        if (collider == null)
            return;

        Marble marble = collider.GetComponent<Marble>();

        if (marble == null)
            return;

        // --------------------------------------------------------
        // Fireball
        // --------------------------------------------------------

        if (marble.fireball)
        {
            marble.HitIceShardWithFireball(this);

            // The special UnseasonablyCold behavior should also
            // receive the contact.
            goToTargetHandler?.ProcessIceShardContact(
                this,
                marble
            );

            return;
        }

        // --------------------------------------------------------
        // Already frozen
        // --------------------------------------------------------

        if (marble.isFrozen)
            return;

        // --------------------------------------------------------
        // Freeze invulnerability
        // --------------------------------------------------------

        if (marble.LastFreezeTime +
            FREEZE_TIME +
            INVULN_TIME >=
            Time.time)
        {
            return;
        }

        // --------------------------------------------------------
        // Normal Ice Shard behavior
        // --------------------------------------------------------

        marble.Freeze(this);

        // --------------------------------------------------------
        // Special mission behavior
        // --------------------------------------------------------

        goToTargetHandler?.ProcessIceShardContact(
            this,
            marble
        );
    }

    // ============================================================
    // Sounds
    // ============================================================

    public void PlayFreezeSound(Marble marble)
    {
        if (freezeSound == null)
            return;

        GameManager.instance.PlayAudioClip(
            freezeSound
        );
    }

    public void PlayCrackSound(Marble marble)
    {
        if (crackSound == null)
            return;

        GameManager.instance.PlayAudioClip(
            crackSound
        );
    }

    // ============================================================
    // Effects
    // ============================================================

    private void UpdateAmbientEffects()
    {
        // Ambient effects are shown only when:
        //
        // 1. This is a regular Ice Shard (Points == 0)
        // 2. Particles are enabled
        //
        // Point Ice Shards never show ambient effects.
        bool showAmbientEffects =
            Points == 0 &&
            !noParticles;

        SetParticleState(
            mistParticle,
            showAmbientEffects
        );

        SetParticleState(
            shineParticle,
            showAmbientEffects
        );
    }

    public void SetParticles(bool value)
    {
        noParticles = !value;
        UpdateAmbientEffects();
    }

    private void SetParticleState(
        ParticleSystem particle,
        bool enabled)
    {
        if (particle == null)
            return;

        if (enabled)
        {
            if (!particle.isPlaying)
                particle.Play();
        }
        else
        {
            if (particle.isPlaying)
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear
                );
            }
        }
    }

    // ============================================================
    // Fireball
    // ============================================================

    public void DestroyByFireball()
    {
        GameManager.instance.PlayAudioClip(
            destroyedSound
        );

        StartCoroutine(
            DelayBeforeDestroy()
        );
    }

    private IEnumerator DelayBeforeDestroy()
    {
        yield return new WaitForSeconds(0.02f);

        ArcticInfernoMode mode =
            GameManager.instance.specialGameMode
                as ArcticInfernoMode;

        if (mode != null)
            mode.OnIceShardDestroyed(this);

        gameObject.SetActive(false);
    }

    // ============================================================
    // Reset
    // ============================================================

    public void ResetShard()
    {
        gameObject.SetActive(true);

        UpdateAmbientEffects();
    }

    // ============================================================
    // Unseasonably Cold / GoToTarget
    // ============================================================

    private PlatinumQuestScripts.UnseasonablyColdMode
        goToTargetHandler;

    public bool GoToTargetTriggered
    {
        get;
        private set;
    }

    public void SetGoToTargetHandler(
        PlatinumQuestScripts.UnseasonablyColdMode handler)
    {
        goToTargetHandler = handler;
    }

    public void SetGoToTargetTriggered(
        bool value)
    {
        GoToTargetTriggered = value;
    }

    public void ResetGoToTarget()
    {
        GoToTargetTriggered = false;
    }
}