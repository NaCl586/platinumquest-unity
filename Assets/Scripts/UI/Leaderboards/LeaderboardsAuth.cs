using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Server;
using Server.Authentication;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardsAuth : MonoBehaviour
{
    [Header("Login")]
    public GameObject blackout;
    public GameObject loginMenu;
    public GameObject registerMenu;
    public GameObject loadingMenu;
    public GameObject errorMenu;

    [Header("Login")]
    public TMP_InputField nameLoginField;
    public TMP_InputField passwordLoginField;
    public Toggle rememberPasswordCheck;
    public Button createAccountButton;
    public Button homeButton;
    public Button loginButton;

    [Header("Register")]
    public TMP_InputField nameRegisterField;
    public TMP_InputField passwordRegisterField;
    public TMP_InputField confirmPasswordRegisterField;
    public Toggle tosAgreement;
    public TextMeshProUGUI credentialValidationMessage;
    public Button createButton;
    public Button cancelButton;

    [Header("Error")]
    public TextMeshProUGUI errorTitle;
    public TextMeshProUGUI errorMessage;
    public Button yahooButton;
    public ErrorSound errorSound;

    [Header("Loading")]
    public TextMeshProUGUI loadingMessage;

    [Header("TOS")]
    [SerializeField]
    private Scrollbar scrollbar;
    public ScrollRect scrollRect;

    [SerializeField]
    private Button scrollUpButton;

    [SerializeField]
    private Button scrollDownButton;

    [SerializeField]
    private float step = 0.1f;

    [Header("Startup")]
    public float blackoutDuration = 2f;

    private bool isProcessing;

    private void OnDestroy()
    {
        scrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);

        if (OnlineManager.Instance != null)
        {
            OnlineManager.Instance.Chat.ConnectionLost -= OnChatConnectionLost;

            OnlineManager.Instance.Chat.ForceLoggedOut -= OnForceLoggedOut;
        }
    }

    private void Start()
    {
        JukeboxManager.instance.PlayMusic("Quiet Lab");
        JukeboxManager.instance.ForceStop();

        SetupButtons();
        InitializeUI();
        LoadRememberedCredentials();

        if (OnlineManager.Instance != null)
        {
            OnlineManager.Instance.Chat.ConnectionLost += OnChatConnectionLost;

            OnlineManager.Instance.Chat.ForceLoggedOut += OnForceLoggedOut;
        }

        StartCoroutine(ShowLoginAfterDelay());
    }

    private void OnChatConnectionLost()
    {
        if (!isProcessing)
            return;

        HideLoading();

        isProcessing = false;

        ShowError("Connection Failed", "The connection to the online server was lost.");
    }

    private void OnForceLoggedOut()
    {
        if (!isProcessing)
            return;

        HideLoading();

        isProcessing = false;

        ShowError("Session Ended", "Your account was logged in from another session.");
    }

    private IEnumerator ShowLoginAfterDelay()
    {
        loginMenu.SetActive(false);
        registerMenu.SetActive(false);
        loadingMenu.SetActive(false);
        errorMenu.SetActive(false);

        blackout.SetActive(true);

        yield return new WaitForSeconds(blackoutDuration);

        blackout.SetActive(false);

        yield return new WaitForSeconds(blackoutDuration);

        loginMenu.SetActive(true);
    }

    private void SetupButtons()
    {
        scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        OnScrollbarValueChanged(scrollbar.value);
        scrollUpButton.onClick.AddListener(ScrollUp);
        scrollDownButton.onClick.AddListener(ScrollDown);

        loginButton.onClick.AddListener(OnLoginClicked);
        createAccountButton.onClick.AddListener(OnCreateAccountClicked);
        homeButton.onClick.AddListener(OnHomeClicked);

        createButton.onClick.AddListener(OnRegisterClicked);
        cancelButton.onClick.AddListener(OnCancelRegisterClicked);

        yahooButton.onClick.AddListener(OnCloseErrorClicked);
    }

    private void InitializeUI()
    {
        loginMenu.SetActive(true);
        registerMenu.SetActive(false);
        loadingMenu.SetActive(false);
        errorMenu.SetActive(false);
        blackout.SetActive(true);
    }

    // --------------------------------------------------
    // LOGIN
    // --------------------------------------------------

    public async void OnLoginClicked()
    {
        if (isProcessing)
            return;

        string username = nameLoginField.text.Trim();
        string password = passwordLoginField.text;

        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Login Failed", "Please enter your username.");

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Login Failed", "Please enter your password.");

            return;
        }

        isProcessing = true;

        ShowLoading("Logging in...");

        try
        {
            await OnlineManager.Instance.Auth.LoginAsync(
                username,
                password,
                rememberPasswordCheck.isOn
            );

            if (string.IsNullOrEmpty(OnlineManager.Instance.Auth.AccessToken))
            {
                ShowError(
                    "Login Failed",
                    "Authentication succeeded, but no access token was received."
                );

                isProcessing = false;
                HideLoading();

                return;
            }

            bool chatConnected = await OnlineManager.Instance.Chat.Connect(
                OnlineManager.Instance.Auth.AccessToken
            );

            if (!chatConnected)
            {
                ShowError(
                    "Connection Failed",
                    OnlineManager.Instance.Chat.LastError
                        ?? "Unable to connect to the online server."
                );

                isProcessing = false;
                HideLoading();

                return;
            }

            // Authentication succeeded.
            // Now it is safe to process pending online data.
            await OnlineManager.Instance.ProcessPendingOnlineDataAsync();

            await UniTask.Delay(System.TimeSpan.FromSeconds(blackoutDuration));

            // Login successful
            HideLoading();

            isProcessing = false;
            PlayMissionManager.LevelLoadedFromLeaderboards = false;
            ReplayRecorder.leaderboardRecording = true;

            SceneManager.LoadScene("LBPlayMission");
        }
        catch (Exception ex)
        {
            HideLoading();

            isProcessing = false;

            ShowError("Login Failed", GetErrorMessage(ex));
        }
    }

    // --------------------------------------------------
    // REGISTER
    // --------------------------------------------------

    public void OnCreateAccountClicked()
    {
        if (isProcessing)
            return;

        ClearRegisterFields();

        loginMenu.SetActive(false);
        registerMenu.SetActive(true);
    }

    public async void OnRegisterClicked()
    {
        if (isProcessing)
            return;

        string username = nameRegisterField.text.Trim();
        string password = passwordRegisterField.text;
        string confirmPassword = confirmPasswordRegisterField.text;

        ClearValidationMessage();

        // Username validation
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowCredentialValidation("Please enter a username.");

            return;
        }

        // Password validation
        if (string.IsNullOrWhiteSpace(password))
        {
            ShowCredentialValidation("Please enter a password.");

            return;
        }

        if (!ValidatePassword(password))
        {
            ShowCredentialValidation(
                "Password must contain at least one uppercase letter, "
                    + "one lowercase letter, one number, and one symbol."
            );

            return;
        }

        // Confirm password validation
        if (password != confirmPassword)
        {
            ShowCredentialValidation("Passwords do not match.");

            return;
        }

        // Terms of Service
        if (!tosAgreement.isOn)
        {
            ShowCredentialValidation("You must agree to the Terms of Service.");

            return;
        }

        isProcessing = true;

        ShowLoading("Creating account...");

        try
        {
            await OnlineManager.Instance.Auth.RegisterAsync(username, password);

            loadingMessage.text = "Account Created, Returning to Login Menu...";

            await UniTask.Delay(System.TimeSpan.FromSeconds(3));

            HideLoading();

            isProcessing = false;

            // Put the username into the login field
            nameLoginField.text = username;

            // Password can be entered again on login
            passwordLoginField.text = "";

            // Return to login screen
            registerMenu.SetActive(false);
            loginMenu.SetActive(true);

            ClearRegisterFields();
        }
        catch (Exception ex)
        {
            HideLoading();

            isProcessing = false;

            ShowError("Registration Failed", GetErrorMessage(ex));
        }
    }

    public void OnCancelRegisterClicked()
    {
        if (isProcessing)
            return;

        registerMenu.SetActive(false);
        loginMenu.SetActive(true);

        ClearRegisterFields();
    }

    // --------------------------------------------------
    // REMEMBERED CREDENTIALS
    // --------------------------------------------------

    private void LoadRememberedCredentials()
    {
        if (OnlineManager.Instance == null)
            return;

        Credential credential = OnlineManager.Instance.Auth.LoadRememberedCredential();

        if (credential == null)
        {
            rememberPasswordCheck.isOn = false;
            return;
        }

        nameLoginField.text = credential.Username;
        passwordLoginField.text = credential.Password;

        rememberPasswordCheck.isOn = true;
    }

    // --------------------------------------------------
    // LOADING
    // --------------------------------------------------

    private void ShowLoading(string message)
    {
        loadingMessage.text = message;

        loadingMenu.SetActive(true);

        loginMenu.SetActive(false);
        registerMenu.SetActive(false);
        errorMenu.SetActive(false);
    }

    private void HideLoading()
    {
        loadingMenu.SetActive(false);
    }

    // --------------------------------------------------
    // ERROR
    // --------------------------------------------------

    private void ShowError(string title, string message)
    {
        errorSound.PlayErrorSound();

        errorTitle.text = title;
        errorMessage.text = message;

        loginMenu.SetActive(false);
        registerMenu.SetActive(false);
        loadingMenu.SetActive(false);

        errorMenu.SetActive(true);
    }

    private void OnCloseErrorClicked()
    {
        if (isProcessing)
            return;

        ReturnToMainMenuAsync().Forget();
    }

    private async UniTask ReturnToMainMenuAsync()
    {
        errorMenu.SetActive(false);

        loginMenu.SetActive(false);
        registerMenu.SetActive(false);
        loadingMenu.SetActive(false);

        blackout.SetActive(true);

        await UniTask.Delay(TimeSpan.FromSeconds(blackoutDuration));

        if (OnlineManager.Instance != null)
        {
            await OnlineManager.Instance.ShutdownAsync();
        }

        JukeboxManager.instance.Play();

        SceneManager.LoadScene("MainMenu");
    }

    private string GetErrorMessage(Exception ex)
    {
        if (ex == null)
            return "An unknown error occurred.";

        try
        {
            JObject errorJson = JObject.Parse(ex.Message);

            // ASP.NET validation errors
            JToken errors = errorJson["errors"];

            if (errors != null)
            {
                List<string> messages = new List<string>();

                foreach (JProperty property in errors.Children<JProperty>())
                {
                    foreach (JToken message in property.Value)
                    {
                        string text = message.ToString();

                        if (!string.IsNullOrWhiteSpace(text))
                            messages.Add(text);
                    }
                }

                if (messages.Count > 0)
                    return string.Join("\n", messages);
            }

            // API response with a message
            JToken messageToken = errorJson["message"];

            if (messageToken != null && !string.IsNullOrWhiteSpace(messageToken.ToString()))
            {
                return messageToken.ToString();
            }

            // ProblemDetails detail
            JToken detail = errorJson["detail"];

            if (detail != null && !string.IsNullOrWhiteSpace(detail.ToString()))
            {
                return detail.ToString();
            }

            // ProblemDetails title
            JToken title = errorJson["title"];

            if (title != null && !string.IsNullOrWhiteSpace(title.ToString()))
            {
                return title.ToString();
            }
        }
        catch
        {
            // ex.Message wasn't JSON.
        }

        // Normal non-JSON exception
        if (!string.IsNullOrWhiteSpace(ex.Message))
            return ex.Message;

        return "An unknown error occurred.";
    }

    // --------------------------------------------------
    // HOME
    // --------------------------------------------------

    public void OnHomeClicked()
    {
        if (isProcessing)
            return;

        ReturnToMainMenuAsync().Forget();
    }

    // --------------------------------------------------
    // HELPERS
    // --------------------------------------------------

    private void ShowCredentialValidation(string message)
    {
        credentialValidationMessage.text = message;
        credentialValidationMessage.gameObject.SetActive(true);
    }

    private void ClearValidationMessage()
    {
        credentialValidationMessage.text = "";
        credentialValidationMessage.gameObject.SetActive(false);
    }

    private void ClearRegisterFields()
    {
        nameRegisterField.text = "";
        passwordRegisterField.text = "";
        confirmPasswordRegisterField.text = "";

        tosAgreement.isOn = false;

        ClearValidationMessage();
    }

    public void ScrollUp()
    {
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + step
        );
    }

    public void ScrollDown()
    {
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition - step
        );
    }

    private void OnScrollbarValueChanged(float value)
    {
        // Disable when limits reached
        scrollUpButton.interactable = value < 1f;
        scrollDownButton.interactable = value > 0f;
    }

    private bool ValidatePassword(string password)
    {
        if (password.Length < 6)
            return false;

        if (password.Length > 64)
            return false;

        bool hasUppercase = false;
        bool hasLowercase = false;
        bool hasNumber = false;
        bool hasSymbol = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c))
                hasUppercase = true;
            else if (char.IsLower(c))
                hasLowercase = true;
            else if (char.IsDigit(c))
                hasNumber = true;
            else if (!char.IsWhiteSpace(c))
                hasSymbol = true;
        }

        return hasUppercase && hasLowercase && hasNumber && hasSymbol;
    }
}
