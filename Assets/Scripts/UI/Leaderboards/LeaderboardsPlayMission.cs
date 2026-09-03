/*using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Server;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using Server.Replay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardsPlayMission : PlayMissionManager
{
    [Header("Leaderboard UI References")]
    public TextMeshProUGUI globalRecordsNameText;
    public TextMeshProUGUI globalRecordsTimesText;
    public TextMeshProUGUI globalRecordsRatingText;
    public TextMeshProUGUI personalRecordsText;
    public TextMeshProUGUI gameName;
    public Button watchReplay;
    public GameObject personalRecordsRequestingData;
    public GameObject globalRecordsRequestingData;
    public GameObject personalRecordsPanel;
    public GameObject globalRecordsPanel;
    public Button nextPage;
    public Button prevPage;

    // First-place information from the database.
    private int firstPlaceScoreId = -1;
    private string firstPlacePlayerName = "";
    private int firstPlaceTimeMs = -1;

    private bool leaderboardLoading;
    private int leaderboardRequestId;

    private int currentLeaderboardPage = 1;
    private int totalLeaderboardPages = 1;

    // ============================================================
    // START / DESTROY
    // ============================================================

    protected override void Start()
    {
        base.Start();

        if (watchReplay != null)
        {
            watchReplay.onClick.AddListener(OnWatchReplayClicked);
        }

        if (prevPage != null)
        {
            prevPage.onClick.AddListener(OnPreviousPageClicked);
        }

        if (nextPage != null)
        {
            nextPage.onClick.AddListener(OnNextPageClicked);
        }

        UpdatePageButtons();

        SetWatchReplayButton();
    }

    private void OnDestroy()
    {
        if (watchReplay != null)
        {
            watchReplay.onClick.RemoveListener(OnWatchReplayClicked);
        }
    }

    // ============================================================
    // HOME
    // ============================================================

    protected override void OnHomeButtonClicked()
    {
        LeaderboardsMenu.Instance?.ClosePMG();
    }

    // ============================================================
    // MISSION LIST
    // ============================================================

    protected override List<Mission> GetMissionsList(Type difficulty)
    {
        return base.GetMissionsList(difficulty);
    }

    // ============================================================
    // UPDATE MISSION UI
    // ============================================================

    protected override void UpdateMissionSpecificUI(int levelIndex)
    {
        currentLeaderboardPage = 1;
        totalLeaderboardPages = 1;

        UpdatePersonalRecordsAsync();

        ShowLeaderboardRequestingData();

        leaderboardRequestId++;

        UpdateGlobalRecordsFromDatabase(leaderboardRequestId);
    }

    private async void UpdateGlobalRecordsFromDatabase(int requestId)
    {
        leaderboardLoading = true;

        ClearFirstPlace();
        ClearGlobalRecords();

        try
        {
            if (OnlineManager.Instance == null)
            {
                Debug.LogWarning("Cannot load leaderboard: " + "OnlineManager instance not found.");

                return;
            }

            if (OnlineManager.Instance.Auth == null || !OnlineManager.Instance.Auth.IsLoggedIn)
            {
                Debug.LogWarning("Cannot load leaderboard: " + "User is not logged in.");

                return;
            }

            string level = Path.ChangeExtension(MissionInfo.instance.MissionPath, null);

            Debug.Log($"Requesting leaderboard for: {level}");

            LeaderboardResponse response =
                await OnlineManager.Instance.Leaderboard.GetLeaderboardAsync(
                    level,
                    currentLeaderboardPage,
                    10
                );

            // =====================================================
            // Ignore an outdated request.
            // =====================================================

            if (requestId != leaderboardRequestId)
            {
                Debug.Log("Ignoring outdated leaderboard response.");

                return;
            }

            if (response == null || response.Scores == null)
            {
                Debug.LogWarning("Leaderboard response is null.");

                return;
            }

            totalLeaderboardPages = Mathf.Max(1, response.TotalPages);

            // =====================================================
            // Build UI text
            // =====================================================

            string namesText = "  \tPLAYER\n";

            string timesText = "TIME\n";

            string ratingsText = "RATING\n";

            // =====================================================
            // Process scores
            // =====================================================

            foreach (ScoreResponse score in response.Scores)
            {
                if (score == null)
                    continue;

                // -------------------------------------------------
                // Save first-place information for replay viewing.
                // -------------------------------------------------

                if (firstPlaceScoreId <= 0 && score.Rank == 1)
                {
                    firstPlaceScoreId = score.ScoreId;

                    firstPlacePlayerName = score.PlayerName;

                    firstPlaceTimeMs = score.TimeMs;
                }

                // -------------------------------------------------
                // Time formatting
                // -------------------------------------------------

                float time = score.TimeMs;

                bool isGold = time < MissionInfo.instance.platinumTime;

                bool isUltimate = time < MissionInfo.instance.ultimateTime;

                string formattedTime = FormatRecordTime(time, isGold, isUltimate);

                if (formattedTime == "Empty")
                    formattedTime = string.Empty;

                // -------------------------------------------------
                // Player
                // -------------------------------------------------

                namesText += $"{score.Rank}. " + $"{score.PlayerName}\n";

                // -------------------------------------------------
                // Time
                // -------------------------------------------------

                timesText += $"{formattedTime}\n";

                // -------------------------------------------------
                // Rating
                // -------------------------------------------------

                ratingsText += $"{score.Rating:N0}\n";
            }

            // =====================================================
            // Update UI
            // =====================================================

            if (globalRecordsNameText != null)
            {
                globalRecordsNameText.text = namesText;
            }

            if (globalRecordsTimesText != null)
            {
                globalRecordsTimesText.text = timesText;
            }

            if (globalRecordsRatingText != null)
            {
                globalRecordsRatingText.text = ratingsText;
            }

            Debug.Log($"Loaded leaderboard for {level}. " + $"Entries: {response.Scores.Count}");

            if (firstPlaceScoreId > 0)
            {
                Debug.Log(
                    $"1st place: "
                        + $"{firstPlacePlayerName} "
                        + $"({firstPlaceTimeMs} ms), "
                        + $"ScoreId={firstPlaceScoreId}"
                );
            }
        }
        catch (Exception ex)
        {
            // Ignore errors from an old level request.
            if (requestId != leaderboardRequestId)
            {
                return;
            }

            Debug.LogError("Failed to load leaderboard.");

            Debug.LogException(ex);

            ClearFirstPlace();
            ClearGlobalRecords();
        }
        finally
        {
            // Only the currently selected level is allowed
            // to change the loading UI.
            if (requestId == leaderboardRequestId)
            {
                leaderboardLoading = false;

                HideLeaderboardRequestingData();
                UpdatePageButtons();
                SetWatchReplayButton();
            }
        }
    }

    private void ShowLeaderboardRequestingData()
    {
        leaderboardLoading = true;

        if (personalRecordsRequestingData != null)
        {
            personalRecordsRequestingData.SetActive(true);
        }

        if (globalRecordsRequestingData != null)
        {
            globalRecordsRequestingData.SetActive(true);
        }

        if (personalRecordsPanel != null)
        {
            personalRecordsPanel.SetActive(false);
        }

        if (globalRecordsPanel != null)
        {
            globalRecordsPanel.SetActive(false);
        }

        UpdatePageButtons();
    }

    private void HideLeaderboardRequestingData()
    {
        leaderboardLoading = false;

        if (personalRecordsRequestingData != null)
        {
            personalRecordsRequestingData.SetActive(false);
        }

        if (globalRecordsRequestingData != null)
        {
            globalRecordsRequestingData.SetActive(false);
        }

        if (personalRecordsPanel != null)
        {
            personalRecordsPanel.SetActive(true);
        }

        if (globalRecordsPanel != null)
        {
            globalRecordsPanel.SetActive(true);
        }

        UpdatePageButtons();
    }

    // ============================================================
    // PERSONAL RECORDS
    // ============================================================

    private async void UpdatePersonalRecordsAsync()
    {
        List<int> timesMs = new List<int>();

        // =========================================================
        // Read personal records from PlayerPrefs
        // =========================================================

        for (int i = 0; i < 10; i++)
        {
            float time = PlayerPrefs.GetFloat($"{MissionInfo.instance.levelName}_Time_{i}", -1);

            int timeMs = time < 0 ? -1 : Mathf.RoundToInt(time);

            timesMs.Add(timeMs);
        }

        try
        {
            // =====================================================
            // Request ratings from server
            // =====================================================

            if (
                OnlineManager.Instance == null
                || OnlineManager.Instance.Auth == null
                || !OnlineManager.Instance.Auth.IsLoggedIn
            )
            {
                BuildPersonalRecordsText(timesMs, null);

                return;
            }

            string level = Path.ChangeExtension(MissionInfo.instance.MissionPath, null);

            CalculateRatingsResponse response =
                await OnlineManager.Instance.Rating.CalculateRatingsAsync(
                    new CalculateRatingsRequest { Level = level, TimesMs = timesMs }
                );

            if (response == null || response.Ratings == null)
            {
                Debug.LogWarning("Rating response was empty.");

                BuildPersonalRecordsText(timesMs, null);

                return;
            }

            BuildPersonalRecordsText(timesMs, response.Ratings);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to calculate personal ratings.");

            Debug.LogException(ex);

            // Still show the personal times even if the
            // rating request fails.
            BuildPersonalRecordsText(timesMs, null);
        }
    }

    private void BuildPersonalRecordsText(List<int> timesMs, List<int?> ratings)
    {
        string personalText = "  \tTIME\t\tRATING\n";

        for (int i = 0; i < timesMs.Count; i++)
        {
            int timeMs = timesMs[i];

            if (timeMs < 0)
            {
                personalText += $"{i + 1}.\tEmpty\t-\n";

                continue;
            }

            float time = timeMs;

            bool isGold = time < MissionInfo.instance.platinumTime;

            bool isUltimate = time < MissionInfo.instance.ultimateTime;

            string formattedTime = FormatRecordTime(time, isGold, isUltimate);

            string ratingText = string.Empty;

            if (ratings != null && i < ratings.Count && ratings[i].HasValue)
            {
                ratingText = ratings[i].Value.ToString("N0");
            }

            personalText += $"{i + 1}.\t" + $"{formattedTime}\t" + $"{ratingText}\n";
        }

        if (personalRecordsText != null)
        {
            personalRecordsText.text = personalText;
        }
    }

    // ============================================================
    // DATABASE LEADERBOARD
    // ============================================================

    private void ClearGlobalRecords()
    {
        if (globalRecordsNameText != null)
        {
            globalRecordsNameText.text = "  \tPLAYER\n";
        }

        if (globalRecordsTimesText != null)
        {
            globalRecordsTimesText.text = "TIME\n";
        }

        if (globalRecordsRatingText != null)
        {
            globalRecordsRatingText.text = "RATING\n";
        }
    }

    private void ClearFirstPlace()
    {
        firstPlaceScoreId = -1;
        firstPlacePlayerName = "";
        firstPlaceTimeMs = -1;
    }

    // ============================================================
    // WATCH REPLAY BUTTON
    // ============================================================

    private void SetWatchReplayButton()
    {
        bool firstPlaceExists = firstPlaceScoreId > 0;

        if (watchReplay != null)
        {
            watchReplay.gameObject.SetActive(firstPlaceExists);

            watchReplay.interactable = firstPlaceExists;
        }
    }

    private void OnWatchReplayClicked()
    {
        if (firstPlaceScoreId <= 0)
        {
            Debug.LogWarning("Cannot watch replay: first-place ScoreId is unavailable.");
            return;
        }

        StartCoroutine(LoadLeaderboardReplay());
    }

    // ============================================================
    // LOAD REPLAY
    // ============================================================

    private IEnumerator LoadLeaderboardReplay()
    {
        LeaderboardsMenu menu = LeaderboardsMenu.Instance;

        if (menu == null)
        {
            Debug.LogError("LeaderboardsMenu instance not found.");
            yield break;
        }

        if (OnlineManager.Instance == null)
        {
            Debug.LogError("OnlineManager instance not found.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(firstPlacePlayerName))
        {
            Debug.LogError("First-place player name is empty.");
            yield break;
        }

        JukeboxManager.instance.ForceStop();
        menu.blackout.SetActive(true);

        string replayPath = GetLeaderboardReplayPath();

        // Replay already exists
        if (File.Exists(replayPath))
        {
            menu.ShowLoading("Loading Replay...");
            yield return new WaitForSecondsRealtime(1f);

            PrepareReplayForLoading(replayPath);
            yield return null;

            LevelLoadedFromLeaderboards = true;
            SceneManager.LoadScene("Loading");
            yield break;
        }

        // Replay does not exist
        menu.ShowLoading("Downloading Replay...");

        bool finished = false;
        string downloadedPath = null;
        Exception downloadException = null;

        DownloadReplayAsync(
            firstPlaceScoreId,
            replayPath,
            path =>
            {
                downloadedPath = path;
                finished = true;
            },
            exception =>
            {
                downloadException = exception;
                finished = true;
            }
        );

        while (!finished)
        {
            yield return null;
        }

        // Download failed
        if (downloadException != null)
        {
            Debug.LogError("Failed to download leaderboard replay.");

            Debug.LogException(downloadException);

            // The server could not provide the replay.
            // Disable the Watch Replay button.
            if (watchReplay != null)
            {
                watchReplay.interactable = false;
                watchReplay.gameObject.SetActive(false);
            }

            menu.blackout.SetActive(false);
            menu.loadingMenu.SetActive(false);

            JukeboxManager.instance.PlayMusic("Flanked");

            yield break;
        }

        if (string.IsNullOrWhiteSpace(downloadedPath) || !File.Exists(downloadedPath))
        {
            Debug.LogError("Replay download completed, " + "but the replay file does not exist.");

            if (watchReplay != null)
            {
                watchReplay.interactable = false;
                watchReplay.gameObject.SetActive(false);
            }

            menu.blackout.SetActive(false);
            menu.loadingMenu.SetActive(false);

            JukeboxManager.instance.PlayMusic("Flanked");

            yield break;
        }

        // Prepare replay
        PrepareReplayForLoading(downloadedPath);
        yield return null;

        LevelLoadedFromLeaderboards = true;
        SceneManager.LoadScene("Loading");
    }

    // ============================================================
    // DOWNLOAD REPLAY
    // ============================================================

    private async void DownloadReplayAsync(
        int scoreId,
        string savePath,
        Action<string> onSuccess,
        Action<Exception> onFailure
    )
    {
        try
        {
            ReplayPaths.EnsureDirectories();

            string path = await OnlineManager.Instance.ReplayDownload.DownloadReplayAsync(
                scoreId,
                savePath
            );

            onSuccess?.Invoke(path);
        }
        catch (Exception ex)
        {
            onFailure?.Invoke(ex);
        }
    }

    // ============================================================
    // REPLAY PATH
    // ============================================================

    private string GetLeaderboardReplayPath()
    {
        ReplayPaths.EnsureDirectories();

        string levelName = MissionInfo.instance.levelName;

        *//*
         * Replay naming format:
         * [LevelName]_[TimeMs]_[PlayerName].urec
         *//*
        string fileName = $"{levelName}_{firstPlaceTimeMs}_{firstPlacePlayerName}.urec";

        return Path.Combine(ReplayPaths.LeaderboardDirectory, fileName);
    }

    // ============================================================
    // PREPARE REPLAY
    // ============================================================

    private void PrepareReplayForLoading(string replayPath)
    {
        ReplayRecorder.loadedReplayPath = replayPath;
        ReplayRecorder.replayName = Path.GetFileNameWithoutExtension(replayPath);
        ReplayRecorder.loadReplay = true;
        ReplayRecorder.incompleteReplay = false;
    }

    // ============================================================
    // EMPTY MISSION LIST
    // ============================================================

    protected override void HandleEmptyMissionList()
    {
        if (levelDescriptionText)
        {
            levelDescriptionText.gameObject.SetActive(false);
        }

        if (levelImage)
        {
            levelImage.color = UnityEngine.Color.clear;
        }

        if (currentLevelText)
        {
            currentLevelText.text = "Level 0";
        }

        if (notQualifiedImage)
        {
            notQualifiedImage.SetActive(true);
        }

        if (notQualifiedText)
        {
            notQualifiedText.SetActive(true);
        }

        if (prev)
        {
            prev.interactable = false;
        }

        if (next)
        {
            next.interactable = false;
        }

        if (play)
        {
            play.interactable = false;
        }

        string personalText = "  \tTIME\n";

        for (int i = 0; i < 10; i++)
        {
            personalText += $"{i + 1}. Empty\t{Utils.FormatTime(-1)}\n";
        }

        if (personalRecordsText)
        {
            personalRecordsText.text = personalText;
        }

        if (globalRecordsNameText)
        {
            globalRecordsNameText.text = "  \tPLAYER\n";
        }

        if (globalRecordsTimesText)
        {
            globalRecordsTimesText.text = "TIME\n";
        }

        ClearFirstPlace();
        SetWatchReplayButton();
    }

    // ============================================================
    // FORMAT TIME
    // ============================================================

    private string FormatRecordTime(float time, bool isGold, bool isUltimate)
    {
        if (time < 0)
            return "Empty";

        string formattedTime = Utils.FormatTime(time);

        // =========================================================
        // ULTIMATE
        // =========================================================

        if (isUltimate)
        {
            return $"<color=#FFCC33>" + $"{formattedTime}" + $"</color>";
        }

        // =========================================================
        // PLATINUM
        // =========================================================

        if (selectedGame == Game.platinum)
        {
            // In Platinum, "goldTime" internally represents
            // the Platinum time and is displayed in gray.
            if (isGold)
            {
                return $"<color=#CCCCCC>" + $"{formattedTime}" + $"</color>";
            }

            return formattedTime;
        }

        // =========================================================
        // GOLD
        // =========================================================

        if (selectedGame == Game.gold)
        {
            // Gold custom missions display Gold time in gray.
            if (currentlySelectedType == Type.custom && isGold)
            {
                return $"<color=#CCCCCC>" + $"{formattedTime}" + $"</color>";
            }

            // Normal Gold missions display Gold time in yellow.
            if (isGold)
            {
                return $"<color=#FFEE11>" + $"{formattedTime}" + $"</color>";
            }
        }

        // =========================================================
        // NORMAL
        // =========================================================

        return formattedTime;
    }

    // ============================================================
    // GAME / DIFFICULTY UI
    // ============================================================

    protected override void UpdateUIForGameAndDifficulty()
    {
        gameName.text = CapitalizeFirst(
            currentlySelectedType == Type.custom ? "Custom" : selectedGame.ToString()
        );

        if (expertButton)
        {
            expertButton.SetActive(selectedGame == Game.platinum);
        }
    }

    private void OnPreviousPageClicked()
    {
        if (leaderboardLoading)
            return;

        if (currentLeaderboardPage <= 1)
            return;

        currentLeaderboardPage--;

        ShowLeaderboardRequestingData();

        leaderboardRequestId++;

        UpdateGlobalRecordsFromDatabase(leaderboardRequestId);
    }

    private void OnNextPageClicked()
    {
        if (leaderboardLoading)
            return;

        if (currentLeaderboardPage >= totalLeaderboardPages)
            return;

        currentLeaderboardPage++;

        ShowLeaderboardRequestingData();

        leaderboardRequestId++;

        UpdateGlobalRecordsFromDatabase(leaderboardRequestId);
    }

    private void UpdatePageButtons()
    {
        if (prevPage != null)
        {
            prevPage.interactable = !leaderboardLoading && currentLeaderboardPage > 1;
        }

        if (nextPage != null)
        {
            nextPage.interactable =
                !leaderboardLoading && currentLeaderboardPage < totalLeaderboardPages;
        }
    }
}
*/