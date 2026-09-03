using System.Collections.Generic;

namespace Server.DTOs.Responses
{
    public class GameRatingLeaderboardResponse
    {
        public List<GameRatingResponse> Players { get; set; } = new List<GameRatingResponse>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPlayers { get; set; }

        public int TotalPages { get; set; }
    }
}
