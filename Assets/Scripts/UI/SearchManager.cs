using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SearchManager : MonoBehaviour
{
    public Button cancelButton;
    public Button playButton;
    public Button optionsButton;

    public Transform content;
    public Button buttonInstance;

    public TMP_InputField inputField;

    public GameObject searchOptionsWindow;

    [Header("Search Mode")]
    [SerializeField] private TMP_Text searchModeLabel;
    [SerializeField] private Button nameButton;
    [SerializeField] private Button artistButton;
    [SerializeField] private Button fileButton;
    [SerializeField] private Button randomButton;

    private enum SearchMode
    {
        Name,
        Artist,
        File
    }

    [SerializeField] private SearchMode searchMode = SearchMode.Name;

    private Button highlightedButton;
    private readonly Dictionary<Button, Mission> buttonMissions = new Dictionary<Button, Mission>();

    public void Start()
    {
        optionsButton.onClick.AddListener(OpenSearchOptions);

        cancelButton.onClick.AddListener(() =>
        {
            PlayMissionManager manager = GetComponent<PlayMissionManager>();
            manager.ToggleSearchWindow(false);
            manager.SetLevelInfo(manager.selectedLevelNum);
            manager.raycastBlocker.SetActive(false);
        });

        playButton.onClick.AddListener(() =>
        {
            ExecuteHighlighted();
            if (highlightedButton != null)
            {
                SceneManager.LoadScene("Loading");
            }
        });

        inputField.onValueChanged.AddListener(FilterButtons);

        if (nameButton != null) nameButton.onClick.AddListener(() => SetSearchMode(SearchMode.Name));
        if (artistButton != null) artistButton.onClick.AddListener(() => SetSearchMode(SearchMode.Artist));
        if (fileButton != null) fileButton.onClick.AddListener(() => SetSearchMode(SearchMode.File));
        if (randomButton != null) randomButton.onClick.AddListener(PlayRandomMission);

        UpdateSearchModeLabel();

        highlightedButton = null;

        if (playButton != null)
            playButton.interactable = false;

        FilterButtons(inputField.text);
    }

    private void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(FilterButtons);
        }
    }

    private void SetSearchMode(SearchMode mode)
    {
        searchMode = mode;
        inputField.text = string.Empty;

        UpdateSearchModeLabel();
        RefreshButtonTexts();
        FilterButtons(string.Empty);

        playButton.interactable = false;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        }
    }

    private void UpdateSearchModeLabel()
    {
        if (searchModeLabel == null) return;

        switch (searchMode)
        {
            case SearchMode.Name:
                searchModeLabel.text = "Title:";
                break;
            case SearchMode.Artist:
                searchModeLabel.text = "Artist:";
                break;
            case SearchMode.File:
                searchModeLabel.text = "File:";
                break;
        }
    }

    private void HighlightButton(Button button)
    {
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable || highlightedButton == button)
            return;

        ClearHighlight();

        highlightedButton = button;
        ColorBlock colors = button.colors;

        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = colors.selectedColor;
        }

        if (playButton != null)
        {
            playButton.interactable = true;
        }
    }

    private void ClearHighlight()
    {
        if (highlightedButton != null)
        {
            ColorBlock colors = highlightedButton.colors;

            if (highlightedButton.targetGraphic != null)
            {
                highlightedButton.targetGraphic.color = colors.normalColor;
            }
        }

        highlightedButton = null;

        if (playButton != null)
        {
            playButton.interactable = false;
        }
    }

    private void ExecuteHighlighted()
    {
        if (highlightedButton == null) return;

        highlightedButton.onClick.Invoke();

        if (highlightedButton != null)
        {
            HighlightButton(highlightedButton);
        }
    }

    private void FilterButtons(string input)
    {
        string search = input.Trim().ToLowerInvariant();
        bool found = false;

        Button previousSelected = highlightedButton;

        for (int i = 0; i < content.childCount; i++)
        {
            Transform t = content.GetChild(i);
            Button button = t.GetComponent<Button>();

            if (button == null || !buttonMissions.TryGetValue(button, out Mission mission))
                continue;

            string searchableText = GetSearchText(mission);
            bool matches = string.IsNullOrEmpty(search) || searchableText.ToLowerInvariant().Contains(search);

            t.gameObject.SetActive(matches);

            if (matches)
            {
                found = true;
            }
        }

        if (previousSelected != null)
        {
            bool selectedStillExists = buttonMissions.ContainsKey(previousSelected);
            bool selectedStillActive = selectedStillExists && previousSelected.gameObject.activeInHierarchy;
            bool selectedStillInteractable = selectedStillActive && previousSelected.interactable;

            if (!selectedStillInteractable)
            {
                ClearHighlight();
            }
            else
            {
                highlightedButton = previousSelected;
            }
        }

        if (playButton != null)
        {
            playButton.interactable = highlightedButton != null &&
                                      highlightedButton.gameObject.activeInHierarchy &&
                                      highlightedButton.interactable;
        }

        if (randomButton != null)
        {
            randomButton.interactable = found;
        }
    }

    private string GetSearchText(Mission m)
    {
        if (m == null) return string.Empty;

        switch (searchMode)
        {
            case SearchMode.Name:
                return m.levelName ?? string.Empty;
            case SearchMode.Artist:
                return m.artist ?? string.Empty;
            case SearchMode.File:
                return GetFilePath(m);
            default:
                return string.Empty;
        }
    }

    private string GetDisplayText(Mission m)
    {
        if (m == null) return string.Empty;

        switch (searchMode)
        {
            case SearchMode.Name:
                return m.levelName ?? string.Empty;
            case SearchMode.Artist:
                if (string.IsNullOrWhiteSpace(m.artist))
                {
                    return m.levelName ?? string.Empty;
                }
                return (m.levelName ?? string.Empty) + " By " + m.artist;
            case SearchMode.File:
                return GetFilePath(m);
            default:
                return string.Empty;
        }
    }

    private string GetFilePath(Mission m)
    {
        if (m == null) return string.Empty;

        string missionType = GetMissionTypeName(m);
        string levelName = m.levelName ?? string.Empty;

        if (!levelName.EndsWith(".mis", StringComparison.OrdinalIgnoreCase))
        {
            levelName += ".mis";
        }

        return missionType + "/" + levelName;
    }

    private string GetMissionTypeName(Mission mission)
    {
        if (mission == null || MissionInfo.instance == null)
            return string.Empty;

        if (MissionInfo.instance.missionsTutorial?.Contains(mission) == true) return "tutorial";
        if (MissionInfo.instance.missionsBeginner?.Contains(mission) == true) return "beginner";
        if (MissionInfo.instance.missionsIntermediate?.Contains(mission) == true) return "intermediate";
        if (MissionInfo.instance.missionsAdvanced?.Contains(mission) == true) return "advanced";
        if (MissionInfo.instance.missionsExpert?.Contains(mission) == true) return "expert";
        if (MissionInfo.instance.missionsBonus?.Contains(mission) == true) return "bonus";
        if (MissionInfo.instance.missionsDC?.Contains(mission) == true) return "dc";

        return string.Empty;
    }

    private void RefreshButtonTexts()
    {
        foreach (var pair in buttonMissions)
        {
            Button button = pair.Key;
            Mission mission = pair.Value;

            if (button == null) continue;

            Transform textTransform = button.transform.Find("Text");
            if (textTransform == null) continue;

            TMP_Text text = textTransform.GetComponent<TMP_Text>();
            if (text == null) continue;

            text.text = GetDisplayText(mission);
        }

        SortButtons();
    }

    public List<Mission> GetMissionList(Type type)
    {
        if (MissionInfo.instance == null) return new List<Mission>();

        switch (type)
        {
            case Type.tutorial: return MissionInfo.instance.missionsTutorial;
            case Type.beginner: return MissionInfo.instance.missionsBeginner;
            case Type.intermediate: return MissionInfo.instance.missionsIntermediate;
            case Type.advanced: return MissionInfo.instance.missionsAdvanced;
            case Type.expert: return MissionInfo.instance.missionsExpert;
            case Type.bonus: return MissionInfo.instance.missionsBonus;
            case Type.dc: return MissionInfo.instance.missionsDC;
            default: return new List<Mission>();
        }
    }

    public void InitSearchElements()
    {
        PlayMissionManager pm = GetComponent<PlayMissionManager>();

        buttonMissions.Clear();
        ClearHighlight();

        for (int i = content.childCount - 1; i >= 1; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        (Type type, string prefKey)[] categories = new[]
        {
            (Type.tutorial, "QualifiedLevelTutorial"),
            (Type.beginner, "QualifiedLevelBeginner"),
            (Type.intermediate, "QualifiedLevelIntermediate"),
            (Type.advanced, "QualifiedLevelAdvanced"),
            (Type.expert, "QualifiedLevelExpert"),
            (Type.bonus, "QualifiedLevelBonus"),
            (Type.dc, "QualifiedLevelDC")
        };

        foreach (var (type, prefKey) in categories)
        {
            List<Mission> missionList = GetMissionList(type);
            if (missionList == null) continue;

            int qualifiedLevel = PlayerPrefs.GetInt(prefKey, 0);
            foreach (Mission m in missionList)
            {
                if (pm.debug || m.levelNumber <= qualifiedLevel + 1)
                {
                    InstantiateButton(m, Color.black);
                }
            }
        }

        SortButtons();
        FilterButtons(inputField != null ? inputField.text : string.Empty);
    }

    private void InstantiateButton(Mission m, Color color)
    {
        Button missionButton = Instantiate(buttonInstance, content);
        missionButton.gameObject.SetActive(true);

        buttonMissions[missionButton] = m;

        Transform textTransform = missionButton.transform.Find("Text");
        if (textTransform != null)
        {
            TMP_Text text = textTransform.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = GetDisplayText(m);
                text.color = color;
            }
        }

        missionButton.onClick.AddListener(() =>
        {
            SetMissionInfo(m);
            GetComponent<PlayMissionManager>().SetMission(m);
        });

        EventTrigger trigger = missionButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };

        entry.callback.AddListener(_ => HighlightButton(missionButton));
        trigger.triggers.Add(entry);
    }

    private void PlayRandomMission()
    {
        List<Button> availableButtons = new List<Button>();

        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);
            if (!child.gameObject.activeInHierarchy) continue;

            Button button = child.GetComponent<Button>();
            if (button == null || !button.interactable || !buttonMissions.ContainsKey(button))
                continue;

            availableButtons.Add(button);
        }

        if (availableButtons.Count == 0) return;

        Button selectedButton = availableButtons[UnityEngine.Random.Range(0, availableButtons.Count)];
        Mission selectedMission = buttonMissions[selectedButton];

        HighlightButton(selectedButton);
        SetMissionInfo(selectedMission);
        SceneManager.LoadScene("Loading");
    }

    public void SortButtons()
    {
        var buttons = content
            .Cast<Transform>()
            .Select(t =>
            {
                Button button = t.GetComponent<Button>();
                TMP_Text text = t.GetComponentInChildren<TMP_Text>();
                Mission mission = null;

                if (button != null)
                {
                    buttonMissions.TryGetValue(button, out mission);
                }

                return new
                {
                    Transform = t,
                    Mission = mission,
                    Text = text != null ? text.text : string.Empty,
                    Color = text != null ? text.color : Color.black
                };
            })
            .OrderBy(b => ColorPriority(b.Color))
            .ThenBy(b => GetSortText(b.Mission, b.Text), StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].Transform.SetSiblingIndex(i);
        }
    }

    private string GetSortText(Mission mission, string displayText)
    {
        if (mission == null) return displayText;

        switch (searchMode)
        {
            case SearchMode.Name:
            case SearchMode.Artist:
                return mission.levelName ?? string.Empty;
            case SearchMode.File:
                return GetFilePath(mission);
            default:
                return displayText;
        }
    }

    private int ColorPriority(Color c)
    {
        if (Approximately(c, Color.black)) return 0;
        if (Approximately(c, Color.green)) return 1;
        return 2;
    }

    private bool Approximately(Color a, Color b)
    {
        return Mathf.Approximately(a.r, b.r) &&
               Mathf.Approximately(a.g, b.g) &&
               Mathf.Approximately(a.b, b.b);
    }

    private void SetMissionInfo(Mission m)
    {
        MissionInfo.instance.MissionPath = m.directory;
        MissionInfo.instance.directory = m.directory;

        MissionInfo.instance.missionName = m.missionName;
        MissionInfo.instance.levelName = m.levelName;
        MissionInfo.instance.levelNumber = m.levelNumber;
        MissionInfo.instance.description = m.description;
        MissionInfo.instance.startHelpText = m.startHelpText;
        MissionInfo.instance.artist = m.artist;
        MissionInfo.instance.music = m.music;
        MissionInfo.instance.hasEgg = m.hasEgg;

        MissionInfo.instance.time = m.time;
        MissionInfo.instance.platinumTime = m.platinumTime;
        MissionInfo.instance.ultimateTime = m.ultimateTime;
        MissionInfo.instance.awesomeTime = m.awesomeTime;
        MissionInfo.instance.alarmTime = m.alarmTime;

        MissionInfo.instance.parScore = m.parScore;

        MissionInfo.instance.generalHint = m.generalHint;
        MissionInfo.instance.platinumHint = m.platinumHint;
        MissionInfo.instance.ultimateHint = m.ultimateHint;
        MissionInfo.instance.awesomeHint = m.awesomeHint;
        MissionInfo.instance.nestEggHint = m.nestEggHint;
        MissionInfo.instance.trivia = m.trivia;

        MissionInfo.instance.gameModes = m.gameModes != null ? new List<Mode>(m.gameModes) : new List<Mode> { Mode.Null };

        MissionInfo.instance.gemQuota = m.gemQuota;
        MissionInfo.instance.lapsNumber = m.lapsNumber;
        MissionInfo.instance.noLapsCheckpoint = m.noLapsCheckpoint;

        MissionInfo.instance.cameraPlane = m.cameraPlane;
        MissionInfo.instance.invertCameraPlane = m.invertCameraPlane;
        MissionInfo.instance.hasCameraPitch = m.hasCameraPitch;
        MissionInfo.instance.cameraPitch = m.cameraPitch;
        MissionInfo.instance.hasInitialCameraDistance = m.hasInitialCameraDistance;
        MissionInfo.instance.initialCameraDistance = m.initialCameraDistance;
        MissionInfo.instance.hasCameraFov = m.hasCameraFov;
        MissionInfo.instance.cameraFov = m.cameraFov;

        MissionInfo.instance.minimumSpeed = m.minimumSpeed;
        MissionInfo.instance.penaltyDelay = m.penaltyDelay;
        MissionInfo.instance.gracePeriod = m.gracePeriod;

        MissionInfo.instance.speedToQualify = m.speedToQualify;

        MissionInfo.instance.maxGemsPerSpawn = m.maxGemsPerSpawn;
        MissionInfo.instance.radiusFromGem = m.radiusFromGem;
        MissionInfo.instance.spawnBlock = m.spawnBlock;
        MissionInfo.instance.minPointsPerSpawn = m.minPointsPerSpawn;
        MissionInfo.instance.minGemsPerSpawn = m.minGemsPerSpawn;
        MissionInfo.instance.redSpawnChance = m.redSpawnChance;
        MissionInfo.instance.yellowSpawnChance = m.yellowSpawnChance;
        MissionInfo.instance.blueSpawnChance = m.blueSpawnChance;
        MissionInfo.instance.platinumSpawnChance = m.platinumSpawnChance;
        MissionInfo.instance.gemGroups = m.gemGroups;

        MissionInfo.instance.radar = m.radar;
        MissionInfo.instance.customRadarRule = m.customRadarRule;
        MissionInfo.instance.forceRadar = m.forceRadar;
        MissionInfo.instance.hideRadar = m.hideRadar;

        MissionInfo.instance.gravity = m.gravity;
        MissionInfo.instance.angularAcceleration = m.angularAcceleration;
        MissionInfo.instance.brakingAcceleration = m.brakingAcceleration;
        MissionInfo.instance.maxRollVelocity = m.maxRollVelocity;
        MissionInfo.instance.jumpImpulse = m.jumpImpulse;
        MissionInfo.instance.specialMissionMode = m.specialMissionMode;

        string skyboxName = string.IsNullOrEmpty(m.skyboxName) ? "blender3" : m.skyboxName;
        MissionInfo.instance.skyboxName = Application.CanStreamedLevelBeLoaded(skyboxName) ? skyboxName : "blender3";
    }

    public void OpenSearchOptions()
    {
        playButton.interactable = false;
        searchOptionsWindow.SetActive(true);
    }

    public void CloseSearchOption()
    {
        searchOptionsWindow.SetActive(false);
    }
}