using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadePlatform : MonoBehaviour
{
    public enum Functionality
    {
        Trapdoor,
        Fading,
        Periodic,
    }

    public enum FadeStyle
    {
        Cloak,
        Fade,
    }

    private const float FADING_CONTACT_COOLDOWN = 0.25f;
    private const float CLOAK_RATE = 2.0f;

    [Header("Fade Platform Settings")]
    [SerializeField]
    private Functionality functionality = Functionality.Trapdoor;

    public bool RequiresMarbleCollisionDetection
    {
        get
        {
            return functionality == Functionality.Fading
                || functionality == Functionality.Trapdoor;
        }
    }

    [SerializeField]
    private FadeStyle fadeStyle = FadeStyle.Cloak;

    [SerializeField]
    private float fadeInTime = 0.5f;

    [SerializeField]
    private float fadeOutTime = 0.5f;

    [SerializeField]
    private float visibleTime = 0.5f;

    [SerializeField]
    private float invisibleTime = 0.5f;

    [SerializeField]
    private float startOffset = 0f;

    [SerializeField]
    private bool permanent;

    [SerializeField]
    private int fadingLevel = 1;

    [SerializeField]
    private int fadingState;
    public int FadingState => fadingState;

    [Header("Fading Collision")]
    [Tooltip(
        "How long the collider remains enabled after a Fading platform reaches zero opacity. " +
        "0 = immediately. Infinity = never disable."
    )]
    [SerializeField]
    private float colliderDisableDelay = 0f;

    private int fadingInitialState;

    private float totalTime;

    private float lastContactTime = -Mathf.Infinity;
    private float lastFadingContactTime = -Mathf.Infinity;

    // ============================================================
    // Fading collision delay
    // ============================================================

    private bool fadingCollisionDelayActive;
    private float fadingCollisionDisableTime;

    // ============================================================
    // Child components
    // ============================================================

    private Renderer[] childRenderers;
    private Collider[] childColliders;

    // ============================================================
    // Texture cache
    // ============================================================

    private Texture[][] originalTextures;
    private bool originalTexturesCached;

    private Texture whiteTexture;

    private bool isTextureWhite;
    private float currentOpacity = 1f;
    public float CurrentOpacity => currentOpacity;

    private bool initialized;

    // ============================================================
    // Unity
    // ============================================================

    private void Awake()
    {
        CacheChildComponents();

        whiteTexture = GameManager.instance.whiteTexture;
    }

    private void Update()
    {
        if (!initialized)
            return;

        /*
         * IMPORTANT:
         *
         * Keep this in Update().
         *
         * SkinSwapper applies the skin before this first Update,
         * so the cache contains the actual skin texture.
         */
        CacheOriginalTextures();

        float currentAttemptTime =
            GetCurrentAttemptTime();

        switch (functionality)
        {
            case Functionality.Fading:
                UpdateFading();
                break;

            case Functionality.Periodic:
                UpdatePeriodic(currentAttemptTime);
                break;

            case Functionality.Trapdoor:
                UpdateTrapdoor(currentAttemptTime);
                break;
        }
    }

    // ============================================================
    // Initialization
    // ============================================================

    public void Initialize(
        string functionality,
        string fadeStyle,
        float fadeInTime,
        float fadeOutTime,
        float visibleTime,
        float invisibleTime,
        float startOffset,
        bool permanent,
        int fadingLevel,
        int fadingState
    )
    {
        this.functionality =
            ParseFunctionality(functionality);

        if (this.functionality == Functionality.Periodic)
        {
            foreach (
                FadePlatformCollision fp
                in transform.GetComponentsInChildren<FadePlatformCollision>(true)
            )
            {
                fp.enabled = false;
            }
        }

        this.fadeStyle =
            ParseFadeStyle(fadeStyle);

        this.fadeInTime =
            fadeInTime;

        this.fadeOutTime =
            fadeOutTime;

        this.visibleTime =
            Mathf.Clamp(
                visibleTime,
                0.1f,
                120f
            );

        this.invisibleTime =
            Mathf.Clamp(
                invisibleTime,
                0.1f,
                120f
            );

        this.startOffset =
            startOffset;

        this.permanent =
            permanent;

        this.fadingLevel =
            Mathf.Max(
                fadingLevel,
                1
            );

        this.fadingState =
            fadingState;

        /*
         * This is the state that a mission reset should restore.
         */
        this.fadingInitialState =
            fadingState;

        this.totalTime =
            this.fadeOutTime +
            this.invisibleTime +
            this.fadeInTime +
            this.visibleTime;

        this.lastContactTime =
            -Mathf.Infinity;

        this.lastFadingContactTime =
            -Mathf.Infinity;

        /*
         * A new initialization must not inherit an old
         * collider-delay timer.
         */
        ResetFadingCollisionDelay();

        this.isTextureWhite =
            false;

        this.currentOpacity =
            1f;

        /*
         * IMPORTANT:
         *
         * Do NOT clear originalTexturesCached here.
         *
         * The first Update() captures the skin-applied texture.
         * Clearing it during a mission reset would make the next
         * cache potentially capture the wrong texture.
         */

        initialized = true;

        /*
         * Enable/disable the collision detector based on the
         * functionality.
         *
         * Periodic platforms do not respond to marble contact.
         */
        FadePlatformCollision collision =
            GetComponent<FadePlatformCollision>();

        if (collision != null)
        {
            collision.enabled =
                RequiresMarbleCollisionDetection;
        }

        /*
         * Only Periodic/Fade needs to start invisible.
         * This is the existing behavior.
         */
        if (
            this.functionality ==
            Functionality.Periodic &&
            this.fadeStyle !=
            FadeStyle.Cloak
        )
        {
            SetOpacity(0f);
        }
    }

    // ============================================================
    // Child Components
    // ============================================================

    private void CacheChildComponents()
    {
        childRenderers =
            GetComponentsInChildren<Renderer>(true);

        childColliders =
            GetComponentsInChildren<Collider>(true);
    }

    // ============================================================
    // Texture Cache
    // ============================================================

    private void CacheOriginalTextures()
    {
        if (originalTexturesCached)
            return;

        if (
            childRenderers == null ||
            childRenderers.Length == 0
        )
        {
            return;
        }

        originalTextures =
            new Texture[childRenderers.Length][];

        for (
            int i = 0;
            i < childRenderers.Length;
            i++
        )
        {
            Material[] materials =
                childRenderers[i].materials;

            originalTextures[i] =
                new Texture[materials.Length];

            for (
                int j = 0;
                j < materials.Length;
                j++
            )
            {
                Material material =
                    materials[j];

                if (
                    material != null &&
                    material.HasProperty("_MainTex")
                )
                {
                    originalTextures[i][j] =
                        material.mainTexture;
                }
            }
        }

        originalTexturesCached =
            true;
    }

    // ============================================================
    // Fading
    // ============================================================

    private void UpdateFading()
    {
        float ratio =
            (fadingLevel - fadingState) /
            (float)fadingLevel;

        /*
         * Platform is still visible.
         *
         * Cancel any old collider-delay timer.
         */
        if (ratio > 0f)
        {
            ResetFadingCollisionDelay();

            SetCollisionEnabled(true);
        }
        else
        {
            /*
             * The platform has completely faded.
             *
             * This is the ONLY functionality that uses
             * colliderDisableDelay.
             */
            UpdateFadingCollisionDelay();
        }

        SetOpacity(
            Mathf.Max(
                ratio,
                0f
            )
        );
    }

    // ============================================================
    // Fading Collision Delay
    // ============================================================

    private void UpdateFadingCollisionDelay()
    {
        /*
         * Infinity:
         *
         * The fading platform's collider never gets disabled.
         */
        if (
            float.IsPositiveInfinity(
                colliderDisableDelay
            )
        )
        {
            fadingCollisionDelayActive =
                false;

            SetCollisionEnabled(true);

            return;
        }

        /*
         * NaN:
         *
         * Treat as immediate.
         */
        if (
            float.IsNaN(
                colliderDisableDelay
            )
        )
        {
            fadingCollisionDelayActive =
                false;

            SetCollisionEnabled(false);

            return;
        }

        /*
         * Zero or negative:
         *
         * Disable immediately.
         */
        if (
            colliderDisableDelay <= 0f
        )
        {
            fadingCollisionDelayActive =
                false;

            SetCollisionEnabled(false);

            return;
        }

        /*
         * Start the timer once.
         */
        if (!fadingCollisionDelayActive)
        {
            fadingCollisionDelayActive =
                true;

            fadingCollisionDisableTime =
                Time.time +
                colliderDisableDelay;

            /*
             * Collider remains active during the delay.
             */
            SetCollisionEnabled(true);

            return;
        }

        /*
         * Delay has not expired yet.
         */
        if (
            Time.time <
            fadingCollisionDisableTime
        )
        {
            SetCollisionEnabled(true);

            return;
        }

        /*
         * Delay expired.
         */
        SetCollisionEnabled(false);
    }

    private void ResetFadingCollisionDelay()
    {
        fadingCollisionDelayActive =
            false;

        fadingCollisionDisableTime =
            0f;
    }

    // ============================================================
    // Periodic
    // ============================================================

    private void UpdatePeriodic(
        float currentAttemptTime
    )
    {
        if (totalTime <= 0f)
            return;

        float progress =
            AdjustedMod(
                Mathf.Max(
                    0f,
                    currentAttemptTime -
                    startOffset
                ),
                totalTime
            );

        // --------------------------------------------------------
        // Fade out
        // --------------------------------------------------------

        if (
            progress <
            fadeOutTime
        )
        {
            if (
                fadeStyle ==
                FadeStyle.Cloak
            )
            {
                float cloakLevel =
                    Mathf.Min(
                        progress *
                        CLOAK_RATE,
                        1f
                    );

                ApplyCloak(
                    cloakLevel
                );
            }
            else
            {
                float opacity =
                    1f -
                    Mathf.Clamp01(
                        progress /
                        Mathf.Max(
                            fadeOutTime,
                            0.001f
                        )
                    );

                SetOpacity(
                    opacity
                );
            }
        }

        // --------------------------------------------------------
        // Invisible
        // --------------------------------------------------------

        else if (
            progress -
            fadeOutTime <
            invisibleTime
        )
        {
            SetCollisionEnabled(false);
            SetOpacity(0f);
        }

        // --------------------------------------------------------
        // Fade in
        // --------------------------------------------------------

        else if (
            progress -
            fadeOutTime -
            invisibleTime <
            fadeInTime
        )
        {
            SetCollisionEnabled(true);

            if (
                fadeStyle ==
                FadeStyle.Cloak
            )
            {
                float progressIn =
                    Mathf.Min(
                        (
                            progress -
                            fadeOutTime -
                            invisibleTime
                        ) *
                        CLOAK_RATE,
                        1f
                    );

                ApplyCloak(
                    1f -
                    progressIn
                );
            }
            else
            {
                float opacity =
                    Mathf.Clamp01(
                        (
                            progress -
                            fadeOutTime -
                            invisibleTime
                        ) /
                        Mathf.Max(
                            fadeInTime,
                            0.001f
                        )
                    );

                SetOpacity(
                    opacity
                );
            }
        }

        // --------------------------------------------------------
        // Fully visible
        // --------------------------------------------------------

        else
        {
            SetCollisionEnabled(true);

            if (
                fadeStyle ==
                FadeStyle.Cloak
            )
            {
                ApplyCloak(0f);
            }

            SetOpacity(1f);
        }
    }

    // ============================================================
    // Trapdoor
    // ============================================================

    private void UpdateTrapdoor(
        float currentAttemptTime
    )
    {
        if (
            lastContactTime <=
            -1e7f
        )
        {
            SetCollisionEnabled(true);

            if (
                fadeStyle ==
                FadeStyle.Cloak
            )
            {
                ApplyCloak(0f);
            }
            else
            {
                SetOpacity(1f);
            }

            return;
        }

        float progress =
            currentAttemptTime -
            lastContactTime;

        float hideEnd =
            fadeOutTime +
            invisibleTime;

        // --------------------------------------------------------
        // Fade out
        // --------------------------------------------------------

        if (
            progress <
            fadeOutTime
        )
        {
            if (
                fadeStyle ==
                FadeStyle.Cloak
            )
            {
                ApplyCloak(
                    Mathf.Min(
                        progress *
                        CLOAK_RATE,
                        1f
                    )
                );
            }
            else
            {
                SetOpacity(
                    1f -
                    Mathf.Clamp01(
                        progress /
                        Mathf.Max(
                            fadeOutTime,
                            0.001f
                        )
                    )
                );
            }
        }

        // --------------------------------------------------------
        // Invisible
        // --------------------------------------------------------

        else if (
            permanent ||
            progress -
            fadeOutTime <
            invisibleTime
        )
        {
            SetCollisionEnabled(false);
            SetOpacity(0f);
        }

        // --------------------------------------------------------
        // Fade in
        // --------------------------------------------------------

        else if (
            progress -
            hideEnd <
            fadeInTime
        )
        {
            SetCollisionEnabled(true);

            if (
                fadeStyle ==
                FadeStyle.Cloak
            )
            {
                float progressIn =
                    Mathf.Min(
                        (
                            progress -
                            hideEnd
                        ) *
                        CLOAK_RATE,
                        1f
                    );

                ApplyCloak(
                    1f -
                    progressIn
                );
            }
            else
            {
                SetOpacity(
                    Mathf.Clamp01(
                        (
                            progress -
                            hideEnd
                        ) /
                        Mathf.Max(
                            fadeInTime,
                            0.001f
                        )
                    )
                );
            }
        }

        // --------------------------------------------------------
        // Fully visible
        // --------------------------------------------------------

        else
        {
            SetCollisionEnabled(true);

            if (
                fadeStyle ==
                FadeStyle.Cloak
            )
            {
                ApplyCloak(0f);
            }

            SetOpacity(1f);
        }
    }

    // ============================================================
    // Collision
    // ============================================================

    public void OnCollisionWithMarble(
        Marble marble
    )
    {
        if (marble == null)
            return;

        if (!initialized)
            return;

        float currentAttemptTime =
            GetCurrentAttemptTime();

        switch (functionality)
        {
            case Functionality.Trapdoor:
                HandleTrapdoorContact(
                    currentAttemptTime
                );
                break;

            case Functionality.Fading:
                HandleFadingContact(
                    currentAttemptTime
                );
                break;

            // Periodic intentionally does nothing.
            case Functionality.Periodic:
                break;
        }
    }

    private void HandleTrapdoorContact(
        float currentAttemptTime
    )
    {
        bool isVisible =
            lastContactTime <=
            -1e7f
            ||
            (
                !permanent &&
                currentAttemptTime -
                lastContactTime >=
                fadeOutTime +
                invisibleTime +
                fadeInTime
            );

        if (isVisible)
        {
            lastContactTime =
                currentAttemptTime;
        }
    }

    private void HandleFadingContact(
        float currentAttemptTime
    )
    {
        if (
            currentAttemptTime -
            lastFadingContactTime <
            FADING_CONTACT_COOLDOWN
        )
        {
            return;
        }

        lastFadingContactTime =
            currentAttemptTime;

        fadingState++;

        /*
         * A new contact/fade cycle gets a new collider delay.
         */
        ResetFadingCollisionDelay();
    }

    // ============================================================
    // Cloak
    // ============================================================

    private void ApplyCloak(
        float cloakLevel
    )
    {
        bool shouldBeWhite =
            cloakLevel > 0f;

        /*
         * Make absolutely sure the skin texture has been cached
         * before replacing it with white.
         */
        if (shouldBeWhite)
        {
            CacheOriginalTextures();
        }

        if (
            shouldBeWhite !=
            isTextureWhite
        )
        {
            isTextureWhite =
                shouldBeWhite;

            for (
                int i = 0;
                i < childRenderers.Length;
                i++
            )
            {
                Material[] materials =
                    childRenderers[i].materials;

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (
                        material == null ||
                        !material.HasProperty(
                            "_MainTex"
                        )
                    )
                    {
                        continue;
                    }

                    if (shouldBeWhite)
                    {
                        material.mainTexture =
                            whiteTexture;
                    }
                    else
                    {
                        /*
                         * Restore the texture that was present
                         * after SkinSwapper applied the skin.
                         */
                        if (
                            originalTextures[i] != null &&
                            j <
                            originalTextures[i].Length
                        )
                        {
                            material.mainTexture =
                                originalTextures[i][j];
                        }
                    }
                }
            }
        }

        SetOpacity(
            cloakLevel > 0f
                ? 0.125f +
                  (1f - cloakLevel) *
                  0.875f
                : 1f
        );
    }

    // ============================================================
    // Opacity
    // ============================================================

    private void SetOpacity(
        float opacity
    )
    {
        opacity =
            Mathf.Clamp01(opacity);

        if (
            Mathf.Approximately(
                currentOpacity,
                opacity
            )
        )
        {
            return;
        }

        currentOpacity =
            opacity;

        for (
            int i = 0;
            i < childRenderers.Length;
            i++
        )
        {
            Material[] materials =
                childRenderers[i].materials;

            for (
                int j = 0;
                j < materials.Length;
                j++
            )
            {
                Material material =
                    materials[j];

                if (material == null)
                    continue;

                if (
                    material.HasProperty(
                        "_Color"
                    )
                )
                {
                    Color color =
                        material.color;

                    color.a =
                        opacity;

                    material.color =
                        color;
                }

                if (
                    material.HasProperty(
                        "_BaseColor"
                    )
                )
                {
                    Color color =
                        material.GetColor(
                            "_BaseColor"
                        );

                    color.a =
                        opacity;

                    material.SetColor(
                        "_BaseColor",
                        color
                    );
                }
            }
        }
    }

    // ============================================================
    // Collision Enable / Disable
    // ============================================================

    private void SetCollisionEnabled(
        bool enabled
    )
    {
        if (childColliders == null)
            return;

        for (
            int i = 0;
            i < childColliders.Length;
            i++
        )
        {
            if (childColliders[i] != null)
            {
                childColliders[i].enabled =
                    enabled;
            }
        }
    }

    // ============================================================
    // Reset
    // ============================================================

    public void ResetPlatform()
    {
        StopAllCoroutines();
        StartCoroutine(
            ResetPlatformCoroutine()
        );
    }

    /// <summary>
    /// Restores whether this platform was completely hidden in Vice.
    /// </summary>
    public void SetViceVersaHidden(bool hidden)
    {
        currentOpacity = -1f;

        SetCollisionEnabled(!hidden);
        SetOpacity(hidden ? 0f : 1f);

        ResetFadingCollisionDelay();
    }

    public IEnumerator ResetPlatformCoroutine()
    {
        /*
         * --------------------------------------------------------
         * Reset fading collision state
         * --------------------------------------------------------
         */
        yield return null;

        ResetFadingCollisionDelay();

        /*
         * --------------------------------------------------------
         * Reset contact state
         * --------------------------------------------------------
         */

        lastContactTime =
            -Mathf.Infinity;

        lastFadingContactTime =
            -Mathf.Infinity;

        /*
         * --------------------------------------------------------
         * Restore initial fading state
         * --------------------------------------------------------
         */

        fadingState =
            fadingInitialState;

        /*
         * --------------------------------------------------------
         * Restore cloak state
         * --------------------------------------------------------
         */

        isTextureWhite =
            false;

        SetCollisionEnabled(true);

        /*
         * --------------------------------------------------------
         * Restore the SKIN
         * --------------------------------------------------------
         *
         * Do NOT recache textures here.
         *
         * originalTextures contains the texture that was captured
         * after SkinSwapper applied the correct skin.
         *
         * This is the important part that preserves the skin.
         */

        if (
            originalTexturesCached &&
            originalTextures != null
        )
        {
            for (
                int i = 0;
                i < childRenderers.Length;
                i++
            )
            {
                Material[] materials =
                    childRenderers[i].materials;

                for (
                    int j = 0;
                    j < materials.Length;
                    j++
                )
                {
                    Material material =
                        materials[j];

                    if (
                        material == null ||
                        !material.HasProperty(
                            "_MainTex"
                        )
                    )
                    {
                        continue;
                    }

                    if (
                        originalTextures[i] != null &&
                        j <
                        originalTextures[i].Length
                    )
                    {
                        material.mainTexture =
                            originalTextures[i][j];
                    }
                }
            }
        }

        /*
         * --------------------------------------------------------
         * Force opacity restoration
         * --------------------------------------------------------
         *
         * currentOpacity can already be 1 while the actual
         * material may have been modified. Force the write.
         */

        currentOpacity =
            -1f;

        SetOpacity(1f);

        /*
         * Make sure the next Fading contact starts with no
         * leftover delay state.
         */
        ResetFadingCollisionDelay();

        /*
         * Make sure the collision detector is in the correct state
         * after a mission reset.
         */
        FadePlatformCollision collision =
            GetComponent<FadePlatformCollision>();

        if (collision != null)
        {
            collision.ResetCollision();

            collision.enabled =
                RequiresMarbleCollisionDetection;
        }
    }

    // ============================================================
    // Attempt Time
    // ============================================================

    private float GetCurrentAttemptTime()
    {
        /*
         * TODO:
         * Replace this with the actual Marble Blast attempt timer.
         *
         * Time.time is only a temporary fallback.
         */
        return Time.time;
    }

    // ============================================================
    // Parsing
    // ============================================================

    private static Functionality ParseFunctionality(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return Functionality.Trapdoor;
        }

        switch (value.ToLowerInvariant())
        {
            case "fading":
                return Functionality.Fading;

            case "periodic":
                return Functionality.Periodic;

            case "trapdoor":
            default:
                return Functionality.Trapdoor;
        }
    }

    private static FadeStyle ParseFadeStyle(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return FadeStyle.Cloak;
        }

        return value.ToLowerInvariant() == "fade"
            ? FadeStyle.Fade
            : FadeStyle.Cloak;
    }

    private static float AdjustedMod(
        float value,
        float modulus
    )
    {
        if (modulus <= 0f)
            return 0f;

        return (
            (value % modulus) +
            modulus
        ) % modulus;
    }
}