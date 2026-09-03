namespace Server.DTOs.Responses
{
    public class ScoreResponse
    {
        public int ScoreId { get; set; }
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public int TimeMs { get; set; }
        public int Rating { get; set; }
    }
}
