using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Server.DTOs.Requests;
using Server.DTOs.Responses;

namespace Server.API
{
    public class RatingApi
    {
        private readonly ApiClient _client;

        public RatingApi(ApiClient client)
        {
            _client = client;
        }

        public UniTask<CalculateRatingsResponse> CalculateRatingsAsync(
            CalculateRatingsRequest request
        )
        {
            return _client.PostJsonAsync<CalculateRatingsRequest, CalculateRatingsResponse>(
                "/api/ratings/calculate",
                request
            );
        }

        public UniTask<SyncAchievementsResponse> SyncAchievementsAsync(List<int> achievementIds)
        {
            return _client.PostJsonAsync<SyncAchievementsRequest, SyncAchievementsResponse>(
                "/api/ratings/achievements",
                new SyncAchievementsRequest { AchievementIds = achievementIds }
            );
        }

        public UniTask<GlobalRatingLeaderboardResponse> GetGlobalLeaderboardAsync(
            int page,
            int pageSize
        )
        {
            return _client.GetAsync<GlobalRatingLeaderboardResponse>(
                $"/api/ratings/global" + $"?page={page}" + $"&pageSize={pageSize}"
            );
        }

        public UniTask<GameRatingLeaderboardResponse> GetGameRatingLeaderboardAsync(
            string game,
            int page,
            int pageSize
        )
        {
            return _client.GetAsync<GameRatingLeaderboardResponse>(
                $"/api/ratings/total" + $"?game={game}" + $"&page={page}" + $"&pageSize={pageSize}"
            );
        }

        public UniTask<GlobalRatingResponse> GetMyRatingAsync()
        {
            return _client.GetAsync<GlobalRatingResponse>("/api/ratings/me");
        }
    }
}
