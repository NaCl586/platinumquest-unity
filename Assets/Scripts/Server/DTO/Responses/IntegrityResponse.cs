using System.Collections.Generic;

namespace Server.DTOs.Responses
{
    public class IntegrityResponse
    {
        public List<IntegrityFileResponse> Files { get; set; } = new List<IntegrityFileResponse>();
    }

    public class IntegrityFileResponse
    {
        public string Path { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;
    }
}
