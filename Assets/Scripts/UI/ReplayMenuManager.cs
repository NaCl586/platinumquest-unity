/*using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Server;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReplayMenuManager : MonoBehaviour
{
    [System.Serializable]
    public class Replay
    {
        public Mission mission;

        public string fileName;
        public string filePath;
        public string replayName;
        public string author;
        public string description;

        public string completed;
    }

    public List<Replay> replays = new List<Replay>();

    [Space]
    public Button homeButton;
    public Button playReplayButton;

    [Space]
    [SerializeField]
    private Scrollbar replayScrollbar;
    public ScrollRect replayScrollRect;

    [SerializeField]
    private Button replayScrollUpButton;

    [SerializeField]
    private Button replayScrollDownButton;

    [SerializeField]
    private Scrollbar descScrollbar;
    public ScrollRect descScrollRect;

    [SerializeField]
    private Button descScrollUpButton;

    [SerializeField]
    private Button descScrollDownButton;

    [SerializeField]
    private float step = 0.1f;

    [Space]
    public Transform content;
    public Button buttonInstance;
    public Image levelImage;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI authorText;
    public TextMeshProUGUI descriptionText;

    private Replay currentReplay;
    private Button selectedButton;

    LeaderboardsMenu lm;

    public void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ReplayRecorder.loadReplay = false;
        lm = null;
        lm = GetComponent<LeaderboardsMenu>();

        descScrollbar.onValueChanged.AddListener(OnDescScrollbarValueChanged);
        OnDescScrollbarValueChanged(descScrollbar.value);

        replayScrollbar.onValueChanged.AddListener(OnReplayScrollbarValueChanged);
        OnReplayScrollbarValueChanged(replayScrollbar.value);

        homeButton.onClick.AddListener(() =>
        {
            if (lm != null)
                lm.CloseReplayMenu();
            else
                SceneManager.LoadScene("MainMenu");
        });

        playReplayButton.onClick.AddListener(() =>
        {
            LoadReplay();
        });

        descScrollDownButton.onClick.AddListener(() =>
        {
            ScrollDown(descScrollRect);
        });

        descScrollUpButton.onClick.AddListener(() =>
        {
            ScrollUp(descScrollRect);
        });

        replayScrollDownButton.onClick.AddListener(() =>
        {
            ScrollDown(replayScrollRect);
        });

        replayScrollUpButton.onClick.AddListener(() =>
        {
            ScrollUp(replayScrollRect);
        });

        playReplayButton.interactable = false;

        selectedButton = null;

        StartCoroutine(WaitUntilMissionsLoaded());
    }

    private IEnumerator WaitUntilMissionsLoaded()
    {
        MissionInfo mi = MissionInfo.instance;

        while (
            mi == null
            || mi.missionsPlatinumBeginner == null
            || mi.missionsPlatinumBeginner.Count == 0
            || mi.missionsPlatinumIntermediate.Count == 0
            || mi.missionsPlatinumAdvanced.Count == 0
            || mi.missionsPlatinumExpert.Count == 0
            || mi.missionsGoldBeginner.Count == 0
            || mi.missionsGoldIntermediate.Count == 0
            || mi.missionsGoldAdvanced.Count == 0
            || mi.missionsGoldCustom.Count == 0
        )
        {
            yield return null;
        }

        InitReplayList();
    }

    void LoadReplay()
    {
        if (currentReplay == null || currentReplay.mission == null)
            return;

        ReplayRecorder.loadedReplayPath = currentReplay.filePath;
        ReplayRecorder.replayName = currentReplay.fileName;
        ReplayRecorder.loadReplay = true;
        ReplayRecorder.incompleteReplay = currentReplay.completed == "Incomplete";

        Mission selectedMission = currentReplay.mission;

        MissionInfo.instance.MissionPath = selectedMission.directory;
        MissionInfo.instance.missionName = selectedMission.missionName;
        MissionInfo.instance.time = selectedMission.time;
        MissionInfo.instance.levelName = selectedMission.levelName;
        MissionInfo.instance.description = selectedMission.description;
        MissionInfo.instance.startHelpText = selectedMission.startHelpText;
        MissionInfo.instance.level = selectedMission.levelNumber;
        MissionInfo.instance.artist = selectedMission.artist;
        MissionInfo.instance.platinumTime = selectedMission.platinumTime;
        MissionInfo.instance.ultimateTime = selectedMission.ultimateTime;
        MissionInfo.instance.alarmTime = selectedMission.alarmTime;
        MissionInfo.instance.hasEgg = selectedMission.hasEgg;

        string musicName = selectedMission.music;
        musicName = string.IsNullOrEmpty(musicName)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(musicName.Trim());
        musicName = musicName.Replace(".ogg", "");
        MissionInfo.instance.music = musicName;

        string skyboxName = selectedMission.skyboxName;
        skyboxName = string.IsNullOrEmpty(skyboxName) ? "intermediate_sky" : skyboxName;
        MissionInfo.instance.skybox = Application.CanStreamedLevelBeLoaded(skyboxName)
            ? skyboxName
            : "intermediate_sky";

        LeaderboardsMenu.ReplayCenterLoadedFromLeaderboards = false;

        if (lm != null)
            StartCoroutine(LoadReplayLeaderboard(lm));
        else
            SceneManager.LoadScene("Loading");
    }

    IEnumerator LoadReplayLeaderboard(LeaderboardsMenu lm)
    {
        LeaderboardsMenu.ReplayCenterLoadedFromLeaderboards = true;

        JukeboxManager.instance.ForceStop();
        lm.blackout.SetActive(true);

        lm.ShowLoading("Loading Replay...");
        yield return new WaitForSecondsRealtime(1f);

        SceneManager.LoadScene("Loading");
    }

    public void RefreshTMPLayout(TextMeshProUGUI tmp)
    {
        tmp.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tmp.rectTransform);
    }

    public void Update()
    {
        selectedButton?.Select();

        if (lm == null)
            if (Input.GetKeyDown(KeyCode.Escape))
                SceneManager.LoadScene("MainMenu");
    }

    public void InitReplayList()
    {
        replays.Clear();

        string replayFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Replay");

        if (!Directory.Exists(replayFolder))
            return;

        // --------------------------------------------------
        // Main Replay folder
        // --------------------------------------------------

        string[] files = Directory.GetFiles(replayFolder, "*.urec");

        foreach (string file in files)
        {
            Replay replay = ReadReplayMetadata(file);

            if (replay != null)
                replays.Add(replay);
        }

        // --------------------------------------------------
        // Leaderboard replays
        // --------------------------------------------------

        string leaderboardFolder = Path.Combine(replayFolder, "Leaderboard");

        if (Directory.Exists(leaderboardFolder))
        {
            files = Directory.GetFiles(leaderboardFolder, "*.urec");

            foreach (string file in files)
            {
                Replay replay = ReadReplayMetadata(file);

                if (replay != null)
                    replays.Add(replay);
            }
        }

        // --------------------------------------------------
        // Pending replays
        // --------------------------------------------------

        string pendingFolder = Path.Combine(replayFolder, "Pending");

        if (Directory.Exists(pendingFolder))
        {
            files = Directory.GetFiles(pendingFolder, "*.urec");

            foreach (string file in files)
            {
                Replay replay = ReadReplayMetadata(file);

                if (replay != null)
                    replays.Add(replay);
            }
        }

        // --------------------------------------------------
        // Sort
        // --------------------------------------------------

        replays = replays.OrderBy(r => r.replayName).ToList();

        PopulateReplayButtons();
    }

    private void PopulateReplayButtons()
    {
        foreach (Replay replay in replays)
        {
            Button button = Instantiate(buttonInstance, content);

            button.gameObject.SetActive(true);

            string replayName = string.IsNullOrWhiteSpace(replay.replayName)
                ? replay.fileName
                : replay.replayName;

            button.GetComponentInChildren<TextMeshProUGUI>().text = replayName;

            button.onClick.AddListener(() =>
            {
                if (selectedButton != null)
                    selectedButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.black;

                button.Select();
                selectedButton = button;
                button.GetComponentInChildren<TextMeshProUGUI>().color = new Color(
                    0.9804f,
                    0.7843f,
                    0.5137f,
                    1f
                );

                SelectReplay(replay);
                playReplayButton.interactable = true;
            });
        }

        var children = content
            .Cast<Transform>()
            .OrderBy(t => t.Find("Text").GetComponent<TextMeshProUGUI>().text)
            .ToList();

        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }
    }

    private void SelectReplay(Replay replay)
    {
        currentReplay = replay;

        authorText.text = string.IsNullOrWhiteSpace(replay.author)
            ? "Not Specified"
            : replay.author;

        descriptionText.text = string.IsNullOrWhiteSpace(replay.description)
            ? "No Description."
            : replay.description;

        RefreshTMPLayout(descriptionText);

        if (replay.mission != null)
        {
            levelNameText.text = replay.mission.levelName;
            levelImage.sprite = replay.mission.levelImage;
            levelImage.color = replay.mission.levelImage ? Color.white : Color.clear;
        }
        else
        {
            levelNameText.text = string.Empty;
            levelImage.sprite = null;
            levelImage.color = Color.clear;
        }
    }

    private Replay ReadReplayMetadata(string path)
    {
        try
        {
            using (FileStream stream = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Header
                if (
                    reader.ReadByte() != 'U'
                    || reader.ReadByte() != 'R'
                    || reader.ReadByte() != 'E'
                    || reader.ReadByte() != 'C'
                )
                    return null;

                if (reader.ReadByte() != ReplayRecorder.ReplayVersion)
                    return null;

                // Metadata size
                stream.Seek(-4, SeekOrigin.End);
                int metadataSize = reader.ReadInt32();

                // Jump directly to metadata
                stream.Position = stream.Length - 4 - metadataSize;

                Replay replay = new Replay();

                replay.fileName = Path.GetFileNameWithoutExtension(path);

                replay.filePath = path;

                string missionPath = reader.ReadString();

                reader.ReadString(); // Stored level name (unused)
                reader.ReadString(); // Marble ID (unused)

                replay.replayName = reader.ReadString();

                replay.author = reader.ReadString();

                replay.description = reader.ReadString();

                replay.completed = reader.ReadString();

                replay.mission = FindMission(missionPath);

                return replay;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to read replay '{path}': {e.Message}");
            return null;
        }
    }

    private Mission FindMission(string missionPath)
    {
        MissionInfo mi = MissionInfo.instance;

        List<Mission>[] lists =
        {
            mi.missionsGoldBeginner,
            mi.missionsGoldIntermediate,
            mi.missionsGoldAdvanced,
            mi.missionsGoldCustom,
            mi.missionsPlatinumBeginner,
            mi.missionsPlatinumIntermediate,
            mi.missionsPlatinumAdvanced,
            mi.missionsPlatinumExpert,
        };

        foreach (var list in lists)
        {
            Mission mission = list.Find(m => m.directory == missionPath);

            if (mission != null)
                return mission;
        }

        return null;
    }

    public void ScrollUp(ScrollRect scrollRect)
    {
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + step
        );
    }

    public void ScrollDown(ScrollRect scrollRect)
    {
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition - step
        );
    }

    private void OnReplayScrollbarValueChanged(float value)
    {
        replayScrollUpButton.interactable = value < 1f;
        replayScrollDownButton.interactable = value > 0f;
    }

    private void OnDescScrollbarValueChanged(float value)
    {
        descScrollUpButton.interactable = value < 1f;
        descScrollDownButton.interactable = value > 0f;
    }
}
*/