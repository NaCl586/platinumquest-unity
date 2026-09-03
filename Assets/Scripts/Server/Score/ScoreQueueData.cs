using System;
using System.Collections.Generic;

namespace Server.Score
{
    [Serializable]
    public class ScoreQueueData
    {
        public List<PendingScore> Items = new List<PendingScore>();
    }
}
