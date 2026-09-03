using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotalTimeTracker : MonoBehaviour
{
    public static TotalTimeTracker instance;

    private float sessionStart;

    private const string TotalTimeKey = "TotalRuntimeSeconds";

    // ================================================================
    // HARDest LEVEL
    // ================================================================

    private string currentLevelName;
    private float levelStartTime;

    private int currentLevelOOBs;
    private int currentLevelRespawns;

    private bool levelTrackingActive;

    private const string HardestLevelKey = "HardestLevel";

    private const string LevelOOBPrefix =
        "LevelStats_OOB_";

    private const string LevelRespawnPrefix =
        "LevelStats_Respawns_";

    private const string LevelTimePrefix =
        "LevelStats_Time_";

    // ================================================================
    // UNITY
    // ================================================================

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        sessionStart = Time.realtimeSinceStartup;
    }

    void OnApplicationQuit()
    {
        SaveTotalTime();

        // If a level is currently being tracked, save its current
        // statistics as well.
        SaveCurrentLevelStatistics();
    }

    // ================================================================
    // TOTAL RUNTIME
    // ================================================================

    public void SaveTotalTime()
    {
        float now = Time.realtimeSinceStartup;
        float sessionTime = now - sessionStart;

        if (sessionTime <= 0f)
            return;

        float total =
            PlayerPrefs.GetFloat(
                TotalTimeKey,
                0f
            );

        PlayerPrefs.SetFloat(
            TotalTimeKey,
            total + sessionTime
        );

        PlayerPrefs.Save();

        sessionStart = now;
    }

    // ================================================================
    // LEVEL TRACKING
    // ================================================================

    /// <summary>
    /// Begins tracking statistics for a level.
    /// Call this when a level starts.
    /// </summary>
    public void StartLevelTracking(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return;

        if (levelTrackingActive)
        {
            SaveCurrentLevelStatistics();
        }

        currentLevelName = levelName;
        levelStartTime = Time.realtimeSinceStartup;

        currentLevelOOBs = 0;
        currentLevelRespawns = 0;

        levelTrackingActive = true;

        RegisterTrackedLevel(levelName);
    }

    /// <summary>
    /// Stops tracking the current level and saves its statistics.
    /// Call this when leaving/restarting the level.
    /// </summary>
    public void StopLevelTracking()
    {
        if (!levelTrackingActive)
            return;

        SaveCurrentLevelStatistics();

        currentLevelName = null;
        levelTrackingActive = false;
    }

    /// <summary>
    /// Records one out-of-bounds event for the current level.
    /// </summary>
    public void RecordOutOfBounds()
    {
        if (!levelTrackingActive)
            return;

        currentLevelOOBs++;
    }

    /// <summary>
    /// Records one respawn for the current level.
    /// </summary>
    public void RecordRespawn()
    {
        if (!levelTrackingActive)
            return;

        currentLevelRespawns++;
    }

    /// <summary>
    /// Saves the current level's accumulated statistics.
    /// </summary>
    public void SaveCurrentLevelStatistics()
    {
        if (!levelTrackingActive ||
            string.IsNullOrEmpty(currentLevelName))
        {
            return;
        }

        float currentTime =
            Time.realtimeSinceStartup -
            levelStartTime;

        if (currentTime < 0f)
            currentTime = 0f;

        string oobKey =
            GetLevelOOBKey(currentLevelName);

        string respawnKey =
            GetLevelRespawnKey(currentLevelName);

        string timeKey =
            GetLevelTimeKey(currentLevelName);

        // ------------------------------------------------------------
        // Existing saved statistics
        // ------------------------------------------------------------

        int savedOOBs =
            PlayerPrefs.GetInt(
                oobKey,
                0
            );

        int savedRespawns =
            PlayerPrefs.GetInt(
                respawnKey,
                0
            );

        float savedTime =
            PlayerPrefs.GetFloat(
                timeKey,
                0f
            );

        // ------------------------------------------------------------
        // Add the current session
        // ------------------------------------------------------------

        PlayerPrefs.SetInt(
            oobKey,
            savedOOBs + currentLevelOOBs
        );

        PlayerPrefs.SetInt(
            respawnKey,
            savedRespawns + currentLevelRespawns
        );

        PlayerPrefs.SetFloat(
            timeKey,
            savedTime + currentTime
        );

        PlayerPrefs.Save();

        // Reset the current session portion so that calling this
        // method twice doesn't count the same time twice.
        levelStartTime =
            Time.realtimeSinceStartup;

        currentLevelOOBs = 0;
        currentLevelRespawns = 0;

        // Recalculate the hardest level.
        UpdateHardestLevel();
    }

    // ================================================================
    // LEVEL STATISTICS
    // ================================================================

    public int GetLevelOOBs(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return 0;

        return PlayerPrefs.GetInt(
            GetLevelOOBKey(levelName),
            0
        );
    }

    public int GetLevelRespawns(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return 0;

        return PlayerPrefs.GetInt(
            GetLevelRespawnKey(levelName),
            0
        );
    }

    public float GetLevelTime(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return 0f;

        return PlayerPrefs.GetFloat(
            GetLevelTimeKey(levelName),
            0f
        );
    }

    // ================================================================
    // HARDEST LEVEL
    // ================================================================

    /// <summary>
    /// Returns the currently calculated hardest level name.
    /// </summary>
    public string GetHardestLevel()
    {
        return PlayerPrefs.GetString(
            HardestLevelKey,
            ""
        );
    }

    /// <summary>
    /// Recalculates the hardest level from all levels whose
    /// statistics have been recorded.
    ///
    /// PQ difficulty formula:
    ///
    ///     OOBs + Respawns + (Time in minutes)
    /// </summary>
    public void UpdateHardestLevel()
    {
        string[] keys =
            PlayerPrefs.GetString(
                "TrackedLevelNames",
                ""
            ).Split('|');

        float highestDifficulty = -1f;
        string hardestLevel = "";

        foreach (string levelName in keys)
        {
            if (string.IsNullOrEmpty(levelName))
                continue;

            int oobs =
                GetLevelOOBs(levelName);

            int respawns =
                GetLevelRespawns(levelName);

            float timeSeconds =
                GetLevelTime(levelName);

            float timeMinutes =
                timeSeconds / 60f;

            float difficulty =
                oobs +
                respawns +
                timeMinutes;

            if (difficulty > highestDifficulty)
            {
                highestDifficulty = difficulty;
                hardestLevel = levelName;
            }
        }

        PlayerPrefs.SetString(
            HardestLevelKey,
            hardestLevel
        );

        PlayerPrefs.Save();
    }

    // ================================================================
    // REGISTER LEVEL
    // ================================================================

    private void RegisterTrackedLevel(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return;

        string storedLevels =
            PlayerPrefs.GetString(
                "TrackedLevelNames",
                ""
            );

        string[] levels =
            storedLevels.Split('|');

        foreach (string level in levels)
        {
            if (level == levelName)
                return;
        }

        if (string.IsNullOrEmpty(storedLevels))
        {
            storedLevels = levelName;
        }
        else
        {
            storedLevels += "|" + levelName;
        }

        PlayerPrefs.SetString(
            "TrackedLevelNames",
            storedLevels
        );
    }

    // ================================================================
    // KEY HELPERS
    // ================================================================

    private string GetLevelOOBKey(string levelName)
    {
        return LevelOOBPrefix + levelName;
    }

    private string GetLevelRespawnKey(string levelName)
    {
        return LevelRespawnPrefix + levelName;
    }

    private string GetLevelTimeKey(string levelName)
    {
        return LevelTimePrefix + levelName;
    }
}