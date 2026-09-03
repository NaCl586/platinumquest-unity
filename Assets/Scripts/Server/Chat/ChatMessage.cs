namespace Server
{
    public class ChatMessage
    {
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public string Type { get; set; } = "Normal";
    }
}
