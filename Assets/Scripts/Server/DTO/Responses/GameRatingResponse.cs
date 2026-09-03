using System.Collections;
using System.Collections.Generic;

namespace Server.DTOs.Responses
{
    public class GameRatingResponse
    {
        public int PlayerId { get; set; }

        public string PlayerName { get; set; }

        public int Rating { get; set; }

        public int Rank { get; set; }
    }
}
