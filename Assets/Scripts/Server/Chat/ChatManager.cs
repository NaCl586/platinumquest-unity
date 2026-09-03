using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Server.Config;
using UnityEngine;

namespace Server
{
    public class ChatManager
    {
        private readonly ServerConfig serverConfig;

        private HubConnection connection;

        private readonly Dictionary<string, string> onlinePlayers =
            new Dictionary<string, string>();

        private readonly List<ChatMessage> recentMessages = new List<ChatMessage>();

        private const string ChatHubPath = "/hubs/chat";

        private bool isDisconnecting;

        public event Action ConnectionLost;
        public event Action ForceLoggedOut;

        public event Action<string, string, string> MessageReceived;
        public event Action<string> PlayerJoined;
        public event Action<string> PlayerLeft;

        public event Action<IReadOnlyList<OnlinePlayer>> OnlinePlayersUpdated;

        public event Action<IReadOnlyList<ChatMessage>> RecentMessagesReceived;

        public event Action<string> SystemMessageReceived;
        public event Action<string> WorldRecordReceived;
        public string? LastError { get; private set; }

        private string ChatUrl => $"{serverConfig.BaseUrl.TrimEnd('/')}{ChatHubPath}";

        public bool IsConnected =>
            connection != null && connection.State == HubConnectionState.Connected;

        public ChatManager(ServerConfig serverConfig)
        {
            this.serverConfig = serverConfig;
        }

        // =========================================================
        // CONNECTION
        // =========================================================

        public async UniTask<bool> Connect(string accessToken)
        {
            LastError = null;

            if (string.IsNullOrEmpty(accessToken))
            {
                Debug.LogError("Cannot connect to chat: access token is empty.");

                return false;
            }

            if (connection != null)
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    Debug.Log("Chat connection is already active.");

                    return true;
                }

                await DisposeConnection();
            }

            isDisconnecting = false;

