using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [Header("Tabs")]
    public Button graphicsButton;
    public Button inputButton;
    public Button audioButton;

    [Header("Menus")]
    public GameObject graphicsMenu;
    public GameObject inputMenu;
    public GameObject audioMenu;

    [Header("Common Buttons")]
    public Button homeButton;
    public Button applyButton;

    [Header("Graphics")]
    public TMP_Dropdown screenResolutionDropdown;
    public TMP_Dropdown screenStyleDropdown;
    public TMP_Dropdown postProcessingDropdown;
    public TMP_Dropdown maxFramerateDropdown;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown frameRateDropdown;
    public TMP_Dropdown oobInsultsDropdown;
    public TMP_Dropdown verticalSyncDropdown;

    public Slider fieldOfViewSlider;
    public Slider maxRadarItemsSlider;

    [Header("Input")]
    public Slider mouseSpeedSlider;
    public Slider keyboardSpeedSlider;

    public TMP_Dropdown freeLookDropdown;
    public TMP_Dropdown invertAxisDropdown;

    [Header("Audio")]
    public Slider musicSlider;
    public Slider soundSlider;

    [Header("Input Bindings")]
    public Button moveForwardButton;
    public Button moveBackwardButton;
    public Button moveLeftButton;
    public Button moveRightButton;

    public Button lookUpButton;
    public Button lookDownButton;
    public Button lookLeftButton;
    public Button lookRightButton;

    public Button jumpButton;
    public Button freeLookButton;
    public Button usePowerupButton;

    public Button blastButton;
    public Button respawnButton;
    public Button toggleRadarButton;

    [Header("Remapping")]
    public GameObject remapMenu;
    public GameObject confirmMenu;

    public TextMeshProUGUI remapCaption;
    public TextMeshProUGUI confirmCaption;

    public Button yesButton;
    public Button noButton;

    private Resolution[] availableResolutions;

    private int selectedResolutionIndex;
    private bool selectedFullscreen;
    private int selectedQuality;
    private int selectedMaxFramerate;
    private bool selectedVSync;
    private bool selectedPostProcessing;
    private bool selectedFrameRate;
    private bool selectedOobInsults;
    private float selectedFOV;
    private int selectedMaxRadarItems;
    private bool selectedInvertAxis;

    private float mouseSensitivity;
    private float keyboardSensitivity;

    private string bindToBeRemapped;
    private Button buttonToBeRemapped;
    private string conflictedMapping;
    private Button conflictedButton;
    private KeyCode tempKeycode;

    private const string MainMenuScene = "MainMenu";

    private void Start()
    {
        fieldOfViewSlider.minValue = 40f;
        fieldOfViewSlider.maxValue = 140f;

        maxRadarItemsSlider.minValue = 5f;
        maxRadarItemsSlider.maxValue = 85f;

        mouseSpeedSlider.minValue = 5f;
        mouseSpeedSlider.maxValue = 95f;

        keyboardSpeedSlider.minValue = 5f;
        keyboardSpeedSlider.maxValue = 95f;

        SetupButtons();

        PopulateResolutionDropdown();
        PopulateDropdowns();

        LoadSettings();
        LoadInputSettings();
        LoadAudioSettings();

        UpdateUI();
        SetMenu(0);
    }

    private void SetupButtons()
    {
        graphicsButton.onClick.AddListener(() => SetMenu(0));
        inputButton.onClick.AddListener(() => SetMenu(1));
        audioButton.onClick.AddListener(() => SetMenu(2));

        applyButton.onClick.AddListener(ApplySettings);
        homeButton.onClick.AddListener(Home);

        moveForwardButton.onClick.AddListener(() => RemapButton("Move Forward", moveForwardButton));
        moveBackwardButton.onClick.AddListener(() => RemapButton("Move Backward", moveBackwardButton));
        moveLeftButton.onClick.AddListener(() => RemapButton("Move Left", moveLeftButton));
        moveRightButton.onClick.AddListener(() => RemapButton("Move Right", moveRightButton));

        lookUpButton.onClick.AddListener(() => RemapButton("Rotate Camera Up", lookUpButton));
        lookDownButton.onClick.AddListener(() => RemapButton("Rotate Camera Down", lookDownButton));
        lookLeftButton.onClick.AddListener(() => RemapButton("Rotate Camera Left", lookLeftButton));
        lookRightButton.onClick.AddListener(() => RemapButton("Rotate Camera Right", lookRightButton));

        jumpButton.onClick.AddListener(() => RemapButton("Jump", jumpButton));
        freeLookButton.onClick.AddListener(() => RemapButton("Free-Look Key", freeLookButton));
        usePowerupButton.onClick.AddListener(() => RemapButton("Use Powerup", usePowerupButton));

        blastButton.onClick.AddListener(() => RemapButton("Blast", blastButton));
        respawnButton.onClick.AddListener(() => RemapButton("Respawn", respawnButton));
        toggleRadarButton.onClick.AddListener(() => RemapButton("Toggle Radar", toggleRadarButton));

        yesButton.onClick.AddListener(CancelMapping);
        noButton.onClick.AddListener(ForceMapping);

        mouseSpeedSlider.onValueChanged.AddListener(SetMouseSpeed);
        keyboardSpeedSlider.onValueChanged.AddListener(SetKeyboardSpeed);
        fieldOfViewSlider.onValueChanged.AddListener(SetFieldOfView);
        maxRadarItemsSlider.onValueChanged.AddListener(SetMaxRadarItems);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        soundSlider.onValueChanged.AddListener(SetSoundVolume);

        screenResolutionDropdown.onValueChanged.AddListener(SetResolution);
        screenStyleDropdown.onValueChanged.AddListener(SetScreenStyle);
        postProcessingDropdown.onValueChanged.AddListener(SetPostProcessing);
        maxFramerateDropdown.onValueChanged.AddListener(SetMaxFramerate);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        frameRateDropdown.onValueChanged.AddListener(SetFrameRate);
        oobInsultsDropdown.onValueChanged.AddListener(SetOobInsults);
        verticalSyncDropdown.onValueChanged.AddListener(SetVerticalSync);
        invertAxisDropdown.onValueChanged.AddListener(SetInvertAxis);
        freeLookDropdown.onValueChanged.AddListener(SetFreeLook);
    }

    public void SetMenu(int index)
    {
        graphicsMenu.SetActive(false);
        inputMenu.SetActive(false);
        audioMenu.SetActive(false);

        switch (index)
        {
            case 0:
                graphicsMenu.SetActive(true);
                break;
            case 1:
                inputMenu.SetActive(true);
                break;
            case 2:
                audioMenu.SetActive(true);
                break;
        }
    }

    private void PopulateResolutionDropdown()
    {
        Resolution[] allResolutions = Screen.resolutions;

        List<Resolution> filteredResolutions = new List<Resolution>();
        List<string> options = new List<string>();

        int defaultIndex = 0;

        const float targetAspectRatio = 16f / 9f;
        const float aspectTolerance = 0.01f;

        foreach (Resolution resolution in allResolutions)
        {
            float aspectRatio =
                (float)resolution.width / resolution.height;

            if (Mathf.Abs(aspectRatio - targetAspectRatio) > aspectTolerance)
                continue;

            filteredResolutions.Add(resolution);
        }

        availableResolutions = filteredResolutions.ToArray();

        screenResolutionDropdown.ClearOptions();

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution resolution = availableResolutions[i];

            options.Add(
                resolution.width + " x " + resolution.height
            );

            if (resolution.width == 1280 &&
                resolution.height == 720)
            {
                defaultIndex = i;
            }
        }

        screenResolutionDropdown.AddOptions(options);

        if (availableResolutions.Length > 0)
        {
            screenResolutionDropdown.SetValueWithoutNotify(
                defaultIndex
            );
        }
    }

    private void PopulateDropdowns()
    {
        screenStyleDropdown.ClearOptions();
        screenStyleDropdown.AddOptions(new List<string>
        {
            "Windowed",
            "Fullscreen"
        });

        postProcessingDropdown.ClearOptions();
        postProcessingDropdown.AddOptions(new List<string>
        {
            "Enable",
            "Disable"
        });

        maxFramerateDropdown.ClearOptions();
        maxFramerateDropdown.AddOptions(new List<string>
        {
            "30",
            "45",
            "60",
            "75",
            "120",
            "200",
            "Unlimited",
            "VSync"
        });

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>
        {
            "Very Low",
            "Low",
            "Medium",
            "High",
            "Very High",
            "Ultra"
        });

        frameRateDropdown.ClearOptions();
        frameRateDropdown.AddOptions(new List<string>
        {
            "Visible",
            "Hidden"
        });

        oobInsultsDropdown.ClearOptions();
        oobInsultsDropdown.AddOptions(new List<string>
        {
            "Enabled",
            "Disabled"
        });

        verticalSyncDropdown.ClearOptions();
        verticalSyncDropdown.AddOptions(new List<string>
        {
            "Enabled",
            "Disabled"
        });

        freeLookDropdown.ClearOptions();
        freeLookDropdown.AddOptions(new List<string>
        {
            "Enabled",
            "Disabled"
        });

        invertAxisDropdown.ClearOptions();
        invertAxisDropdown.AddOptions(new List<string>
        {
            "Enabled",
            "Disabled"
        });
    }

    private void LoadSettings()
    {
        int savedWidth = PlayerPrefs.GetInt("Graphics_ScreenWidth", 1280);
        int savedHeight = PlayerPrefs.GetInt("Graphics_ScreenHeight", 720);

        selectedResolutionIndex = FindResolutionIndex(savedWidth, savedHeight);

        if (selectedResolutionIndex < 0)
            selectedResolutionIndex = FindResolutionIndex(1280, 720);

        if (selectedResolutionIndex < 0 && availableResolutions.Length > 0)
            selectedResolutionIndex = 0;

        selectedFullscreen = PlayerPrefs.GetInt("Graphics_Fullscreen", 1) == 1;

        selectedPostProcessing =
            PlayerPrefs.GetInt("Graphics_PostProcessing", 1) == 1;

        selectedMaxFramerate =
            PlayerPrefs.GetInt("Graphics_MaxFramerate", 7);

        selectedQuality =
            PlayerPrefs.GetInt("Graphics_Quality", 5);

        selectedFrameRate =
            PlayerPrefs.GetInt("Graphics_FrameRate", 1) == 1;

        selectedOobInsults =
            PlayerPrefs.GetInt("Graphics_OobInsults", 1) == 1;

        selectedVSync =
            PlayerPrefs.GetInt("Graphics_VerticalSync", 1) == 1;

        selectedFOV =
            PlayerPrefs.GetFloat("Graphics_FieldOfView", 70f);

        selectedMaxRadarItems =
            PlayerPrefs.GetInt("Graphics_MaxRadarItems", 25);

        selectedInvertAxis =
            PlayerPrefs.GetInt("Controls_Mouse_InvertYAxis", 0) == 1;

        mouseSensitivity =
            PlayerPrefs.GetFloat("Controls_MouseSensitivity", 1f);

        keyboardSensitivity =
            PlayerPrefs.GetFloat("Controls_KeyboardSensitivity", 1f);
    }

    private void LoadInputSettings()
    {
        if (ControlBinding.instance == null)
            return;

        ControlBinding.instance.LoadBindings();

        mouseSensitivity = ControlBinding.instance.mouseSensitivity;
        keyboardSensitivity = ControlBinding.instance.keyboardSensitivity;

        SetBindingText(moveForwardButton, ControlBinding.instance.moveForward);
        SetBindingText(moveBackwardButton, ControlBinding.instance.moveBackward);
        SetBindingText(moveLeftButton, ControlBinding.instance.moveLeft);
        SetBindingText(moveRightButton, ControlBinding.instance.moveRight);

        SetBindingText(lookUpButton, ControlBinding.instance.rotateCameraUp);
        SetBindingText(lookDownButton, ControlBinding.instance.rotateCameraDown);
        SetBindingText(lookLeftButton, ControlBinding.instance.rotateCameraLeft);
        SetBindingText(lookRightButton, ControlBinding.instance.rotateCameraRight);

        SetBindingText(jumpButton, ControlBinding.instance.jump);
        SetBindingText(freeLookButton, ControlBinding.instance.freelookKey);
        SetBindingText(usePowerupButton, ControlBinding.instance.usePowerup);

        SetBindingText(blastButton, ControlBinding.instance.blast);
        SetBindingText(respawnButton, ControlBinding.instance.respawn);
        SetBindingText(toggleRadarButton, ControlBinding.instance.toggleRadar);

        selectedInvertAxis = ControlBinding.instance.invertMouseYAxis;
    }

    private void LoadAudioSettings()
    {
        float music = PlayerPrefs.GetFloat("Audio_MusicVolume", 0.5f);
        float sound = PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);

        musicSlider.SetValueWithoutNotify(music);
        soundSlider.SetValueWithoutNotify(sound);

        ApplyMusicVolume(music);
        ApplySoundVolume(sound);
    }

    private void UpdateUI()
    {
        if (selectedResolutionIndex >= 0 &&
            selectedResolutionIndex < availableResolutions.Length)
        {
            screenResolutionDropdown.SetValueWithoutNotify(selectedResolutionIndex);
        }

        screenStyleDropdown.SetValueWithoutNotify(selectedFullscreen ? 1 : 0);
        postProcessingDropdown.SetValueWithoutNotify(selectedPostProcessing ? 0 : 1);
        maxFramerateDropdown.SetValueWithoutNotify(selectedMaxFramerate);
        qualityDropdown.SetValueWithoutNotify(selectedQuality);
        frameRateDropdown.SetValueWithoutNotify(selectedFrameRate ? 0 : 1);
        oobInsultsDropdown.SetValueWithoutNotify(selectedOobInsults ? 0 : 1);
        verticalSyncDropdown.SetValueWithoutNotify(selectedVSync ? 0 : 1);

        bool alwaysFreeLook = true;

        if (ControlBinding.instance != null)
            alwaysFreeLook = ControlBinding.instance.alwaysFreeLook;

        freeLookDropdown.SetValueWithoutNotify(alwaysFreeLook ? 0 : 1);
        invertAxisDropdown.SetValueWithoutNotify(selectedInvertAxis ? 0 : 1);

        fieldOfViewSlider.SetValueWithoutNotify(selectedFOV);
        maxRadarItemsSlider.SetValueWithoutNotify(selectedMaxRadarItems);
        mouseSpeedSlider.SetValueWithoutNotify(
            ControlBinding.SensitivityToSliderValue(mouseSensitivity));
        keyboardSpeedSlider.SetValueWithoutNotify(
            ControlBinding.SensitivityToSliderValue(keyboardSensitivity));

        UpdateSliderValueText(
            mouseSpeedSlider,
            Mathf.RoundToInt(mouseSpeedSlider.value).ToString());

        UpdateSliderValueText(
            keyboardSpeedSlider,
            Mathf.RoundToInt(keyboardSpeedSlider.value).ToString());

        UpdateSliderValueText(
            fieldOfViewSlider,
            Mathf.RoundToInt(selectedFOV).ToString());

        UpdateSliderValueText(
            maxRadarItemsSlider,
            selectedMaxRadarItems.ToString());

        UpdateSliderValueText(
            musicSlider,
            Mathf.RoundToInt(musicSlider.value * 100f) + "%");

        UpdateSliderValueText(
            soundSlider,
            Mathf.RoundToInt(soundSlider.value * 100f) + "%");
    }

    public void SetResolution(int index)
    {
        selectedResolutionIndex = index;
    }

    public void SetScreenStyle(int index)
    {
        selectedFullscreen = index == 1;

        PlayerPrefs.SetInt(
            "Graphics_Fullscreen",
            selectedFullscreen ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void SetPostProcessing(int index)
    {
        selectedPostProcessing = index == 0;

        PlayerPrefs.SetInt(
            "Graphics_PostProcessing",
            selectedPostProcessing ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void SetMaxFramerate(int index)
    {
        selectedMaxFramerate = index;

        PlayerPrefs.SetInt(
            "Graphics_MaxFramerate",
            selectedMaxFramerate);

        PlayerPrefs.Save();
    }

    public void SetQuality(int index)
    {
        selectedQuality = index;

        PlayerPrefs.SetInt(
            "Graphics_Quality",
            selectedQuality);

        PlayerPrefs.Save();
    }

    public void SetFrameRate(int index)
    {
        selectedFrameRate = index == 0;

        PlayerPrefs.SetInt(
            "Graphics_FrameRate",
            selectedFrameRate ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void SetOobInsults(int index)
    {
        selectedOobInsults = index == 0;

        PlayerPrefs.SetInt(
            "Graphics_OobInsults",
            selectedOobInsults ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void SetVerticalSync(int index)
    {
        selectedVSync = index == 0;

        PlayerPrefs.SetInt(
            "Graphics_VerticalSync",
            selectedVSync ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void SetFieldOfView(float value)
    {
        selectedFOV = value;

        UpdateSliderValueText(
            fieldOfViewSlider,
            Mathf.RoundToInt(value).ToString());

        PlayerPrefs.SetFloat(
            "Graphics_FieldOfView",
            selectedFOV);

        PlayerPrefs.Save();
    }

    public void SetMaxRadarItems(float value)
    {
        selectedMaxRadarItems = Mathf.RoundToInt(value);

        UpdateSliderValueText(
            maxRadarItemsSlider,
            selectedMaxRadarItems.ToString());

        PlayerPrefs.SetInt(
            "Graphics_MaxRadarItems",
            selectedMaxRadarItems);

        PlayerPrefs.Save();
    }

    public void SetInvertAxis(int index)
    {
        selectedInvertAxis = index == 0;

        PlayerPrefs.SetInt(
            "Controls_Mouse_InvertYAxis",
            selectedInvertAxis ? 1 : 0);

        if (ControlBinding.instance != null)
            ControlBinding.instance.invertMouseYAxis = selectedInvertAxis;

        PlayerPrefs.Save();
    }

    public void SetFreeLook(int index)
    {
        bool alwaysFreeLook = index == 0;

        PlayerPrefs.SetInt(
            "Controls_Mouse_Freelook",
            alwaysFreeLook ? 1 : 0);

        if (ControlBinding.instance != null)
            ControlBinding.instance.alwaysFreeLook = alwaysFreeLook;

        PlayerPrefs.Save();
    }

    public void SetMouseSpeed(float value)
    {
        mouseSensitivity =
            ControlBinding.SliderValueToSensitivity(value);

        UpdateSliderValueText(
            mouseSpeedSlider,
            Mathf.RoundToInt(value).ToString());

        if (ControlBinding.instance != null)
        {
            ControlBinding.instance.mouseSensitivity =
                mouseSensitivity;
        }

        PlayerPrefs.SetFloat(
            "Controls_MouseSensitivity",
            mouseSensitivity);

        PlayerPrefs.Save();
    }

    public void SetKeyboardSpeed(float value)
    {
        keyboardSensitivity =
            ControlBinding.SliderValueToSensitivity(value);

        UpdateSliderValueText(
            keyboardSpeedSlider,
            Mathf.RoundToInt(value).ToString());

        if (ControlBinding.instance != null)
        {
            ControlBinding.instance.keyboardSensitivity =
                keyboardSensitivity;
        }

        PlayerPrefs.SetFloat(
            "Controls_KeyboardSensitivity",
            keyboardSensitivity);

        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicSlider.SetValueWithoutNotify(volume);

        UpdateSliderValueText(
            musicSlider,
            Mathf.RoundToInt(volume * 100f) + "%");

        PlayerPrefs.SetFloat(
            "Audio_MusicVolume",
            volume);

        ApplyMusicVolume(volume);
        PlayerPrefs.Save();
    }

    public void SetSoundVolume(float volume)
    {
        soundSlider.SetValueWithoutNotify(volume);

        UpdateSliderValueText(
            soundSlider,
            Mathf.RoundToInt(volume * 100f) + "%");

        PlayerPrefs.SetFloat(
            "Audio_SoundVolume",
            volume);

        ApplySoundVolume(volume);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume(float volume)
    {
        if (JukeboxManager.instance == null)
            return;

        AudioSource source =
            JukeboxManager.instance.GetComponent<AudioSource>();

        if (source != null)
            source.volume = volume;
    }

    private void ApplySoundVolume(float volume)
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource source in sources)
        {
            if (source == null)
                continue;

            if (source.GetComponent<JukeboxManager>() != null)
                continue;

            source.volume = volume;
        }
    }

    public void ApplySettings()
    {
        ApplyResolutionAndScreenStyle();
        ApplyQuality();
        ApplyFramerate();
        ApplyVSync();

        PlayerPrefs.Save();
    }

    private void ApplyResolutionAndScreenStyle()
    {
        if (availableResolutions == null ||
            availableResolutions.Length == 0)
            return;

        if (selectedResolutionIndex < 0 ||
            selectedResolutionIndex >= availableResolutions.Length)
            return;

        Resolution resolution =
            availableResolutions[selectedResolutionIndex];

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            selectedFullscreen);

        PlayerPrefs.SetInt(
            "Graphics_ScreenWidth",
            resolution.width);

        PlayerPrefs.SetInt(
            "Graphics_ScreenHeight",
            resolution.height);

        PlayerPrefs.SetInt(
            "Graphics_Fullscreen",
            selectedFullscreen ? 1 : 0);
    }

    private void ApplyQuality()
    {
        QualitySettings.SetQualityLevel(
            selectedQuality,
            true);
    }

    private void ApplyFramerate()
    {
        switch (selectedMaxFramerate)
        {
            case 0:
                Application.targetFrameRate = 30;
                break;

            case 1:
                Application.targetFrameRate = 45;
                break;

            case 2:
                Application.targetFrameRate = 60;
                break;

            case 3:
                Application.targetFrameRate = 75;
                break;

            case 4:
                Application.targetFrameRate = 120;
                break;

            case 5:
                Application.targetFrameRate = 200;
                break;

            case 6:
                Application.targetFrameRate = -1;
                break;

            case 7:
                Application.targetFrameRate = -1;
                break;
        }
    }

    private void ApplyVSync()
    {
        if (selectedMaxFramerate == 7)
        {
            QualitySettings.vSyncCount = 1;
            return;
        }

        QualitySettings.vSyncCount =
            selectedVSync ? 1 : 0;
    }

    public void RemapButton(
        string controlName,
        Button button)
    {
        bindToBeRemapped = controlName;
        buttonToBeRemapped = button;

        conflictedMapping = string.Empty;
        conflictedButton = null;

        remapCaption.text =
            "Press a new key or button for \"" +
            controlName +
            "\"";

        remapMenu.transform.SetAsLastSibling();
        remapMenu.SetActive(true);
    }

    private void Update()
    {
        if (!remapMenu.activeInHierarchy)
            return;

        if (confirmMenu.activeInHierarchy)
            return;

        if (!Input.anyKeyDown)
            return;

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(key))
                continue;

            if (key == KeyCode.Escape ||
                key == KeyCode.Return)
            {
                remapMenu.SetActive(false);
                ClearRemapping();
                return;
            }

            if (ValidateInput(key))
            {
                AssignMapping(key);
            }
            else
            {
                ShowMappingConflict(key);
            }

            break;
        }
    }

    private void AssignMapping(KeyCode key)
    {
        if (ControlBinding.instance == null)
            return;

        ControlBinding.instance.AssignKey(
            bindToBeRemapped,
            key);

        SetBindingText(
            buttonToBeRemapped,
            key);

        ClearRemapping();
        remapMenu.SetActive(false);
    }

    private void ShowMappingConflict(KeyCode key)
    {
        if (bindToBeRemapped == conflictedMapping)
        {
            ClearRemapping();
            remapMenu.SetActive(false);
            return;
        }

        tempKeycode = key;

        remapMenu.SetActive(false);

        confirmCaption.text =
            "\"" +
            Utils.KeyCodeToString(key) +
            "\" is already bound to \"" +
            conflictedMapping +
            "\"!\nDo you want to undo this mapping?";

        confirmMenu.transform.SetAsLastSibling();
        confirmMenu.SetActive(true);
    }

    private void CancelMapping()
    {
        ClearRemapping();
        confirmMenu.SetActive(false);
    }

    private void ForceMapping()
    {
        if (ControlBinding.instance == null)
            return;

        ControlBinding.instance.AssignKey(
            bindToBeRemapped,
            tempKeycode);

        ControlBinding.instance.AssignKey(
            conflictedMapping,
            KeyCode.None);

        SetBindingText(
            buttonToBeRemapped,
            tempKeycode);

        if (conflictedButton != null)
        {
            SetBindingText(
                conflictedButton,
                KeyCode.None);
        }

        ClearRemapping();
        confirmMenu.SetActive(false);
    }

    private bool ValidateInput(KeyCode key)
    {
        conflictedButton = null;
        conflictedMapping = string.Empty;

        if (ControlBinding.instance == null)
            return true;

        if (key == ControlBinding.instance.moveForward)
        {
            SetConflict("Move Forward", moveForwardButton);
            return false;
        }

        if (key == ControlBinding.instance.moveBackward)
        {
            SetConflict("Move Backward", moveBackwardButton);
            return false;
        }

        if (key == ControlBinding.instance.moveLeft)
        {
            SetConflict("Move Left", moveLeftButton);
            return false;
        }

        if (key == ControlBinding.instance.moveRight)
        {
            SetConflict("Move Right", moveRightButton);
            return false;
        }

        if (key == ControlBinding.instance.rotateCameraUp)
        {
            SetConflict("Rotate Camera Up", lookUpButton);
            return false;
        }

        if (key == ControlBinding.instance.rotateCameraDown)
        {
            SetConflict("Rotate Camera Down", lookDownButton);
            return false;
        }

        if (key == ControlBinding.instance.rotateCameraLeft)
        {
            SetConflict("Rotate Camera Left", lookLeftButton);
            return false;
        }

        if (key == ControlBinding.instance.rotateCameraRight)
        {
            SetConflict("Rotate Camera Right", lookRightButton);
            return false;
        }

        if (key == ControlBinding.instance.jump)
        {
            SetConflict("Jump", jumpButton);
            return false;
        }

        if (key == ControlBinding.instance.freelookKey)
        {
            SetConflict("Free-Look Key", freeLookButton);
            return false;
        }

        if (key == ControlBinding.instance.usePowerup)
        {
            SetConflict("Use Powerup", usePowerupButton);
            return false;
        }

        if (key == ControlBinding.instance.blast)
        {
            SetConflict("Blast", blastButton);
            return false;
        }

        if (key == ControlBinding.instance.respawn)
        {
            SetConflict("Respawn", respawnButton);
            return false;
        }

        if (key == ControlBinding.instance.toggleRadar)
        {
            SetConflict("Toggle Radar", toggleRadarButton);
            return false;
        }

        return true;
    }

    private void SetConflict(
        string mapping,
        Button button)
    {
        conflictedMapping = mapping;
        conflictedButton = button;
    }

    private void ClearRemapping()
    {
        bindToBeRemapped = string.Empty;
        buttonToBeRemapped = null;
        conflictedMapping = string.Empty;
        conflictedButton = null;
        tempKeycode = KeyCode.None;
    }

    private void SetBindingText(
        Button button,
        KeyCode key)
    {
        if (button == null)
            return;

        Transform value = button.transform.Find("Value");

        if (value != null)
        {
            TextMeshProUGUI text =
                value.GetComponent<TextMeshProUGUI>();

            if (text != null)
            {
                text.text = Utils.KeyCodeToString(key);
                return;
            }
        }

        if (button.transform.childCount > 0)
        {
            TextMeshProUGUI text =
                button.transform.GetChild(0)
                    .GetComponent<TextMeshProUGUI>();

            if (text != null)
                text.text = Utils.KeyCodeToString(key);
        }
    }

    private void UpdateSliderValueText(
        Slider slider,
        string value)
    {
        if (slider == null)
            return;

        Transform valueTransform =
            slider.transform.Find("Value");

        if (valueTransform == null)
            return;

        TextMeshProUGUI text =
            valueTransform.GetComponent<TextMeshProUGUI>();

        if (text != null)
            text.text = value;
    }

    private int FindResolutionIndex(
        int width,
        int height)
    {
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == width &&
                availableResolutions[i].height == height)
            {
                return i;
            }
        }

        return -1;
    }

    private void Home()
    {
        ApplySettings();
        SceneManager.LoadScene(MainMenuScene);
    }
}
