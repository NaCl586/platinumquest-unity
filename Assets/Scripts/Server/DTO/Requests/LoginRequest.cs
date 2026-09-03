namespace Server.DTOs.Requests
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string GameVersion { get; set; } = string.Empty;
    }
}
