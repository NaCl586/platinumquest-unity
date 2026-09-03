using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Server.Config;
using Server.DTOs.Responses;
using Server.Exceptions;
using UnityEngine;
using UnityEngine.Networking;

namespace Server.API
{
    public class ApiClient
    {
        private readonly ServerConfig _config;

        public string? Token { get; private set; }

        public bool HasToken => !string.IsNullOrWhiteSpace(Token);

        public ApiClient(ServerConfig config)
        {
            _config = config;
        }

        public void SetToken(string token)
        {
            Token = token;
        }

        public void ClearToken()
        {
            Token = null;
        }

        private string BuildUrl(string route)
        {
            route = route.TrimStart('/');

            return $"{_config.BaseUrl.TrimEnd('/')}/{route}";
        }

        private void ApplyHeaders(UnityWebRequest request)
        {
            request.timeout = _config.Timeout;

            if (!string.IsNullOrWhiteSpace(Token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {Token}");
            }
        }

        private async UniTask SendAsync(UnityWebRequest request)
        {
            ApplyHeaders(request);

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException)
            {
                // UniTask throws for HTTP errors.
                // Handle the actual HTTP status ourselves below.
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<-- {(int)request.responseCode} " + $"{request.method} success");

                return;
            }

            if (request.responseCode != 404)
            {
                Debug.LogError($"<-- {(int)request.responseCode} {request.error}");
            }

            if (request.responseCode != 0)
            {
                ThrowHttpException(request);
            }

            throw new NetworkException(request.error);
        }

        private TResponse ReadResponse<TResponse>(DownloadHandler handler)
        {
            ApiResponse<TResponse>? response = JsonConvert.DeserializeObject<
                ApiResponse<TResponse>
            >(handler.text);

            if (response == null)
            {
                throw new ApiException(500, "Failed to deserialize server response.");
            }

            if (response.Data == null)
            {
                throw new ApiException(500, "Server returned empty response.");
            }

            return response.Data;
        }

        public async UniTask<TResponse> GetAsync<TResponse>(string route)
        {
            using UnityWebRequest request = UnityWebRequest.Get(BuildUrl(route));

            request.downloadHandler = new DownloadHandlerBuffer();

            await SendAsync(request);

            return ReadResponse<TResponse>(request.downloadHandler);
        }

        public UniTask<TResponse> PostJsonAsync<TRequest, TResponse>(string route, TRequest body)
        {
            return SendJsonAsync<TRequest, TResponse>(UnityWebRequest.kHttpVerbPOST, route, body);
        }

        public async UniTask<TResponse> UploadFileAsync<TResponse>(
            string route,
            string formFieldName,
            string filePath
        )
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            WWWForm form = new WWWForm();

            form.AddBinaryData(formFieldName, bytes, Path.GetFileName(filePath));

            using UnityWebRequest request = UnityWebRequest.Post(BuildUrl(route), form);

            await SendAsync(request);

            return ReadResponse<TResponse>(request.downloadHandler);
        }

        public async UniTask<string> DownloadFileAsync(string route, string downloadDirectory)
        {
            Directory.CreateDirectory(downloadDirectory);

            using var request = UnityWebRequest.Get(BuildUrl(route));

            string tempPath = Path.Combine(downloadDirectory, Guid.NewGuid().ToString() + ".tmp");

            request.downloadHandler = new DownloadHandlerFile(tempPath);

            try
            {
                await SendAsync(request);

                string fileName = GetFileNameFromContentDisposition(request);

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ApiException(500, "Server did not provide a replay filename.");
                }

                string finalPath = Path.Combine(downloadDirectory, fileName);

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                File.Move(tempPath, finalPath);

                return finalPath;
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                throw;
            }
        }

        private string GetFileNameFromContentDisposition(UnityWebRequest request)
        {
            string header = request.GetResponseHeader("Content-Disposition");

            if (string.IsNullOrWhiteSpace(header))
            {
                return null;
            }

            try
            {
                System.Net.Http.Headers.ContentDispositionHeaderValue disposition =
                    System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(header);

                // Prefer filename*
                if (!string.IsNullOrWhiteSpace(disposition.FileNameStar))
                {
                    return Uri.UnescapeDataString(disposition.FileNameStar.Trim('"'));
                }

                // Fallback to filename
                if (!string.IsNullOrWhiteSpace(disposition.FileName))
                {
                    return disposition.FileName.Trim('"');
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse Content-Disposition: {header}");

                Debug.LogException(ex);
            }

            return null;
        }

        private async UniTask<TResponse> SendJsonAsync<TRequest, TResponse>(
            string method,
            string route,
            TRequest body
        )
        {
            string json = JsonConvert.SerializeObject(body);

            using UnityWebRequest request = new UnityWebRequest(BuildUrl(route), method);

            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            await SendAsync(request);

            return ReadResponse<TResponse>(request.downloadHandler);
        }

        public UniTask<TResponse> PutJsonAsync<TRequest, TResponse>(string route, TRequest body)
        {
            return SendJsonAsync<TRequest, TResponse>(UnityWebRequest.kHttpVerbPUT, route, body);
        }

        public async UniTask<TResponse> DeleteAsync<TResponse>(string route)
        {
            using UnityWebRequest request = UnityWebRequest.Delete(BuildUrl(route));

            request.downloadHandler = new DownloadHandlerBuffer();

            await SendAsync(request);

            return ReadResponse<TResponse>(request.downloadHandler);
        }

        private static string ExtractErrorMessage(UnityWebRequest request)
        {
            if (request.downloadHandler is DownloadHandlerFile)
            {
                return request.error ?? "Unknown server error.";
            }

            return request.downloadHandler?.text ?? request.error ?? "Unknown server error.";
        }

        private static void ThrowHttpException(UnityWebRequest request)
        {
            string message = ExtractErrorMessage(request);

            switch (request.responseCode)
            {
                case 400:
                    throw new ValidationException(message);

                case 401:
                    throw new UnauthorizedException(message);

                case 403:
                    throw new ForbiddenException(message);

                case 404:
                    throw new NotFoundException(message);

                case 409:
                    throw new ConflictException(message);

                default:
                    throw new ApiException((int)request.responseCode, message);
            }
        }

        public bool IsAuthenticated
        {
            get { return !string.IsNullOrWhiteSpace(Token); }
        }

        public async UniTask<TResponse> UploadFileAsync<TResponse>(
            string route,
            string formFieldName,
            string filePath,
            string additionalFieldName,
            string additionalFieldValue
        )
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            WWWForm form = new WWWForm();

            form.AddBinaryData(formFieldName, bytes, Path.GetFileName(filePath));

            form.AddField(additionalFieldName, additionalFieldValue);

            using UnityWebRequest request = UnityWebRequest.Post(BuildUrl(route), form);

            await SendAsync(request);

            return ReadResponse<TResponse>(request.downloadHandler);
        }

        public async UniTask<ApiResponse<TResponse>> PostJsonResponseAsync<TRequest, TResponse>(
            string route,
            TRequest body
        )
        {
            string json = JsonConvert.SerializeObject(body);

            using UnityWebRequest request = new UnityWebRequest(
                BuildUrl(route),
                UnityWebRequest.kHttpVerbPOST
            );

            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            await SendAsync(request);

            ApiResponse<TResponse>? response = JsonConvert.DeserializeObject<
                ApiResponse<TResponse>
            >(request.downloadHandler.text);

            if (response == null)
            {
                throw new ApiException(500, "Failed to deserialize server response.");
            }

            return response;
        }
    }
}
