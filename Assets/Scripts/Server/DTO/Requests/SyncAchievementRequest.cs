using System.Collections;
using System.Collections.Generic;

namespace Server.DTOs.Requests
{
    public class SyncAchievementsRequest
    {
        public List<int> AchievementIds { get; set; } = new List<int>();
    }
}
