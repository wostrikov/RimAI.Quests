using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ustas.RimAI.Communication;
using Ustas.RimAI.Communication.Client;
using Ustas.RimAI.Communication.Client.Player2;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Util;
using Ustas.RimAI.Core.Net;
using Ustas.RimAI.Core.Player2;
using Ustas.RimAI.Quests.Util;
using Verse;
using Ustas.RimAI.Core.Diagnostics;

namespace Ustas.RimAI.Quests.Services.Streaming
{
    /// <summary>
    /// Plain text streaming client for Player2 API.
    /// Supports both local Player2 app (auto-authentication) and remote API (manual key).
    /// </summary>
    public class Player2StreamingClient : StreamingClient
    {
        public Player2StreamingClient(IAIClient client) : base(client) { }

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
            var session = await Player2Session.Current.EnsureAuthenticatedAsync(
                new Player2AuthRequest { FallbackApiKey = fallbackApiKey });
            if (!session.Succeeded)
            {
                throw new InvalidOperationException(
                    session.Error ?? "Player2 not available: no local app detected and no API key configured");
            }

            QuestLogger.Debug(session.IsLocal
                ? "Player2: Using local app connection"
                : "Player2: Using remote connection with API key");

            string endpointUrl = Player2Endpoints.ChatCompletions(session.BaseUrl);

            // Build request JSON
            string jsonContent = BuildRequestJson(instruction, messages, stream: true);

            // Create stream handler with callback
            var streamHandler = new Player2StreamHandler(SafeChunkCallback(onTextChunkReceived));

            // Send request
            await SendRequestAsync(endpointUrl, jsonContent, session.ApiKey, streamHandler, session.IsLocal);

            return new Payload(
                session.BaseUrl,
                null,
                jsonContent,
                streamHandler.GetFullText(),
                streamHandler.GetTotalTokens()
            );
        }

        public static void ClearLocalKeyCache()
        {
            Player2Session.Current.Invalidate("quests-stream");
        }

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
                RimAiLog.Info(RimAiLogCategory.Quests, 
                    $"[RimAI.Quests] Request URL ({(isLocal ? "local" : "remote")}): {url}"
                );
            }

            QuestLogger.Debug(
                $"Player2 API request ({(isLocal ? "local" : "remote")}): {url}\n{jsonContent}"
            );

            var extraHeaders = new Dictionary<string, string>
            {
                [Player2GameKeys.HeaderName] = Player2GameKeys.Canonical
            };
            const float connectTimeout = 60f;
            const float readTimeout = 60f;
            var http = await SendJsonPostAsync(
                url,
                jsonContent,
                apiKey,
                extraHeaders,
                streamHandler.AppendUtf8,
                connectTimeout,
                readTimeout,
                "quests-player2");

            if (http.Cancelled && string.Equals(http.ErrorMessage, "game-exit", StringComparison.Ordinal))
                return null;

            if (http.TimedOut)
                throw new TimeoutException(http.ErrorMessage ?? "Player2 request timed out");

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

            if (HasTransportError(http))
            {
                ThrowRequestFailed(
                    http,
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
    }
}
