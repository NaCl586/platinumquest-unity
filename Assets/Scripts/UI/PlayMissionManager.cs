using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Server;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class Mission
{
    [Header("Mission Info")]
    public Sprite levelImage;
    public string directory;
    public int levelNumber;

    [Space]
    public int time;
    public string missionName;
    public string levelName;

    [Space]
    [TextArea(2, 10)]
    public string description;

    [Space]
    [TextArea(2, 10)]
    public string startHelpText;

    public string artist;
    public string music;
    public string skyboxName;

    public int parScore = 0;
    public int platinumTime = -1;
    public int ultimateTime = -1;
    public int awesomeTime = -1;

    public int alarmTime = 15;

    public bool hasEgg;

    public string generalHint;
    public string platinumHint;
    public string ultimateHint;
    public string awesomeHint;
    public string nestEggHint;
    public string trivia;

    [Header("Game Modes")]
    public List<Mode> gameModes = new List<Mode>();

    [Header("Quota")]
    public int gemQuota = -1;

    [Header("Laps")]
    public int lapsNumber = -1;
    public bool noLapsCheckpoint = false;

    [Header("2D")]
    public string cameraPlane;
    public bool invertCameraPlane;

    public bool hasCameraPitch;
    public float cameraPitch;

    public bool hasInitialCameraDistance;
    public float initialCameraDistance;

    public bool hasCameraFov;
    public float cameraFov;

    [Header("Consistency")]
    public float minimumSpeed;
    public float penaltyDelay;
    public float gracePeriod;

    [Header("Haste")]
    public float speedToQualify;

    [Header("Hunt")]
    public int maxGemsPerSpawn = 7;
    public float radiusFromGem = 15f;
    public float spawnBlock = 30f;

    public int minPointsPerSpawn = 5;
    public int minGemsPerSpawn = 3;

    public float redSpawnChance = 0.9f;
    public float yellowSpawnChance = 0.65f;
    public float blueSpawnChance = 0.35f;
    public float platinumSpawnChance = 0.18f;

    public int gemGroups = 0;

    [Header("Radar")]
    public string radar;
    public string customRadarRule;
    public bool forceRadar;
    public bool hideRadar;

    [Header("Physics")]
    public float gravity = 20f;
    public float angularAcceleration = 75f;
    public float brakingAcceleration = 30f;
    public float maxRollVelocity = 15f;
    public float jumpImpulse = 7.5f;

    [Header("Mission Mode")]
    public SpecialMissionMode specialMissionMode =
        SpecialMissionMode.None;
}

public enum Type
{
    none,
    tutorial,
    beginner,
    intermediate,
    advanced,
    expert,
    dc,
    bonus
}

public enum Mode
{
    Null,
    Quota,
    Laps,
    Consistency,
    Haste,
    Hunt,
    Madness,
    TwoD
}

public enum SpecialMissionMode
{
    None,
    Arkanoid,
    BagOfSecrets,
    BlastToTheBeat,
    SacredGround,
    TakeTheGold,
    Vice,
    WhiteNoise,
    MinuteMinute,
    ArcticInferno,
    Versa,
    UnseasonablyCold
}

public class PlayMissionManager : MonoBehaviour
{
    public List<Mission> missions = new List<Mission>();

    [Header("Common UI References")]
    public Image levelImage;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI levelDescriptionText;
    public TextMeshProUGUI levelAuthorText;
    public TextMeshProUGUI timeToQualifyText;

    [Space]
    public TextMeshProUGUI bestTimeLevelText;
    public TextMeshProUGUI bestTimeNames, bestTimeScores;
    public TextMeshProUGUI platinumTimeText, ultimateTimeText, awesomeTimeText;
    public GameObject awesomeTimeGameObject, platinumTimeGameObject, ultimateTimeGameObject;
    public TextMeshProUGUI generalHintText, ultimateHintText, awesomeHintText, nestEggHintText, triviaText;

    [Space]
    public Button categoryButton;
    public TextMeshProUGUI categoryText;

    [Space]
    public Button bestTimeToggle;
    public TextMeshProUGUI bestTimeToggleText;
    public string bestTimeToggleString;

    [Space]
    public GameObject levelInfoMenu;
    public GameObject bestTimesMenu;

    [Space]
    public GameObject notQualifiedText;
    public GameObject notQualifiedImage;

    [Space]
    public GameObject tutorialButton;
    public GameObject beginnerButton;
    public GameObject intermediateButton;
    public GameObject advancedButton;
    public GameObject expertButton;
    public GameObject dcButton;
    public GameObject bonusButton;

    [Space]
    public Image eggImage;
    public Sprite egg;
    public Sprite egg_nf;

    [Space]
    public Button moreButton;

    [Space]
    public Button prev;
    public Button next;
    public Button play;
    public Button home;

    [Header("Window Panels")]
    public GameObject marbleSelectWindow;
    public GameObject hintsWindow;
    public GameObject replayWindow;
    public GameObject statisticsWindow;

    [Space]
    public GameObject searchWindow;
    public GameObject versaRun;
    public GameObject categoryWindow;
    public GameObject moreWindow;

