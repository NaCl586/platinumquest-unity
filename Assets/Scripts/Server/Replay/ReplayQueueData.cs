using System;
using System.Collections.Generic;

namespace Server.Replay
{
    [Serializable]
    public class ReplayQueueData
    {
        public List<PendingReplay> Items = new List<PendingReplay>();
    }
}
