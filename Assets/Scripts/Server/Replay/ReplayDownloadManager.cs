using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Server.API;

namespace Server.Replay
{
    public class ReplayDownloadManager
    {
        private readonly ReplayApi _replayApi;

        public ReplayDownloadManager(ReplayApi replayApi)
        {
            _replayApi = replayApi;
        }

        public async UniTask<string> DownloadReplayAsync(int scoreId, string finalPath)
        {
            string directory = Path.GetDirectoryName(finalPath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new Exception("Invalid replay download path.");
            }

            Directory.CreateDirectory(directory);

            // Download using the directory.
            // ReplayApi returns the actual downloaded
            // GUID filename.
            string downloadedPath = await _replayApi.DownloadReplayAsync(scoreId, directory);

            if (string.IsNullOrWhiteSpace(downloadedPath))
            {
                throw new Exception("Replay download returned an empty path.");
            }

            if (!File.Exists(downloadedPath))
            {
                throw new FileNotFoundException(
                    "Downloaded replay file was not found.",
                    downloadedPath
                );
            }

            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            File.Move(downloadedPath, finalPath);

            return finalPath;
        }
    }
}
