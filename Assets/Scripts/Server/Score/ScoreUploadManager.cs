using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Server.API;
using Server.DTOs.Requests;
using Server.DTOs.Responses;
using Server.Replay;
using UnityEngine;

namespace Server.Score
{
    public class ScoreUploadManager
    {
        private readonly ScoreApi _scoreApi;
        private readonly ScoreQueue _scoreQueue;
        private readonly ReplayUploadManager _replayUpload;

        public ScoreUploadManager(
            ScoreApi scoreApi,
            ScoreQueue scoreQueue,
            ReplayUploadManager replayUpload
        )
        {
            _scoreApi = scoreApi;
            _scoreQueue = scoreQueue;
            _replayUpload = replayUpload;
        }

        public void QueueScore(PendingScore score)
        {
            if (score == null)
                throw new ArgumentNullException(nameof(score));

            _scoreQueue.Enqueue(score);
        }

        public int PendingScoreCount => _scoreQueue.Count;

        public async UniTask ProcessPendingScoresAsync()
        {
            int? userId = OnlineManager.Instance.Auth.UserId;

            if (!userId.HasValue)
            {
                Debug.LogWarning("Cannot process pending scores: " + "UserId unavailable.");

                return;
            }

            try
            {
                while (true)
                {
                    PendingScore score = _scoreQueue.PeekForUser(userId.Value);

                    if (score == null)
                        break;

                    string filePath = ReplayPaths.GetPendingReplayPath(score.ReplayFileName);

                    if (!File.Exists(filePath))
                    {
                        Debug.LogError($"Pending replay file not found: " + $"{filePath}");

                        _scoreQueue.Remove(score);
                        continue;
                    }

                    int replayTimeMs = ReplayRecorder.GetReplayFileTimeMs(filePath);

                    Debug.Log(
                        $"Submitting pending score: "
                            + $"UserId={score.UserId}, "
                            + $"Level={score.Level}, "
                            + $"TimeMs={replayTimeMs}"
                    );

                    SubmitScoreResponse response = await _scoreApi.SubmitScoreAsync(
                        new SubmitScoreRequest
                        {
                            Level = score.Level,
                            LevelName = score.LevelName,
                            TimeMs = replayTimeMs,
                        }
                    );

                    Debug.Log(
                        $"Pending score submitted. "
                            + $"ScoreId={response.ScoreId}, "
                            + $"PB={response.IsNewPersonalBest}, "
                            + $"ServerWR={response.IsWorldRecord}"
                    );

                    bool isCurrentWorldRecord = await IsCurrentWorldRecordAsync(
                        score.Level,
                        response.ScoreId
                    );

                    Debug.Log(
                        $"Pending score WR check: "
                            + $"ScoreId={response.ScoreId}, "
                            + $"CurrentWR={isCurrentWorldRecord}"
                    );

                    if (isCurrentWorldRecord)
                    {
                        HandleWorldRecord(score, response);
                    }

                    _scoreQueue.Remove(score);
                }

                // ========================================
                // ALL SCORES HAVE BEEN PROCESSED.
                // NOW UPLOAD EVERYTHING IN replay_queue.json.
                // ========================================

                bool allReplayUploadsSuccessful = await _replayUpload.UploadPendingReplayAsync();

                // ========================================
                // ONLY EMPTY THE FOLDER IF:
                //
                // 1. Every replay upload succeeded
                // 2. replay_queue.json is completely empty
                // ========================================

                if (allReplayUploadsSuccessful && _replayUpload.PendingReplayCount == 0)
                {
                    ClearPendingReplayFolder();
                }
                else
                {
                    Debug.LogWarning(
                        "Not all pending replays were successfully "
                            + "uploaded. Pending replay folder will NOT "
                            + "be cleared."
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "Failed to process pending scores or replays. "
                        + "Pending replay folder will NOT be cleared."
                );

                Debug.LogException(ex);

                throw;
            }
        }

        private async UniTask<bool> IsCurrentWorldRecordAsync(string level, int scoreId)
        {
            if (OnlineManager.Instance == null || OnlineManager.Instance.Leaderboard == null)
            {
                throw new InvalidOperationException("Leaderboard API is unavailable.");
            }

            LeaderboardResponse response =
                await OnlineManager.Instance.Leaderboard.GetLeaderboardAsync(level, 1, 10);

            if (response == null || response.Scores == null)
            {
                throw new InvalidOperationException("Leaderboard response is empty.");
            }

            foreach (ScoreResponse leaderboardScore in response.Scores)
            {
                if (leaderboardScore == null)
                    continue;

                if (leaderboardScore.Rank == 1)
                {
                    Debug.Log(
                        $"Current #1 score: "
                            + $"ScoreId={leaderboardScore.ScoreId}, "
                            + $"Player={leaderboardScore.PlayerName}, "
                            + $"TimeMs={leaderboardScore.TimeMs}"
                    );

                    return leaderboardScore.ScoreId == scoreId;
                }
            }

            return false;
        }

        private void HandleWorldRecord(PendingScore score, SubmitScoreResponse response)
        {
            if (string.IsNullOrWhiteSpace(score.ReplayFileName))
            {
                throw new InvalidOperationException(
                    $"Pending score became a World Record, "
                        + $"but no replay file exists. "
                        + $"Level={score.Level}, "
                        + $"ScoreId={response.ScoreId}"
                );
            }

            string filePath = ReplayPaths.GetPendingReplayPath(score.ReplayFileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Pending WR replay file not found.", filePath);
            }

            PendingReplay pendingReplay = new PendingReplay
            {
                UserId = score.UserId,
                ScoreId = response.ScoreId,
                Level = score.Level,
                FileName = score.ReplayFileName,
                RetryCount = 0,
            };

            Debug.Log(
                $"Pending score became a World Record. "
                    + $"Adding replay to replay queue. "
                    + $"ScoreId={response.ScoreId}"
            );

            _replayUpload.QueueReplay(pendingReplay);
        }

        private void ClearPendingReplayFolder()
        {
            string folderPath = ReplayPaths.PendingDirectory;

            if (!Directory.Exists(folderPath))
            {
                Debug.Log("Pending replay folder does not exist. " + "Nothing to clear.");

                return;
            }

            string[] files = Directory.GetFiles(folderPath);

            int deletedCount = 0;

            foreach (string filePath in files)
            {
                try
                {
                    File.Delete(filePath);
                    deletedCount++;

                    Debug.Log($"Deleted processed pending replay: " + $"{filePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to delete pending replay: " + $"{filePath}");

                    Debug.LogException(ex);
                }
            }

            Debug.Log($"Pending replay folder cleanup complete. " + $"Deleted={deletedCount}");
        }
    }
}
