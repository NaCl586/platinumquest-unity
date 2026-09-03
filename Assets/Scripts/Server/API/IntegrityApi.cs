using Cysharp.Threading.Tasks;
using Server.DTOs.Requests;
using Server.DTOs.Responses;

namespace Server.API
{
    public class IntegrityApi
    {
        private readonly ApiClient _client;

        public IntegrityApi(ApiClient client)
        {
            _client = client;
        }

        public async UniTask<IntegrityResponse> CheckAsync(IntegrityRequest request)
        {
            return await _client.PostJsonAsync<IntegrityRequest, IntegrityResponse>(
                "/api/integrity/check",
                request
            );
        }
    }
}
