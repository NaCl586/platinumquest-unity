using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Server;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("Core References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject recordingIcon;
    [SerializeField] private TextMeshProUGUI centerText;
    [SerializeField] private TextMeshProUGUI bottomText;
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private TextMeshProUGUI lbStatusText;

    [Header("Digits & Numbers")]
    [SerializeField] private Sprite[] numbers;
    [SerializeField] private Sprite[] numbersGreen;
    [SerializeField] private Sprite[] numbersRed;
    [SerializeField] private Image[] timerNumbers;
    [SerializeField] private Image[] targetGem;
    [SerializeField] private Image[] currentGem;
    [SerializeField] private GameObject gemCountUI;
    [SerializeField] private GameObject quotaGemCountUI;
    [SerializeField] private Image[] quotaGem;
    [SerializeField] private GameObject madnessHuntGemCountUI;
    [SerializeField] private Image[] madnessHuntGem;

    [Header("Speedometer")]
    public GameObject speedometerMenu;
    public RectTransform speedometer;
    public Image[] speedometerSpeed;
    public RectTransform indicatorConsistencyRectTransform;
    public RectTransform indicatorHasteRectTransform;
    public Image indicatorIconHaste;
    public Image indicatorIconConsistency;
    public Sprite cons_normal, cons_tooslow, haste_achieved, haste_notachieved;

    [Header("Countdown and Laps")]
    [SerializeField] private GameObject subTimer;
    [SerializeField] private GameObject countdownMenu;
    [SerializeField] private GameObject lapsMenu;
    [SerializeField] private Image[] countdownTimer;
    [SerializeField] private Sprite[] countdownIcons;
    [SerializeField] private Image countdownIcon;
    [SerializeField] private Image[] lapsNumber;

    [Header("Powerups & HUD")]
    [SerializeField] private Texture[] powerupIcon;
    [SerializeField] private RawImage powerupHUD;
    [SerializeField] private GameObject powerupLocked;
    [SerializeField] private Camera[] HUDCameras;

    [Header("Cannon")]
    public GameObject cannonMenu;
    public Sprite[] charge1, charge2, charge3, charge4;
    public Image charge1_img, charge2_img, charge3_img, charge4_img;

    [Header("Layout Shifts (Online/Offline)")]
    [SerializeField] private RectTransform bottomText_offline;
    [SerializeField] private RectTransform bottomText_online;
    [SerializeField] private RectTransform exitText_offline;
    [SerializeField] private RectTransform exitText_online;
    [SerializeField] private RectTransform exitText;
    [SerializeField] private GameObject fpsBox;
    [SerializeField] private RectTransform fpsBox_offline;
    [SerializeField] private RectTransform fpsBox_online;

    [Header("Timers")]
    public GameObject bubbleTimerGameObject;
    public TextMeshProUGUI bubbleTimerText;
    public Image bubbleTimerBar;

    public GameObject bubbleInfiniteGameObject;

    public GameObject fireballunlitTimerGameObject;
    public TextMeshProUGUI fireballunlitTimerText;
    public Image fireballunlitTimerBar;

    public GameObject fireballlitTimerGameObject;
    public TextMeshProUGUI fireballlitTimerText;
    public Image fireballlitTimerBar;

    public GameObject shockAbsorberTimerGameObject;
    public TextMeshProUGUI shockAbsorberTimerText;
    public Image shockAbsorberTimerBar;

    public GameObject superBounceTimerGameObject;
    public TextMeshProUGUI superBounceTimerText;
    public Image superBounceTimerBar;

    public GameObject gyrocopterTimerGameObject;
    public TextMeshProUGUI gyrocopterTimerText;
    public Image gyrocopterTimerBar;

    public GameObject teleporterTimerGameObject;
    public TextMeshProUGUI teleporterTimerText;
    public Image teleporterTimerBar;

    public GameObject teleportTriggerTimerGameObject;
    public TextMeshProUGUI teleportTriggerTimerText;
    public Image teleportTriggerTimerBar;

    public GameObject transporterTimerGameObject;
    public TextMeshProUGUI transporterTimerText;
    public Image transporterTimerBar;

    [Header("Time Travel")]
    [SerializeField] private GameObject timeTravelTimer;
    [SerializeField] private Image[] timeTravelNumbers;
    [SerializeField] private GameObject[] timeTravelSecTenth;
    [SerializeField] private GameObject[] timeTravelSecHundreth;

    [Header("Popups")]
    [SerializeField] private RectTransform pickupPopupContainer;
    [SerializeField] private TextMeshProUGUI pickupPopupPrefab;

    [Header("Center Overlay Images")]
    [SerializeField] private GameObject readyImage;
    [SerializeField] private GameObject setImage;
    [SerializeField] private GameObject goImage;
    [SerializeField] private GameObject outOfBoundsImage;

    [Header("OOB Insult Menu")]
    public GameObject oobInsultMenu;
    [SerializeField] private TextMeshProUGUI oobInsultTitleText;
    [SerializeField] private TextMeshProUGUI oobInsultCaptionText;
    [SerializeField] private Button oobInsultCloseButton;

    [Header("Vice Save Menu")]
    public GameObject viceSaveWindow;

    [Header("Save Replay Menu")]
    public GameObject saveReplayMenu;
    [SerializeField] private TMP_InputField replayMenuName;
    [SerializeField] private TMP_InputField replayMenuAuthor;
    [SerializeField] private TMP_InputField replayMenuDescription;
    [SerializeField] private Button replayMenuApply;
    [SerializeField] private Button replayMenuCancel;
    [SerializeField] private Scrollbar scrollbar;
    public ScrollRect scrollRect;
    [SerializeField] private Button scrollUpButton;
    [SerializeField] private Button scrollDownButton;
    [SerializeField] private float step = 0.1f;

    [Header("Global Chat")]
    [SerializeField] private GameObject globalChat;
    [SerializeField] private TextMeshProUGUI globalChatText;
    [SerializeField] private TMP_InputField globalChatInput;

    private const int MaxChatLines = 8;
    private static readonly Color TimeTravelMessageColor = new Color32(153, 255, 153, 255);
    private static readonly Color TimeTravelZeroMessageColor = new Color32(204, 204, 204, 255);
    private static readonly Color TimePenaltyMessageColor = new Color32(255, 153, 153, 255);

    private readonly List<string> chatLines = new List<string>();
    private bool chatInputOpen;
    private Tween centerTextFade;
    private Tween bottomTextFade;
    private Sprite[] timerColor;
    private Sprite[] timeTravelTimerColor;
    private Sprite[] speedometerColor;
    private float timer;

    [HideInInspector] public bool isInitialized;

    public bool IsChatInputOpen => chatInputOpen && !ReplayRecorder.loadReplay && Time.timeScale > 0;

    private void Awake()
    {
        instance = this;
        UpdateHUDMaterial();
    }

    public void Init()
    {
        timerColor = new Sprite[numbers.Length];
        isInitialized = true;

        oobInsultCloseButton.onClick.AddListener(() =>
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1;
            oobInsultMenu.SetActive(false);
        });

        ReplayRecorder.actualReplayName = string.Empty;
        ReplayRecorder.replayAuthor = string.Empty;
        ReplayRecorder.replayDesc = string.Empty;

        replayMenuName.text = ReplayRecorder.actualReplayName;
        replayMenuAuthor.text = ReplayRecorder.replayAuthor;
        replayMenuDescription.text = ReplayRecorder.replayDesc;

        replayMenuName.onValueChanged.AddListener(SetName);
        replayMenuAuthor.onValueChanged.AddListener(SetAuthor);
        replayMenuDescription.onValueChanged.AddListener(SetDesc);

        scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        OnScrollbarValueChanged(scrollbar.value);
        scrollUpButton.onClick.AddListener(ScrollUp);
        scrollDownButton.onClick.AddListener(ScrollDown);

        recordingIcon.SetActive(ReplayRecorder.recordReplay);

        if (OnlineManager.Instance?.Chat != null)
        {
            OnlineManager.Instance.Chat.MessageReceived += OnChatMessageReceived;
            OnlineManager.Instance.Chat.SystemMessageReceived += OnSystemMessageReceived;
            OnlineManager.Instance.Chat.WorldRecordReceived += OnWorldRecordReceived;
            OnlineManager.Instance.Chat.RecentMessagesReceived += OnRecentMessagesReceived;
            LoadChatHistory();
        }

        bool isOnlineMode = OnlineManager.Instance?.Chat != null &&
                            !ReplayRecorder.loadReplay &&
                            !LeaderboardsMenu.ReplayCenterLoadedFromLeaderboards;

        globalChat.SetActive(isOnlineMode);
        ApplyLayoutMode(isOnlineMode);
    }

    private void ApplyLayoutMode(bool online)
    {
        SetRectTransformProperties(fpsBox.GetComponent<RectTransform>(), online ? fpsBox_online : fpsBox_offline);
        SetRectTransformProperties(bottomText.GetComponent<RectTransform>(), online ? bottomText_online : bottomText_offline);
        SetRectTransformProperties(exitText, online ? exitText_online : exitText_offline);

        fpsBox.SetActive(PlayerPrefs.GetInt("Graphics_FrameRate", 1) == 1);
    }

    private static void SetRectTransformProperties(RectTransform target, RectTransform source)
    {
        if (target == null || source == null) return;
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    public void SetLBStatus(string text)
    {
        Debug.Log(text);
        lbStatusText.text = text;
    }

    public void SaveAndReturn() => HandleReplayAction(() =>
    {
        if (OnlineManager.Instance == null || !OnlineManager.Instance.Auth.IsLoggedIn)
        {
            JukeboxManager.instance.PlayMusic("Pianoforte", true);
            SceneManager.LoadScene("PlayMission");
        }
        else
        {
            JukeboxManager.instance.PlayMusic("Flanked");
            SceneManager.LoadScene("LBPlayMission");
        }
    });

    public void SaveAndRetry() => HandleReplayAction(() => GameManager.instance?.ReplayLevel());

    private void HandleReplayAction(Action onCompleted)
    {
        replayMenuApply.onClick.RemoveAllListeners();
        replayMenuApply.onClick.AddListener(() =>
        {
            ReplayRecorder.Instance.SaveReplay();
            Debug.Log("Replay Saved");
            onCompleted?.Invoke();
        });

        replayMenuCancel.onClick.RemoveAllListeners();
        replayMenuCancel.onClick.AddListener(() =>
        {
            Debug.Log("Replay Not Saved");
            onCompleted?.Invoke();
        });
    }

    public void SetName(string s) => ReplayRecorder.actualReplayName = s;
    public void SetAuthor(string s) => ReplayRecorder.replayAuthor = s;
    public void SetDesc(string s)
    {
        Canvas.ForceUpdateCanvases();
        ReplayRecorder.replayDesc = s;
    }

    private void Update()
    {
        if (fpsText != null)
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= 0.5f)
            {
                fpsText.text = $"FPS: {RoundSmart(1f / Time.unscaledDeltaTime)}";
                timer = 0f;
            }
        }

        if (!ReplayRecorder.loadReplay && Time.timeScale > 0)
            HandleChatInput();
    }

    private void HandleChatInput()
    {
        if (saveReplayMenu.activeSelf)
        {
            if (chatInputOpen) CancelChatInput();
            return;
        }

        if (!chatInputOpen)
        {
            if (Input.GetKeyDown(KeyCode.T)) OpenChatInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return)) SendChatInput();
        else if (Input.GetKeyDown(KeyCode.Escape)) CancelChatInput();
    }

    private static float RoundSmart(float value)
    {
        int decimals = Mathf.Abs(value) >= 1000f ? 0 : 1;
        return (float)Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    public void SetOutOfBoundsMessage(int oobCount, string message)
    {
        if (PlayerPrefs.GetInt("Graphics_OobInsults", 1) == 0) 
            return;

        oobInsultMenu.SetActive(true);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        oobInsultTitleText.text = $"Out of Bounds {oobCount} times";
        oobInsultCaptionText.text = message;
    }

    public void UpdateHUDMaterial()
    {
        int targetLayer = LayerMask.NameToLayer("HUD");
        const float smoothness01 = 1f;

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            if (r.gameObject.layer != targetLayer) continue;

            foreach (var mat in r.materials)
            {
                if (mat == null) continue;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness01);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness01);
            }
        }
    }

    public void SetPowerupLocked(bool show) => powerupLocked.SetActive(show);

    public void ShowGemCountUI(bool show)
    {
        if (madnessHuntGemCountUI.activeSelf) return;

        gemCountUI.SetActive(show);
        if (HUDCameras.Length > 1 && HUDCameras[1] != null)
            HUDCameras[1].gameObject.SetActive(show);
    }

    public void ShowMadnessHuntGemCountUI(bool show)
    {
        madnessHuntGemCountUI.SetActive(show);
        if (HUDCameras.Length > 1 && HUDCameras[1] != null)
            HUDCameras[1].gameObject.SetActive(show);

        if (show) gemCountUI.SetActive(false);
    }

    public void SetCurrentMadnessHuntGem(int count)
    {
        madnessHuntGem[0].sprite = numbers[count / 100];
        madnessHuntGem[1].sprite = numbers[(count / 10) % 10];
        madnessHuntGem[2].sprite = numbers[count % 10];
    }

    public void ShowCannonMenu(bool show)
    {
        CannonCharge(0);
        cannonMenu.SetActive(show);
    }

    public void CannonCharge(int level)
    {
        level = Mathf.Clamp(level, 0, 10);

        if (level == 0)
        {
            charge1_img.gameObject.SetActive(false);
            charge2_img.gameObject.SetActive(false);
            charge3_img.gameObject.SetActive(false);
            charge4_img.gameObject.SetActive(false);
        }
        else if (level <= 5)
        {
            charge1_img.gameObject.SetActive(false);
            charge2_img.gameObject.SetActive(false);
            charge3_img.gameObject.SetActive(true);
            charge4_img.gameObject.SetActive(true);

            charge3_img.sprite = charge3[level - 1];
            charge4_img.sprite = charge4[level - 1];
        }
        else
        {
            charge1_img.gameObject.SetActive(true);
            charge2_img.gameObject.SetActive(true);
            charge3_img.gameObject.SetActive(true);
            charge4_img.gameObject.SetActive(true);

            charge3_img.sprite = charge3[4];
            charge4_img.sprite = charge4[4];
            charge1_img.sprite = charge1[level - 6];
            charge2_img.sprite = charge2[level - 6];
        }
    }

    public void SetTargetGem(int count) => SetGemDigits(targetGem, count);
    public void SetCurrentGem(int count) => SetGemDigits(currentGem, count);

    private void SetGemDigits(Image[] digitImages, int count)
    {
        if (digitImages == null || digitImages.Length < 3) return;
        digitImages[0].sprite = numbers[count / 100];
        digitImages[1].sprite = numbers[(count / 10) % 10];
        digitImages[2].sprite = numbers[count % 10];
    }

    public bool IsQuota() => quotaGemCountUI.activeSelf;

    public void SetQuotaGemDigit(int count)
    {
        quotaGemCountUI.SetActive(true);
        int checkHundreth = count / 100;

        if (checkHundreth != 0)
            quotaGem[0].sprite = numbers[checkHundreth];
        else
            quotaGem[0].gameObject.SetActive(false);

        quotaGem[1].sprite = numbers[(count / 10) % 10];
        quotaGem[2].sprite = numbers[count % 10];
    }

    public void SetPowerupIcon(PowerupType _powerUp)
    {
        for (int i = 2; i < HUDCameras.Length; i++)
        {
            if (HUDCameras[i] != null)
                HUDCameras[i].gameObject.SetActive(false);
        }

        switch (_powerUp)
        {
            case PowerupType.None:
                powerupHUD.texture = powerupIcon[0];
                break;
            case PowerupType.SuperJump:
                powerupHUD.texture = powerupIcon[1];
                if (HUDCameras.Length > 2) HUDCameras[2].gameObject.SetActive(true);
                break;
            case PowerupType.SuperSpeed:
                powerupHUD.texture = powerupIcon[2];
                if (HUDCameras.Length > 3) HUDCameras[3].gameObject.SetActive(true);
                break;
            case PowerupType.SuperBounce:
                powerupHUD.texture = powerupIcon[3];
                if (HUDCameras.Length > 4) HUDCameras[4].gameObject.SetActive(true);
                break;
            case PowerupType.ShockAbsorber:
                powerupHUD.texture = powerupIcon[4];
                if (HUDCameras.Length > 5) HUDCameras[5].gameObject.SetActive(true);
                break;
            case PowerupType.Gyrocopter:
                powerupHUD.texture = powerupIcon[5];
                if (HUDCameras.Length > 6) HUDCameras[6].gameObject.SetActive(true);
                break;
            case PowerupType.Anvil:
                powerupHUD.texture = powerupIcon[6];
                if (HUDCameras.Length > 7) HUDCameras[7].gameObject.SetActive(true);
                break;
            case PowerupType.Teleporter:
                powerupHUD.texture = powerupIcon[7];
                if (HUDCameras.Length > 8) HUDCameras[8].gameObject.SetActive(true);
                break;
            case PowerupType.Transporter:
                powerupHUD.texture = powerupIcon[8];
                if (HUDCameras.Length > 8) HUDCameras[8].gameObject.SetActive(true);
                break;
            default:
                powerupHUD.texture = powerupIcon[0];
                break;
        }
    }

    public void SetCenterText(string text, float time = 3f)
    {
        centerTextFade?.Kill();
        centerText.color = Color.white;
        centerText.text = Utils.Resolve(Regex.Unescape(text));
        centerTextFade = centerText.DOColor(Color.white, time)
            .OnComplete(() => centerText.DOColor(Color.clear, 0.25f));
    }

    public void SetBottomText(string text, float time = 3f)
    {
        if (string.IsNullOrEmpty(text)) 
            return;

        bottomTextFade?.Kill();
        bottomText.color = Color.yellow;
        bottomText.text = Utils.Resolve(text).Replace("\\", "");
        bottomTextFade = bottomText.DOColor(Color.yellow, time)
            .OnComplete(() => bottomText.DOColor(Color.clear, 0.25f));
    }

    public void ShowSpeedometer(bool show) => speedometerMenu.SetActive(show);

    public void SetSpeedometer(float speed)
    {
        speed = Mathf.Max(0f, speed);

        Vector2 pos = speedometer.anchoredPosition;
        pos.y = GetSpeedometerY(speed);
        speedometer.anchoredPosition = pos;

        if (speedometerColor == null) speedometerColor = numbers;

        int displaySpeed = Mathf.FloorToInt(speed);

        if (displaySpeed >= 100)
        {
            speedometerSpeed[0].gameObject.SetActive(true);
            speedometerSpeed[0].sprite = speedometerColor[Mathf.Clamp(displaySpeed / 100, 0, numbers.Length - 1)];
        }
        else
        {
            speedometerSpeed[0].gameObject.SetActive(false);
        }

        speedometerSpeed[1].sprite = speedometerColor[Mathf.Clamp((displaySpeed / 10) % 10, 0, numbers.Length - 1)];
        speedometerSpeed[2].sprite = speedometerColor[Mathf.Clamp(displaySpeed % 10, 0, numbers.Length - 1)];
    }

    public void SetThresholdIconConsistency(float targetSpeed)
    {
        Vector2 pos = indicatorConsistencyRectTransform.anchoredPosition;
        pos.y = GetThresholdIconY(targetSpeed);
        indicatorConsistencyRectTransform.anchoredPosition = pos;
    }

    public void SetThresholdIconHaste(float targetSpeed)
    {
        Vector2 pos = indicatorHasteRectTransform.anchoredPosition;
        pos.y = GetThresholdIconY(targetSpeed);
        indicatorHasteRectTransform.anchoredPosition = pos;
    }

    public void InitVisibilityTresholdConsistencyIcon(bool consistency) => indicatorIconConsistency.gameObject.SetActive(consistency);
    public void InitVisibilityTresholdHasteIcon(bool haste) => indicatorIconHaste.gameObject.SetActive(haste);

    public void SetTreasholdConsistencyIcon(bool achieved)
    {
        indicatorIconConsistency.sprite = achieved ? cons_normal : cons_tooslow;
        speedometerColor = achieved ? numbers : numbersRed;

        if (Movement.instance != null)
            SetSpeedometer(Movement.instance.marbleVelocity.magnitude);
    }

    public void SetTreasholdHasteIcon(bool achieved)
    {
        indicatorIconHaste.sprite = achieved ? haste_achieved : haste_notachieved;
        speedometerColor = achieved ? numbersGreen : numbers;

        if (Movement.instance != null)
            SetSpeedometer(Movement.instance.marbleVelocity.magnitude);
    }

    private float GetThresholdIconY(float speed) => 14.30241f + speed * 8.1209379f;
    private float GetSpeedometerY(float speed) => -speed * 8.105176f;

    public void TeleportFadeOutBottomText()
    {
        if (bottomText.text == "Teleporter has been activated, please wait.")
        {
            bottomTextFade?.Kill();
            bottomTextFade = bottomText.DOColor(Color.clear, 0.25f);
        }
    }

    public void SetTimerColor(bool isRed)
    {
        timerColor = isRed ? numbersRed : numbers;
        if (GameManager.instance.timeTravelActive)
            timerColor = numbersGreen;
    }

    public void SetTimerGreen() => timerColor = numbersGreen;

    public void SetTimerText(float timeMs, bool isGreen = false)
    {
        timerColor = isGreen ? numbersGreen : timerColor;

        int ms = (int)timeMs;
        int decaminutes = ms / (10 * 60 * 1000);
        int remainder = ms % (10 * 60 * 1000);

        int minutes = remainder / (60 * 1000);
        remainder %= 60 * 1000;

        int decaseconds = remainder / (10 * 1000);
        remainder %= 10 * 1000;

        int seconds = remainder / 1000;
        remainder %= 1000;

        int deciseconds = remainder / 100;
        remainder %= 100;

        int centiseconds = remainder / 10;
        int milliseconds = remainder % 10;

        MadnessMode madnessMode = GameManager.instance.GetGameMode<MadnessMode>();
        HuntMode huntMode = GameManager.instance.GetGameMode<HuntMode>();

        bool madnessAlarm = madnessMode != null && madnessMode.AlarmActive;
        bool huntAlarm = huntMode != null && huntMode.AlarmActive;

        if (!GameManager.alarmIsPlaying && !madnessAlarm && !huntAlarm)
        {
            timerColor = numbers;

            if (!GameManager.gameStart || GameManager.gameFinish ||
                GameManager.instance.timeTravelActive || GameManager.instance.timeStopTriggerCount > 0)
            {
                timerColor = numbersGreen;
            }
            else if (GameManager.notQualified)
            {
                timerColor = numbersRed;
            }
        }

        timerNumbers[0].sprite = timerColor[decaminutes];
        timerNumbers[1].sprite = timerColor[minutes];
        timerNumbers[2].sprite = timerColor[decaseconds];
        timerNumbers[3].sprite = timerColor[seconds];
        timerNumbers[4].sprite = timerColor[deciseconds];
        timerNumbers[5].sprite = timerColor[centiseconds];
        timerNumbers[6].sprite = timerColor[milliseconds];
        timerNumbers[7].sprite = timerColor[10];
        timerNumbers[8].sprite = timerColor[11];
    }

    public void SetCountdownTimer(float timeMs, string icon)
    {
        if (timeMs < 0f)
        {
            countdownMenu.SetActive(false);
            if (!lapsMenu.activeSelf) subTimer.SetActive(false);
            return;
        }

        subTimer.SetActive(true);
        countdownMenu.SetActive(true);
        lapsMenu.SetActive(false);

        if (countdownIcons != null && countdownIcons.Length > 0)
        {
            int iconIndex = icon switch
            {
                "timerDiminishingReturns" => 0,
                "timerHawkingsDilemma" => 1,
                "timerHuntRespawn" => 2,
                "timerTimeTravel" => 3,
                _ => -1
            };

            if (iconIndex >= 0 && iconIndex < countdownIcons.Length)
                countdownIcon.sprite = countdownIcons[iconIndex];
        }

        if (countdownTimer == null || countdownTimer.Length < 9 || numbers == null || numbers.Length < 12)
            return;

        timeMs = Mathf.Max(0f, timeMs);
        int ms = Mathf.FloorToInt(timeMs);

        int decaminutes = Mathf.Clamp(ms / (10 * 60 * 1000), 0, 9);
        int remainder = ms % (10 * 60 * 1000);

        int minutes = remainder / (60 * 1000);
        remainder %= 60 * 1000;

        int decaseconds = remainder / (10 * 1000);
        remainder %= 10 * 1000;

        int seconds = remainder / 1000;
        remainder %= 1000;

        int deciseconds = remainder / 100;
        remainder %= 100;

        int centiseconds = remainder / 10;
        int milliseconds = remainder % 10;

        countdownTimer[0].sprite = numbers[decaminutes];
        countdownTimer[1].sprite = numbers[minutes];
        countdownTimer[2].sprite = numbers[decaseconds];
        countdownTimer[3].sprite = numbers[seconds];
        countdownTimer[4].sprite = numbers[deciseconds];
        countdownTimer[5].sprite = numbers[centiseconds];
        countdownTimer[6].sprite = numbers[milliseconds];
        countdownTimer[7].sprite = numbers[10];
        countdownTimer[8].sprite = numbers[11];
    }

    public void EnableLaps()
    {
        subTimer.SetActive(true);
        lapsMenu.SetActive(true);
    }

    public void SetLapsText(int current, int total)
    {
        if (current > total) current = total;

        lapsNumber[0].sprite = numbers[current / 10];
        lapsNumber[1].sprite = numbers[current % 10];
        lapsNumber[2].sprite = numbers[total / 10];
        lapsNumber[3].sprite = numbers[total % 10];
    }

    public void SetTimeTravelTimer(float timeMs, bool green = false)
    {
        timeTravelTimer.SetActive(timeMs >= 0);
        if (HUDCameras.Length > 0 && HUDCameras[0] != null)
            HUDCameras[0].gameObject.SetActive(timeMs >= 0);

        timeMs = Mathf.Max(0, timeMs);
        float displayMs = Mathf.Min(timeMs, 999999f);

        int totalSeconds = Mathf.FloorToInt(displayMs / 1000f);
        int seconds = totalSeconds % 10;
        int tens = (totalSeconds / 10) % 10;
        int hundreds = totalSeconds / 100;

        int deciseconds = Mathf.FloorToInt(displayMs / 100f) % 10;
        int centiseconds = Mathf.FloorToInt(displayMs / 10f) % 10;
        int milliseconds = Mathf.FloorToInt(displayMs) % 10;

        timeTravelTimerColor = green ? numbersGreen : numbers;

        if (totalSeconds < 10)
        {
            timeTravelNumbers[2].sprite = timeTravelTimerColor[seconds];
            timeTravelNumbers[3].sprite = timeTravelTimerColor[deciseconds];
            timeTravelNumbers[4].sprite = timeTravelTimerColor[centiseconds];
            timeTravelNumbers[5].sprite = timeTravelTimerColor[milliseconds];
        }
        else if (totalSeconds < 100)
        {
            timeTravelNumbers[1].sprite = timeTravelTimerColor[tens];
            timeTravelNumbers[2].sprite = timeTravelTimerColor[seconds];
            timeTravelNumbers[3].sprite = timeTravelTimerColor[deciseconds];
            timeTravelNumbers[4].sprite = timeTravelTimerColor[centiseconds];
            timeTravelNumbers[5].sprite = timeTravelTimerColor[milliseconds];
        }
        else
        {
            timeTravelNumbers[0].sprite = timeTravelTimerColor[hundreds];
            timeTravelNumbers[1].sprite = timeTravelTimerColor[tens];
            timeTravelNumbers[2].sprite = timeTravelTimerColor[seconds];
            timeTravelNumbers[3].sprite = timeTravelTimerColor[deciseconds];
            timeTravelNumbers[4].sprite = timeTravelTimerColor[centiseconds];
            timeTravelNumbers[5].sprite = timeTravelTimerColor[milliseconds];
        }

        foreach (var g in timeTravelSecTenth) g.SetActive(timeMs >= 10000);
        foreach (var g in timeTravelSecHundreth) g.SetActive(timeMs >= 100000);
    }

    public void DisplayGemMessage(string amount, Color color)
    {
        if (pickupPopupPrefab == null || pickupPopupContainer == null) return;

        TextMeshProUGUI popup = Instantiate(pickupPopupPrefab, pickupPopupContainer);
        RectTransform rect = popup.rectTransform;

        rect.anchoredPosition = Vector2.zero;
        popup.text = amount;
        popup.color = color;

        Shadow shadow = popup.GetComponent<Shadow>() ?? popup.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(1f, -1f);

        Color startColor = popup.color;
        startColor.a = 1f;
        popup.color = startColor;

        rect.DOAnchorPosY(rect.anchoredPosition.y + 60f, 0.6f).SetEase(Ease.Linear).SetUpdate(true);
        popup.DOFade(0f, 0.3f).SetDelay(0.3f).SetEase(Ease.Linear).SetUpdate(true);

        Destroy(popup.gameObject, 0.7f);
    }

    public void DisplayTimeTravelMessage(float bonusSeconds)
    {
        Color color = Mathf.Approximately(bonusSeconds, 0f) ? TimeTravelZeroMessageColor : TimeTravelMessageColor;

        if(GameManager.instance.GetGameMode<HuntMode>() != null || GameManager.instance.GetGameMode<MadnessMode>() != null)
            DisplayGemMessage($"+ {Mathf.Abs(bonusSeconds):0.###} s", color);
        else
            DisplayGemMessage($"- {Mathf.Abs(bonusSeconds):0.###} s", color);
    }

    public void DisplayTimePenaltyMessage(float penaltySeconds)
    {
        Color color = Mathf.Approximately(penaltySeconds, 0f) ? TimeTravelZeroMessageColor : TimePenaltyMessageColor;
        if (GameManager.instance.GetGameMode<HuntMode>() != null || GameManager.instance.GetGameMode<MadnessMode>() != null)
            DisplayGemMessage($"- {Mathf.Abs(penaltySeconds):0.###} s", color);
        else
            DisplayGemMessage($"+ {Mathf.Abs(penaltySeconds):0.###} s", color);
    }

    public void SetCenterImage(int index)
    {
        readyImage.SetActive(index == 0);
        setImage.SetActive(index == 1);
        goImage.SetActive(index == 2);
        outOfBoundsImage.SetActive(index == 3);
    }

    public void ScrollUp() => scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + step);
    public void ScrollDown() => scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - step);

    private void OnScrollbarValueChanged(float value)
    {
        scrollUpButton.interactable = value < 1f;
        scrollDownButton.interactable = value > 0f;
    }

    private void OnChatMessageReceived(string username, string message, string status) => AddNormalChatMessage(username, message, status);
    private void OnSystemMessageReceived(string message) => AddSystemChatMessage(message);
    private void OnWorldRecordReceived(string message) => AddWorldRecordChatMessage(message);

    private void OnRecentMessagesReceived(IReadOnlyList<ChatMessage> messages)
    {
        chatLines.Clear();
        foreach (ChatMessage message in messages)
        {
            if (message.Type == "WorldRecord") AddWorldRecordChatMessage(message.Message);
            else if (message.IsSystem) AddSystemChatMessage(message.Message);
            else AddNormalChatMessage(message.Username, message.Message, message.Status);
        }
    }

    private void AddChatLine(string line)
    {
        chatLines.Add(line);
        if (chatLines.Count > MaxChatLines) chatLines.RemoveAt(0);
        globalChatText.text = string.Join("\n", chatLines);
    }

    private void AddNormalChatMessage(string username, string message, string status)
    {
        string displayName = string.IsNullOrEmpty(status) ? username : $"{username} ({status})";
        AddChatLine($"<color=#9D0000>{displayName}:</color> <color=#000000>{message}</color>");
    }

    private void AddSystemChatMessage(string message) => AddChatLine($"<color=#939612>{message}</color>");
    private void AddWorldRecordChatMessage(string message) => AddChatLine($"<color=#006400>{message}</color>");

    private void LoadChatHistory()
    {
        chatLines.Clear();
        var messages = OnlineManager.Instance.Chat.GetRecentMessages();
        foreach (ChatMessage message in messages)
        {
            if (message.Type == "WorldRecord") AddWorldRecordChatMessage(message.Message);
            else if (message.IsSystem) AddSystemChatMessage(message.Message);
            else AddNormalChatMessage(message.Username, message.Message, message.Status);
        }
    }

    private void OpenChatInput()
    {
        if (chatInputOpen || OnlineManager.Instance?.Chat == null || !OnlineManager.Instance.Chat.IsConnected)
            return;

        if (globalChatInput == null) return;

        chatInputOpen = true;
        globalChatInput.gameObject.SetActive(true);
        globalChatInput.text = string.Empty;
        globalChatInput.ActivateInputField();
        globalChatInput.Select();
    }

    private void SendChatInput()
    {
        if (!chatInputOpen || globalChatInput == null) return;

        string message = globalChatInput.text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            OnlineManager.Instance?.Chat?.SendChat(message).Forget();
        }

        CloseChatInput();
    }

    private void CancelChatInput()
    {
        if (chatInputOpen) CloseChatInput();
    }

    private void CloseChatInput()
    {
        chatInputOpen = false;
        if (globalChatInput == null) return;

        globalChatInput.text = string.Empty;
        globalChatInput.DeactivateInputField();
        globalChatInput.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (OnlineManager.Instance?.Chat == null) return;

        OnlineManager.Instance.Chat.MessageReceived -= OnChatMessageReceived;
        OnlineManager.Instance.Chat.SystemMessageReceived -= OnSystemMessageReceived;
        OnlineManager.Instance.Chat.RecentMessagesReceived -= OnRecentMessagesReceived;
        OnlineManager.Instance.Chat.WorldRecordReceived -= OnWorldRecordReceived;
    }

    public void SetBubbleTimer(float time, float fullTime)
    {
        UpdatePowerupBar(time, fullTime, bubbleTimerGameObject, bubbleTimerText, bubbleTimerBar);
        bubbleInfiniteGameObject.SetActive(time == Mathf.Infinity);
        if (time == Mathf.Infinity) bubbleTimerGameObject.SetActive(false);
    }

    public void SetFireballTimer(float time, float fullTime)
    {
        bool canBlast = Marble.instance != null && Marble.instance.canBlast;
        fireballlitTimerGameObject.SetActive(false);
        fireballunlitTimerGameObject.SetActive(false);

        if (canBlast)
            UpdatePowerupBar(time, fullTime, fireballlitTimerGameObject, fireballlitTimerText, fireballlitTimerBar);
        else
            UpdatePowerupBar(time, fullTime, fireballunlitTimerGameObject, fireballunlitTimerText, fireballunlitTimerBar);
    }

    public void SetShockAbsorberTimer(float time, float fullTime)
    {
        UpdatePowerupBar(
            time,
            fullTime,
            shockAbsorberTimerGameObject,
            shockAbsorberTimerText,
            shockAbsorberTimerBar
        );
    }

    public void SetSuperBounceTimer(float time, float fullTime)
    {
        UpdatePowerupBar(
            time,
            fullTime,
            superBounceTimerGameObject,
            superBounceTimerText,
            superBounceTimerBar
        );
    }

    public void SetGyrocopterTimer(float time, float fullTime)
    {
        UpdatePowerupBar(
            time,
            fullTime,
            gyrocopterTimerGameObject,
            gyrocopterTimerText,
            gyrocopterTimerBar
        );
    }

    public void SetTeleporterTimer(float time, float fullTime)
    {
        UpdatePowerupBar(
            time,
            fullTime,
            teleporterTimerGameObject,
            teleporterTimerText,
            teleporterTimerBar
        );
    }

    public void SetTeleportTriggerTimer(float time, float fullTime)
    {
        UpdatePowerupBar(
            time,
            fullTime,
            teleportTriggerTimerGameObject,
            teleportTriggerTimerText,
            teleportTriggerTimerBar
        );
    }

    public void SetTransporterTimer(float time, float fullTime)
    {
        UpdatePowerupBar(
            time,
            fullTime,
            transporterTimerGameObject,
            transporterTimerText,
            transporterTimerBar
        );
    }

    private static void UpdatePowerupBar(float time, float fullTime, GameObject obj, TextMeshProUGUI text, Image bar)
    {
        bool active = time > 0f && time != Mathf.Infinity;
        obj.SetActive(active);

        if (active)
        {
            text.text = time.ToString("0.0");
            bar.fillAmount = fullTime > 0f ? Mathf.Clamp01(time / fullTime) : 0f;
        }
    }
}