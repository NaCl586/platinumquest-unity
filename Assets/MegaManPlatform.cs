using System;
using UnityEngine;

public class MegaManPlatform : MonoBehaviour
{
    // Original PQ:
    //
    // $VV::MegaManFadeInOut = 500;
    // $VV::MegaManTimer = 1500;
    // $VV::MegaManHideAgain = MegaManFadeInOut + MegaManTimer;
    //
    // All values here are in seconds.

    private const float FADE_S = 0.5f;
    private const float MEGAMAN_TIMER = 1.5f;

    // Original:
    // startFade(MegaManFadeInOut, 0, 1)
    // scheduled after MegaManTimer * 2
    private const float FADE_OUT_START = MEGAMAN_TIMER * 2f; // 3.0

    // Original:
    // hide(true) scheduled after MegaManHideAgain * 2
    //
    // MegaManHideAgain = 0.5 + 1.5 = 2.0
    // 2.0 * 2 = 4.0
    private const float HIDE_TIME =
        (FADE_S + MEGAMAN_TIMER) * 2f; // 4.0

    // Original:
    // vv_megaManLoop is scheduled every MegaManTimer.
    private const float NEXT_DELAY = MEGAMAN_TIMER; // 1.5

    private const float HIDDEN_TIME = -999999f;

    [Header("Mega Man Platform")]
    public string next;
    public bool respondToCollision = false;

    public bool RespondToCollision
    {
        get => respondToCollision;
        set => respondToCollision = value;
    }

    public bool HasCollided
    {
        get => hasCollided;
        set => hasCollided = value;
    }

    private MegaManPlatform nextPlatform;

    private bool hasCollided;
    private bool queuedNext;

    private float showTime = HIDDEN_TIME;

    private Renderer[] childRenderers;
    private Collider[] childColliders;

    private float currentOpacity = -1f;

    private void Awake()
    {
        CacheChildComponents();
    }

    private void Start()
    {
        // Original MegaManPlatform::onAdd:
        //
        // %obj.setSkinName("skin2");
        // %obj.hide(true);
        // %obj.startFade(0,0,1);
        //
        // The parser is responsible for applying skin2.
        // Start hidden here.

        SetCollisionEnabled(false);
        SetOpacity(0f);

        ResolveNext();
    }

    public MegaManPlatform GetNextPlatform()
    {
        return ResolveNext();
    }

    private void Update()
    {
        float currentTime = GetAttemptTime();

        /*
         * Platform has not been activated yet.
         */
        if (showTime <= HIDDEN_TIME)
        {
            SetCollisionEnabled(false);
            SetOpacity(0f);
            return;
        }

        float elapsed = currentTime - showTime;

        /*
         * ------------------------------------------------------------
         * Last platform
         * ------------------------------------------------------------
         *
         * Original MCS:
         *
         * if (!isObject(%obj.next)) {
         *     cancel(%obj.fadeTimer);
         *     cancel(%obj.hideAgainTimer);
         *     cancel($vv_mmLoop[%obj]);
         * }
         *
         * Therefore the final platform does NOT fade out or hide.
         */
        bool isLastPlatform = ResolveNext() == null;

        /*
         * ------------------------------------------------------------
         * Start the next platform
         * ------------------------------------------------------------
         *
         * Original:
         *
         * $vv_mmLoop[%start] =
         *     schedule(
         *         $VV::MegaManTimer,
         *         0,
         *         vv_megaManLoop,
         *         %start,
         *         %current.next
         *     );
         *
         * The next platform appears every 1.5 seconds.
         */
        if (!queuedNext && !isLastPlatform && elapsed >= NEXT_DELAY)
        {
            queuedNext = true;

            MegaManPlatform n = ResolveNext();

            if (n != null)
                n.Show(currentTime);
        }

        /*
         * ------------------------------------------------------------
         * Last platform
         * ------------------------------------------------------------
         *
         * It remains fully visible indefinitely.
         */
        if (isLastPlatform)
        {
            SetCollisionEnabled(true);
            SetOpacity(1f);
            return;
        }

        /*
         * ------------------------------------------------------------
         * Fade in
         * ------------------------------------------------------------
         *
         * 0.0 -> 0.5 seconds
         *
         * Original:
         * startFade(500, 0, 0)
         */
        if (elapsed < FADE_S)
        {
            SetCollisionEnabled(true);

            float opacity =
                Mathf.Clamp01(
                    elapsed / FADE_S
                );

            SetOpacity(opacity);
        }

        /*
         * ------------------------------------------------------------
         * Fully visible
         * ------------------------------------------------------------
         *
         * 0.5 -> 3.0 seconds
         *
         * IMPORTANT:
         * This is 2.5 seconds of fully-visible time.
         *
         * The next platform appears at 1.5 seconds, but this platform
         * remains active until its fade begins at 3.0 seconds.
         */
        else if (elapsed < FADE_OUT_START)
        {
            SetCollisionEnabled(true);
            SetOpacity(1f);
        }

        /*
         * ------------------------------------------------------------
         * Fade out
         * ------------------------------------------------------------
         *
         * 3.0 -> 3.5 seconds
         *
         * Original:
         *
         * schedule(
         *     MegaManTimer * 2,
         *     "startFade",
         *     MegaManFadeInOut,
         *     0,
         *     1
         * );
         */
        else if (elapsed < FADE_OUT_START + FADE_S)
        {
            SetCollisionEnabled(true);

            float opacity =
                1f -
                Mathf.Clamp01(
                    (elapsed - FADE_OUT_START) / FADE_S
                );

            SetOpacity(opacity);
        }

        /*
         * ------------------------------------------------------------
         * Hidden
         * ------------------------------------------------------------
         *
         * 3.5 -> 4.0 seconds:
         * The visual fade has completed, but the original MCS doesn't
         * call hide(true) until 4.0 seconds.
         */
        else if (elapsed < HIDE_TIME)
        {
            SetCollisionEnabled(true);
            SetOpacity(0f);
        }

        /*
         * ------------------------------------------------------------
         * Fully hidden / non-collidable
         * ------------------------------------------------------------
         *
         * 4.0+ seconds
         */
        else
        {
            SetCollisionEnabled(false);
            SetOpacity(0f);
        }
    }

