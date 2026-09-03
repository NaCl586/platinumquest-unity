using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Server.API;
using Server.DTOs.Responses;
using Server.Exceptions;
using UnityEngine;

namespace Server.Replay
{
    public class ReplayUploadManager
    {
        private readonly ReplayApi _replayApi;
        private readonly ReplayQueue _replayQueue;

        public ReplayUploadManager(ReplayApi replayApi, ReplayQueue replayQueue)
        {
            _replayApi = replayApi;
            _replayQueue = replayQueue;
        }

        public void QueueReplay(PendingReplay replay)
        {
            if (replay == null)
                throw new ArgumentNullException(nameof(replay));

            _replayQueue.Enqueue(replay);
        }

        public async UniTask<bool> UploadPendingReplayAsync()
        {
            int? userId = OnlineManager.Instance.Auth.UserId;

            if (!userId.HasValue)
            {
                Debug.LogWarning("Cannot upload pending replays: " + "UserId unavailable.");

                return false;
            }

            bool allUploadsSuccessful = true;

            while (true)
            {
                PendingReplay replay = _replayQueue.PeekForUser(userId.Value);

                if (replay == null)
                    break;

                string filePath = ReplayPaths.GetPendingReplayPath(replay.FileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"Replay file not found: {filePath}");

                    replay.RetryCount++;

                    _replayQueue.Update(replay);

                    return false;
                }

                try
                {
                    Debug.Log(
                        $"Uploading pending replay: "
                            + $"UserId={replay.UserId}, "
                            + $"ScoreId={replay.ScoreId}, "
                            + $"Level={replay.Level}"
                    );

                    int replayTimeMs = ReplayRecorder.GetReplayFileTimeMs(filePath);

                    Debug.Log($"Replay final time: {replayTimeMs} ms");

                    UploadReplayResponse response = await _replayApi.UploadReplayAsync(
                        replay.ScoreId,
                        replayTimeMs,
                        filePath
                    );

                    Debug.Log($"Replay uploaded successfully. " + $"ReplayId={response.ReplayId}");

                    _replayQueue.Remove(replay);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (ConflictException)
                {
                    Debug.Log(
                        $"Replay is no longer valid. "
                            + $"Removing ScoreId={replay.ScoreId} "
                            + $"from queue."
                    );

                    _replayQueue.Remove(replay);

                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    // This replay was NOT successfully uploaded.
                    allUploadsSuccessful = false;
                }
                catch (Exception ex)
                {
                    replay.RetryCount++;

                    _replayQueue.Update(replay);

                    Debug.LogError(
                        $"Replay upload failed. "
                            + $"UserId={replay.UserId}, "
                            + $"ScoreId={replay.ScoreId}, "
                            + $"RetryCount={replay.RetryCount}"
                    );

                    Debug.LogException(ex);

                    return false;
                }
            }

            return allUploadsSuccessful;
        }

        public int PendingReplayCount => _replayQueue.Count;

        public void RemovePendingReplayForUserAndLevel(int userId, string level)
        {
            _replayQueue.RemovePendingReplayForUserAndLevel(userId, level);
        }
    }
}
