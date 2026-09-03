using System;
using Cysharp.Threading.Tasks;
using Server.DTOs.Responses;
using UnityEngine;

namespace Server.API
{
    public class ReplayApi
    {
        private readonly ApiClient _client;
        public static bool simulateUploadFailure = false;

        public ReplayApi(ApiClient client)
        {
            _client = client;
        }

        public async UniTask<UploadReplayResponse> UploadReplayAsync(
            int scoreId,
            int timeMs,
            string filePath
        )
        {
            if (simulateUploadFailure)
            {
                Debug.Log("TEST: Simulating replay upload failure.");
                throw new Exception("TEST: Replay upload failed.");
            }

            return await _client.UploadFileAsync<UploadReplayResponse>(
                $"/api/scores/{scoreId}/replay",
                "replay",
                filePath,
                "timeMs",
                timeMs.ToString()
            );
        }

        public UniTask<string> DownloadReplayAsync(int scoreId, string downloadDirectory)
        {
            return _client.DownloadFileAsync($"/api/scores/{scoreId}/replay", downloadDirectory);
        }
    }
}
