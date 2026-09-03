using System;

namespace Server.Score
{
    [Serializable]
    public class PendingScore
    {
        public int UserId;
        public string Level = "";
        public string LevelName = "";
        public string ReplayFileName = "";
        public int RetryCount;
    }
}
