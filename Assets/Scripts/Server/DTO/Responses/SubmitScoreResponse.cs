namespace Server.DTOs.Responses
{
    public class SubmitScoreResponse
    {
        public int ScoreId { get; set; }

        public bool IsNewPersonalBest { get; set; }

        public int TimeMs { get; set; }

        public bool IsWorldRecord { get; set; }

        public int Rating { get; set; }
    }
}
