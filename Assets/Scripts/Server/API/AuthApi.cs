using Cysharp.Threading.Tasks;
using Server.DTOs.Requests;
using Server.DTOs.Responses;

namespace Server.API
{
    public class AuthApi
    {
        private readonly ApiClient _client;

        public bool IsLoggedIn => _client.HasToken;

        public AuthApi(ApiClient client)
        {
            _client = client;
        }

        public async UniTask<LoginResponse> LoginAsync(LoginRequest request)
        {
            LoginResponse response = await _client.PostJsonAsync<LoginRequest, LoginResponse>(
                "/api/players/login",
                request
            );

            _client.SetToken(response.Token);

            return response;
        }

        public async UniTask RegisterAsync(RegisterRequest request)
        {
            await _client.PostJsonResponseAsync<RegisterRequest, object>(
                "/api/players/register",
                request
            );
        }

        public UniTask<MeResponse> GetMeAsync()
        {
            return _client.GetAsync<MeResponse>("/api/players/me");
        }

        public void Logout()
        {
            _client.ClearToken();
        }
    }
}
