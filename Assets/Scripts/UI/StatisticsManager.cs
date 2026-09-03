using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticsManager : MonoBehaviour
{
    [Header("UI")]
    public Button returnButton;

    [Header("Left Column")]
    public TextMeshProUGUI leftHeaderText;
    public TextMeshProUGUI leftCaptionText;
    public TextMeshProUGUI leftValueText;

    [Header("Right Column")]
    public TextMeshProUGUI rightHeaderText;
    public TextMeshProUGUI rightCaptionText;
    public TextMeshProUGUI rightValueText;

    [Header("Hardest Level")]
    public TextMeshProUGUI hardestLevelCaptionText;
    public TextMeshProUGUI hardestLevelValueText;
    public TextMeshProUGUI hardestLevelStatsText;

    [Header("Right Bottom")]
    public TextMeshProUGUI rightBottomCaptionText;
    public TextMeshProUGUI rightBottomValueText;

    private void Start()
    {
        returnButton.onClick.AddListener(() =>
        {
            GetComponent<PlayMissionManager>().ToggleStatisticsWindow(false);
            GetComponent<PlayMissionManager>().raycastBlocker.SetActive(false);
        });
    }

    public void InitStatistics()
    {
        TotalTimeTracker.instance?.SaveTotalTime();
        TotalTimeTracker.instance?.SaveCurrentLevelStatistics();

        leftHeaderText.text = "";
        leftCaptionText.text = "";
        leftValueText.text = "";

        rightHeaderText.text = "";
        rightCaptionText.text = "";
        rightValueText.text = "";

        rightBottomCaptionText.text = "";
        rightBottomValueText.text = "";

        hardestLevelCaptionText.text = "";
        hardestLevelValueText.text = "";
        hardestLevelStatsText.text = "";

        DisplayPlatinumQuestStatistics();
    }

    private void DisplayPlatinumQuestStatistics()
    {
        List<Mission> tutorial =
            MissionInfo.instance.missionsTutorial;

        List<Mission> beginner =
            MissionInfo.instance.missionsBeginner;

        List<Mission> intermediate =
            MissionInfo.instance.missionsIntermediate;

        List<Mission> advanced =
            MissionInfo.instance.missionsAdvanced;

        List<Mission> expert =
            MissionInfo.instance.missionsExpert;

        List<Mission> bonus =
            MissionInfo.instance.missionsBonus;

        // DC is intentionally not included in the Platinum Quest
        // campaign statistics.
        //
        // It remains available through:
        // GetMissionList(Type.dc)

        // ============================================================
        // LEVEL COUNTS
        // ============================================================

        int tutorialTotal = tutorial.Count;
        int beginnerTotal = beginner.Count;
        int intermediateTotal = intermediate.Count;
        int advancedTotal = advanced.Count;
        int expertTotal = expert.Count;
        int bonusTotal = bonus.Count;

        // Main campaign excludes Bonus.
        int totalLevelCount =
            tutorialTotal +
            beginnerTotal +
            intermediateTotal +
            advancedTotal +
            expertTotal;

        // Grand Total includes Bonus.
        int grandTotalLevelCount =
            totalLevelCount +
            bonusTotal;

        // ============================================================
        // COMPLETION
        // ============================================================

        int tutorialCompleted =
            GetTotalCompletion(tutorial);

        int beginnerCompleted =
            GetTotalCompletion(beginner);

        int intermediateCompleted =
            GetTotalCompletion(intermediate);

        int advancedCompleted =
            GetTotalCompletion(advanced);

        int expertCompleted =
            GetTotalCompletion(expert);

        int bonusCompleted =
            GetTotalCompletion(bonus);

        int totalCompleted =
            tutorialCompleted +
            beginnerCompleted +
            intermediateCompleted +
            advancedCompleted +
            expertCompleted;

        int grandTotalCompleted =
            totalCompleted +
            bonusCompleted;

        // ============================================================
        // MEDALS
        // ============================================================

        int platinumCount =
            GetTotalPlatinum(tutorial) +
            GetTotalPlatinum(beginner) +
            GetTotalPlatinum(intermediate) +
            GetTotalPlatinum(advanced) +
            GetTotalPlatinum(expert) +
            GetTotalPlatinum(bonus);

        int ultimateCount =
            GetTotalUltimate(tutorial) +
            GetTotalUltimate(beginner) +
            GetTotalUltimate(intermediate) +
            GetTotalUltimate(advanced) +
            GetTotalUltimate(expert) +
            GetTotalUltimate(bonus);

        int awesomeCount = 0;

        if (ShowAwesomeHints())
        {
            awesomeCount =
                GetTotalAwesome(tutorial) +
                GetTotalAwesome(beginner) +
                GetTotalAwesome(intermediate) +
                GetTotalAwesome(advanced) +
                GetTotalAwesome(expert) +
                GetTotalAwesome(bonus);
        }

        // ============================================================
        // EASTER EGGS
        // ============================================================

        int totalEggs =
            GetTotalEggs(tutorial) +
            GetTotalEggs(beginner) +
            GetTotalEggs(intermediate) +
            GetTotalEggs(advanced) +
            GetTotalEggs(expert) +
            GetTotalEggs(bonus);

        int collectedEggs =
            PlayerPrefs.GetInt(
                "EasterEggCollected",
                0
            );

        // ============================================================
        // OTHER STATISTICS
        // ============================================================

        int outOfBounds =
            PlayerPrefs.GetInt(
                "OutOfBoundsCount",
                0
            );

        float totalRuntimeSeconds =
            PlayerPrefs.GetFloat(
                "TotalRuntimeSeconds",
                0f
            );

        // Total Level Times has special rules for Hunt and Madness.
        int totalLevelTime =
            GetTotalTime(tutorial) +
            GetTotalTime(beginner) +
            GetTotalTime(intermediate) +
            GetTotalTime(advanced) +
            GetTotalTime(expert) +
            GetTotalTime(bonus);

        // ============================================================
        // LEFT HEADER
        // ============================================================

        leftHeaderText.text =
            "Levels Completed (Platinum Quest)";

        // ============================================================
        // LEFT CAPTIONS
        // ============================================================

        leftCaptionText.text =
            "Tutorial:\n" +
            "Beginner:\n" +
            "Intermediate:\n" +
            "Advanced:\n" +
            "Expert:\n" +
            "Total:\n\n" +
            "Bonus:\n\n" +
            "Grand Total:";

        // ============================================================
        // LEFT VALUES
        // ============================================================

        leftValueText.text =
            FormatCompletion(
                tutorialCompleted,
                tutorialTotal
            ) + "\n" +

            FormatCompletion(
                beginnerCompleted,
                beginnerTotal
            ) + "\n" +

            FormatCompletion(
                intermediateCompleted,
                intermediateTotal
            ) + "\n" +

            FormatCompletion(
                advancedCompleted,
                advancedTotal
            ) + "\n" +

            FormatCompletion(
                expertCompleted,
                expertTotal
            ) + "\n" +

            FormatCompletion(
                totalCompleted,
                totalLevelCount
            ) + "\n\n" +

            FormatCompletion(
                bonusCompleted,
                bonusTotal
            ) + "\n\n" +

            FormatCompletion(
                grandTotalCompleted,
                grandTotalLevelCount
            );

        // ============================================================
        // RIGHT HEADER
        // ============================================================

        rightHeaderText.text =
            "Times / Other (Platinum Quest)";

        // ============================================================
        // RIGHT CAPTIONS
        // ============================================================

        string rightCaptions =
            "Platinum Times:\n" +
            "Ultimate Times:\n";

        if (ShowAwesomeHints())
        {
            rightCaptions +=
                "Awesome Times:\n";
        }

        rightCaptions +=
            "Nest Eggs:";

        rightCaptionText.text =
            rightCaptions;

        // ============================================================
        // RIGHT VALUES
        // ============================================================

        string rightValues =
            FormatPercentageValue(
                platinumCount,
                grandTotalLevelCount
            ) + "\n" +

            FormatPercentageValue(
                ultimateCount,
                grandTotalLevelCount
            ) + "\n";

        if (ShowAwesomeHints())
        {
            rightValues +=
                FormatPercentageValue(
                    awesomeCount,
                    grandTotalLevelCount
                ) + "\n";
        }

        rightValues +=
            FormatPercentageValue(
                collectedEggs,
                totalEggs
            );

        rightValueText.text =
            rightValues;

        string bottomCaptions =
            "Marbles Lost:\n" +
            "Total Level Times:\n" +
            "Total Wasted Time:";

        string bottomValues =
            outOfBounds + "\n" +
            FormatLevelTime(totalLevelTime) + "\n" +
            FormatWastedTime(
                Mathf.RoundToInt(totalRuntimeSeconds * 1000f)
            );

        string hardestLevel = TotalTimeTracker.instance?.GetHardestLevel();

        if (!string.IsNullOrEmpty(hardestLevel))
        {
            Mission hardestMission = FindMissionByLevelName(hardestLevel);

            if (hardestMission != null)
            {
                hardestLevelCaptionText.text = "Hardest Level:";
                hardestLevelValueText.text = hardestMission.levelName;
                hardestLevelStatsText.text = "(" + TotalTimeTracker.instance.GetLevelOOBs(hardestLevel) + " OOBs, " + TotalTimeTracker.instance.GetLevelRespawns(hardestLevel) + " Respawns, " + " and " + FormatLevelTime ((int)(TotalTimeTracker.instance.GetLevelTime(hardestLevel) * 1000)) + ")";
            }
        }

        rightBottomCaptionText.text = bottomCaptions;
        rightBottomValueText.text = bottomValues;
        // Bonus Unlock intentionally omitted.
    }

    // ================================================================
    // MISSION LISTS
    // ================================================================

    private Mission FindMissionByLevelName(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
            return null;

        List<Mission>[] missionLists =
        {
            MissionInfo.instance.missionsTutorial,
            MissionInfo.instance.missionsBeginner,
            MissionInfo.instance.missionsIntermediate,
            MissionInfo.instance.missionsAdvanced,
            MissionInfo.instance.missionsExpert,
            MissionInfo.instance.missionsBonus,
            MissionInfo.instance.missionsDC
        };

        foreach (List<Mission> missions in missionLists)
        {
            if (missions == null)
                continue;

            foreach (Mission mission in missions)
            {
                if (mission == null)
                    continue;

                if (mission.levelName == levelName)
                    return mission;
            }
        }

        return null;
    }

    private List<Mission> GetMissionList(Type type)
    {
        switch (type)
        {
            case Type.tutorial:
                return MissionInfo.instance.missionsTutorial;

            case Type.beginner:
                return MissionInfo.instance.missionsBeginner;

            case Type.intermediate:
                return MissionInfo.instance.missionsIntermediate;

            case Type.advanced:
                return MissionInfo.instance.missionsAdvanced;

            case Type.expert:
                return MissionInfo.instance.missionsExpert;

            case Type.bonus:
                return MissionInfo.instance.missionsBonus;

            case Type.dc:
                return MissionInfo.instance.missionsDC;

            default:
                return new List<Mission>();
        }
    }

    // ================================================================
    // COMPLETION
    // ================================================================

    private int GetTotalCompletion(List<Mission> missions)
    {
        int count = 0;

        if (missions == null)
            return 0;

        foreach (Mission mission in missions)
        {
            if (mission == null)
                continue;

            string key =
                $"{mission.levelName}_Time_0";

            if (!PlayerPrefs.HasKey(key))
                continue;

            // GameManager stores leaderboard results as FLOATS.
            float bestValue =
                PlayerPrefs.GetFloat(
                    key,
                    -1f
                );

            if (bestValue >= 0f)
                count++;
        }

        return count;
    }

    // ================================================================
    // PLATINUM
    // ================================================================

    private int GetTotalPlatinum(List<Mission> missions)
    {
        int count = 0;

        if (missions == null)
            return 0;

        foreach (Mission mission in missions)
        {
            if (mission == null)
                continue;

            if (mission.platinumTime < 0)
                continue;

            if (!TryGetBestValue(
                mission,
                out float bestValue))
            {
                continue;
            }

            if (BeatsThreshold(
                mission,
                bestValue,
                mission.platinumTime))
            {
                count++;
            }
        }

        return count;
    }

    // ================================================================
    // ULTIMATE
    // ================================================================

    private int GetTotalUltimate(List<Mission> missions)
    {
        int count = 0;

        if (missions == null)
            return 0;

        foreach (Mission mission in missions)
        {
            if (mission == null)
                continue;

            if (mission.ultimateTime < 0)
                continue;

            if (!TryGetBestValue(
                mission,
                out float bestValue))
            {
                continue;
            }

            if (BeatsThreshold(
                mission,
                bestValue,
                mission.ultimateTime))
            {
                count++;
            }
        }

        return count;
    }

    // ================================================================
    // AWESOME
    // ================================================================

    private int GetTotalAwesome(List<Mission> missions)
    {
        int count = 0;

        if (missions == null)
            return 0;

        foreach (Mission mission in missions)
        {
            if (mission == null)
                continue;

            if (mission.awesomeTime < 0)
                continue;

            if (!TryGetBestValue(
                mission,
                out float bestValue))
            {
                continue;
            }

            if (BeatsThreshold(
                mission,
                bestValue,
                mission.awesomeTime))
            {
                count++;
            }
        }

        return count;
    }

    // ================================================================
    // BEST VALUE
    // ================================================================

    private bool TryGetBestValue(
        Mission mission,
        out float value)
    {
        value = -1f;

        if (mission == null)
            return false;

        string key =
            $"{mission.levelName}_Time_0";

        if (!PlayerPrefs.HasKey(key))
            return false;

        value =
            PlayerPrefs.GetFloat(
                key,
                -1f
            );

        return value >= 0f;
    }

    // ================================================================
    // THRESHOLD COMPARISON
    // ================================================================

    private bool BeatsThreshold(
        Mission mission,
        float bestValue,
        int threshold)
    {
        if (mission == null)
            return false;

        // -1 means the medal does not exist.
        if (threshold < 0)
            return false;

        bool isHunt =
            ContainsMode(
                mission,
                Mode.Hunt
            );

        bool isMadness =
            ContainsMode(
                mission,
                Mode.Madness
            );

        // ============================================================
        // HUNT
        // ============================================================
        //
        // Hunt is always score-based.
        //
        // Higher score is better.
        //

        if (isHunt)
        {
            return bestValue >= threshold;
        }

        // ============================================================
        // MADNESS
        // ============================================================
        //
        // < 1000  = SCORE
        // >= 1000 = TIME
        //
        // Score vs Score:
        //     Higher score is better.
        //
        // Time vs Time:
        //     Lower time is better.
        //
        // Time vs Score:
        //     TIME ALWAYS WINS.
        //
        // Score vs Time:
        //     SCORE NEVER WINS.
        //

        if (isMadness)
        {
            bool bestIsScore =
                bestValue < 1000f;

            bool bestIsTime =
                bestValue >= 1000f;

            bool thresholdIsScore =
                threshold < 1000;

            bool thresholdIsTime =
                threshold >= 1000;

            // --------------------------------------------------------
            // Score threshold
            // --------------------------------------------------------

            if (thresholdIsScore)
            {
                // A time is always better than a score.
                if (bestIsTime)
                    return true;

                // Both are scores.
                // Higher score is better.
                if (bestIsScore)
                    return bestValue >= threshold;

                return false;
            }

            // --------------------------------------------------------
            // Time threshold
            // --------------------------------------------------------

            if (thresholdIsTime)
            {
                // A score can never beat a time.
                if (bestIsScore)
                    return false;

                // Both are times.
                // Lower time is better.
                if (bestIsTime)
                    return bestValue < threshold;

                return false;
            }

            return false;
        }

        // ============================================================
        // NORMAL TIME-BASED MODES
        // ============================================================
        //
        // Lower time is better.
        //

        return bestValue < threshold;
    }

    // ================================================================
    // MODE CHECK
    // ================================================================

    private bool ContainsMode(
        Mission mission,
        Mode mode)
    {
        if (mission == null ||
            mission.gameModes == null)
        {
            return false;
        }

        foreach (Mode missionMode in mission.gameModes)
        {
            if (missionMode == mode)
                return true;
        }

        return false;
    }

    // ================================================================
    // EASTER EGGS
    // ================================================================

    private int GetTotalEggs(List<Mission> missions)
    {
        int count = 0;

        if (missions == null)
            return 0;

        foreach (Mission mission in missions)
        {
            if (mission == null)
                continue;

            if (mission.hasEgg)
                count++;
        }

        return count;
    }

    // ================================================================
    // TOTAL LEVEL TIME
    // ================================================================

    private int GetTotalTime(List<Mission> missions)
    {
        int total = 0;

        if (missions == null)
            return 0;

        foreach (Mission mission in missions)
        {
            if (mission == null)
                continue;

            if (!TryGetBestValue(
                mission,
                out float bestValue))
            {
                continue;
            }

            bool isHunt =
                ContainsMode(
                    mission,
                    Mode.Hunt
                );

            bool isMadness =
                ContainsMode(
                    mission,
                    Mode.Madness
                );

            // ========================================================
            // HUNT
            // ========================================================
            //
            // Hunt's saved result is a score.
            // Use the mission's time limit instead.
            //

            if (isHunt)
            {
                if (mission.time >= 0)
                    total += mission.time;

                continue;
            }

            // ========================================================
            // MADNESS
            // ========================================================
            //
            // < 1000 = score
            // >= 1000 = time
            //
            // Score result:
            //     use mission.time
            //
            // Time result:
            //     use saved time
            //

            if (isMadness)
            {
                if (bestValue < 1000f)
                {
                    // Score-based Madness result.
                    if (mission.time >= 0)
                        total += mission.time;
                }
                else
                {
                    // Time-based Madness result.
                    total += Mathf.RoundToInt(
                        bestValue
                    );
                }

                continue;
            }

            // ========================================================
            // NORMAL TIME-BASED MODES
            // ========================================================
            //
            // The saved value is the player's best time.
            //

            total += Mathf.RoundToInt(
                bestValue
            );
        }

        return total;
    }

    // ================================================================
    // FORMATTING
    // ================================================================

    private string FormatCompletion(
        int completed,
        int total)
    {
        float percentage =
            GetPercentage(
                completed,
                total
            );

        return completed +
               "/" +
               total +
               " (" +
               percentage.ToString("0") +
               "%)";
    }

    private string FormatPercentageValue(
        int value,
        int total)
    {
        float percentage =
            GetPercentage(
                value,
                total
            );

        return value +
               "/" +
               total +
               " (" +
               percentage.ToString("0") +
               "%)";
    }

    private float GetPercentage(
        int value,
        int total)
    {
        if (total <= 0)
            return 0f;

        return (float)value /
               total *
               100f;
    }

    private string FormatLevelTime(int milliseconds)
    {
        if (milliseconds < 0)
            milliseconds = 0;

        int totalSeconds = milliseconds / 1000;

        int seconds = totalSeconds % 60;
        int minutes = totalSeconds / 60;
        int millisecondsPart = milliseconds % 1000;

        return string.Format(
            "{0:00}:{1:00}.{2:000}",
            minutes,
            seconds,
            millisecondsPart
        );
    }

    private string FormatWastedTime(int milliseconds)
    {
        if (milliseconds < 0)
            milliseconds = 0;

        int totalSeconds = milliseconds / 1000;

        int seconds = totalSeconds % 60;
        int minutes = (totalSeconds / 60) % 60;
        int hours = (totalSeconds / 3600) % 24;
        int days = totalSeconds / 86400;

        return string.Format(
            "{0:00}:{1:00}:{2:00}:{3:00}",
            days,
            hours,
            minutes,
            seconds
        );
    }

    // ================================================================
    // AWESOME HINTS
    // ================================================================

    private bool ShowAwesomeHints()
    {
        return PlayerPrefs.GetInt(
            "ShowAwesomeHints",
            0
        ) != 0;
    }

    // ================================================================
    // COLOR
    // ================================================================

    private string GetColor(
        int value,
        int total)
    {
        if (total <= 0)
            return "white";

        float percentage =
            GetPercentage(
                value,
                total
            );

        if (percentage >= 100f)
            return "green";

        if (percentage >= 50f)
            return "yellow";

        return "white";
    }
}