            connection = new HubConnectionBuilder()
                .WithUrl(
                    ChatUrl,
                    options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(accessToken);
                    }
                )
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();

            connection.Reconnecting += error =>
            {
                Debug.LogWarning("Chat connection lost. " + "Attempting to reconnect...");

                if (error != null)
                {
                    Debug.LogWarning($"Chat reconnect reason: " + $"{error.Message}");
                }

                return Task.CompletedTask;
            };

            connection.Reconnected += connectionId =>
            {
                Debug.Log($"Chat reconnected. " + $"Connection ID: {connectionId}");

                return Task.CompletedTask;
            };

            connection.Closed += error =>
            {
                HandleConnectionClosed(error);

                return Task.CompletedTask;
            };

            try
            {
                Debug.Log($"Connecting to MarbleServer chat: " + $"{ChatUrl}");

                await connection.StartAsync();

                Debug.Log("Connected to MarbleServer chat.");

                return true;
            }
            catch (Exception exception)
            {
                LastError = exception.Message;

                Debug.LogError($"Failed to connect to MarbleServer chat:\n" + $"{exception}");

                await DisposeConnection();

                return false;
            }
        }

        private void HandleConnectionClosed(Exception error)
        {
            if (error != null)
            {
                Debug.LogError($"Chat connection permanently closed:\n" + $"{error}");
            }
            else
            {
                Debug.LogWarning("Chat connection permanently closed.");
            }

            if (isDisconnecting)
                return;

            Debug.LogError("Unable to reconnect to MarbleServer chat.");

            ConnectionLost?.Invoke();
        }

        private void RegisterHandlers()
        {
            connection.On<string, string>("PlayerStatusChanged", OnPlayerStatusChanged);

            connection.On<OnlinePlayer[]>("OnlinePlayers", OnOnlinePlayers);

            connection.On<string>("PlayerJoined", OnPlayerJoined);

            connection.On<string>("PlayerLeft", OnPlayerLeft);

            connection.On<string>("SystemMessage", OnSystemMessage);

            connection.On<string>("WorldRecord", OnWorldRecord);

            connection.On<string, string, string>("ReceiveMessage", OnReceiveMessage);

            connection.On<ChatMessage[]>("RecentMessages", OnRecentMessages);

            connection.On("ForceLogout", OnForceLogout);
        }

        // =========================================================
        // CHAT
        // =========================================================

        private void OnSystemMessage(string message)
        {
            Debug.Log($"[SYSTEM] {message}");

            SystemMessageReceived?.Invoke(message);
        }

        private void OnWorldRecord(string message)
        {
            Debug.Log($"[WORLD RECORD] {message}");

            ChatMessage chatMessage = new ChatMessage
            {
                Username = string.Empty,
                Message = message,
                Status = string.Empty,
                IsSystem = true,
                Type = "WorldRecord",
            };

            recentMessages.Add(chatMessage);

            const int maxRecentMessages = 20;

            while (recentMessages.Count > maxRecentMessages)
            {
                recentMessages.RemoveAt(0);
            }

            WorldRecordReceived?.Invoke(message);
        }

        public async UniTask SendChat(string message)
        {
            if (connection == null)
                return;

            if (connection.State != HubConnectionState.Connected)
            {
                Debug.LogWarning("Cannot send chat: not connected.");

                return;
            }

            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                await connection.InvokeAsync("SendMessage", message);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to send chat message:\n" + $"{exception}");
            }
        }

        public IReadOnlyList<ChatMessage> GetRecentMessages()
        {
            return recentMessages;
        }

        // =========================================================
        // SERVER EVENTS
        // =========================================================

        private void OnReceiveMessage(string username, string message, string status)
        {
            Debug.Log($"[{username}] ({status}) {message}");

            ChatMessage chatMessage = new ChatMessage
            {
                Username = username,
                Message = message,
                Status = status,
                IsSystem = false,
            };

            recentMessages.Add(chatMessage);

            const int maxRecentMessages = 20;

            while (recentMessages.Count > maxRecentMessages)
            {
                recentMessages.RemoveAt(0);
            }

            MessageReceived?.Invoke(username, message, status);
        }

        private void OnRecentMessages(ChatMessage[] messages)
        {
            Debug.Log(
                $"[ChatManager] RecentMessages received. " + $"Count = {messages?.Length ?? 0}"
            );

            recentMessages.Clear();

            if (messages != null)
            {
                recentMessages.AddRange(messages);
            }

            foreach (ChatMessage message in recentMessages)
            {
                Debug.Log(
                    $"[ChatManager] History: " + $"[{message.Username}] " + $"{message.Message}"
                );
            }

            Debug.Log($"[ChatManager] Cached history count = " + $"{recentMessages.Count}");

            RecentMessagesReceived?.Invoke(recentMessages);
        }

        // =========================================================
        // PRESENCE
        // =========================================================

        private void OnOnlinePlayers(OnlinePlayer[] players)
        {
            onlinePlayers.Clear();

            if (players != null)
            {
                foreach (OnlinePlayer player in players)
                {
                    if (player == null)
                        continue;

                    onlinePlayers[player.Username] = player.Status ?? string.Empty;
                }
            }

            OnlinePlayersUpdated?.Invoke(GetOnlinePlayers());
        }

        private void OnPlayerJoined(string username)
        {
            onlinePlayers[username] = string.Empty;

            PlayerJoined?.Invoke(username);

            OnlinePlayersUpdated?.Invoke(GetOnlinePlayers());
        }

        private void OnPlayerLeft(string username)
        {
            onlinePlayers.Remove(username);

            PlayerLeft?.Invoke(username);

            OnlinePlayersUpdated?.Invoke(GetOnlinePlayers());
        }

        private void OnPlayerStatusChanged(string username, string status)
        {
            onlinePlayers[username] = status ?? string.Empty;

            OnlinePlayersUpdated?.Invoke(GetOnlinePlayers());
        }

        public IReadOnlyList<OnlinePlayer> GetOnlinePlayers()
        {
            List<OnlinePlayer> result = new List<OnlinePlayer>();

            foreach (KeyValuePair<string, string> player in onlinePlayers)
            {
                result.Add(new OnlinePlayer { Username = player.Key, Status = player.Value });
            }

            return result;
        }

        // =========================================================
        // STATUS
        // =========================================================

        public async UniTask SetStatus(string status)
        {
            if (connection == null)
                return;

            if (connection.State != HubConnectionState.Connected)
                return;

            try
            {
                await connection.InvokeAsync("SetStatus", status);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to set player status:\n" + $"{exception}");
            }
        }

        // =========================================================
        // FORCE LOGOUT
        // =========================================================

        private async void OnForceLogout()
        {
            Debug.LogWarning(
                "This session has been kicked because "
                    + "the account was opened in another session."
            );

            ForceLoggedOut?.Invoke();

            await Disconnect();
        }

        // =========================================================
        // DISCONNECT
        // =========================================================

        public async UniTask Disconnect()
        {
            isDisconnecting = true;

            if (connection == null)
                return;

            try
            {
                if (connection.State != HubConnectionState.Disconnected)
                {
                    await connection.StopAsync();

                    Debug.Log("Disconnected from MarbleServer chat.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to disconnect from chat:\n" + $"{exception}");
            }

            await DisposeConnection();
        }

        private async UniTask DisposeConnection()
        {
            if (connection == null)
                return;

            try
            {
                await connection.DisposeAsync();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to dispose chat connection:\n" + $"{exception}");
            }

            connection = null;

            onlinePlayers.Clear();

            RefreshPlayerList();
        }

        private void RefreshPlayerList()
        {
            // UI is handled by LeaderboardsMenu / PlayGUI.
        }
    }
}
