using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ustas.RimAI.Communication;
using Ustas.RimAI.Communication.Client;
using Ustas.RimAI.Communication.Client.Player2;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Util;
using Ustas.RimAI.Quests.Util;
using UnityEngine.Networking;
using Verse;

namespace Ustas.RimAI.Quests.Services.Streaming
{
    /// <summary>
    /// Plain text streaming client for Player2 API.
    /// Supports both local Player2 app (auto-authentication) and remote API (manual key).
    /// </summary>
    public class Player2StreamingClient : StreamingClient
    {
        private const string GameClientId = "019a8368-b00b-72bc-b367-2825079dc6fb";
        private const string LocalUrl = "http://localhost:4315";

        // Cache for local API key to avoid repeated login requests
        private static string _cachedLocalApiKey;
        private static DateTime _localKeyExpiry = DateTime.MinValue;
        private static readonly TimeSpan LocalKeyTTL = TimeSpan.FromMinutes(30);

        public Player2StreamingClient(IAIClient client) : base(client) { }

        /// <summary>
        /// Stream chat completion using settings from RimTalk configuration.
        /// Automatically tries local Player2 app first, falls back to configured API key.
        /// </summary>
        public override async Task<Payload> StreamFromSettingsAsync(
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            var settings = Settings.Get();
            var config = settings.GetActiveConfig();

            string remoteBaseUrl = AIProvider.Player2.GetEndpointUrl();
            string fallbackApiKey = config?.ApiKey ?? "";

            return await StreamAsync(
                remoteBaseUrl,
                fallbackApiKey,
                instruction,
                messages,
                onTextChunkReceived
            );
        }

        /// <summary>
        /// Stream chat completion from Player2 API with explicit parameters.
        /// Automatically tries local app first, falls back to remote with provided apiKey.
        /// </summary>
        public async Task<Payload> StreamAsync(
            string remoteBaseUrl,
            string fallbackApiKey,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            // Try to get local connection first
            var (baseUrl, apiKey, isLocal) = await ResolveConnectionAsync(
                remoteBaseUrl,
                fallbackApiKey
            );

            // Build endpoint URL
            string endpointUrl = $"{baseUrl}/v1/chat/completions";

            // Build request JSON
            string jsonContent = BuildRequestJson(instruction, messages, stream: true);

            // Create stream handler with callback
            var streamHandler = new Player2StreamHandler(SafeChunkCallback(onTextChunkReceived));

            // Send request
            await SendRequestAsync(endpointUrl, jsonContent, apiKey, streamHandler, isLocal);

            return new Payload(
                baseUrl,
                null,
                jsonContent,
                streamHandler.GetFullText(),
                streamHandler.GetTotalTokens()
            );
        }

        #region Connection Resolution

        /// <summary>
        /// Resolve the best available connection: local app preferred, then remote.
        /// </summary>
        private static async Task<(string baseUrl, string apiKey, bool isLocal)> ResolveConnectionAsync(
            string remoteBaseUrl,
            string fallbackApiKey
        )
        {
            // Try local app first
            string localKey = await TryGetLocalApiKeyAsync();
            if (!string.IsNullOrEmpty(localKey))
            {
                QuestLogger.Debug("Player2: Using local app connection");
                return (LocalUrl, localKey, true);
            }

            // Fall back to remote
            if (!string.IsNullOrEmpty(fallbackApiKey))
            {
                QuestLogger.Debug("Player2: Using remote connection with API key");
                return (remoteBaseUrl, fallbackApiKey, false);
            }

            throw new InvalidOperationException(
                "Player2 not available: no local app detected and no API key configured"
            );
        }

