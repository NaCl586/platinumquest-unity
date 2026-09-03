using System;

namespace Server.DTOs.Responses
{
    [Serializable]
    public class MyRankResponse
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; } = "";
        public int TimeMs { get; set; }
        public int TotalPlayers { get; set; }
    }
}
