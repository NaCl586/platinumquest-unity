using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Server.Authentication
{
    public static class JwtHelper
    {
        public static int? GetUserId(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                string[] parts = token.Split('.');

                if (parts.Length != 3)
                    return null;

                // JWT payload is the second section.
                string payload = parts[1];

                // Base64URL -> Base64
                payload = payload.Replace('-', '+').Replace('_', '/');

                switch (payload.Length % 4)
                {
                    case 2:
                        payload += "==";
                        break;

                    case 3:
                        payload += "=";
                        break;

                    case 0:
                        break;

                    default:
                        return null;
                }

                byte[] bytes = Convert.FromBase64String(payload);

                string json = Encoding.UTF8.GetString(bytes);

                JObject claims = JObject.Parse(json);

                string? value =
                    claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"]
                        ?.ToString()
                    ?? claims["nameid"]?.ToString()
                    ?? claims["sub"]?.ToString()
                    ?? claims["userId"]?.ToString()
                    ?? claims["userid"]?.ToString();

                if (int.TryParse(value, out int userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