        /// <summary>
        /// Try to get API key from local Player2 app with caching.
        /// </summary>
        private static async Task<string> TryGetLocalApiKeyAsync()
        {
            // Return cached key if still valid
            if (!string.IsNullOrEmpty(_cachedLocalApiKey) && DateTime.Now < _localKeyExpiry)
            {
                return _cachedLocalApiKey;
            }

            try
            {
                QuestLogger.Debug("Player2: Checking for local app...");

                // Health check
                using (var healthRequest = UnityWebRequest.Get($"{LocalUrl}/v1/health"))
                {
                    healthRequest.timeout = 2;
                    await SendWebRequestAsync(healthRequest);

                    if (
                        healthRequest.result == UnityWebRequest.Result.ConnectionError
                        || healthRequest.result == UnityWebRequest.Result.ProtocolError
                    )
                    {
                        QuestLogger.Debug(
                            $"Player2: Local health check failed: {healthRequest.error}"
                        );
                        return null;
                    }

                    QuestLogger.Debug("Player2: Local health check passed");
                }

                // Login to get API key
                using (
                    var loginRequest = new UnityWebRequest(
                        $"{LocalUrl}/v1/login/web/{GameClientId}",
                        "POST"
                    )
                )
                {
                    loginRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
                    loginRequest.downloadHandler = new DownloadHandlerBuffer();
                    loginRequest.SetRequestHeader("Content-Type", "application/json");
                    loginRequest.timeout = 3;

                    await SendWebRequestAsync(loginRequest);

                    if (
                        loginRequest.result == UnityWebRequest.Result.ConnectionError
                        || loginRequest.result == UnityWebRequest.Result.ProtocolError
                    )
                    {
                        QuestLogger.Debug(
                            $"Player2: Local login failed: {loginRequest.responseCode} - {loginRequest.error}"
                        );
                        return null;
                    }

                    var response = JsonUtil.DeserializeFromJson<LocalPlayer2Response>(
                        loginRequest.downloadHandler.text
                    );
                    if (!string.IsNullOrEmpty(response?.P2Key))
                    {
                        QuestLogger.Message("[Player2] ✓ Local app authenticated successfully");
                        _cachedLocalApiKey = response.P2Key;
                        _localKeyExpiry = DateTime.Now + LocalKeyTTL;
                        return _cachedLocalApiKey;
                    }

                    QuestLogger.Warning("Player2: Local app responded but no API key in response");
                    return null;
                }
            }
            catch (Exception ex)
            {
                QuestLogger.Debug($"Player2: Local detection failed: {ex.Message}");
                return null;
            }
        }

        private static Task SendWebRequestAsync(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }

        /// <summary>
        /// Clear cached local API key (useful when connection fails)
        /// </summary>
        public static void ClearLocalKeyCache()
        {
            _cachedLocalApiKey = null;
            _localKeyExpiry = DateTime.MinValue;
        }

        #endregion

        #region Private Helper Methods

        private static string BuildRequestJson(
            string instruction,
            List<(Role role, string message)> messages,
            bool stream
        )
        {
            var normalized = BuildNormalizedMessages(
                instruction,
                messages,
                mergeConsecutiveSameRole: true
            );
            var allMessages = normalized.ConvertAll(
                m => new Ustas.RimAI.Communication.Client.Player2.Message { Role = m.role, Content = m.content }
            );

            var request = new Player2Request { Messages = allMessages, Stream = stream };

            return JsonUtil.SerializeToJson(request);
        }

        private async Task<string> SendRequestAsync(
            string url,
            string jsonContent,
            string apiKey,
            Player2StreamHandler streamHandler,
            bool isLocal
        )
        {
            if (Prefs.DevMode)
            {
                Log.Message(
                    $"[RimAI.Quests] Request URL ({(isLocal ? "local" : "remote")}): {url}"
                );
            }

            QuestLogger.Debug(
                $"Player2 API request ({(isLocal ? "local" : "remote")}): {url}\n{jsonContent}"
            );

            using var webRequest = CreateJsonPostRequest(url, jsonContent, streamHandler, apiKey);

            webRequest.SetRequestHeader("player2-game-key", GameClientId);

            const float connectTimeout = 60f;
            const float readTimeout = 60f;

            bool completed = await AwaitStreamingResponseAsync(
                webRequest,
                connectTimeout,
                readTimeout,
                timeout => $"Connection timed out ({timeout}s)",
                timeout => $"Read timed out ({timeout}s)"
            );

            if (!completed)
            {
                return null;
            }

            // Flush remaining buffer
            streamHandler.Flush();

            // Check for streaming errors - clear cache if auth failed
            if (!string.IsNullOrEmpty(streamHandler.DetectedError))
            {
                string errorMsg = streamHandler.DetectedError;
                if (isLocal && (errorMsg.Contains("auth") || errorMsg.Contains("401")))
                {
                    ClearLocalKeyCache();
                }
                QuestLogger.Error($"Player2 streaming error: {errorMsg}");
                throw new Exception($"Player2 streaming error: {errorMsg}");
            }

            if (HasTransportError(webRequest))
            {
                ThrowRequestFailed(
                    webRequest,
                    "Request failed",
                    () =>
                    {
                        if (isLocal)
                        {
                            ClearLocalKeyCache();
                        }
                    }
                );
            }

            QuestLogger.Debug($"Player2 API response: \n{streamHandler.GetRawJson()}");
            return streamHandler.GetFullText();
        }

        #endregion
    }
}
