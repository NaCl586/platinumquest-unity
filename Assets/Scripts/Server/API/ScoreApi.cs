using Cysharp.Threading.Tasks;
using Server.DTOs.Requests;
using Server.DTOs.Responses;

namespace Server.API
{
    public class ScoreApi
    {
        private readonly ApiClient _client;

        public ScoreApi(ApiClient client)
        {
            _client = client;
        }

        public UniTask<SubmitScoreResponse> SubmitScoreAsync(SubmitScoreRequest request)
        {
            return _client.PostJsonAsync<SubmitScoreRequest, SubmitScoreResponse>(
                "/api/scores",
                request
            );
        }
    }
}
