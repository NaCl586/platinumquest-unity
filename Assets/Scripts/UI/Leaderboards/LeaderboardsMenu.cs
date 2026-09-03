using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Server;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardsMenu : MonoBehaviour
{
    public static LeaderboardsMenu Instance;

    [Header("Buttons")]
    public Button logout;
    public Button play;
    public Button replay;
    public Button total;
    public Button general;

    [Header("Windows")]
    public GameObject playMissionWindow;
    public GameObject replayWindow;
    public GameObject raycastBlocker;
    public GameObject totalWindow;
    public GameObject generalWindow;

    [Header("Menu")]
    public GameObject gameWindow;
    public GameObject loadingMenu;
    public GameObject errorMenu;
    public GameObject blackout;

    [Header("Blackout")]
    [SerializeField]
    private float blackoutDuration = 0.5f;

    [Header("Error")]
    public TextMeshProUGUI errorTitle;
    public TextMeshProUGUI errorMessage;
    public Button yahooButton;
    public ErrorSound errorSound;

    [Header("Loading")]
    public TextMeshProUGUI loadingMessage;

    [Header("Chatbox")]
    public TextMeshProUGUI globalChatText;
    public TextMeshProUGUI onlinePlayerText;
    public TMP_InputField clienChatText;
    public ScrollRect globalChatScrollRect;
    public Scrollbar globalChatScrollBar;
    public Button globalChatScrollUp;
    public Button globalChatScrollDown;
    public ScrollRect playerScrollRect;
    public Scrollbar playerScrollBar;
    public Button playerScrollUp;
    public Button playerScrollDown;
    public RectTransform globalChatContent;

    [SerializeField]
    private float step = 0.1f;

    private bool isProcessing;
    private bool returnToLBPlayMission;
    public static bool ReplayCenterLoadedFromLeaderboards;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (clienChatText != null)
        {
            clienChatText.onEndEdit.RemoveListener(OnChatInputSubmitted);
        }

        if (OnlineManager.Instance != null)
        {
            OnlineManager.Instance.Chat.ConnectionLost -= OnChatConnectionLost;

            OnlineManager.Instance.Chat.ForceLoggedOut -= OnForceLoggedOut;

            OnlineManager.Instance.Chat.MessageReceived -= OnChatMessageReceived;

            OnlineManager.Instance.Chat.OnlinePlayersUpdated -= OnOnlinePlayersUpdated;

            OnlineManager.Instance.Chat.RecentMessagesReceived -= OnRecentMessagesReceived;

            OnlineManager.Instance.Chat.SystemMessageReceived -= OnSystemMessageReceived;

            OnlineManager.Instance.Chat.WorldRecordReceived -= OnWorldRecordReceived;
        }
    }

    private void OnChatConnectionLost()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        ShowError(
            "Connection Lost",
            "The connection to the online server was lost. "
                + "You will be returned to the main menu."
        );
    }

    private void OnForceLoggedOut()
    {
        if (isProcessing)
            return;

        isProcessing = true;

        ShowError("Session Ended", "Your account was logged in from another session.");
    }

    private void Update()
    {
        if (clienChatText == null)
            return;

        if (!clienChatText.isFocused)
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            string message = clienChatText.text;

            if (!string.IsNullOrWhiteSpace(message))
            {
                SendChatMessage(message);
            }
        }
    }

    private void Start()
    {
        JukeboxManager.instance.PlayMusic("Flanked");

        play.onClick.AddListener(OnPlayClicked);
        logout.onClick.AddListener(OnLogoutClicked);
        replay.onClick.AddListener(OnReplayClicked);
        total.onClick.AddListener(() =>
        {
            GetComponent<RatingViewManager>().ShowTotal();
            raycastBlocker.SetActive(true);
        });
        general.onClick.AddListener(() =>
        {
            GetComponent<RatingViewManager>().ShowGeneral();
            raycastBlocker.SetActive(true);
        });

        yahooButton.onClick.AddListener(OnCloseErrorClicked);

        clienChatText.onEndEdit.AddListener(OnChatInputSubmitted);

        InitializeUI();

        ReplayRecorder.loadReplay = false;

        if (OnlineManager.Instance != null)
        {
            OnlineManager.Instance.Chat.ConnectionLost += OnChatConnectionLost;

            OnlineManager.Instance.Chat.ForceLoggedOut += OnForceLoggedOut;

            OnlineManager.Instance.Chat.MessageReceived += OnChatMessageReceived;

            OnlineManager.Instance.Chat.OnlinePlayersUpdated += OnOnlinePlayersUpdated;

            OnlineManager.Instance.Chat.RecentMessagesReceived += OnRecentMessagesReceived;

            OnlineManager.Instance.Chat.SystemMessageReceived += OnSystemMessageReceived;

            OnlineManager.Instance.Chat.WorldRecordReceived += OnWorldRecordReceived;

            RefreshOnlinePlayers();
            RefreshChatHistory();

            globalChatScrollBar.onValueChanged.AddListener(OnGlobalScrollbarValueChanged);
            OnGlobalScrollbarValueChanged(globalChatScrollBar.value);

            globalChatScrollUp.onClick.AddListener(GlobalScrollUp);
            globalChatScrollDown.onClick.AddListener(GlobalScrollDown);

            playerScrollBar.onValueChanged.AddListener(OnPlayerScrollbarValueChanged);
            OnPlayerScrollbarValueChanged(playerScrollBar.value);

            playerScrollUp.onClick.AddListener(PlayerScrollUp);
            playerScrollDown.onClick.AddListener(PlayerScrollDown);

            OnlineManager.Instance.Chat.SetStatus("").Forget();
        }

        if (ReplayCenterLoadedFromLeaderboards)
            StartCoroutine(FromReplayCenter());
        else if (PlayMissionManager.LevelLoadedFromLeaderboards)
            StartCoroutine(FromGame());

        UploadPendingScores();
    }

    public void GlobalScrollUp()
    {
        globalChatScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            globalChatScrollRect.verticalNormalizedPosition + step
        );
    }

    public void GlobalScrollDown()
    {
        globalChatScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            globalChatScrollRect.verticalNormalizedPosition - step
        );
    }

    private void OnGlobalScrollbarValueChanged(float value)
    {
        globalChatScrollUp.interactable = value < 1f;
        globalChatScrollDown.interactable = value > 0f;
    }

    public void PlayerScrollUp()
    {
        playerScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            playerScrollRect.verticalNormalizedPosition + step
        );
    }

    public void PlayerScrollDown()
    {
        playerScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            playerScrollRect.verticalNormalizedPosition - step
        );
    }

    private void OnPlayerScrollbarValueChanged(float value)
    {
        playerScrollUp.interactable = value < 1f;
        playerScrollDown.interactable = value > 0f;
    }

    async void UploadPendingScores()
    {
        await OnlineManager.Instance.ProcessPendingOnlineDataAsync();
    }

    IEnumerator FromGame()
    {
        PlayMissionManager.LevelLoadedFromLeaderboards = false;
        OnPlayClicked();
        blackout.SetActive(true);

        yield return new WaitForSeconds(blackoutDuration);

        raycastBlocker.SetActive(true);
        blackout.SetActive(false);
    }

    IEnumerator FromReplayCenter()
    {
        ReplayCenterLoadedFromLeaderboards = false;
        OnReplayClicked();
        blackout.SetActive(true);

        yield return new WaitForSeconds(blackoutDuration);

        raycastBlocker.SetActive(true);
        blackout.SetActive(false);
    }

    private void InitializeUI()
    {
        loadingMenu.SetActive(false);
        errorMenu.SetActive(false);
        blackout.SetActive(false);

        playMissionWindow.SetActive(false);
        raycastBlocker.SetActive(false);
    }

    // --------------------------------------------------
    // PLAY
    // --------------------------------------------------

    private void OnPlayClicked()
    {
        if (isProcessing)
            return;

        OnlineManager.Instance.Chat.SetStatus("Level Select").Forget();

        playMissionWindow.SetActive(true);
        raycastBlocker.SetActive(true);
    }

    private void OnReplayClicked()
    {
        if (isProcessing)
            return;

        replayWindow.SetActive(true);
        raycastBlocker.SetActive(true);
    }

    // --------------------------------------------------
    // LOGOUT
    // --------------------------------------------------

    private async void OnLogoutClicked()
    {
        if (isProcessing)
            return;

        if (OnlineManager.Instance == null)
        {
            ShowError("Logout Failed", "Online services are unavailable.");

            return;
        }

        isProcessing = true;
        gameWindow.SetActive(false);

        ShowLoading("Logging out...");

        try
        {
            OnlineManager.Instance.Auth.Logout();

            await UniTask.Delay(System.TimeSpan.FromSeconds(blackoutDuration));

            HideLoading();
            blackout.SetActive(true);

            await UniTask.Delay(TimeSpan.FromSeconds(blackoutDuration));

            ReplayRecorder.leaderboardRecording = false;
            JukeboxManager.instance.PlayMusic("Pianoforte");
            SceneManager.LoadScene("MainMenu");
        }
        catch (Exception ex)
        {
            isProcessing = false;

            HideLoading();

            ShowError("Logout Failed", GetErrorMessage(ex));
        }
    }

    // --------------------------------------------------
    // LOADING
    // --------------------------------------------------

    public void ShowLoading(string message)
    {
        loadingMessage.text = message;

        loadingMenu.SetActive(true);

        errorMenu.SetActive(false);

        playMissionWindow.SetActive(false);
        raycastBlocker.SetActive(false);
    }

    private void HideLoading()
    {
        loadingMenu.SetActive(false);
    }

    // --------------------------------------------------
    // ERROR
    // --------------------------------------------------

    public void ShowError(string title, string message, bool returnToLBPlayMission = false)
    {
        errorSound.PlayErrorSound();

        GetComponent<PlayMissionManager>().raycastBlocker.SetActive(true);

        errorTitle.text = title;
        errorMessage.text = message;

        this.returnToLBPlayMission = returnToLBPlayMission;

        errorMenu.SetActive(true);

        loadingMenu.SetActive(false);
    }

    void OnCloseErrorClicked()
    {
        StartCoroutine(OnCloseErrorClickedRoutine());
    }

    private IEnumerator OnCloseErrorClickedRoutine()
    {
        if (returnToLBPlayMission)
        {
            errorMenu.SetActive(false);
            loadingMenu.SetActive(false);

            blackout.SetActive(true);

            yield return new WaitForSeconds(blackoutDuration);

            blackout.SetActive(false);

            playMissionWindow.SetActive(true);
            raycastBlocker.SetActive(true);

            returnToLBPlayMission = false;

            JukeboxManager.instance.PlayMusic("Flanked");

            yield break;
        }

        ReturnToMainMenuAsync().Forget();
    }

    private async UniTask ReturnToMainMenuAsync()
    {
        errorMenu.SetActive(false);
        loadingMenu.SetActive(false);

        blackout.SetActive(true);

        await UniTask.Delay(System.TimeSpan.FromSeconds(blackoutDuration));

        if (OnlineManager.Instance != null)
            await OnlineManager.Instance.ShutdownAsync();

        ReplayRecorder.leaderboardRecording = false;

        SceneManager.LoadScene("MainMenu");
    }

    private string GetErrorMessage(Exception ex)
    {
        if (ex == null)
            return "An unknown error occurred.";

        if (!string.IsNullOrWhiteSpace(ex.Message))
            return ex.Message;

        return "An unknown error occurred.";
    }

    // --------------------------------------------------
    // PLAY MISSION WINDOW
    // --------------------------------------------------

    public void ClosePMG()
    {
        if (isProcessing)
            return;

        OnlineManager.Instance.Chat.SetStatus("").Forget();

        playMissionWindow.SetActive(false);
        raycastBlocker.SetActive(false);
    }

    public void CloseReplayMenu()
    {
        if (isProcessing)
            return;

        replayWindow.SetActive(false);
        raycastBlocker.SetActive(false);
    }

    // --------------------------------------------------
    // CHAT
    // --------------------------------------------------

    private void OnChatMessageReceived(string username, string message, string status)
    {
        if (globalChatText == null)
            return;

        string displayName = string.IsNullOrEmpty(status) ? username : $"{username} ({status})";

        globalChatText.text +=
            $"<color=#9D0000>{displayName}:</color> " + $"<color=#000000>{message}</color>\n";

        RefreshChatTextSize().Forget();
    }

    private void OnOnlinePlayersUpdated(IReadOnlyList<OnlinePlayer> players)
    {
        RefreshOnlinePlayers();
    }

    private void OnChatInputSubmitted(string message)
    {
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
            return;

        SendChatMessage(message);
    }

    private async void SendChatMessage(string message)
    {
        if (OnlineManager.Instance == null)
            return;

        if (OnlineManager.Instance.Chat == null)
            return;

        await OnlineManager.Instance.Chat.SendChat(message);

        clienChatText.text = string.Empty;

        clienChatText.ActivateInputField();

        RefreshChatTextSize().Forget();
    }

    private void OnRecentMessagesReceived(IReadOnlyList<ChatMessage> messages)
    {
        Debug.Log(
            $"[LeaderboardsMenu] RecentMessagesReceived fired. " + $"Count = {messages?.Count ?? 0}"
        );

        RefreshChatHistory();
    }

    private void RefreshOnlinePlayers()
    {
        if (onlinePlayerText == null)
            return;

        if (OnlineManager.Instance == null)
            return;

        if (OnlineManager.Instance.Chat == null)
            return;

        IReadOnlyList<OnlinePlayer> players = OnlineManager.Instance.Chat.GetOnlinePlayers();

        List<string> lines = new List<string>();

        foreach (OnlinePlayer player in players)
        {
            if (string.IsNullOrEmpty(player.Status))
            {
                lines.Add($"<color=#9C0000>{player.Username}</color>");
            }
            else
            {
                lines.Add(
                    $"<color=#9C0000>{player.Username}</color> "
                        + $"<color=#9C0000>({player.Status})</color>"
                );
            }
        }

        onlinePlayerText.text = string.Join("\n", lines);

        RefreshOnlinePlayerSize().Forget();
    }

    private void OnSystemMessageReceived(string message)
    {
        if (globalChatText == null)
            return;

        globalChatText.text += $"<color=#939612>{message}</color>\n";

        RefreshChatTextSize().Forget();
    }

    private void OnWorldRecordReceived(string message)
    {
        if (globalChatText == null)
            return;

        globalChatText.text += $"<color=#006400>{message}</color>\n";

        RefreshChatTextSize().Forget();
    }

    private async UniTask RefreshOnlinePlayerSize()
    {
        await UniTask.Yield();

        onlinePlayerText.GetComponent<TMPTextAutoSize>()?.Refresh();
    }

    private void RefreshChatHistory()
    {
        Debug.Log("[LeaderboardsMenu] RefreshChatHistory() called.");

        if (globalChatText == null)
        {
            Debug.LogWarning("[LeaderboardsMenu] globalChatText is null.");

            return;
        }

        if (OnlineManager.Instance == null)
        {
            Debug.LogWarning("[LeaderboardsMenu] OnlineManager is null.");

            return;
        }

        if (OnlineManager.Instance.Chat == null)
        {
            Debug.LogWarning("[LeaderboardsMenu] ChatManager is null.");

            return;
        }

        IReadOnlyList<ChatMessage> messages = OnlineManager.Instance.Chat.GetRecentMessages();

        Debug.Log($"[LeaderboardsMenu] Cached history count = " + $"{messages.Count}");

        foreach (ChatMessage message in messages)
        {
            if (message.Type == "WorldRecord")
            {
                globalChatText.text += $"<color=#006400>{message.Message}</color>\n";
            }
            else if (message.IsSystem)
            {
                globalChatText.text += $"<color=#939612>{message.Message}</color>\n";
            }
            else
            {
                string displayName = string.IsNullOrEmpty(message.Status)
                    ? message.Username
                    : $"{message.Username} ({message.Status})";

                globalChatText.text +=
                    $"<color=#9D0000>{displayName}:</color> "
                    + $"<color=#000000>{message.Message}</color>\n";
            }
        }

        RefreshChatTextSize().Forget();
    }

    private async UniTask RefreshChatTextSize()
    {
        await UniTask.Yield();

        if (globalChatText == null || globalChatScrollRect == null || globalChatContent == null)
            return;

        // Resize the text.
        globalChatText.GetComponent<TMPTextAutoSize>()?.Refresh();

        Canvas.ForceUpdateCanvases();

        // Make Content match the text height.
        float textHeight = globalChatText.rectTransform.rect.height;

        globalChatContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        Canvas.ForceUpdateCanvases();

        await UniTask.Yield();

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            "[ChatScroll] "
                + $"Viewport Height = "
                + $"{globalChatScrollRect.viewport.rect.height}, "
                + $"Content Height = "
                + $"{globalChatContent.rect.height}, "
                + $"Text Height = "
                + $"{globalChatText.rectTransform.rect.height}, "
                + $"Content Position = "
                + $"{globalChatContent.anchoredPosition}, "
                + $"Normalized Position = "
                + $"{globalChatScrollRect.verticalNormalizedPosition}"
        );

        // Go to bottom.
        globalChatScrollRect.StopMovement();

        globalChatScrollRect.verticalNormalizedPosition = 0f;

        Canvas.ForceUpdateCanvases();
    }

    private string GetChatUsername(string username)
    {
        if (OnlineManager.Instance == null || OnlineManager.Instance.Chat == null)
        {
            return username;
        }

        IReadOnlyList<OnlinePlayer> players = OnlineManager.Instance.Chat.GetOnlinePlayers();

        foreach (OnlinePlayer player in players)
        {
            if (player.Username != username)
                continue;

            if (string.IsNullOrEmpty(player.Status))
                return username;

            return $"{username} ({player.Status})";
        }

        return username;
    }
}
