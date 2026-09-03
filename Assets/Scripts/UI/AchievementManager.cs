/*using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Server;
using Server.DTOs.Responses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementManager : MonoBehaviour
{
    public Sprite[] achievSprite;
    public Image[] achievImage;
    public Button okayButton;

    public GameObject loadingText;

    public Button nextPage;
    public Button prevPage;
    public GameObject[] pages;
    int currPage = 0;

    private readonly Dictionary<string, MyRankResponse> userLeaderboardData =
        new Dictionary<string, MyRankResponse>();

    // Cached mission list to avoid recreating/re-fetching lists repeatedly
    private List<Mission> cachedAllMissions;

    private void Start()
    {
        // Cache all missions first before running any logic
        CacheAllMissions();

        okayButton.onClick.AddListener(() =>
        {
            GetComponent<PlayMissionManager>().ToggleAchievementWindow(false);
            GetComponent<PlayMissionManager>().raycastBlocker.SetActive(false);
        });

        currPage = 0;

        if (GetComponent<OfflinePlayMission>())
        {
            ShowCurrentPage();
            InitAchiev();
        }
        else if (GetComponent<LeaderboardsPlayMission>())
        {
            nextPage.onClick.AddListener(NextPage);
            prevPage.onClick.AddListener(PrevPage);
            InitLBAchiev();
        }
    }

    private void CacheAllMissions()
    {
        cachedAllMissions = new List<Mission>();

        if (MissionInfo.instance == null)
            return;

        if (MissionInfo.instance.missionsPlatinumBeginner != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsPlatinumBeginner);
        if (MissionInfo.instance.missionsPlatinumIntermediate != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsPlatinumIntermediate);
        if (MissionInfo.instance.missionsPlatinumAdvanced != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsPlatinumAdvanced);
        if (MissionInfo.instance.missionsPlatinumExpert != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsPlatinumExpert);

        if (MissionInfo.instance.missionsGoldBeginner != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsGoldBeginner);
        if (MissionInfo.instance.missionsGoldIntermediate != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsGoldIntermediate);
        if (MissionInfo.instance.missionsGoldAdvanced != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsGoldAdvanced);
        if (MissionInfo.instance.missionsGoldCustom != null)
            cachedAllMissions.AddRange(MissionInfo.instance.missionsGoldCustom);
    }

    async void InitLBAchiev()
    {
        loadingText.SetActive(true);

        for (int i = 0; i < pages.Length; i++)
            pages[i].SetActive(false);

        await LoadUserLeaderboardData();

        // =========================================================
        // ACHIEVEMENTS UNLOCKED
        // =========================================================

        List<int> unlockedAchievementIds = new List<int>();

        // 1. Find any Easter Egg.
        int totalEasterEgg = PlayerPrefs.GetInt("EasterEggCollected", 0);

        if (totalEasterEgg >= 1)
        {
            achievImage[0].sprite = achievSprite[0];

            unlockedAchievementIds.Add(1);
        }

        // 2. Find all Easter Eggs.
        const int totalEasterEggs = 120;

        if (totalEasterEgg >= totalEasterEggs)
        {
            achievImage[1].sprite = achievSprite[1];

            unlockedAchievementIds.Add(2);
        }

        // 3. Beat any level with a specified par time.
        if (HasAnySpecifiedParTime())
        {
            achievImage[2].sprite = achievSprite[2];

            unlockedAchievementIds.Add(3);
        }

        // 4. Learn the Time Modifier < 1.75 seconds.
        if (HasTime("Learn the Time Modifier", 1.75f))
        {
            achievImage[3].sprite = achievSprite[3];

            unlockedAchievementIds.Add(4);
        }

        // 5. Arch Acropolis < 7 seconds.
        if (HasTime("Arch Acropolis", 7f))
        {
            achievImage[4].sprite = achievSprite[4];

            unlockedAchievementIds.Add(5);
        }

        // 6. King of the Mountain < 9 seconds.
        if (HasTime("King of the Mountain", 9f))
        {
            achievImage[5].sprite = achievSprite[5];

            unlockedAchievementIds.Add(6);
        }

        // 7. Pinball Wizard < 10 seconds.
        if (HasTime("Pinball Wizard", 10f))
        {
            achievImage[6].sprite = achievSprite[6];

            unlockedAchievementIds.Add(7);
        }

        // 8. Ramps Reloaded < 15 seconds.
        if (HasTime("Ramps Reloaded", 15f))
        {
            achievImage[7].sprite = achievSprite[7];

            unlockedAchievementIds.Add(8);
        }

        // 9. Dive! < 17 seconds.
        if (HasTime("Dive!", 17f))
        {
            achievImage[8].sprite = achievSprite[8];

            unlockedAchievementIds.Add(9);
        }

        // 10. Acrobat < 18 seconds.
        if (HasTime("Acrobat", 18f))
        {
            achievImage[9].sprite = achievSprite[9];

            unlockedAchievementIds.Add(10);
        }

        // 11. Icarus < 20 seconds.
        if (HasTime("Icarus", 20f))
        {
            achievImage[10].sprite = achievSprite[10];

            unlockedAchievementIds.Add(11);
        }

        // 12. Airwalk < 25 seconds.
        if (HasTime("Airwalk", 25f))
        {
            achievImage[11].sprite = achievSprite[11];

            unlockedAchievementIds.Add(12);
        }

        // 13. Pathways < 30 seconds.
        if (HasTime("Pathways", 30f))
        {
            achievImage[12].sprite = achievSprite[12];

            unlockedAchievementIds.Add(13);
        }

        // 14. Siege < 40 seconds.
        if (HasTime("Siege", 40f))
        {
            achievImage[13].sprite = achievSprite[13];

            unlockedAchievementIds.Add(14);
        }

        // 15. Tightrope's gold time.
        if (HasBeatenGoldTime("Tightrope"))
        {
            achievImage[14].sprite = achievSprite[14];

            unlockedAchievementIds.Add(15);
        }

        // 16. Combo Course < 60 seconds.
        if (HasTime("Combo Course", 60f))
        {
            achievImage[15].sprite = achievSprite[15];

            unlockedAchievementIds.Add(16);
        }

        // 17. Thief < 60 seconds.
        if (HasTime("Thief", 60f))
        {
            achievImage[16].sprite = achievSprite[16];

            unlockedAchievementIds.Add(17);
        }

        // 18. Space Station's Ultimate Time.
        if (HasBeatenUltimateTime("Space Station"))
        {
            achievImage[17].sprite = achievSprite[17];

            unlockedAchievementIds.Add(18);
        }

        // 19. Battlecube Finale's Ultimate Time.
        if (HasBeatenUltimateTime("Battlecube Finale"))
        {
            achievImage[18].sprite = achievSprite[18];

            unlockedAchievementIds.Add(19);
        }

        // 20. Battlecube Finale < 8:30 = 510 seconds.
        if (HasTime("Battlecube Finale", 510f))
        {
            achievImage[19].sprite = achievSprite[19];

            unlockedAchievementIds.Add(20);
        }

        // 21. Catwalks Ultimate + Slowropes Ultimate.
        if (HasBeatenUltimateTime("Catwalks") && HasBeatenUltimateTime("Slowropes"))
        {
            achievImage[20].sprite = achievSprite[20];

            unlockedAchievementIds.Add(21);
        }

        // 22. Learn the Super Jump < 3.50 AND
        // There and Back Again < 10.00.
        if (HasTime("Learn the Super Jump", 3.50f) && HasTime("There and Back Again", 10f))
        {
            achievImage[21].sprite = achievSprite[21];

            unlockedAchievementIds.Add(22);
        }

        // 23. All three times required.
        if (
            HasTime("Moto-Marblecross", 5f)
            && HasTime("Monster Speedway Qualifying", 20f)
            && HasTime("Monster Speedway", 15f)
        )
        {
            achievImage[22].sprite = achievSprite[22];

            unlockedAchievementIds.Add(23);
        }

        // 24. All four times required.
        if (
            HasTime("Shimmy", 3f)
            && HasTime("Path of Least Resistance", 10f)
            && HasTime("Daedalus", 15f)
            && HasTime("Tango", 14f)
        )
        {
            achievImage[23].sprite = achievSprite[23];

            unlockedAchievementIds.Add(24);
        }

        // 25. ANY TWO of the six required times.
        int achievement25Count = 0;

        if (HasTime("Skyscraper", 60f))
            achievement25Count++;

        if (HasTime("Survival of the Fittest", 30f))
            achievement25Count++;

        if (HasTime("Great Divide Revisited", 30f))
            achievement25Count++;

        if (HasTime("Tower Maze", 20f))
            achievement25Count++;

        if (HasTime("Battlements", 17f))
            achievement25Count++;

        if (HasTime("Natural Selection", 20f))
            achievement25Count++;

        if (achievement25Count >= 2)
        {
            achievImage[24].sprite = achievSprite[24];

            unlockedAchievementIds.Add(25);
        }

        // 26. First place on any leaderboard.
        if (HasFirstPlaceOnAnyLeaderboard())
        {
            achievImage[25].sprite = achievSprite[25];

            unlockedAchievementIds.Add(26);
        }

        // =========================================================
        // RATING-BASED ACHIEVEMENTS
        //
        // These achievements have 0 rating value and therefore
        // are NOT sent to the achievement rating sync.
        // =========================================================

        GlobalRatingResponse? myRating = null;

        if (
            OnlineManager.Instance != null
            && OnlineManager.Instance.Auth != null
            && OnlineManager.Instance.Auth.IsLoggedIn
            && OnlineManager.Instance.Rating != null
        )
        {
            try
            {
                myRating = await OnlineManager.Instance.Rating.GetMyRatingAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "Failed to load player rating " + "for rating-based achievements."
                );

                Debug.LogException(ex);
            }
        }

        // 27. Reach 7 million points on your ranking for the MBG Leaderboards.
        if (myRating != null && myRating.MbgRating >= 7_000_000)
        {
            achievImage[26].sprite = achievSprite[26];
        }

        // 28. Reach 12 million points on your ranking for the MBP Leaderboards.
        if (myRating != null && myRating.MbpRating >= 12_000_000)
        {
            achievImage[27].sprite = achievSprite[27];
        }

        // 29. Achieve 30 million points on your username
        //     from the total of all leaderboards.
        if (myRating != null && myRating.GlobalRating >= 30_000_000)
        {
            achievImage[28].sprite = achievSprite[28];
        }

        // 30. Achieve 75 million points on your username
        //     from the total of all leaderboards.
        if (myRating != null && myRating.GlobalRating >= 75_000_000)
        {
            achievImage[29].sprite = achievSprite[29];
        }

        // =========================================================
        // SEND ACHIEVEMENT IDS TO SERVER
        // =========================================================

        Debug.Log($"Unlocked achievements: " + $"{string.Join(", ", unlockedAchievementIds)}");

        if (
            OnlineManager.Instance != null
            && OnlineManager.Instance.Auth != null
            && OnlineManager.Instance.Auth.IsLoggedIn
            && OnlineManager.Instance.Rating != null
        )
        {
            try
            {
                SyncAchievementsResponse response =
                    await OnlineManager.Instance.Rating.SyncAchievementsAsync(
                        unlockedAchievementIds
                    );

                Debug.Log($"Achievement Rating: " + $"{response.AchievementRating}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to sync achievements with server.");

                Debug.LogException(ex);
            }
        }

        loadingText.SetActive(false);
        ShowCurrentPage();
    }

    private bool HasFirstPlaceOnAnyLeaderboard()
    {
        foreach (KeyValuePair<string, MyRankResponse> pair in userLeaderboardData)
        {
            MyRankResponse data = pair.Value;

            if (data == null)
                continue;

            if (data.Rank == 1)
            {
                return true;
            }
        }

        return false;
    }

    private async UniTask LoadUserLeaderboardData()
    {
        userLeaderboardData.Clear();

        if (OnlineManager.Instance == null)
        {
            return;
        }

        if (OnlineManager.Instance.Auth == null || !OnlineManager.Instance.Auth.IsLoggedIn)
        {
            return;
        }

        foreach (Mission mission in GetAllMissions())
        {
            if (mission == null || string.IsNullOrWhiteSpace(mission.levelName))
            {
                continue;
            }

            string leaderboardLevel = Path.ChangeExtension(mission.directory, null);

            try
            {
                MyRankResponse response = await OnlineManager.Instance.Leaderboard.GetMyRankAsync(
                    leaderboardLevel
                );

                if (response != null)
                {
                    userLeaderboardData[mission.levelName] = response;
                }
            }
            catch (Server.Exceptions.NotFoundException)
            {
                // User has no score for this level.
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"Failed to load leaderboard data for " + $"{leaderboardLevel}: {ex.Message}"
                );
            }
        }
    }

    private List<Mission> GetAllMissions()
    {
        if (cachedAllMissions == null || cachedAllMissions.Count == 0)
        {
            CacheAllMissions();
        }
        return cachedAllMissions;
    }

    private bool HasAnySpecifiedParTime()
    {
        foreach (Mission mission in GetAllMissions())
        {
            float time = GetPlayerBestTime(mission.levelName);
            if (time != -1 && mission.time > 0 && time < mission.time)
                return true;
        }

        return false;
    }

    private bool HasTime(string levelName, float requiredTime)
    {
        float time = GetPlayerBestTime(levelName);
        return time != -1 && time < requiredTime;
    }

    private bool HasBeatenGoldTime(string levelName)
    {
        Mission mission = FindMission(levelName);
        if (mission == null)
            return false;

        float time = GetPlayerBestTime(levelName);
        return time != -1 && time < mission.platinumTime;
    }

    private bool HasBeatenUltimateTime(string levelName)
    {
        Mission mission = FindMission(levelName);
        if (mission == null)
            return false;

        float time = GetPlayerBestTime(levelName);
        return time != -1 && time < mission.ultimateTime;
    }

    private float GetPlayerBestTime(string levelName)
    {
        if (!userLeaderboardData.TryGetValue(levelName, out MyRankResponse data))
        {
            return -1f;
        }

        return data.TimeMs / 1000f;
    }

    private Mission FindMission(string levelName)
    {
        foreach (Mission mission in GetAllMissions())
        {
            if (mission.levelName == levelName)
                return mission;
        }

        return null;
    }

    void InitAchiev()
    {
        int totalBeginnerPlatinum = GetTotalCompletion(Game.platinum, Type.beginner);
        int totalIntermediatePlatinum = GetTotalCompletion(Game.platinum, Type.intermediate);
        int totalAdvancedPlatinum = GetTotalCompletion(Game.platinum, Type.advanced);
        int totalExpertPlatinum = GetTotalCompletion(Game.platinum, Type.expert);
        int platinumTimes =
            GetTotalPlatinumGold(Game.platinum, Type.beginner)
            + GetTotalPlatinumGold(Game.platinum, Type.intermediate)
            + GetTotalPlatinumGold(Game.platinum, Type.advanced)
            + GetTotalPlatinumGold(Game.platinum, Type.expert);
        int ultimateTimes =
            GetTotalUltimate(Game.platinum, Type.beginner)
            + GetTotalUltimate(Game.platinum, Type.intermediate)
            + GetTotalUltimate(Game.platinum, Type.advanced)
            + GetTotalUltimate(Game.platinum, Type.expert);
        int totalEasterEgg = PlayerPrefs.GetInt("EasterEggCollected", 0);

        if (totalBeginnerPlatinum >= 25)
            achievImage[0].sprite = achievSprite[0];
        if (totalIntermediatePlatinum >= 35)
            achievImage[1].sprite = achievSprite[1];
        if (totalAdvancedPlatinum >= 35)
            achievImage[2].sprite = achievSprite[2];
        if (totalExpertPlatinum >= 25)
            achievImage[3].sprite = achievSprite[3];
        if (platinumTimes >= 120)
            achievImage[4].sprite = achievSprite[4];
        if (ultimateTimes >= 120)
            achievImage[5].sprite = achievSprite[5];
        if (totalEasterEgg >= 1)
            achievImage[6].sprite = achievSprite[6];
        if (totalEasterEgg >= 120)
            achievImage[7].sprite = achievSprite[7];
    }

    private void ShowCurrentPage()
    {
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currPage);
        }
    }

    public void NextPage()
    {
        if (pages == null || pages.Length == 0 || loadingText.activeSelf)
            return;

        currPage = (currPage + 1) % pages.Length;
        ShowCurrentPage();
    }

    public void PrevPage()
    {
        if (pages == null || pages.Length == 0 || loadingText.activeSelf)
            return;

        currPage = (currPage - 1 + pages.Length) % pages.Length;
        ShowCurrentPage();
    }

    public int GetTotalCompletion(Game game, Type type)
    {
        List<Mission> missionList = GetMissionList(game, type);

        int totalCompletion = 0;
        foreach (Mission m in missionList)
        {
            if (PlayerPrefs.GetFloat(m.levelName + "_Time_" + 0, -1) != -1)
                totalCompletion++;
        }
        return totalCompletion;
    }

    public int GetTotalPlatinumGold(Game game, Type type)
    {
        List<Mission> missionList = GetMissionList(game, type);

        int totalPlatinumGold = 0;
        foreach (Mission m in missionList)
        {
            float time = PlayerPrefs.GetFloat(m.levelName + "_Time_" + 0, -1);
            if (time != -1 && time < m.platinumTime)
                totalPlatinumGold++;
        }
        return totalPlatinumGold;
    }

    public List<Mission> GetMissionList(Game game, Type type)
    {
        if (MissionInfo.instance == null)
            return new List<Mission>();

        if (game == Game.platinum)
        {
            switch (type)
            {
                case Type.beginner:
                    return MissionInfo.instance.missionsPlatinumBeginner;
                case Type.intermediate:
                    return MissionInfo.instance.missionsPlatinumIntermediate;
                case Type.advanced:
                    return MissionInfo.instance.missionsPlatinumAdvanced;
                case Type.expert:
                    return MissionInfo.instance.missionsPlatinumExpert;
            }
        }
        else if (game == Game.gold)
        {
            switch (type)
            {
                case Type.beginner:
                    return MissionInfo.instance.missionsGoldBeginner;
                case Type.intermediate:
                    return MissionInfo.instance.missionsGoldIntermediate;
                case Type.advanced:
                    return MissionInfo.instance.missionsGoldAdvanced;
                case Type.custom:
                    return MissionInfo.instance.missionsGoldCustom;
            }
        }

        return new List<Mission>();
    }

    public int GetTotalUltimate(Game game, Type type)
    {
        List<Mission> missionList = GetMissionList(game, type);

        int totalUltimate = 0;
        foreach (Mission m in missionList)
        {
            float time = PlayerPrefs.GetFloat(m.levelName + "_Time_" + 0, -1);
            if (time != -1 && time < m.ultimateTime)
                totalUltimate++;
        }
        return totalUltimate;
    }
}
*/