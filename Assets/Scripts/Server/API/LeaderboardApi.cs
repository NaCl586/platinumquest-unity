using Cysharp.Threading.Tasks;
using Server.DTOs.Responses;
using UnityEngine.Networking;

namespace Server.API
{
    public class LeaderboardApi
    {
        private readonly ApiClient _client;

        public LeaderboardApi(ApiClient client)
        {
            _client = client;
        }

        public UniTask<LeaderboardResponse> GetLeaderboardAsync(
            string level,
            int page,
            int pageSize
        )
        {
            string route =
                "/api/leaderboard"
                + "?level="
                + UnityWebRequest.EscapeURL(level)
                + "&page="
                + page
                + "&pageSize="
                + pageSize;

            return _client.GetAsync<LeaderboardResponse>(route);
        }

        public UniTask<MyRankResponse> GetMyRankAsync(string level)
        {
            string route =
                "/api/leaderboard/my-rank" + "?level=" + UnityWebRequest.EscapeURL(level);

            return _client.GetAsync<MyRankResponse>(route);
        }
    }
}
