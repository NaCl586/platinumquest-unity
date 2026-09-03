using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntMode : NullMode
{
    // ============================================================
    // GEM SPAWN DATA
    // ============================================================

    private class GemSpawnPoint
    {
        public Gem gem;
        public int netIndex;
        public float weight;

        public GemSpawnPoint(
            Gem gem,
            int netIndex)
        {
            this.gem = gem;
            this.netIndex = netIndex;
            this.weight = 0f;
        }
    }

    private class GemSpawnCandidate
    {
        public int gemIndex;
        public float weight;

        public GemSpawnCandidate(
            int gemIndex,
            float weight)
        {
            this.gemIndex = gemIndex;
            this.weight = weight;
        }
    }

    private class HuntGemGroup
    {
        public List<int> gemIndices =
            new List<int>();

        public int spawnCount;

        public HuntGemGroup(
            IEnumerable<int> gemIndices)
        {
            this.gemIndices.AddRange(
                gemIndices
            );

            spawnCount = 0;
        }
    }

    // ============================================================
    // GEM STATE
    // ============================================================

    private readonly List<GemSpawnPoint>
        gemSpawnPoints =
            new List<GemSpawnPoint>();

    private readonly List<Gem>
        activeGems =
            new List<Gem>();

    private readonly List<int>
        activeGemSpawnGroup =
            new List<int>();

    private readonly List<Gem>
        collectedGems =
            new List<Gem>();

    private GemSpawnPoint lastSpawn;

    private bool gemsPrepared;
    private bool isFirstGemSpawn;

    // ============================================================
    // GEM GROUPS
    // ============================================================

    private readonly List<HuntGemGroup>
        huntGemGroups =
            new List<HuntGemGroup>();

    /*
     * MissionInfo.gemGroups:
     *
     * 0 = No
     * 1 = Spawn Whole Group
     * 2 = Random Spawn in Group
     */
    private int huntGemGroupsMode;

    // ============================================================
    // SCORE
    // ============================================================

    private int points;

    // ============================================================
    // ALARM
    // ============================================================

    // MissionInfo.alarmTime is the number of seconds remaining
    // at which the Hunt alarm should begin.
    private float alarmStartTime;
    private bool alarmActive;
    private Coroutine alarmCoroutine;

    // ============================================================
    // HUNT PARAMETERS
    // ============================================================

    /*
     * These correspond to the parameters exposed by the
     * PlatinumQuest Hunt editor.
     */

    private float gemGroupRadius = 15f;

    private int maxGemsPerSpawn = 7;

    private float spawnBlock = 30f;

    private float redSpawnChance = 0.9f;
    private float yellowSpawnChance = 0.65f;
    private float blueSpawnChance = 0.35f;
    private float platinumSpawnChance = 0.18f;

    // ============================================================
    // INTERNAL HUNT CONSTANTS
    // ============================================================

    /*
     * These are NOT MissionInfo parameters.
     *
     * They are internal values used by the Hunt spawning
     * algorithm in the Haxe implementation.
     */

    private const int MinPointsPerSpawn = 5;
    private const int MinGemsPerSpawn = 3;
    private const int MaxSpawnSearchLoops = 15;

    private const float MaxPoints = 9999f;

    // ============================================================
    // RANDOM
    // ============================================================

    private System.Random rng;
    private System.Random rng2;

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public HuntMode(GameManager gameManager)
        : base(gameManager)
    {
    }

    // ============================================================
    // MISSION LOAD
    // ============================================================

    public override void OnMissionLoad()
    {
        base.OnMissionLoad();

        points = 0;

        activeGems.Clear();
        activeGemSpawnGroup.Clear();
        collectedGems.Clear();

        huntGemGroups.Clear();

        lastSpawn = null;

        gemsPrepared = false;
        isFirstGemSpawn = true;

        // --------------------------------------------------------
        // Hunt defaults
        // --------------------------------------------------------

        gemGroupRadius = 15f;

        maxGemsPerSpawn = 7;

        spawnBlock =
            gemGroupRadius * 2f;

        /*
         * The editor uses the chance sliders as mission
         * parameters. Zero means no additional chance.
         */
        redSpawnChance = 0.9f;
        yellowSpawnChance = 0.65f;
        blueSpawnChance = 0.35f;
        platinumSpawnChance = 0.18f;

        huntGemGroupsMode = 0;

        // --------------------------------------------------------
        // Alarm
        // --------------------------------------------------------

        alarmStartTime =
            MissionInfo.instance != null
                ? MissionInfo.instance.alarmTime
                : 0f;

        alarmActive = false;
        StopHuntAlarm();

        // --------------------------------------------------------
        // Mission overrides
        // --------------------------------------------------------

        if (MissionInfo.instance != null)
        {
            if (
                MissionInfo.instance.maxGemsPerSpawn
                > 0
            )
            {
                maxGemsPerSpawn =
                    MissionInfo.instance
                        .maxGemsPerSpawn;
            }

            if (
                MissionInfo.instance.radiusFromGem
                > 0f
            )
            {
                gemGroupRadius =
                    MissionInfo.instance
                        .radiusFromGem;
            }

            /*
             * spawnBlock defaults to:
             *
             *     radiusFromGem * 2
             *
             * but the mission may explicitly override it.
             */
            if (
                MissionInfo.instance.spawnBlock
                > 0f
            )
            {
                spawnBlock =
                    MissionInfo.instance
                        .spawnBlock;
            }
            else
            {
                spawnBlock =
                    gemGroupRadius * 2f;
            }

            redSpawnChance =
                Mathf.Clamp01(
                    MissionInfo.instance
                        .redSpawnChance
                );

            yellowSpawnChance =
                Mathf.Clamp01(
                    MissionInfo.instance
                        .yellowSpawnChance
                );

            blueSpawnChance =
                Mathf.Clamp01(
                    MissionInfo.instance
                        .blueSpawnChance
                );

            platinumSpawnChance =
                Mathf.Clamp01(
                    MissionInfo.instance
                        .platinumSpawnChance
                );

            huntGemGroupsMode =
                Mathf.Clamp(
                    MissionInfo.instance.gemGroups,
                    0,
                    2
                );
        }

        // --------------------------------------------------------
        // Random
        // --------------------------------------------------------

        rng =
            new System.Random(
                UnityEngine.Random.Range(
                    0,
                    int.MaxValue
                )
            );

        rng2 =
            new System.Random(
                UnityEngine.Random.Range(
                    0,
                    int.MaxValue
                )
            );

        // --------------------------------------------------------
        // Prepare gems
        // --------------------------------------------------------

        PrepareGems();

        // --------------------------------------------------------
        // Prepare authored groups
        // --------------------------------------------------------

        BuildMissionGemGroups();

        // --------------------------------------------------------
        // First spawn
        // --------------------------------------------------------

        isFirstGemSpawn = true;

        SpawnHuntGems();

        isFirstGemSpawn = false;

        UpdateScoreUI();
    }

    // ============================================================
    // PREPARE GEMS
    // ============================================================

    private void PrepareGems()
    {
        gemSpawnPoints.Clear();

        Gem[] gems =
            gameManager.Gems;

        if (gems == null)
            return;

        for (
            int i = 0;
            i < gems.Length;
            i++
        )
        {
            Gem gem = gems[i];

            if (gem == null)
                continue;

            GemSpawnPoint spawn =
                new GemSpawnPoint(
                    gem,
                    gemSpawnPoints.Count
                );

            gemSpawnPoints.Add(
                spawn
            );

            /*
             * All Hunt gems start hidden.
             */
            gem.gameObject.SetActive(
                false
            );
        }

        gemsPrepared = true;
    }

    // ============================================================
    // BUILD MISSION GEM GROUPS
    // ============================================================

    private void BuildMissionGemGroups()
    {
        huntGemGroups.Clear();

        /*
         * MissionInfo.gemGroups is only the MODE.
         *
         * The actual authored groups must be populated by the
         * mission importer.
         *
         * Until then, normal Hunt spawning will be used if the
         * list is empty.
         */
        if (MissionInfo.instance == null)
            return;

        /*
         * This expects MissionInfo to eventually expose:
         *
         *     List<MissionGemGroup> huntGemGroups
         *
         * populated from the "GemGroups" SimGroup.
         *
         * Do not create fake groups here.
         */
    }

    // ============================================================
    // SPAWN ENTRY
    // ============================================================

    private void SpawnHuntGems(
        bool force = false)
    {
        if (
            activeGems.Count != 0 &&
            !force
        )
        {
            return;
        }

        if (
            huntGemGroupsMode > 0 &&
            huntGemGroups.Count > 0
        )
        {
            SpawnHuntGemGroupsMode(
                force
            );

            return;
        }

        SpawnHuntGemsFromPool(
            gemSpawnPoints,
            force
        );
    }

    // ============================================================
    // NORMAL HUNT SPAWN
    // ============================================================

    private void SpawnHuntGemsFromPool(
        List<GemSpawnPoint> pool,
        bool force = false)
    {
        if (
            pool == null ||
            pool.Count == 0
        )
        {
            return;
        }

        // --------------------------------------------------------
        // Last spawn position
        // --------------------------------------------------------

        Vector3? lastPosition = null;

        if (
            lastSpawn != null &&
            lastSpawn.gem != null
        )
        {
            lastPosition =
                lastSpawn.gem.transform.position;
        }

        // --------------------------------------------------------
        // Find valid spawn center
        // --------------------------------------------------------

        float furthestDistance = 0f;

        GemSpawnPoint furthest = null;

        List<GemSpawnPoint> validCenters =
            new List<GemSpawnPoint>();

        /*
         * Haxe samples ten random candidates.
         */
        for (
            int i = 0;
            i < 10;
            i++
        )
        {
            GemSpawnPoint gem =
                pool[
                    rng.Next(
                        0,
                        pool.Count
                    )
                ];

            if (
                gem == null ||
                gem.gem == null
            )
            {
                continue;
            }

            if (!lastPosition.HasValue)
            {
                validCenters.Add(
                    gem
                );

                continue;
            }

            float distance =
                Vector3.Distance(
                    gem.gem.transform.position,
                    lastPosition.Value
                );

            distance +=
                gem.weight;

            if (
                distance <
                spawnBlock
            )
            {
                if (
                    distance >
                    furthestDistance
                )
                {
                    furthestDistance =
                        distance -
                        gem.weight;

                    furthest = gem;
                }
            }
            else
            {
                validCenters.Add(
                    gem
                );
            }
        }

        if (furthest != null)
        {
            validCenters.Add(
                furthest
            );
        }

        GemSpawnPoint validGem = null;

        if (validCenters.Count > 0)
        {
            /*
             * PQ draws five times and uses the final result.
             */
            for (
                int i = 0;
                i < 5;
                i++
            )
            {
                validGem =
                    validCenters[
                        rng.Next(
                            0,
                            validCenters.Count
                        )
                    ];
            }
        }

        if (validGem == null)
        {
            validGem =
                pool[
                    rng.Next(
                        0,
                        pool.Count
                    )
                ];
        }

        if (
            validGem == null ||
            validGem.gem == null
        )
        {
            return;
        }

        Vector3 centerPosition =
            validGem.gem.transform.position;

        // --------------------------------------------------------
        // Gather candidates
        // --------------------------------------------------------

        List<GemSpawnCandidate> spawnables =
            new List<GemSpawnCandidate>();

        HashSet<int> spawnablesSet =
            new HashSet<int>();

        int gatherPoints =
            1 +
            GetGemWeight(
                validGem.gem
            );

        int gatherLoops = 0;

        float searchRadius =
            gemGroupRadius;

        while (
            (
                gatherPoints <
                MinPointsPerSpawn
                ||
                spawnables.Count <
                MinGemsPerSpawn
            )
            &&
            gatherLoops < 2
        )
        {
            foreach (
                GemSpawnPoint gemElem
                in pool
            )
            {
                if (
                    gemElem == null ||
                    gemElem.gem == null
                )
                {
                    continue;
                }

                if (
                    gemElem ==
                    validGem
                )
                {
                    continue;
                }

                if (
                    spawnablesSet.Contains(
                        gemElem.netIndex
                    )
                )
                {
                    continue;
                }

                Vector3 delta =
                    gemElem.gem.transform.position -
                    centerPosition;

                float distance =
                    delta.magnitude;

                if (
                    distance >=
                    searchRadius
                )
                {
                    continue;
                }

                spawnablesSet.Add(
                    gemElem.netIndex
                );

                /*
                 * Unity level coordinates use Y as vertical.
                 * The original Haxe code uses the vertical
                 * coordinate in its weighting calculation.
                 */
                float randomPart =
                    rng.Next(
                        0,
                        GetGemWeight(
                            gemElem.gem
                        ) + 4
                    );

                float candidateWeight =
                    searchRadius -
                    distance -
                    Mathf.Abs(
                        delta.y
                    ) +
                    randomPart;

                spawnables.Add(
                    new GemSpawnCandidate(
                        gemElem.netIndex,
                        candidateWeight
                    )
                );

                gatherPoints +=
                    GetGemWeight(
                        gemElem.gem
                    ) + 1;
            }

            searchRadius *= 2f;

            gatherLoops++;
        }

        // --------------------------------------------------------
        // Sort candidates
        // --------------------------------------------------------

        spawnables.Sort(
            (a, b) =>
                b.weight.CompareTo(
                    a.weight
                )
        );

        // --------------------------------------------------------
        // Existing active set
        // --------------------------------------------------------

        HashSet<int> activeSet =
            new HashSet<int>();

        if (force)
        {
            foreach (
                int index
                in activeGemSpawnGroup
            )
            {
                activeSet.Add(
                    index
                );
            }
        }

        // --------------------------------------------------------
        // Select spawn set
        // --------------------------------------------------------

        List<int> spawnSet =
            new List<int>();

        spawnSet.Add(
            validGem.netIndex
        );

        int spawned = 1;

        int selectedPoints =
            1 +
            GetGemWeight(
                validGem.gem
            );

        int selectionLoops = 0;

        while (
            (
                selectedPoints <
                MinPointsPerSpawn
                ||
                spawned <
                MinGemsPerSpawn
            )
            &&
            selectionLoops <
            MaxSpawnSearchLoops
        )
        {
            int count =
                Mathf.Min(
                    spawnables.Count,
                    maxGemsPerSpawn - 1
                );

            for (
                int i = 0;
                i < count;
                i++
            )
            {
                GemSpawnCandidate candidate =
                    spawnables[i];

                if (
                    activeSet.Contains(
                        candidate.gemIndex
                    )
                )
                {
                    continue;
                }

                if (
                    candidate.gemIndex < 0 ||
                    candidate.gemIndex >=
                    gemSpawnPoints.Count
                )
                {
                    continue;
                }

                Gem candidateGem =
                    gemSpawnPoints[
                        candidate.gemIndex
                    ].gem;

                if (
                    !TestSpawn(
                        candidateGem
                    )
                )
                {
                    continue;
                }

                int value =
                    1 +
                    GetGemWeight(
                        candidateGem
                    );

                if (
                    selectedPoints +
                    value >
                    MaxPoints
                )
                {
                    continue;
                }

                if (
                    !spawnSet.Contains(
                        candidate.gemIndex
                    )
                )
                {
                    spawnSet.Add(
                        candidate.gemIndex
                    );
                }

                selectedPoints +=
                    value;

                spawned++;

                if (
                    spawned >=
                    maxGemsPerSpawn
                )
                {
                    break;
                }
            }

            selectionLoops++;
        }

        // --------------------------------------------------------
        // Update spawn weights
        // --------------------------------------------------------

        float maxDistance = 0f;

        foreach (
            int index
            in spawnSet
        )
        {
            if (
                index < 0 ||
                index >=
                gemSpawnPoints.Count
            )
            {
                continue;
            }

            Gem gem =
                gemSpawnPoints[
                    index
                ].gem;

            if (gem == null)
                continue;

            float distance =
                Vector3.Distance(
                    gem.transform.position,
                    centerPosition
                );

            if (
                distance >
                maxDistance
            )
            {
                maxDistance =
                    distance;
            }
        }

        if (maxDistance > 0f)
        {
            foreach (
                int index
                in spawnSet
            )
            {
                if (
                    index < 0 ||
                    index >=
                    gemSpawnPoints.Count
                )
                {
                    continue;
                }

                Gem gem =
                    gemSpawnPoints[
                        index
                    ].gem;

                if (gem == null)
                    continue;

                float distance =
                    Vector3.Distance(
                        gem.transform.position,
                        centerPosition
                    );

                distance /=
                    maxDistance;

                float distanceWeight =
                    Mathf.Floor(
                        (
                            1f -
                            distance
                        ) *
                        10f
                    );

                gemSpawnPoints[
                    index
                ].weight +=
                    distanceWeight;
            }
        }

        // --------------------------------------------------------
        // Normalize weights
        // --------------------------------------------------------

        float minimumWeight =
            float.MaxValue;

        foreach (
            GemSpawnPoint gem
            in gemSpawnPoints
        )
        {
            if (
                gem.weight <
                minimumWeight
            )
            {
                minimumWeight =
                    gem.weight;
            }
        }

        if (
            minimumWeight !=
            float.MaxValue
        )
        {
            foreach (
                GemSpawnPoint gem
                in gemSpawnPoints
            )
            {
                gem.weight -=
                    minimumWeight;
            }
        }

        // --------------------------------------------------------
        // Spawn
        // --------------------------------------------------------

        foreach (
            int index
            in spawnSet
        )
        {
            SpawnGem(
                index
            );
        }

        CommitSpawnSet(
            spawnSet,
            force
        );

        lastSpawn =
            validGem;
    }

    // ============================================================
    // SPAWN CHANCE
    // ============================================================

    private bool TestSpawn(
        Gem gem)
    {
        if (gem == null)
            return false;

        float chance;

        switch (gem.gemType)
        {
            case GemType.Red:
                chance =
                    redSpawnChance;
                break;

            case GemType.Yellow:
                chance =
                    yellowSpawnChance;
                break;

            case GemType.Blue:
                chance =
                    blueSpawnChance;
                break;

            case GemType.Platinum:
                chance =
                    platinumSpawnChance;
                break;

            default:
                /*
                 * Non-scoring gem types aren't controlled by
                 * Hunt's four chance sliders.
                 */
                return true;
        }

        return
            UnityEngine.Random.value <=
            chance;
    }

    // ============================================================
    // SPAWN GEM
    // ============================================================

    private void SpawnGem(
        int index)
    {
        if (
            index < 0 ||
            index >=
            gemSpawnPoints.Count
        )
        {
            return;
        }

        Gem gem =
            gemSpawnPoints[
                index
            ].gem;

        if (gem == null)
            return;

        gem.gameObject.SetActive(
            true
        );

        if (
            !activeGems.Contains(
                gem
            )
        )
        {
            activeGems.Add(
                gem
            );
        }
    }

    // ============================================================
    // COMMIT SPAWN SET
    // ============================================================

    private void CommitSpawnSet(
        List<int> spawnSet,
        bool force)
    {
        if (!force)
        {
            activeGemSpawnGroup.Clear();

            activeGemSpawnGroup.AddRange(
                spawnSet
            );

            return;
        }

        List<int> uncollected =
            new List<int>();

        foreach (
            int index
            in activeGemSpawnGroup
        )
        {
            if (
                index < 0 ||
                index >=
                gemSpawnPoints.Count
            )
            {
                continue;
            }

            Gem gem =
                gemSpawnPoints[
                    index
                ].gem;

            if (
                gem != null &&
                gem.gameObject.activeSelf
            )
            {
                uncollected.Add(
                    index
                );
            }
        }

        activeGemSpawnGroup.Clear();

        activeGemSpawnGroup.AddRange(
            uncollected
        );

        foreach (
            int index
            in spawnSet
        )
        {
            if (
                !activeGemSpawnGroup.Contains(
                    index
                )
            )
            {
                activeGemSpawnGroup.Add(
                    index
                );
            }
        }
    }

    // ============================================================
    // AUTHORED GEM GROUPS
    // ============================================================

    private void SpawnHuntGemGroupsMode(
        bool force)
    {
        if (
            huntGemGroups.Count == 0
        )
        {
            return;
        }

        HuntGemGroup group = null;

        // --------------------------------------------------------
        // First spawn
        // --------------------------------------------------------

        if (isFirstGemSpawn)
        {
            int highestWeight = 0;

            foreach (
                HuntGemGroup g
                in huntGemGroups
            )
            {
                foreach (
                    int index
                    in g.gemIndices
                )
                {
                    if (
                        index < 0 ||
                        index >=
                        gemSpawnPoints.Count
                    )
                    {
                        continue;
                    }

                    int weight =
                        GetGemWeight(
                            gemSpawnPoints[
                                index
                            ].gem
                        );

                    if (
                        weight >
                        highestWeight
                    )
                    {
                        highestWeight =
                            weight;
                    }
                }
            }

            List<HuntGemGroup>
                validGroups =
                    new List<HuntGemGroup>();

            foreach (
                HuntGemGroup g
                in huntGemGroups
            )
            {
                foreach (
                    int index
                    in g.gemIndices
                )
                {
                    if (
                        index < 0 ||
                        index >=
                        gemSpawnPoints.Count
                    )
                    {
                        continue;
                    }

                    if (
                        GetGemWeight(
                            gemSpawnPoints[
                                index
                            ].gem
                        ) ==
                        highestWeight
                    )
                    {
                        if (
                            !validGroups.Contains(
                                g
                            )
                        )
                        {
                            validGroups.Add(
                                g
                            );
                        }
                    }
                }
            }

            if (
                validGroups.Count > 0
            )
            {
                group =
                    validGroups[
                        rng.Next(
                            0,
                            validGroups.Count
                        )
                    ];
            }
        }

        // --------------------------------------------------------
        // One group
        // --------------------------------------------------------

        else if (
            huntGemGroups.Count == 1
        )
        {
            group =
                huntGemGroups[0];
        }

        // --------------------------------------------------------
        // Weighted group selection
        // --------------------------------------------------------

        else
        {
            int maxSpawnCount = 0;

            foreach (
                HuntGemGroup g
                in huntGemGroups
            )
            {
                if (
                    g.spawnCount >
                    maxSpawnCount
                )
                {
                    maxSpawnCount =
                        g.spawnCount;
                }
            }

            List<HuntGemGroup>
                weighted =
                    new List<HuntGemGroup>();

            foreach (
                HuntGemGroup g
                in huntGemGroups
            )
            {
                int repeats =
                    maxSpawnCount -
                    g.spawnCount +
                    2;

                for (
                    int i = 0;
                    i < repeats;
                    i++
                )
                {
                    weighted.Add(
                        g
                    );
                }
            }

            if (
                weighted.Count > 0
            )
            {
                group =
                    weighted[
                        rng.Next(
                            0,
                            weighted.Count
                        )
                    ];
            }
        }

        if (group == null)
            return;

        group.spawnCount++;

        // --------------------------------------------------------
        // Random Spawn in Group
        // --------------------------------------------------------

        if (
            huntGemGroupsMode == 2
        )
        {
            List<GemSpawnPoint>
                pool =
                    new List<GemSpawnPoint>();

            foreach (
                int index
                in group.gemIndices
            )
            {
                if (
                    index >= 0 &&
                    index <
                    gemSpawnPoints.Count
                )
                {
                    pool.Add(
                        gemSpawnPoints[
                            index
                        ]
                    );
                }
            }

            SpawnHuntGemsFromPool(
                pool,
                force
            );
        }

        // --------------------------------------------------------
        // Spawn Whole Group
        // --------------------------------------------------------

        else
        {
            SpawnEntireGemGroup(
                group,
                force
            );
        }
    }

    // ============================================================
    // SPAWN ENTIRE GROUP
    // ============================================================

    private void SpawnEntireGemGroup(
        HuntGemGroup group,
        bool force)
    {
        List<int> spawnSet =
            new List<int>();

        foreach (
            int index
            in group.gemIndices
        )
        {
            SpawnGem(
                index
            );

            if (
                !spawnSet.Contains(
                    index
                )
            )
            {
                spawnSet.Add(
                    index
                );
            }
        }

        CommitSpawnSet(
            spawnSet,
            force
        );
    }

    // ============================================================
    // FIND GROUP
    // ============================================================

    private HuntGemGroup FindGemGroup(
        int gemIndex)
    {
        foreach (
            HuntGemGroup group
            in huntGemGroups
        )
        {
            if (
                group.gemIndices.Contains(
                    gemIndex
                )
            )
            {
                return group;
            }
        }

        return null;
    }

    // ============================================================
    // GEM PICKUP
    // ============================================================

    public override void OnGemCollected(
        Gem gem,
        int newGemCount)
    {
        if (gem == null)
            return;

        if (
            !activeGems.Contains(
                gem
            )
        )
        {
            return;
        }

        activeGems.Remove(
            gem
        );

        if (
            !collectedGems.Contains(
                gem
            )
        )
        {
            collectedGems.Add(
                gem
            );
        }

        int increment =
            GetGemScore(
                gem
            );

        if (increment > 0)
        {
            points +=
                increment;

            GameUIManager.instance
                .DisplayGemMessage(
                    "+" + increment,
                    GetGemMessageColor(
                        gem.gemType
                    )
                );

            UpdateScoreUI();
        }

        /*
         * When the currently active spawn group is empty,
         * spawn another group.
         */
        if (
            activeGems.Count == 0
        )
        {
            SpawnHuntGems();
        }
    }

    // ============================================================
    // SCORE
    // ============================================================

    private int GetGemScore(
        Gem gem)
    {
        if (gem == null)
            return 0;

        switch (gem.gemType)
        {
            case GemType.Red:
                return 1;

            case GemType.Yellow:
                return 2;

            case GemType.Blue:
                return 5;

            case GemType.Platinum:
                return 10;

            default:
                return 0;
        }
    }

    // ============================================================
    // GEM WEIGHT
    // ============================================================

    private int GetGemWeight(
        Gem gem)
    {
        if (gem == null)
            return 0;

        switch (gem.gemType)
        {
            case GemType.Red:
                return 0;

            case GemType.Yellow:
                return 1;

            case GemType.Blue:
                return 4;

            case GemType.Platinum:
                return 9;

            default:
                return 0;
        }
    }

    // ============================================================
    // MESSAGE COLOR
    // ============================================================

    private Color GetGemMessageColor(
        GemType gemType)
    {
        switch (gemType)
        {
            case GemType.Red:
                return new Color32(
                    255,
                    102,
                    102,
                    255
                );

            case GemType.Yellow:
                return new Color32(
                    255,
                    255,
                    102,
                    255
                );

            case GemType.Blue:
                return new Color32(
                    102,
                    102,
                    255,
                    255
                );

            case GemType.Platinum:
                return new Color32(
                    221,
                    221,
                    221,
                    255
                );

            default:
                return Color.white;
        }
    }

    // ============================================================
    // SCORE UI
    // ============================================================

    private void UpdateScoreUI()
    {
        if (
            GameUIManager.instance == null
        )
        {
            return;
        }

        GameUIManager.instance
            .SetCurrentMadnessHuntGem(
                points
            );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    public override void OnUpdate()
    {
        if (!GameManager.gameStart)
            return;

        if (GameManager.gameFinish)
        {
            StopHuntAlarm();
            return;
        }

        if (alarmStartTime <= 0f)
            return;

        // GameManager owns the authoritative Hunt countdown.
        // huntTimeRemaining is stored in milliseconds.
        float remainingSeconds =
            gameManager.huntTimeRemaining / 1000f;

        if (
            remainingSeconds <= alarmStartTime &&
            !alarmActive
        )
        {
            alarmActive = true;

            if (alarmCoroutine == null)
            {
                alarmCoroutine =
                    GameManager.instance.StartCoroutine(
                        HuntAlarmCoroutine()
                    );
            }
        }
    }

    // ============================================================
    // ALARM COROUTINE
    // ============================================================

    private IEnumerator HuntAlarmCoroutine()
    {
        GameUIManager.instance.SetCenterText(
            $"You have {Mathf.CeilToInt(alarmStartTime)} seconds remaining."
        );

        if (
            Marble.instance != null &&
            Marble.instance.alarmSound != null
        )
        {
            Marble.instance.alarmSound.Play();
        }

        while (alarmActive)
        {
            if (GameManager.gameFinish)
                break;

            // Use the actual Hunt countdown rather than maintaining
            // a second independent alarm timer.
            float remainingSeconds =
                gameManager.huntTimeRemaining / 1000f;

            GameUIManager.instance.SetTimerColor(
                Mathf.FloorToInt(remainingSeconds) % 2 == 0
            );

            yield return null;
        }

        GameUIManager.instance.SetTimerColor(false);

        if (
            Marble.instance != null &&
            Marble.instance.alarmSound != null
        )
        {
            Marble.instance.alarmSound.Stop();
        }

        alarmCoroutine = null;
    }

    // ============================================================
    // STOP ALARM
    // ============================================================

    private void StopHuntAlarm()
    {
        alarmActive = false;

        if (alarmCoroutine != null)
        {
            GameManager.instance.StopCoroutine(
                alarmCoroutine
            );

            alarmCoroutine = null;
        }

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.SetTimerColor(false);
        }

        if (
            Marble.instance != null &&
            Marble.instance.alarmSound != null
        )
        {
            Marble.instance.alarmSound.Stop();
        }
    }

    // ============================================================
    // RESTART
    // ============================================================

    public override void OnRestart()
    {
        StopHuntAlarm();

        points = 0;

        activeGems.Clear();
        activeGemSpawnGroup.Clear();
        collectedGems.Clear();

        lastSpawn = null;

        foreach (
            GemSpawnPoint spawn
            in gemSpawnPoints
        )
        {
            spawn.weight = 0f;

            if (spawn.gem != null)
            {
                spawn.gem.gameObject
                    .SetActive(false);
            }
        }

        foreach (
            HuntGemGroup group
            in huntGemGroups
        )
        {
            group.spawnCount = 0;
        }

        isFirstGemSpawn = true;

        SpawnHuntGems();

        isFirstGemSpawn = false;

        UpdateScoreUI();

        GameUIManager.instance.SetTimerText(
            MissionInfo.instance.time
        );
    }

    // ============================================================
    // RESPAWN
    // ============================================================

    public override void OnRespawn()
    {
        // Only do Hunt-specific reset work when this was a
        // full level restart (R or Pause -> Restart).
        //
        // Normal OOB respawns must preserve:
        // - Hunt timer
        // - gem count
        // - current Hunt gem positions
        if (!GameManager.instance.WasFullReset)
            return;

        OnRestart();
    }

    // ============================================================
    // CHECKPOINT
    // ============================================================

    public override void OnCheckpointReached()
    {
        /*
         * Hunt has no checkpoint-based score reset.
         */
    }

    // ============================================================
    // FINISH
    // ============================================================

    public override bool CanFinish()
    {
        /*
         * GameManager controls the Hunt timer.
         */
        return true;
    }

    public override string GetFinishMessage()
    {
        return
            "Congratulations! You've finished!";
    }

    // ============================================================
    // GEM MESSAGE
    // ============================================================

    public override string GetGemPickupMessage()
    {
        /*
         * Hunt displays its own +1/+2/+5/+10 message.
         */
        return string.Empty;
    }

    // ============================================================
    // GEM TARGET
    // ============================================================

    public override int GetGemTarget()
    {
        return 0;
    }

    // ============================================================
    // ALL-GEMS SOUND
    // ============================================================

    public override bool
        ShouldPlayCollectAllGemsSound(
            int newGemCount)
    {
        /*
         * Hunt doesn't have a normal "all gems collected"
         * condition.
         */
        return false;
    }

    // ============================================================
    // PUBLIC PROPERTIES
    // ============================================================

    public int Points =>
        points;

    public int HuntGemGroupsMode =>
        huntGemGroupsMode;

    public float GemGroupRadius =>
        gemGroupRadius;

    public int MaxGemsPerSpawn =>
        maxGemsPerSpawn;

    public float SpawnBlock =>
        spawnBlock;

    public float RedSpawnChance =>
        redSpawnChance;

    public float YellowSpawnChance =>
        yellowSpawnChance;

    public float BlueSpawnChance =>
        blueSpawnChance;

    public float PlatinumSpawnChance =>
        platinumSpawnChance;

    public float AlarmStartTime =>
        alarmStartTime;

    public bool AlarmActive =>
        alarmActive;

    public IReadOnlyList<Gem> ActiveGems =>
        activeGems;

    public IReadOnlyList<Gem> CollectedGems =>
        collectedGems;

    public IReadOnlyList<int>
        ActiveGemSpawnGroup =>
            activeGemSpawnGroup;

}