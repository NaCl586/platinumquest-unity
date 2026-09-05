using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Radar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Canvas canvas;

    [Header("Radar Limits & Filtering")]
    [Tooltip("Maximum distance from the camera for radar targets.")]
    [SerializeField] private float maxRadarDistance = 500f;

    [Tooltip("Maximum total number of visible items on the radar. Set to 0 for unlimited.")]
    [SerializeField] private int maxVisibleRadarItems = 25;

    [Header("Radar Icons")]
    [Tooltip("Must match the order of Gem.gemColors.")]
    [SerializeField] private Sprite[] gemRadarIcons;
    [SerializeField] private Sprite endPadIcon;
    [SerializeField] private Sprite checkpointIcon;
    [SerializeField] private Sprite pointerIcon;

    [Header("Cannon Radar Icons")]
    [SerializeField] private Sprite cannonIcon;
    [SerializeField] private Sprite cannonLowIcon;
    [SerializeField] private Sprite cannonMidIcon;
    [SerializeField] private Sprite cannonHighIcon;

    [Header("Powerup Radar Icons")]
    [SerializeField] private Sprite anvilIcon;
    [SerializeField] private Sprite bubbleIcon;
    [SerializeField] private Sprite fireballIcon;
    [SerializeField] private Sprite gravityModifierIcon;
    [SerializeField] private Sprite shockAbsorberIcon;
    [SerializeField] private Sprite superBounceIcon;
    [SerializeField] private Sprite superJumpIcon;
    [SerializeField] private Sprite superSpeedIcon;
    [SerializeField] private Sprite teleporterIcon;

    [Header("Time Travel Radar Icons")]
    [SerializeField] private Sprite timeTravelIcon;

    [Header("Radar Position & Scaling")]
    [SerializeField] private Vector2 ellipseScreenFraction = new Vector2(0.79f, 0.85f);
    [SerializeField] private Vector2 gemIconSize = new Vector2(32f, 32f);
    [SerializeField] private Vector2 endPadIconSize = new Vector2(32f, 32f);
    [SerializeField] private Vector2 checkpointIconSize = new Vector2(32f, 32f);
    [SerializeField] private Vector2 cannonIconSize = new Vector2(32f, 32f);
    [SerializeField] private Vector2 powerupIconSize = new Vector2(32f, 32f);
    [SerializeField] private Vector2 timeTravelIconSize = new Vector2(32f, 32f);

    [Header("Pointer")]
    [SerializeField] private Vector2 pointerSize = new Vector2(100f, 70f);
    [SerializeField, Range(0f, 1f)] private float pointerAlpha = 0.6f;

    // ============================================================
    // MARKER INTERFACE & IMPLS
    // ============================================================

    private interface IRadarMarker
    {
        float DistanceSqr { get; set; }

        bool Render(
            Camera cam,
            Canvas canvas,
            RectTransform canvasRect,
            Vector2 ellipseFrac,
            Vector2 ptrSize,
            float ptrAlpha,
            Sprite ptrIcon,
            bool allowPointer);

        void Hide();
    }

    private class GemMarker : IRadarMarker
    {
        public Gem gem;
        public Image icon;
        public Image pointer;
        public float distanceSqr;
        public Sprite iconSprite;
        public Vector2 iconSize;

        public float DistanceSqr
        {
            get => distanceSqr;
            set => distanceSqr = value;
        }

        public bool Render(
            Camera cam,
            Canvas canvas,
            RectTransform canvasRect,
            Vector2 ellipseFrac,
            Vector2 ptrSize,
            float ptrAlpha,
            Sprite ptrIcon,
            bool allowPointer)
        {
            if (gem == null)
                return false;

            Vector3 worldPos =
                GetBoundsCenter(gem.gameObject);

            Color ptrColor =
                gem.gemColor;

            // Gems are the only radar targets that use the
            // off-screen directional pointer.
            return UpdateTarget(
                worldPos,
                iconSprite,
                ptrColor,
                icon,
                pointer,
                iconSize,
                ptrSize,
                ptrAlpha,
                ptrIcon,
                true,
                cam,
                canvas,
                canvasRect,
                ellipseFrac);
        }

        public void Hide()
        {
            if (icon)
                icon.gameObject.SetActive(false);

            if (pointer)
                pointer.gameObject.SetActive(false);
        }
    }

    private class RadarPriorityEntry
    {
        public IRadarMarker marker;
        public bool isEndPad;
        public float distanceSqr;
    }

    private class GenericMarker : IRadarMarker
    {
        public MonoBehaviour target;
        public Image icon;
        public Image pointer;
        public float distanceSqr;
        public Sprite iconSprite;
        public Vector2 iconSize;
        public Color pointerColor = Color.white;

        public float DistanceSqr
        {
            get => distanceSqr;
            set => distanceSqr = value;
        }

        public bool Render(
            Camera cam,
            Canvas canvas,
            RectTransform canvasRect,
            Vector2 ellipseFrac,
            Vector2 ptrSize,
            float ptrAlpha,
            Sprite ptrIcon,
            bool allowPointer)
        {
            if (target == null)
                return false;

            Vector3 worldPos =
                GetBoundsCenter(target.gameObject);

            // Non-gem targets are icon-only. They never display
            // the off-screen directional pointer.
            return UpdateTarget(
                worldPos,
                iconSprite,
                pointerColor,
                icon,
                pointer,
                iconSize,
                ptrSize,
                ptrAlpha,
                ptrIcon,
                false,
                cam,
                canvas,
                canvasRect,
                ellipseFrac);
        }

        public void Hide()
        {
            if (icon)
                icon.gameObject.SetActive(false);

            if (pointer)
                pointer.gameObject.SetActive(false);
        }
    }

    // ============================================================
    // MARKER TRACKING
    // ============================================================

    private readonly List<GemMarker> gemMarkers =
        new List<GemMarker>();

    private readonly List<GenericMarker> checkpointMarkers =
        new List<GenericMarker>();

    private readonly List<GenericMarker> cannonMarkers =
        new List<GenericMarker>();

    private readonly List<GenericMarker> powerupMarkers =
        new List<GenericMarker>();

    private readonly List<GenericMarker> timeTravelMarkers =
        new List<GenericMarker>();

    // Unified buffer for PQ-style global distance limiting.
    private readonly List<IRadarMarker> globalValidBuffer =
        new List<IRadarMarker>();

    // Global selection buffer. Gems have highest priority, followed by
    // the End Pad, then all other radar targets.
    private readonly List<RadarPriorityEntry> globalPriorityBuffer =
        new List<RadarPriorityEntry>();

    // End Pad UI Elements.
    private Image endPadIconImage;
    private Image endPadPointerImage;

    // State.
    private RectTransform canvasRect;
    private bool initialized;
    private bool radarVisible = true;

    private const string RadarVisibleKey = "RadarVisible";

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        radarVisible =
            PlayerPrefs.GetInt(RadarVisibleKey, 1) == 1;

        maxVisibleRadarItems =
            PlayerPrefs.GetInt("Graphics_MaxRadarItems", 25);
    }

    private void Update()
    {
        if (ControlBinding.instance != null &&
            Input.GetKeyDown(ControlBinding.instance.toggleRadar))
        {
            radarVisible = !radarVisible;

            PlayerPrefs.SetInt(
                RadarVisibleKey,
                radarVisible ? 1 : 0);

            PlayerPrefs.Save();

            if (!radarVisible)
                HideEverything();
        }
    }

    private void LateUpdate()
    {
        if (!radarVisible ||
            GameManager.instance == null)
            return;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null ||
            canvas == null)
            return;

        if (!initialized)
        {
            if (GameManager.instance.Gems == null)
                return;

            InitializeMarkers();
            initialized = true;
        }

        if (GameManager.gameFinish)
        {
            HideEverything();
            return;
        }

        globalValidBuffer.Clear();

        // ========================================================
        // 1. MISSION FLAGS CHECK
        // ========================================================

        bool radarGems =
            RadarContainsAny("gems", "gem");

        bool radarEndPad =
            RadarContainsAny("endpad", "end_pad");

        bool radarCheckpoints =
            RadarContainsAny("checkpoint", "checkpoints");

        bool radarCannon =
            RadarContainsAny(
                "cannon",
                "cannons",
                "defaultcannon",
                "cannon_custom");

        bool radarCannonLow =
            RadarContainsAny("cannon_low");

        bool radarCannonMid =
            RadarContainsAny("cannon_mid");

        bool radarCannonHigh =
            RadarContainsAny("cannon_high");

        bool radarPowerups =
            RadarContainsAny(
                "powerups",
                "powerup");

        bool radarAnvil =
            RadarContainsAny(
                "anvil",
                "anvilitem");

        bool radarBubble =
            RadarContainsAny(
                "bubble",
                "bubbleitem");

        bool radarFireball =
            RadarContainsAny(
                "fireball",
                "fireballitem");

        bool radarGravityModifier =
            RadarContainsAny(
                "gravitymodifier",
                "gravity_modifier",
                "antigravity");

        bool radarShockAbsorber =
            RadarContainsAny(
                "shockabsorber",
                "shock_absorber",
                "shockabsorberitem");

        bool radarSuperBounce =
            RadarContainsAny(
                "superbounce",
                "super_bounce",
                "superbounceitem");

        bool radarSuperJump =
            RadarContainsAny(
                "superjump",
                "super_jump",
                "superjumpitem");

        bool radarSuperSpeed =
            RadarContainsAny(
                "superspeed",
                "super_speed",
                "superspeeditem");

        bool radarTeleporter =
            RadarContainsAny(
                "teleporter",
                "teleport",
                "teleportitem");

        // Time Travel is completely separate from Powerups.
        bool radarTimeTravel =
            RadarContainsAny(
                "timetravel",
                "time_travel",
                "timepenalty",
                "time_penalty",
                "sundial",
                "respawningtimetravel",
                "respawning_time_travel");

        // ========================================================
        // 2. GEM REQUIREMENT CALCULATION
        // ========================================================

        bool noGems =
            GameManager.instance.TotalGems <= 0;

        bool allGemsCollected =
            GameManager.instance.CheckForAllGems();

        bool quotaReached =
            MissionInfo.instance != null &&
            MissionInfo.instance.gemQuota >= 0 &&
            GameManager.instance.CurrentGems >=
                MissionInfo.instance.gemQuota;

        bool gemRequirementSatisfied =
            noGems ||
            allGemsCollected ||
            quotaReached;

        // ========================================================
        // 3. COLLECT ACTIVE ITEMS
        // ========================================================

        if (radarGems && !gemRequirementSatisfied)
            CollectGems();
        else
            HideAllGemMarkers();

        if (radarCheckpoints)
            CollectCheckpoints();
        else
            HideAllCheckpointMarkers();

        CollectCannons(
            radarCannon,
            radarCannonLow,
            radarCannonMid,
            radarCannonHigh);

        // Normal powerups.
        CollectPowerups(
            radarPowerups,
            radarAnvil,
            radarBubble,
            radarFireball,
            radarGravityModifier,
            radarShockAbsorber,
            radarSuperBounce,
            radarSuperJump,
            radarSuperSpeed,
            radarTeleporter);

        // Time Travel is handled independently.
        CollectTimeTravels(radarTimeTravel);

        // ========================================================
        // 4. GLOBAL LIMIT SORTING
        //    Priority:
        //      1. Gems
        //      2. End Pad
        //      3. All other radar targets
        //
        //    Within each priority group, nearest comes first.
        // ========================================================

        globalPriorityBuffer.Clear();

        // Add all normal radar markers.
        foreach (IRadarMarker marker in globalValidBuffer)
        {
            globalPriorityBuffer.Add(
                new RadarPriorityEntry
                {
                    marker = marker,
                    isEndPad = false,
                    distanceSqr = marker.DistanceSqr
                });
        }

        // Add the End Pad to the same global selection.
        // It only becomes eligible under the same conditions that
        // normally allow the End Pad to be displayed.
        bool endPadEligible =
            radarEndPad &&
            gemRequirementSatisfied &&
            GameManager.instance.finishPad != null;

        if (endPadEligible)
        {
            Vector3 padPos =
                GetBoundsCenter(GameManager.instance.finishPad);

            float endPadDistanceSqr =
                (padPos -
                 playerCamera.transform.position).sqrMagnitude;

            if (endPadDistanceSqr <=
                maxRadarDistance * maxRadarDistance)
            {
                globalPriorityBuffer.Add(
                    new RadarPriorityEntry
                    {
                        marker = null,
                        isEndPad = true,
                        distanceSqr = endPadDistanceSqr
                    });
            }
        }

        // Gems always come first, followed by the End Pad, followed
        // by all other radar targets. Within each group, nearest
        // targets come first.
        globalPriorityBuffer.Sort(
            (a, b) =>
            {
                int aPriority =
                    a.marker is GemMarker
                        ? 0
                        : a.isEndPad
                            ? 1
                            : 2;

                int bPriority =
                    b.marker is GemMarker
                        ? 0
                        : b.isEndPad
                            ? 1
                            : 2;

                if (aPriority != bPriority)
                    return aPriority.CompareTo(bPriority);

                return a.distanceSqr.CompareTo(
                    b.distanceSqr);
            });

        int drawLimit =
            globalPriorityBuffer.Count;

        if (maxVisibleRadarItems > 0 &&
            drawLimit > maxVisibleRadarItems)
        {
            for (int i = maxVisibleRadarItems;
                 i < drawLimit;
                 i++)
            {
                RadarPriorityEntry entry =
                    globalPriorityBuffer[i];

                if (entry.isEndPad)
                    HideEndPad();
                else
                    entry.marker.Hide();
            }

            drawLimit =
                maxVisibleRadarItems;
        }

        // ========================================================
        // 5. RENDER VISIBLE ITEMS
        // ========================================================

        bool endPadRendered =
            false;

        for (int i = 0; i < drawLimit; i++)
        {
            RadarPriorityEntry entry =
                globalPriorityBuffer[i];

            if (entry.isEndPad)
            {
                UpdateEndPad();
                endPadRendered = true;
                continue;
            }

            entry.marker.Render(
                playerCamera,
                canvas,
                canvasRect,
                ellipseScreenFraction,
                pointerSize,
                pointerAlpha,
                pointerIcon,
                entry.marker is GemMarker);
        }

        // ========================================================
        // 6. HANDLE END PAD VISIBILITY
        // ========================================================

        // The End Pad is rendered by the global selection above.
        // If it was not selected, make sure it remains hidden.
        if (!endPadRendered)
            HideEndPad();
    }

    // ============================================================
    // COLLECTION METHODS
    // ============================================================

    private void CollectGems()
    {
        Vector3 camPos =
            playerCamera.transform.position;

        float maxSqr =
            maxRadarDistance * maxRadarDistance;

        foreach (GemMarker m in gemMarkers)
        {
            if (m.gem == null ||
                !m.gem.gameObject.activeInHierarchy)
            {
                m.Hide();
                continue;
            }

            float distSqr =
                (GetBoundsCenter(m.gem.gameObject) -
                 camPos).sqrMagnitude;

            if (distSqr > maxSqr)
            {
                m.Hide();
                continue;
            }

            int idx =
                m.gem.gemColorIndex;

            if (gemRadarIcons == null ||
                idx < 0 ||
                idx >= gemRadarIcons.Length ||
                gemRadarIcons[idx] == null)
            {
                m.Hide();
                continue;
            }

            m.iconSprite =
                gemRadarIcons[idx];

            m.iconSize =
                gemIconSize;

            m.distanceSqr =
                distSqr;

            globalValidBuffer.Add(m);
        }
    }

    private void CollectCheckpoints()
    {
        Vector3 camPos =
            playerCamera.transform.position;

        float maxSqr =
            maxRadarDistance * maxRadarDistance;

        foreach (GenericMarker m in checkpointMarkers)
        {
            if (m.target == null ||
                !m.target.gameObject.activeInHierarchy)
            {
                m.Hide();
                continue;
            }

            float distSqr =
                (GetBoundsCenter(m.target.gameObject) -
                 camPos).sqrMagnitude;

            if (distSqr > maxSqr)
            {
                m.Hide();
                continue;
            }

            m.iconSprite =
                checkpointIcon;

            m.iconSize =
                checkpointIconSize;

            m.distanceSqr =
                distSqr;

            globalValidBuffer.Add(m);
        }
    }

    private void CollectCannons(
        bool rCannon,
        bool rLow,
        bool rMid,
        bool rHigh)
    {
        if (!rCannon &&
            !rLow &&
            !rMid &&
            !rHigh)
        {
            HideAllCannonMarkers();
            return;
        }

        Vector3 camPos =
            playerCamera.transform.position;

        float maxSqr =
            maxRadarDistance * maxRadarDistance;

        foreach (GenericMarker m in cannonMarkers)
        {
            Cannon cannon =
                m.target as Cannon;

            if (cannon == null ||
                !cannon.gameObject.activeInHierarchy)
            {
                m.Hide();
                continue;
            }

            string typeStr =
                (cannon.radarCannonType ?? "")
                .Trim()
                .ToLowerInvariant();

            Sprite icon =
                cannonIcon;

            if (typeStr == "cannon_low")
            {
                if (!rCannon && !rLow)
                {
                    m.Hide();
                    continue;
                }

                icon =
                    cannonLowIcon;
            }
            else if (typeStr == "cannon_mid")
            {
                if (!rCannon && !rMid)
                {
                    m.Hide();
                    continue;
                }

                icon =
                    cannonMidIcon;
            }
            else if (typeStr == "cannon_high")
            {
                if (!rCannon && !rHigh)
                {
                    m.Hide();
                    continue;
                }

                icon =
                    cannonHighIcon;
            }
            else if (!rCannon)
            {
                m.Hide();
                continue;
            }

            float distSqr =
                (GetBoundsCenter(cannon.gameObject) -
                 camPos).sqrMagnitude;

            if (distSqr > maxSqr ||
                icon == null)
            {
                m.Hide();
                continue;
            }

            m.iconSprite =
                icon;

            m.iconSize =
                cannonIconSize;

            m.distanceSqr =
                distSqr;

            globalValidBuffer.Add(m);
        }
    }

    // ============================================================
    // NORMAL POWERUPS
    // ============================================================

    private void CollectPowerups(
        bool rPow,
        bool rAnvil,
        bool rBubble,
        bool rFireball,
        bool rGrav,
        bool rShock,
        bool rBounce,
        bool rJump,
        bool rSpeed,
        bool rTele)
    {
        if (!rPow &&
            !rAnvil &&
            !rBubble &&
            !rFireball &&
            !rGrav &&
            !rShock &&
            !rBounce &&
            !rJump &&
            !rSpeed &&
            !rTele)
        {
            HideAllPowerupMarkers();
            return;
        }

        Vector3 camPos =
            playerCamera.transform.position;

        float maxSqr =
            maxRadarDistance * maxRadarDistance;

        foreach (GenericMarker m in powerupMarkers)
        {
            if (m.target == null ||
                !m.target.gameObject.activeInHierarchy)
            {
                m.Hide();
                continue;
            }

            Powerups powerup =
                m.target as Powerups;

            if (powerup == null)
            {
                powerup =
                    m.target.GetComponent<Powerups>();
            }

            if (powerup == null ||
                !powerup.isActive)
            {
                m.Hide();
                continue;
            }

            string name =
                m.target.GetType().Name;

            Sprite icon =
                null;

            bool allowed =
                rPow;

            if (Matches(
                name,
                "Anvil",
                "AnvilItem"))
            {
                allowed |= rAnvil;
                icon = anvilIcon;
            }
            else if (Matches(
                name,
                "Bubble",
                "BubbleItem"))
            {
                allowed |= rBubble;
                icon = bubbleIcon;
            }
            else if (Matches(
                name,
                "Fireball",
                "FireballItem"))
            {
                allowed |= rFireball;
                icon = fireballIcon;
            }
            else if (Matches(
                name,
                "GravityModifier",
                "AntiGravity",
                "AntiGravityItem"))
            {
                allowed |= rGrav;
                icon = gravityModifierIcon;
            }
            else if (Matches(
                name,
                "ShockAbsorber",
                "ShockAbsorberItem"))
            {
                allowed |= rShock;
                icon = shockAbsorberIcon;
            }
            else if (Matches(
                name,
                "SuperBounce",
                "SuperBounceItem"))
            {
                allowed |= rBounce;
                icon = superBounceIcon;
            }
            else if (Matches(
                name,
                "SuperJump",
                "SuperJumpItem"))
            {
                allowed |= rJump;
                icon = superJumpIcon;
            }
            else if (Matches(
                name,
                "SuperSpeed",
                "SuperSpeedItem"))
            {
                allowed |= rSpeed;
                icon = superSpeedIcon;
            }
            else if (Matches(
                name,
                "Teleporter",
                "TeleporterItem",
                "TeleportItem"))
            {
                allowed |= rTele;
                icon = teleporterIcon;
            }

            // Time Travel is intentionally NOT handled here.

            if (!allowed ||
                icon == null)
            {
                m.Hide();
                continue;
            }

            float distSqr =
                (GetBoundsCenter(m.target.gameObject) -
                 camPos).sqrMagnitude;

            if (distSqr > maxSqr)
            {
                m.Hide();
                continue;
            }

            m.iconSprite =
                icon;

            m.iconSize =
                powerupIconSize;

            m.distanceSqr =
                distSqr;

            globalValidBuffer.Add(m);
        }
    }

    // ============================================================
    // TIME TRAVEL
    // ============================================================

    private void CollectTimeTravels(
        bool radarEnabled)
    {
        if (!radarEnabled)
        {
            HideAllTimeTravelMarkers();
            return;
        }

        Vector3 camPos =
            playerCamera.transform.position;

        float maxSqr =
            maxRadarDistance * maxRadarDistance;

        foreach (GenericMarker m in timeTravelMarkers)
        {
            if (m.target == null ||
                !m.target.gameObject.activeInHierarchy)
            {
                m.Hide();
                continue;
            }

            Powerups powerup =
                m.target as Powerups;

            if (powerup == null)
            {
                powerup =
                    m.target.GetComponent<Powerups>();
            }

            if (powerup == null ||
                !powerup.isActive)
            {
                m.Hide();
                continue;
            }

            float distSqr =
                (GetBoundsCenter(m.target.gameObject) -
                 camPos).sqrMagnitude;

            if (distSqr > maxSqr ||
                timeTravelIcon == null)
            {
                m.Hide();
                continue;
            }

            m.iconSprite =
                timeTravelIcon;

            m.iconSize =
                timeTravelIconSize;

            m.distanceSqr =
                distSqr;

            globalValidBuffer.Add(m);
        }
    }

    // ============================================================
    // END PAD
    // ============================================================

    private void UpdateEndPad()
    {
        GameObject finishPad =
            GameManager.instance.finishPad;

        if (finishPad == null)
        {
            HideEndPad();
            return;
        }

        Vector3 padPos =
            GetBoundsCenter(finishPad);

        if ((padPos -
             playerCamera.transform.position).sqrMagnitude >
            maxRadarDistance * maxRadarDistance)
        {
            HideEndPad();
            return;
        }

        UpdateTarget(
            padPos,
            endPadIcon,
            new Color32(
                0xE6,
                0xE6,
                0xE6,
                0xFF),
            endPadIconImage,
            endPadPointerImage,
            endPadIconSize,
            pointerSize,
            pointerAlpha,
            pointerIcon,
            false,
            playerCamera,
            canvas,
            canvasRect,
            ellipseScreenFraction);
    }

    // ============================================================
    // RENDERING
    // ============================================================

    private static bool UpdateTarget(
        Vector3 worldPos,
        Sprite icon,
        Color ptrColor,
        Image iconImg,
        Image ptrImg,
        Vector2 iconSize,
        Vector2 ptrSize,
        float ptrAlpha,
        Sprite ptrIcon,
        bool allowPointer,
        Camera cam,
        Canvas canvas,
        RectTransform canvasRect,
        Vector2 ellipseFrac)
    {
        if (iconImg == null ||
            ptrImg == null ||
            canvas == null ||
            canvasRect == null ||
            cam == null)
        {
            return false;
        }

        iconImg.sprite =
            icon;

        ptrImg.sprite =
            ptrIcon;

        Vector3 screenPos =
            cam.WorldToScreenPoint(worldPos);

        Vector3 vp =
            cam.WorldToViewportPoint(worldPos);

        bool visible =
            vp.z > 0f &&
            vp.x >= 0f &&
            vp.x <= 1f &&
            vp.y >= 0f &&
            vp.y <= 1f;

        if (visible)
        {
            if (icon == null)
            {
                iconImg.gameObject.SetActive(false);
                ptrImg.gameObject.SetActive(false);
                return false;
            }

            iconImg.sprite =
                icon;

            iconImg.color =
                Color.white;

            iconImg.rectTransform.sizeDelta =
                iconSize;

            SetUIPos(
                iconImg.rectTransform,
                screenPos,
                canvas,
                canvasRect);

            iconImg.rectTransform.rotation =
                Quaternion.identity;

            iconImg.gameObject.SetActive(true);
            ptrImg.gameObject.SetActive(false);

            return true;
        }

        // Hide the normal icon whenever the target is off-screen.
        iconImg.gameObject.SetActive(false);

        // Only gems are allowed to use the off-screen pointer.
        // All other target types remain completely hidden.
        ptrImg.gameObject.SetActive(false);

        if (!allowPointer ||
            ptrIcon == null)
        {
            return false;
        }

        Vector2 screenSize =
            new Vector2(
                Screen.width,
                Screen.height);

        Vector2 dir =
            new Vector2(
                screenPos.x,
                screenPos.y) -
            (screenSize * 0.5f);

        if (screenPos.z < 0f)
            dir *= -1f;

        dir =
            dir.sqrMagnitude < 0.0001f
                ? Vector2.up
                : dir.normalized;

        float theta =
            Mathf.Atan2(
                dir.y,
                dir.x);

        Vector2 ellipsePos =
            new Vector2(
                screenSize.x *
                (ellipseFrac.x *
                 Mathf.Cos(theta) + 1f) *
                0.5f,

                screenSize.y *
                (ellipseFrac.y *
                 Mathf.Sin(theta) + 1f) *
                0.5f);

        ptrImg.sprite =
            ptrIcon;

        ptrImg.color =
            new Color(
                ptrColor.r,
                ptrColor.g,
                ptrColor.b,
                ptrAlpha);

        ptrImg.rectTransform.sizeDelta =
            ptrSize;

        SetUIPos(
            ptrImg.rectTransform,
            ellipsePos,
            canvas,
            canvasRect);

        ptrImg.rectTransform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                theta * Mathf.Rad2Deg);

        ptrImg.gameObject.SetActive(true);

        return true;
    }

    private static void SetUIPos(
        RectTransform rect,
        Vector3 screenPos,
        Canvas canvas,
        RectTransform canvasRect)
    {
        Camera uiCamera =
            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

        if (RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                uiCamera,
                out Vector2 localPoint))
        {
            rect.anchoredPosition =
                localPoint;
        }
    }

    private static Vector3 GetBoundsCenter(
        GameObject obj)
    {
        if (obj == null)
            return Vector3.zero;

        Collider col =
            obj.GetComponent<Collider>();

        return col != null
            ? col.bounds.center
            : obj.transform.position;
    }

    // ============================================================
    // UTILITIES
    // ============================================================

    private bool Matches(
        string input,
        params string[] options)
    {
        foreach (string opt in options)
        {
            if (string.Equals(
                input,
                opt,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool RadarContainsAny(
        params string[] flags)
    {
        if (MissionInfo.instance == null)
            return false;

        // MBP uses CustomRadarRule for these flags.
        // Fall back to Radar for older/plain-text missions.
        string radarRule =
            !string.IsNullOrWhiteSpace(
                MissionInfo.instance.customRadarRule)
                ? MissionInfo.instance.customRadarRule
                : MissionInfo.instance.radar;

        if (string.IsNullOrWhiteSpace(radarRule))
            return false;

        foreach (string flag in flags)
        {
            if (string.IsNullOrWhiteSpace(flag))
                continue;

            string normalizedFlag =
                flag.Trim().ToLowerInvariant();

            string mbpFlag = null;

            switch (normalizedFlag)
            {
                case "gem":
                case "gems":
                    mbpFlag =
                        "$Radar::Flags::Gems";
                    break;

                case "timetravel":
                case "time_travel":
                case "timepenalty":
                case "time_penalty":
                case "sundial":
                case "respawningtimetravel":
                case "respawning_time_travel":
                    mbpFlag =
                        "$Radar::Flags::TimeTravels";
                    break;

                case "endpad":
                case "end_pad":
                    mbpFlag =
                        "$Radar::Flags::EndPad";
                    break;

                case "checkpoint":
                case "checkpoints":
                    mbpFlag =
                        "$Radar::Flags::Checkpoints";
                    break;

                case "cannon":
                case "cannons":
                case "defaultcannon":
                case "cannon_custom":
                case "cannon_low":
                case "cannon_mid":
                case "cannon_high":
                    mbpFlag =
                        "$Radar::Flags::Cannons";
                    break;

                case "powerup":
                case "powerups":
                case "anvil":
                case "anvilitem":
                case "bubble":
                case "bubbleitem":
                case "fireball":
                case "fireballitem":
                case "gravitymodifier":
                case "gravity_modifier":
                case "antigravity":
                case "shockabsorber":
                case "shock_absorber":
                case "shockabsorberitem":
                case "superbounce":
                case "super_bounce":
                case "superbounceitem":
                case "superjump":
                case "super_jump":
                case "superjumpitem":
                case "superspeed":
                case "super_speed":
                case "superspeeditem":
                case "teleporter":
                case "teleport":
                case "teleportitem":
                    mbpFlag =
                        "$Radar::Flags::Powerups";
                    break;
            }

            if (mbpFlag != null &&
                radarRule.IndexOf(
                    mbpFlag,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // Also support simple Unity/plain-text radar rules.
            string[] activeFlags =
                radarRule.Split(
                    new[]
                    {
                        ' ',
                        ',',
                        ';',
                        '|',
                        '\t',
                        '\r',
                        '\n'
                    },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string active in activeFlags)
            {
                string cleaned =
                    active.Trim()
                        .TrimStart('$')
                        .Replace(
                            "Radar::Flags::",
                            "")
                        .Replace(
                            "Radar.Flags.",
                            "")
                        .ToLowerInvariant();

                if (cleaned == normalizedFlag)
                    return true;
            }
        }

        return false;
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    private Image CreateImage(string name)
    {
        GameObject obj =
            new GameObject(name);

        obj.transform.SetParent(
            transform,
            false);

        Image img =
            obj.AddComponent<Image>();

        img.raycastTarget =
            false;

        return img;
    }

    private void InitializeMarkers()
    {
        if (GameManager.instance.Gems != null)
        {
            foreach (Gem g in GameManager.instance.Gems)
            {
                if (!g)
                    continue;

                GemMarker m =
                    new GemMarker
                    {
                        gem = g,
                        icon = CreateImage("Gem Icon"),
                        pointer = CreateImage("Gem Pointer")
                    };

                m.pointer.preserveAspect =
                    true;

                m.Hide();

                gemMarkers.Add(m);
            }
        }

        foreach (Checkpoint c in
                 FindObjectsOfType<Checkpoint>(true))
        {
            GenericMarker m =
                new GenericMarker
                {
                    target = c,
                    icon = CreateImage("CP Icon"),
                    pointer = CreateImage("CP Pointer")
                };

            m.pointer.preserveAspect =
                true;

            m.Hide();

            checkpointMarkers.Add(m);
        }

        foreach (Cannon c in
                 FindObjectsOfType<Cannon>(true))
        {
            GenericMarker m =
                new GenericMarker
                {
                    target = c,
                    icon = CreateImage("Cannon Icon"),
                    pointer = CreateImage("Cannon Pointer")
                };

            m.pointer.preserveAspect =
                true;

            m.Hide();

            cannonMarkers.Add(m);
        }

        foreach (MonoBehaviour b in
                 FindObjectsOfType<MonoBehaviour>(true))
        {
            if (b == null)
                continue;

            string n =
                b.GetType().Name;

            // ====================================================
            // TIME TRAVEL
            // ====================================================

            if (Matches(
                n,
                "TimeTravel",
                "TimeTravelItem",
                "RespawningTimeTravel",
                "RespawningTimeTravelItem",
                "TimePenalty",
                "TimePenaltyItem",
                "Sundial",
                "SundialItem"))
            {
                GenericMarker m =
                    new GenericMarker
                    {
                        target = b,
                        icon = CreateImage("Time Travel Icon"),
                        pointer = CreateImage("Time Travel Pointer")
                    };

                m.pointer.preserveAspect =
                    true;

                m.Hide();

                timeTravelMarkers.Add(m);
            }

            // ====================================================
            // NORMAL POWERUPS
            // ====================================================

            else if (Matches(
                n,
                "Anvil",
                "AnvilItem",
                "Bubble",
                "BubbleItem",
                "Fireball",
                "FireballItem",
                "GravityModifier",
                "AntiGravity",
                "AntiGravityItem",
                "ShockAbsorber",
                "ShockAbsorberItem",
                "SuperBounce",
                "SuperBounceItem",
                "SuperJump",
                "SuperJumpItem",
                "SuperSpeed",
                "SuperSpeedItem",
                "Teleporter",
                "TeleporterItem",
                "TeleportItem"))
            {
                GenericMarker m =
                    new GenericMarker
                    {
                        target = b,
                        icon = CreateImage("Powerup Icon"),
                        pointer = CreateImage("Powerup Pointer")
                    };

                m.pointer.preserveAspect =
                    true;

                m.Hide();

                powerupMarkers.Add(m);
            }
        }

        endPadIconImage =
            CreateImage("EndPad Icon");

        endPadPointerImage =
            CreateImage("EndPad Pointer");

        endPadPointerImage.preserveAspect =
            true;

        HideEndPad();
    }

    // ============================================================
    // HIDING
    // ============================================================

    private void HideAllGemMarkers()
    {
        foreach (var m in gemMarkers)
            m.Hide();
    }

    private void HideAllCheckpointMarkers()
    {
        foreach (var m in checkpointMarkers)
            m.Hide();
    }

    private void HideAllCannonMarkers()
    {
        foreach (var m in cannonMarkers)
            m.Hide();
    }

    private void HideAllPowerupMarkers()
    {
        foreach (var m in powerupMarkers)
            m.Hide();
    }

    private void HideAllTimeTravelMarkers()
    {
        foreach (var m in timeTravelMarkers)
            m.Hide();
    }

    private void HideEndPad()
    {
        if (endPadIconImage)
            endPadIconImage.gameObject.SetActive(false);

        if (endPadPointerImage)
            endPadPointerImage.gameObject.SetActive(false);
    }

    private void HideEverything()
    {
        HideAllGemMarkers();
        HideAllCheckpointMarkers();
        HideAllCannonMarkers();
        HideAllPowerupMarkers();
        HideAllTimeTravelMarkers();
        HideEndPad();
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void OnDestroy()
    {
        foreach (var m in gemMarkers)
        {
            if (m.icon)
                Destroy(m.icon.gameObject);

            if (m.pointer)
                Destroy(m.pointer.gameObject);
        }

        foreach (var m in checkpointMarkers)
        {
            if (m.icon)
                Destroy(m.icon.gameObject);

            if (m.pointer)
                Destroy(m.pointer.gameObject);
        }

        foreach (var m in cannonMarkers)
        {
            if (m.icon)
                Destroy(m.icon.gameObject);

            if (m.pointer)
                Destroy(m.pointer.gameObject);
        }

        foreach (var m in powerupMarkers)
        {
            if (m.icon)
                Destroy(m.icon.gameObject);

            if (m.pointer)
                Destroy(m.pointer.gameObject);
        }

        foreach (var m in timeTravelMarkers)
        {
            if (m.icon)
                Destroy(m.icon.gameObject);

            if (m.pointer)
                Destroy(m.pointer.gameObject);
        }

        if (endPadIconImage)
            Destroy(endPadIconImage.gameObject);

        if (endPadPointerImage)
            Destroy(endPadPointerImage.gameObject);
    }
}