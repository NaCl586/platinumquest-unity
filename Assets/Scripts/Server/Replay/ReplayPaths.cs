using System.IO;
using UnityEngine;

namespace Server.Replay
{
    public static class ReplayPaths
    {
        public static string GameDirectory => Directory.GetParent(Application.dataPath).FullName;

        public static string ReplayDirectory => Path.Combine(GameDirectory, "Replay");

        public static string PendingDirectory => Path.Combine(ReplayDirectory, "Pending");

        public static string LeaderboardDirectory => Path.Combine(ReplayDirectory, "Leaderboard");

        public static string QueueFile => Path.Combine(ReplayDirectory, "replay_queue.json");

        public static string ScoreQueueFile => Path.Combine(ReplayDirectory, "score_queue.json");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(ReplayDirectory);

            Directory.CreateDirectory(PendingDirectory);

            Directory.CreateDirectory(LeaderboardDirectory);
        }

        public static string GetPendingReplayPath(string fileName)
        {
            return Path.Combine(PendingDirectory, fileName);
        }

        public static string GetAbsolutePath(string relativePath)
        {
            return Path.Combine(ReplayDirectory, relativePath);
        }
    }
}
