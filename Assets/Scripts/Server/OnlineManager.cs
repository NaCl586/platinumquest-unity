using System;
using Cysharp.Threading.Tasks;
using Server.API;
using Server.Authentication;
using Server.Config;
using Server.Replay;
using Server.Score;
using UnityEngine;

namespace Server
{
    public class OnlineManager : MonoBehaviour
    {
        public static OnlineManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField]
        private ServerConfig serverConfig;

        public AuthManager Auth { get; private set; }

        public ScoreApi Score { get; private set; }

        public ReplayApi Replay { get; private set; }

        public ReplayUploadManager ReplayUpload { get; private set; }

        public LeaderboardApi Leaderboard { get; private set; }

        public ScoreUploadManager ScoreUpload { get; private set; }

        public OnlineScoreManager OnlineScore { get; private set; }

        public ReplayDownloadManager ReplayDownload { get; private set; }

        public IntegrityApi Integrity { get; private set; }

        public ChatManager Chat { get; private set; }

        public RatingApi Rating { get; private set; }

        private ApiClient _apiClient;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        public async UniTask ShutdownAsync()
        {
            if (Chat != null)
                await Chat.Disconnect();

            Auth?.Logout();

            Instance = null;
            Destroy(gameObject);
        }

        private void InitializeServices()
        {
            // Core

            _apiClient = new ApiClient(serverConfig);

            // APIs

            AuthApi authApi = new AuthApi(_apiClient);

            Score = new ScoreApi(_apiClient);

            Replay = new ReplayApi(_apiClient);

            Leaderboard = new LeaderboardApi(_apiClient);

            // Storage

            CredentialStorage credentialStorage = new CredentialStorage();

            ReplayQueue replayQueue = new ReplayQueue();

            ScoreQueue scoreQueue = new ScoreQueue();

            // Managers

            ReplayUpload = new ReplayUploadManager(Replay, replayQueue);

            ScoreUpload = new ScoreUploadManager(Score, scoreQueue, ReplayUpload);

            Auth = new AuthManager(authApi, credentialStorage);

            OnlineScore = new OnlineScoreManager(Score, ScoreUpload, ReplayUpload);

            ReplayDownload = new ReplayDownloadManager(Replay);

            Integrity = new IntegrityApi(_apiClient);

            Chat = new ChatManager(serverConfig);

            Rating = new RatingApi(_apiClient);
        }

        public async UniTask ProcessPendingOnlineDataAsync()
        {
            if (Auth == null || !Auth.IsLoggedIn)
            {
                return;
            }

            Debug.Log(
                $"Processing pending online data for "
                    + $"UserId={Auth.UserId}, "
                    + $"Username={Auth.Username}"
            );

            try
            {
                await ScoreUpload.ProcessPendingScoresAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Pending score processing failed: {ex.Message}");
            }

            try
            {
                await ReplayUpload.UploadPendingReplayAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Pending replay processing failed: {ex.Message}");
            }
        }
    }
}
