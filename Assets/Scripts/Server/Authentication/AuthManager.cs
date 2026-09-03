using Cysharp.Threading.Tasks;
using Server.API;
using Server.DTOs.Requests;
using Server.DTOs.Responses;

namespace Server.Authentication
{
    public class AuthManager
    {
        private readonly AuthApi _authApi;
        private readonly CredentialStorage _credentialStorage;

        public string? Username { get; private set; }
        public int? UserId { get; private set; }
        public string? AccessToken { get; private set; }

        public bool IsLoggedIn => _authApi.IsLoggedIn;

        public AuthManager(AuthApi authApi, CredentialStorage credentialStorage)
        {
            _authApi = authApi;
            _credentialStorage = credentialStorage;
        }

        public async UniTask RegisterAsync(string username, string password)
        {
            await _authApi.RegisterAsync(
                new RegisterRequest { Username = username, Password = password }
            );
        }

        public async UniTask LoginAsync(string username, string password, bool rememberMe)
        {
            LoginResponse response = await _authApi.LoginAsync(
                new LoginRequest
                {
                    Username = username,
                    Password = password,
                    GameVersion = UnityEngine.Application.version,
                }
            );

            Username = username;

            AccessToken = response.Token;

            UserId = JwtHelper.GetUserId(AccessToken);

            UnityEngine.Debug.Log($"Logged in: Username={Username}, UserId={UserId}");

            if (rememberMe)
            {
                _credentialStorage.Save(
                    new Credential { Username = username, Password = password }
                );
            }
            else
            {
                _credentialStorage.Clear();
            }
        }

        public Credential? LoadRememberedCredential()
        {
            return _credentialStorage.Load();
        }

        public void Logout()
        {
            _authApi.Logout();

            Username = null;
            UserId = null;
            AccessToken = null;
        }

        public void ClearRememberedCredential()
        {
            _credentialStorage.Clear();
        }
    }
}
