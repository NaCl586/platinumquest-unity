using System.Collections.Generic;

namespace Server.DTOs.Requests
{
    public class IntegrityRequest
    {
        public string GameVersion { get; set; } = string.Empty;

        public List<string> Files { get; set; } = new List<string>();
    }
}