    [Header("Window Triggers")]
    public Button marbleSelectButton;
    public Button hintsButton;
    public Button closeHintsButton;
    public Toggle replayButton;
    public Button statisticsButton;

    [Space]
    public Button searchButton;

    [Header("Raycast Blockers")]
    public GameObject raycastBlocker;
    public GameObject raycastBlocker2;
    public GameObject raycastBlockerCategoryWindow;
    public GameObject raycastBlockerMoreWindow;

    [Space]
    public bool debug = false;
    public static bool LevelLoadedFromLeaderboards = false;

    [HideInInspector]
    public int selectedLevelNum;

    public static Type currentlySelectedType = Type.none;

    protected virtual bool IsAnyWindowActive()
    {
        return
            (marbleSelectWindow && marbleSelectWindow.activeSelf)
            || (searchWindow && searchWindow.activeSelf)
            || (hintsWindow && hintsWindow.activeSelf)
            || (replayWindow && replayWindow.activeSelf)
            || (statisticsWindow && statisticsWindow.activeSelf)
            || (versaRun && versaRun.activeSelf);
    }

    protected virtual void Update()
    {
        if (!IsAnyWindowActive())
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                PrevButton();

            if (Input.GetKeyDown(KeyCode.RightArrow))
                NextButton();

            if (Input.GetKeyDown(KeyCode.Escape))
                SceneManager.LoadScene("MainMenu");
        }
    }

    protected virtual void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (statisticsWindow)
            statisticsWindow.SetActive(false);

        CloseAllWindows();
        SetBlockerActive(false, false);

        if (marbleSelectButton)
        {
            marbleSelectButton.onClick.AddListener(() =>
            {
                SetBlockerActive(true, false);
                ToggleMarbleSelectWindow(true);
            });
        }

        if (hintsButton)
        {
            hintsButton.onClick.AddListener(() =>
            {
                SetBlockerActive(true, false);
                ToggleHintsWindow(true);
            });
        }

        if (closeHintsButton)
        {
            closeHintsButton.onClick.AddListener(() => 
            {
                raycastBlocker.SetActive(false);
                ToggleHintsWindow(false);
            });
        }

        if (searchButton)
            searchButton.onClick.AddListener(OnSearchButtonClicked);

        if (replayButton)
        {
            replayButton.onValueChanged.AddListener(ToggleReplay);
            replayButton.SetIsOnWithoutNotify(false);
        }

        if (statisticsButton)
        {
            statisticsButton.onClick.AddListener(() =>
            {
                SetBlockerActive(true, false);

                GetComponent<StatisticsManager>()?.InitStatistics();

                ToggleWindow(statisticsWindow, true);
            });
        }

        bestTimeToggle.onClick.AddListener(ToggleBestTimeLevelInfo);
        categoryButton.onClick.AddListener(ShowCategoryMenu);
        moreButton.onClick.AddListener(ShowMoreMenu);

        StartCoroutine(WaitUntilFinishLoading());
    }

    protected virtual void CloseAllWindows()
    {
        if(marbleSelectWindow) ToggleWindow(marbleSelectWindow, false);
        if(searchWindow) ToggleWindow(searchWindow, false);
        if(hintsWindow) ToggleWindow(hintsWindow, false);
        if(replayWindow) ToggleWindow(replayWindow, false);
        if(statisticsWindow) ToggleWindow(statisticsWindow, false);
    }

    void ToggleBestTimeLevelInfo()
    {
        if (levelInfoMenu.activeSelf)
        {
            bestTimesMenu.SetActive(true);
            levelInfoMenu.SetActive(false);
        }
        else if (bestTimesMenu.activeSelf)
        {
            bestTimesMenu.SetActive(false);
            levelInfoMenu.SetActive(true);
        }
    }

    public void SetToggleHoverText()
    {
        if (levelInfoMenu.activeSelf)
            bestTimeToggleText.text = "<color=#DDC1C1>Show Top 5 Times</color>";
        else if(bestTimesMenu.activeSelf)
            bestTimeToggleText.text = "<color=#DDC1C1>Hide Top 5 Times</color>";
    }

    public void SetToggleUnhoverText()
    {
        bestTimeToggleText.text = bestTimeToggleString;
    }

    public void CloseCategory()
    {
        raycastBlockerCategoryWindow.SetActive(false);
        categoryWindow.SetActive(false);
    }

    public void CloseMore()
    {
        raycastBlockerMoreWindow.SetActive(false);
        moreWindow.SetActive(false);
    }

    public void ShowCategoryMenu()
    {
        categoryWindow.SetActive(true);
        raycastBlockerCategoryWindow.SetActive(true);

        tutorialButton.gameObject.SetActive(currentlySelectedType != Type.tutorial);
        beginnerButton.gameObject.SetActive(currentlySelectedType != Type.beginner);
        intermediateButton.gameObject.SetActive(currentlySelectedType != Type.intermediate);
        advancedButton.gameObject.SetActive(currentlySelectedType != Type.advanced);
        expertButton.gameObject.SetActive(currentlySelectedType != Type.expert);
        bonusButton.gameObject.SetActive(currentlySelectedType != Type.bonus);
        dcButton.gameObject.SetActive(currentlySelectedType != Type.dc);
    }

    public void ShowMoreMenu()
    {
        moreWindow.SetActive(true);
        raycastBlockerMoreWindow.SetActive(true);
    }

    protected virtual IEnumerator WaitUntilFinishLoading()
    {
        while (
            MissionInfo.instance == null
            || MissionInfo.instance.missionsTutorial == null
            || MissionInfo.instance.missionsTutorial.Count == 0
        )
        {
            yield return null;
        }

        Time.timeScale = 1;

        BindNavigationAndDifficultyButtons();

        if (currentlySelectedType == Type.none)
            currentlySelectedType = Type.tutorial;

        string dif = currentlySelectedType.ToString();
        categoryText.text = (currentlySelectedType == Type.dc) ? "Director's Cut" : char.ToUpper(dif[0]) + dif.Substring(1);

        LoadMissions(currentlySelectedType);

        SearchManager searchManager = GetComponent<SearchManager>();

        if (searchManager != null)
            searchManager.InitSearchElements();
    }

    protected virtual void BindNavigationAndDifficultyButtons()
    {
        BindButton(tutorialButton, () => LoadMissions(Type.tutorial));
        BindButton(beginnerButton, () => LoadMissions(Type.beginner));
        BindButton(intermediateButton, () => LoadMissions(Type.intermediate));
        BindButton(advancedButton, () => LoadMissions(Type.advanced));
        BindButton(expertButton, () => LoadMissions(Type.expert));
        BindButton(bonusButton, () => LoadMissions(Type.bonus));
        BindButton(dcButton, () => LoadMissions(Type.dc));

        if (home)
            home.onClick.AddListener(OnHomeButtonClicked);

        if (prev)
            prev.onClick.AddListener(PrevButton);

        if (next)
            next.onClick.AddListener(NextButton);

        if (play)
            play.onClick.AddListener(OnPlayButtonClicked);
    }

    protected virtual void OnHomeButtonClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    protected virtual void OnPlayButtonClicked()
    {
        if (
            OnlineManager.Instance == null
            || !OnlineManager.Instance.Auth.IsLoggedIn
        )
        {
            LevelLoadedFromLeaderboards = false;

            if (MissionInfo.instance.specialMissionMode == SpecialMissionMode.Versa)
            {
                ToggleWindow(versaRun, true);
            }
            else
            {
                SceneManager.LoadScene("Loading");
            }
        }
        else
        {
            LeaderboardsMenu lm = GetComponent<LeaderboardsMenu>();

            LevelLoadedFromLeaderboards = true;
            CheckMission(lm).Forget();
        }
    }

    async UniTask CheckMission(LeaderboardsMenu lm)
    {
        JukeboxManager.instance.ForceStop();

        lm.blackout.SetActive(true);
        lm.ShowLoading("Checking Mission Consistency...");

        await UniTask.Delay(TimeSpan.FromSeconds(1));

        string missionPath = MissionInfo.instance.MissionPath;

        try
        {
            List<string> files =
                DataIntegrityManager.GetMissionIntegrityFiles(missionPath);

            if (files.Count == 0)
            {
                throw new Exception(
                    "Could not read the mission integrity data."
                );
            }

            IntegrityResponse response =
                await OnlineManager.Instance.Integrity.CheckAsync(
                    new IntegrityRequest
                    {
                        GameVersion = Application.version,
                        Files = files
                    }
                );

            List<string> invalidFiles =
                DataIntegrityManager.VerifyAgainstServer(response);

            if (invalidFiles.Count > 0)
            {
                string modifiedFiles =
                    string.Join("\n", invalidFiles);

                lm.ShowError(
                    "Invalid game data",
                    "It seems that internal game data was modified in some way. "
                        + "If either you modified any files, or it was done by any virus, "
                        + "please ask the forums for the original data or reinstall MBP.\n\n"
                        + "Modified file(s):\n"
                        + modifiedFiles,
                    true
                );

                Debug.LogError(
                    $"[Integrity] Mission consistency check failed!\n"
                        + $"Mission: {missionPath}\n"
                        + $"Invalid file(s):\n{modifiedFiles}"
                );

                return;
            }

            SceneManager.LoadScene("Loading");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"[Integrity] Integrity check failed:\n{ex}"
            );

            lm.ShowError(
                "Integrity check failed",
                "The game could not verify the mission data with the server.\n\n"
                    + "Please make sure you are connected to the internet and try again.",
                true
            );
        }
    }

    protected virtual void OnSearchButtonClicked()
    {
        SetBlockerActive(true, false);
        ToggleSearchWindow(true);

        SearchManager searchManager =
            GetComponent<SearchManager>();

/*        if (searchManager != null)
        {
            searchManager.SelectFirstButton();
        }*/
    }

    public virtual void LoadMissions(Type difficulty)
    {
        raycastBlockerCategoryWindow.SetActive(false);
        categoryWindow.SetActive(false);

        currentlySelectedType = difficulty;

        missions = GetMissionsList(difficulty);

        // Missions loaded without explicit mode data behave like the old
        // single-mode default: Null.
        foreach (Mission mission in missions)
        {
            if (
                mission != null
                && (
                    mission.gameModes == null
                    || mission.gameModes.Count == 0
                )
            )
            {
                mission.gameModes = new List<Mode>
                {
                    Mode.Null
                };
            }
        }

        string dif = difficulty.ToString();
        categoryText.text = (currentlySelectedType == Type.dc) ? "Director's Cut" : char.ToUpper(dif[0]) + dif.Substring(1);

        SetLevelInfo(ClampSelectedLevel());
    }

    protected virtual List<Mission> GetMissionsList(Type difficulty)
    {
        switch (difficulty)
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
        }

        return new List<Mission>();
    }

    public virtual void SetLevelInfo(int number)
    {
        selectedLevelNum = number;

        if (missions == null || missions.Count == 0)
        {
            HandleEmptyMissionList();
            return;
        }

        Mission mission = missions[number];

        int qualifiedLevel = GetQualifiedLevel();

        bool isQualified = qualifiedLevel >= number;

        // Versa has its own qualification requirement.
        // This bypasses the debug-mode qualification bypass.
        if (mission.specialMissionMode == SpecialMissionMode.Versa)
        {
            isQualified = ViceVersaState.HasSavedState();
        }

        if (play)
            play.interactable = isQualified;

        if (prev)
            prev.interactable = number > 0;

        if (next)
            next.interactable =
                number < missions.Count - 1;

        int lastQualifiedLevel =
            Mathf.Min(number, qualifiedLevel);

        PlayerPrefs.SetInt(
            "SelectedLevel",
            lastQualifiedLevel
        );

        currentLevelText.text = "#" + (number + 1) + ": " + mission.levelName;
        bestTimeLevelText.text = "#" + (number + 1) + ": " + mission.levelName;
        levelAuthorText.text = "Author: " + mission.artist;
        levelDescriptionText.text = mission.description;

        RefreshTMPLayout(currentLevelText);
        RefreshTMPLayout(bestTimeLevelText);
        RefreshTMPLayout(levelAuthorText);
        RefreshTMPLayout(levelDescriptionText);

        timeToQualifyText.text = string.Empty;
        if (mission.gameModes.Contains(Mode.Madness) || mission.gameModes.Contains(Mode.Hunt))
        {
            timeToQualifyText.text += $"Par Score: {mission.parScore}\n";
            timeToQualifyText.text += $"Time Limit: {Utils.FormatTime(mission.time)}";
        }
        else
        {
            timeToQualifyText.text = mission.time != -1 ? $"Par Time: {Utils.FormatTime(mission.time)}" : $"Par Time: N/A";
        }
        

        if (levelImage)
        {
            levelImage.sprite = mission.levelImage;

            levelImage.color =
                mission.levelImage != null
                    ? Color.white
                    : Color.clear;
        }

        if (notQualifiedImage)
            notQualifiedImage.SetActive(!isQualified);

        if (notQualifiedText)
            notQualifiedText.SetActive(!isQualified);

        if (eggImage)
        {
            eggImage.gameObject.SetActive(mission.hasEgg);

            if (mission.hasEgg)
            {
                bool hasFoundEgg =
                    PlayerPrefs.GetInt(
                        mission.levelName + "_EasterEgg",
                        0
                    ) == 1;

                eggImage.sprite =
                    hasFoundEgg
                        ? egg
                        : egg_nf;
            }
        }

        SetMissionInfo(mission);
        UpdateMissionSpecificUI(number);
    }

    protected virtual void UpdateMissionSpecificUI(int levelIndex)
    {
        Mission mission = missions[levelIndex];

        bestTimeNames.text = string.Empty;
        bestTimeScores.text = string.Empty;

        awesomeTimeGameObject.SetActive(false);

        for (int i = 0; i < 5; i++)
        {

            string name = PlayerPrefs.GetString(
                $"{MissionInfo.instance.levelName}_Name_{i}",
                "Matan W."
            );

            float time = PlayerPrefs.GetFloat(
                $"{MissionInfo.instance.levelName}_Time_{i}",
                -1
            );


            if (i == 0)
            {
                if (mission.gameModes.Contains(Mode.Hunt))
                {
                    if (time != -1 && time < mission.awesomeTime)
                    {
                        awesomeTimeGameObject.SetActive(true);
                        bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + $"<color=#FF3333>{Mathf.RoundToInt(time)}</color>";
                    }
                    else if (time != -1 && time < mission.ultimateTime)
                    {
                        bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + $"<color=#FFCC33>{Mathf.RoundToInt(time)}</color>";
                    }
                    else if (time != -1 && time < mission.platinumTime)
                    {
                        bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + $"<color=#CCCCCC>{Mathf.RoundToInt(time)}</color>";
                    }
                    else
                    {
                        bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + (time == -1 ? "0\n" : $"{Mathf.RoundToInt(time)}\n");
                    }
                }
                else if (mission.gameModes.Contains(Mode.Madness))
                {
                    if (time < 1000)
                    {
                        if (time != -1 && time < mission.awesomeTime)
                        {
                            awesomeTimeGameObject.SetActive(true);
                            bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + $"<color=#FF3333>{Mathf.RoundToInt(time)}</color>";
                        }
                        else if (time != -1 && time < mission.ultimateTime)
                        {
                            bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + $"<color=#FFCC33>{Mathf.RoundToInt(time)}</color>";
                        }
                        else if (time != -1 && time < mission.platinumTime)
                        {
                            bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + $"<color=#CCCCCC>{Mathf.RoundToInt(time)}</color>";
                        }
                        else
                        {
                            bestTimeToggleText.text = bestTimeToggleString = "Best Score: " + (time == -1 ? "0\n" : $"{Mathf.RoundToInt(time)}\n");
                        }
                    }
                    else
                    {
                        if (time != -1 && time < mission.awesomeTime)
                        {
                            awesomeTimeGameObject.SetActive(true);
                            bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"<color=#FF3333>{Utils.FormatTime(time)}</color>";
                        }
                        else if (time != -1 && time < mission.ultimateTime)
                        {
                            bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"<color=#FFCC33>{Utils.FormatTime(time)}</color>";
                        }
                        else if (time != -1 && time < mission.platinumTime)
                        {
                            bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"<color=#CCCCCC>{Utils.FormatTime(time)}</color>";
                        }
                        else
                        {
                            bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"{Utils.FormatTime(time)}";
                        }
                    }
                }
                else
                {
                    if (time != -1 && time < mission.awesomeTime)
                    {
                        awesomeTimeGameObject.SetActive(true);
                        bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"<color=#FF3333>{Utils.FormatTime(time)}</color>";
                    }
                    else if (time != -1 && time < mission.ultimateTime)
                    {
                        bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"<color=#FFCC33>{Utils.FormatTime(time)}</color>";
                    }
                    else if (time != -1 && time < mission.platinumTime)
                    {
                        bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"<color=#CCCCCC>{Utils.FormatTime(time)}</color>";
                    }
                    else
                    {
                        bestTimeToggleText.text = bestTimeToggleString = "Best Time: " + $"{Utils.FormatTime(time)}";
                    }
                }
            }

            bestTimeNames.text += ($"{i + 1}.\t{name}\n");

            if (mission.gameModes.Contains(Mode.Hunt))
            {
                if (time != - 1 && time < mission.awesomeTime)
                {
                    bestTimeScores.text +=
                        $"<color=#FF3333>{Mathf.RoundToInt(time)}</color>\n";
                }
                else if (time != -1 && time < mission.ultimateTime)
                {
                    bestTimeScores.text +=
                        $"<color=#FFCC33>{Mathf.RoundToInt(time)}</color>\n";
                }
                else if (time != -1 && time < mission.platinumTime)
                {
                    bestTimeScores.text +=
                        $"<color=#CCCCCC>{Mathf.RoundToInt(time)}</color>\n";
                }
                else
                {
                    bestTimeScores.text +=
                            time == -1 ? "0\n" : $"{Mathf.RoundToInt(time)}\n";
                }
            }
            else if (mission.gameModes.Contains(Mode.Madness))
            {
                if(time < 1000)
                {
                    if (time != -1 && time < mission.awesomeTime)
                    {
                        bestTimeScores.text +=
                            $"<color=#FF3333>{Mathf.RoundToInt(time)}</color>\n";
                    }
                    else if (time != -1 && time < mission.ultimateTime)
                    {
                        bestTimeScores.text +=
                            $"<color=#FFCC33>{Mathf.RoundToInt(time)}</color>\n";
                    }
                    else if (time != -1 && time < mission.platinumTime)
                    {
                        bestTimeScores.text +=
                            $"<color=#CCCCCC>{Mathf.RoundToInt(time)}</color>\n";
                    }
                    else
                    {
                        bestTimeScores.text +=
                            time == -1 ? "0\n" : $"{Mathf.RoundToInt(time)}\n";
                    }
                }
                else
                {
                    if (time != -1 && time < mission.awesomeTime)
                    {
                        bestTimeScores.text +=
                            $"<color=#FF3333>{Utils.FormatTime(time)}</color>\n";
                    }
                    else if (time != -1 && time < mission.ultimateTime)
                    {
                        bestTimeScores.text +=
                            $"<color=#FFCC33>{Utils.FormatTime(time)}</color>\n";
                    }
                    else if (time != -1 && time < mission.platinumTime)
                    {
                        bestTimeScores.text +=
                            $"<color=#CCCCCC>{Utils.FormatTime(time)}</color>\n";
                    }
                    else
                    {
                        bestTimeScores.text +=
                            $"{Utils.FormatTime(time)}\n";
                    }
                }
            }
            else
            {
                if (time != -1 && time < mission.awesomeTime)
                {
                    bestTimeScores.text +=
                        $"<color=#FF3333>{Utils.FormatTime(time)}</color>\n";
                }
                else if (time != -1 && time < mission.ultimateTime)
                {
                    bestTimeScores.text +=
                        $"<color=#FFCC33>{Utils.FormatTime(time)}</color>\n";
                }
                else if (time != -1 && time < mission.platinumTime)
                {
                    bestTimeScores.text +=
                        $"<color=#CCCCCC>{Utils.FormatTime(time)}</color>\n";
                }
                else
                {
                    bestTimeScores.text +=
                        $"{Utils.FormatTime(time)}\n";
                }
            }
        }

        if (mission.gameModes.Contains(Mode.Hunt))
        {
            platinumTimeText.text = "<color=#CCCCCC>" + Utils.FormatScoreNA(mission.platinumTime) + "</color>";
            ultimateTimeText.text = "<color=#FFCC33>" + Utils.FormatScoreNA(mission.ultimateTime) + "</color>";
            awesomeTimeText.text = "<color=#FF3333>" + Utils.FormatScoreNA(mission.awesomeTime) + "</color>";
        }
        else if (mission.gameModes.Contains(Mode.Madness))
        {
            platinumTimeText.text = "<color=#CCCCCC>" + (mission.platinumTime < 1000 ? Utils.FormatScoreNA(mission.platinumTime) : Utils.FormatTimeNA(mission.platinumTime)) + "</color>";
            ultimateTimeText.text = "<color=#FFCC33>" + (mission.ultimateTime < 1000 ? Utils.FormatScoreNA(mission.ultimateTime) : Utils.FormatTimeNA(mission.ultimateTime)) + "</color>";
            awesomeTimeText.text = "<color=#FF3333>" + (mission.awesomeTime < 1000 ? Utils.FormatScoreNA(mission.awesomeTime) : Utils.FormatTimeNA(mission.awesomeTime)) + "</color>";
        }
        else
        {
            platinumTimeText.text = "<color=#CCCCCC>" + Utils.FormatTimeNA(mission.platinumTime) + "</color>";
            ultimateTimeText.text = "<color=#FFCC33>" + Utils.FormatTimeNA(mission.ultimateTime) + "</color>";
            awesomeTimeText.text = "<color=#FF3333>" + Utils.FormatTimeNA(mission.awesomeTime) + "</color>";
        }
    }

    protected virtual void HandleEmptyMissionList()
    {
        if (levelDescriptionText)
            levelDescriptionText.gameObject.SetActive(false);

        if (levelImage)
            levelImage.color = Color.clear;

        if (currentLevelText)
            currentLevelText.text = "#0";

        if (notQualifiedImage)
            notQualifiedImage.SetActive(true);

        if (notQualifiedText)
            notQualifiedText.SetActive(true);

        if (prev)
            prev.interactable = false;

        if (next)
            next.interactable = false;

        if (play)
            play.interactable = false;


        bestTimeNames.text = string.Empty;
        bestTimeScores.text = string.Empty;

        for (int i = 0; i < 5; i++)
        {
            bestTimeNames.text += ($"{i + 1}.\t{name}\n");
            bestTimeScores.text += "99:59.999\n";
        }
    }

    protected virtual int GetQualifiedLevel()
    {
        if (debug && missions != null && missions.Count > 0)
            return missions.Count - 1;

        return PlayerPrefs.GetInt(
            $"QualifiedLevel{CapitalizeFirst(currentlySelectedType.ToString())}",
            0
        );
    }

    private int ClampSelectedLevel()
    {
        int qualifiedLevel = GetQualifiedLevel();

        int savedLevel = PlayerPrefs.GetInt(
            $"SelectedLevel",
            qualifiedLevel
        );

        if (savedLevel < 0)
            return 0;

        if (missions != null && savedLevel >= missions.Count)
            return Mathf.Max(0, missions.Count - 1);

        return savedLevel;
    }

    public void PrevButton()
    {
        if (selectedLevelNum > 0)
            SetLevelInfo(selectedLevelNum - 1);
    }

    public void NextButton()
    {
        if (selectedLevelNum < missions.Count - 1)
            SetLevelInfo(selectedLevelNum + 1);
    }

    public void ToggleReplay(bool value)
    {
        SetBlockerActive(value, false);
        ToggleReplayWindow(value);

        if (value)
            GetComponent<NewReplayManager>()?.Init();
        else
            ReplayRecorder.recordReplay = false;
    }

    public void ToggleMarbleSelectWindow(bool active)
    {
        ToggleWindow(marbleSelectWindow, active);
    }

    public void ToggleSearchWindow(bool active)
    {
        ToggleWindow(searchWindow, active);
    }

    public void ToggleVersaRunWindow(bool active)
    {
        ToggleWindow(versaRun, active);
    }

    public void ToggleReplayWindow(bool active)
    {
        ToggleWindow(replayWindow, active);
    }

    public void ToggleHintsWindow(bool active)
    {
        ToggleWindow(hintsWindow, active);
    }

    public void ToggleStatisticsWindow(bool active)
    {
        ToggleWindow(statisticsWindow, active);
    }

    protected void ToggleWindow(GameObject window, bool active)
    {
        if (window != null)
            window.SetActive(active);
    }

    protected void SetBlockerActive(bool active, bool active2)
    {
        if (raycastBlocker)
            raycastBlocker.SetActive(active);

        if (raycastBlocker2)
            raycastBlocker2.SetActive(active2);
    }

    public void SetMission(Mission m) => SetMissionInfo(m);

    protected void SetMissionInfo(Mission mission)
    {
        MissionInfo.instance.MissionPath = mission.directory;
        MissionInfo.instance.directory = mission.directory;

        // ============================================================
        // BASIC MISSION INFO
        // ============================================================

        MissionInfo.instance.missionName = mission.missionName;
        MissionInfo.instance.levelName = mission.levelName;
        MissionInfo.instance.levelNumber = mission.levelNumber;

        MissionInfo.instance.description = mission.description;
        MissionInfo.instance.startHelpText = mission.startHelpText;

        MissionInfo.instance.artist = mission.artist;
        MissionInfo.instance.music = mission.music;

        MissionInfo.instance.hasEgg = mission.hasEgg;


        // ============================================================
        // TIME
        // ============================================================

        MissionInfo.instance.time = mission.time;
        MissionInfo.instance.platinumTime = mission.platinumTime;
        MissionInfo.instance.ultimateTime = mission.ultimateTime;
        MissionInfo.instance.awesomeTime = mission.awesomeTime;
        MissionInfo.instance.alarmTime = mission.alarmTime;


        // ============================================================
        // SCORE
        // ============================================================

        MissionInfo.instance.parScore = mission.parScore;


        // ============================================================
        // HINTS
        // ============================================================

        MissionInfo.instance.generalHint = mission.generalHint;
        MissionInfo.instance.platinumHint = mission.platinumHint;
        MissionInfo.instance.ultimateHint = mission.ultimateHint;
        MissionInfo.instance.awesomeHint = mission.awesomeHint;
        MissionInfo.instance.nestEggHint = mission.nestEggHint;
        MissionInfo.instance.trivia = mission.trivia;

        generalHintText.text =
            "<size=20>General Hint</size>\n" + mission.generalHint;

        triviaText.text =
            "<size=20>Trivia</size>\n" + mission.trivia;


        // ============================================================
        // HINT UNLOCK CONDITIONS
        //
        // Ultimate Hint:
        //     Visible only after the player has beaten the level.
        //
        // Awesome Hint:
        //     Visible only after the player has beaten Ultimate.
        //
        // Hunt:
        //     Always score-based. Higher score is better.
        //
        // Madness:
        //     Score (< 1000): higher is better.
        //     Time (>= 1000): lower is better.
        //
        //     The player's result type MUST match the medal
        //     threshold type. A score cannot beat a time and
        //     a time cannot beat a score.
        //
        // Normal:
        //     Time-based. Lower time is better.
        // ============================================================

        float bestResult = PlayerPrefs.GetFloat(
            $"{MissionInfo.instance.levelName}_Time_0",
            -1f
        );

        bool hasBeatenLevel = bestResult != -1f;

        bool hasBeatenUltimate = false;

        if (hasBeatenLevel && mission.ultimateTime != -1)
        {
            if (mission.gameModes.Contains(Mode.Hunt))
            {
                // Hunt is always score-based.
                //
                // Both the stored result and Ultimate threshold
                // must be scores.
                bool bestIsScore = bestResult < 1000f;
                bool ultimateIsScore = mission.ultimateTime < 1000f;

                if (bestIsScore && ultimateIsScore)
                {
                    // Higher score is better.
                    hasBeatenUltimate =
                        bestResult > mission.ultimateTime;
                }
            }
            else if (mission.gameModes.Contains(Mode.Madness))
            {
                bool bestIsScore = bestResult < 1000f;
                bool ultimateIsScore = mission.ultimateTime < 1000f;

                // A score cannot beat a time and a time cannot
                // beat a score.
                if (bestIsScore != ultimateIsScore)
                {
                    hasBeatenUltimate = false;
                }
                else if (bestIsScore)
                {
                    // Both are scores.
                    // Higher score is better.
                    hasBeatenUltimate =
                        bestResult > mission.ultimateTime;
                }
                else
                {
                    // Both are times.
                    // Lower time is better.
                    hasBeatenUltimate =
                        bestResult < mission.ultimateTime;
                }
            }
            else
            {
                // Normal levels are time-based.
                //
                // Lower time is better.
                hasBeatenUltimate =
                    bestResult >= 0f &&
                    bestResult < mission.ultimateTime;
            }
        }

        if (ultimateHintText)
        {
            ultimateHintText.gameObject.SetActive(hasBeatenLevel);

            ultimateHintText.text =
                "<size=20>Ultimate Hint</size>\n" + mission.ultimateHint;
        }

        if (awesomeHintText)
        {
            awesomeHintText.gameObject.SetActive(hasBeatenUltimate);

            awesomeHintText.text =
                "<size=20>Awesome Hint</size>\n" + mission.awesomeHint;
        }

        if (nestEggHintText)
        {
            nestEggHintText.gameObject.SetActive(MissionInfo.instance.hasEgg);
            nestEggHintText.text =
                "<size=20>Nest Egg Hint</size>\n" + mission.nestEggHint;
        }

        RefreshTMPLayout(generalHintText);
        RefreshTMPLayout(ultimateHintText);
        RefreshTMPLayout(awesomeHintText);
        RefreshTMPLayout(nestEggHintText);
        RefreshTMPLayout(triviaText);


        // ============================================================
        // GAME MODES
        // ============================================================

        MissionInfo.instance.gameModes =
            mission.gameModes != null
                ? new List<Mode>(mission.gameModes)
                : new List<Mode> { Mode.Null };


        // ============================================================
        // QUOTA
        // ============================================================

        MissionInfo.instance.gemQuota = mission.gemQuota;


        // ============================================================
        // LAPS
        // ============================================================

        MissionInfo.instance.lapsNumber = mission.lapsNumber;

        MissionInfo.instance.noLapsCheckpoint =
            mission.noLapsCheckpoint;


        // ============================================================
        // 2D
        // ============================================================

        MissionInfo.instance.cameraPlane =
            mission.cameraPlane;

        MissionInfo.instance.invertCameraPlane =
            mission.invertCameraPlane;

        MissionInfo.instance.hasCameraPitch =
            mission.hasCameraPitch;

        MissionInfo.instance.cameraPitch =
            mission.cameraPitch;

        MissionInfo.instance.hasInitialCameraDistance =
            mission.hasInitialCameraDistance;

        MissionInfo.instance.initialCameraDistance =
            mission.initialCameraDistance;

        MissionInfo.instance.hasCameraFov =
            mission.hasCameraFov;

        MissionInfo.instance.cameraFov =
            mission.cameraFov;


        // ============================================================
        // CONSISTENCY
        // ============================================================

        MissionInfo.instance.minimumSpeed =
            mission.minimumSpeed;

        MissionInfo.instance.penaltyDelay =
            mission.penaltyDelay;

        MissionInfo.instance.gracePeriod =
            mission.gracePeriod;


        // ============================================================
        // HASTE
        // ============================================================

        MissionInfo.instance.speedToQualify =
            mission.speedToQualify;


        // ============================================================
        // HUNT
        // ============================================================

        MissionInfo.instance.maxGemsPerSpawn =
            mission.maxGemsPerSpawn;

        MissionInfo.instance.radiusFromGem =
            mission.radiusFromGem;

        MissionInfo.instance.spawnBlock =
            mission.spawnBlock;

        MissionInfo.instance.minPointsPerSpawn =
            mission.minPointsPerSpawn;

        MissionInfo.instance.minGemsPerSpawn =
            mission.minGemsPerSpawn;

        MissionInfo.instance.redSpawnChance =
            mission.redSpawnChance;

        MissionInfo.instance.yellowSpawnChance =
            mission.yellowSpawnChance;

        MissionInfo.instance.blueSpawnChance =
            mission.blueSpawnChance;

        MissionInfo.instance.platinumSpawnChance =
            mission.platinumSpawnChance;

        MissionInfo.instance.gemGroups =
            mission.gemGroups;


        // ============================================================
        // RADAR
        // ============================================================

        MissionInfo.instance.radar =
            mission.radar;

        MissionInfo.instance.customRadarRule =
            mission.customRadarRule;

        MissionInfo.instance.forceRadar =
            mission.forceRadar;

        MissionInfo.instance.hideRadar =
            mission.hideRadar;


        // ============================================================
        // PHYSICS
        // ============================================================

        MissionInfo.instance.gravity =
            mission.gravity;

        MissionInfo.instance.angularAcceleration =
            mission.angularAcceleration;

        MissionInfo.instance.brakingAcceleration =
            mission.brakingAcceleration;

        MissionInfo.instance.maxRollVelocity =
            mission.maxRollVelocity;

        MissionInfo.instance.jumpImpulse =
            mission.jumpImpulse;


        // ============================================================
        // SKYBOX
        // ============================================================

        MissionInfo.instance.specialMissionMode =
            mission.specialMissionMode;

        string skyboxName = mission.skyboxName;

        skyboxName =
            string.IsNullOrEmpty(skyboxName)
                ? "blender3"
                : skyboxName;

        MissionInfo.instance.skyboxName =
            Application.CanStreamedLevelBeLoaded(skyboxName)
                ? skyboxName
                : "blender3";
    }


    protected void BindButton(
        GameObject buttonObj,
        UnityEngine.Events.UnityAction action
    )
    {
        if (
            buttonObj
            && buttonObj.TryGetComponent<Button>(out var btn)
        )
        {
            btn.onClick.AddListener(action);
        }
    }

    protected void RefreshTMPLayout(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        tmp.ForceMeshUpdate();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            tmp.rectTransform
        );
    }

    public static string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0])
            + input.Substring(1);
    }
}