    /// <summary>
    /// Starts the Mega Man platform sequence.
    ///
    /// Equivalent to:
    ///     %current.hide(false);
    ///     %current.startFade(MegaManFadeInOut, 0, 0);
    /// </summary>
    public void Show(float currentAttemptTime)
    {
        showTime = currentAttemptTime;
        queuedNext = false;

        /*
         * The platform is going to fade in from zero.
         */
        SetCollisionEnabled(true);
        SetOpacity(0f);
    }

    /// <summary>
    /// Called when the marble contacts this platform.
    /// </summary>
    public void OnMarbleContact(Marble marble)
    {
        if (marble == null)
            return;

        MegaManPlatform n = ResolveNext();

        /*
         * Original:
         *
         * if (!isObject(%obj.next)) {
         *     cancel(...);
         * } else if (!%obj.respondToCollision || %obj.hasCollided) {
         *     return;
         * }
         *
         * The final platform doesn't use the collision response to
         * advance the sequence.
         */
        if (n == null)
        {
            hasCollided = true;
            return;
        }

        if (!respondToCollision || hasCollided)
            return;

        hasCollided = true;

        /*
         * IMPORTANT:
         *
         * The original MCS starts the first platform by explicitly
         * fading it in through the trigger:
         *
         *     %current.hide(false);
         *     %current.startFade(500, 0, 0);
         *
         * The collision event then schedules its fade-out at:
         *
         *     MegaManTimer * 2 = 3 seconds
         *
         * relative to the platform becoming active.
         *
         * Since the first platform was already shown by the trigger,
         * do NOT modify showTime here.
         *
         * The platform's normal Update() sequence handles the timing.
         */
    }

    // ================================================================
    // Next Platform
    // ================================================================

    private MegaManPlatform ResolveNext()
    {
        if (nextPlatform != null)
            return nextPlatform;

        if (string.IsNullOrEmpty(next))
            return null;

        GameObject obj = FindNamedObject(next);

        if (obj == null)
            return null;

        nextPlatform =
            obj.GetComponent<MegaManPlatform>();

        return nextPlatform;
    }

    private GameObject FindNamedObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        /*
         * Normal Unity lookup.
         */
        GameObject obj =
            GameObject.Find(objectName);

        if (obj != null)
            return obj;

        /*
         * Case-insensitive fallback.
         */
        MegaManPlatform[] platforms =
            FindObjectsOfType<MegaManPlatform>(true);

        foreach (MegaManPlatform platform in platforms)
        {
            if (platform == null)
                continue;

            if (string.Equals(
                platform.gameObject.name,
                objectName,
                StringComparison.OrdinalIgnoreCase))
            {
                return platform.gameObject;
            }
        }

        return null;
    }

    // ================================================================
    // Components
    // ================================================================

    private void CacheChildComponents()
    {
        childRenderers =
            GetComponentsInChildren<Renderer>(true);

        childColliders =
            GetComponentsInChildren<Collider>(true);
    }

    // ================================================================
    // Collision
    // ================================================================

    private void SetCollisionEnabled(bool enabled)
    {
        if (childColliders == null)
            return;

        foreach (Collider collider in childColliders)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    // ================================================================
    // Opacity
    // ================================================================

    private void SetOpacity(float opacity)
    {
        opacity =
            Mathf.Clamp01(opacity);

        if (Mathf.Approximately(
            currentOpacity,
            opacity))
        {
            return;
        }

        currentOpacity =
            opacity;

        if (childRenderers == null)
            return;

        /*
         * Follow the same material approach used by FadePlatform:
         *
         * renderer.materials
         *
         * rather than sharedMaterial.
         */
        foreach (Renderer renderer in childRenderers)
        {
            if (renderer == null)
                continue;

            Material[] materials =
                renderer.materials;

            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                /*
                 * Built-in / Standard style shaders.
                 */
                if (material.HasProperty("_Color"))
                {
                    Color color =
                        material.color;

                    color.a =
                        opacity;

                    material.color =
                        color;
                }

                /*
                 * URP style shaders.
                 */
                if (material.HasProperty("_BaseColor"))
                {
                    Color color =
                        material.GetColor("_BaseColor");

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

    // ================================================================
    // Reset
    // ================================================================

    public void ResetPlatform()
    {
        showTime = HIDDEN_TIME;
        hasCollided = false;
        queuedNext = false;
        respondToCollision = false;

        currentOpacity = -1f;

        SetCollisionEnabled(false);
        SetOpacity(0f);
    }

    // ================================================================
    // Attempt Time
    // ================================================================

    private float GetAttemptTime()
    {
        if (GameManager.instance != null)
        {
            /*
             * GameManager.elapsedTime is milliseconds.
             *
             * Mega Man's constants are expressed in seconds here.
             */
            return GameManager.instance.elapsedTime / 1000f;
        }

        return 0f;
    }
}
