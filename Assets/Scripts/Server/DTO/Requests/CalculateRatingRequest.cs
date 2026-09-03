using System.Collections;
using System.Collections.Generic;

namespace Server.DTOs.Requests
{
    public class CalculateRatingsRequest
    {
        public string Level { get; set; }
        public List<int> TimesMs { get; set; }
    }
}
