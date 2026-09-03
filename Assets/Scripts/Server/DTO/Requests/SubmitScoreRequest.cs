namespace Server.DTOs.Requests
{
    public class SubmitScoreRequest
    {
        public string PlayerName { get; set; }
        public string Level { get; set; }
        public string LevelName { get; set; }
        public int TimeMs { get; set; }
    }
}
