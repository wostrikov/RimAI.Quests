using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ustas.RimAI.Communication;
using Ustas.RimAI.Communication.Client;
using Ustas.RimAI.Communication.Client.OpenAI;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Util;
using Ustas.RimAI.Quests.Util;
using Ustas.RimAI.Core.AI;
using Verse;
using Ustas.RimAI.Core.Diagnostics;

namespace Ustas.RimAI.Quests.Services.Streaming
{
    /// <summary>
    /// Plain text streaming client for OpenAI-compatible APIs
    /// </summary>
    public class OpenAIStreamingClient : StreamingClient
    {
        public OpenAIStreamingClient(IAIClient client) : base(client) { }

        /// <summary>
        /// Stream chat completion using settings from RimTalk configuration
        /// </summary>
        public override async Task<Payload> StreamFromSettingsAsync(
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            var prefixMessages = new List<(Role role, string message)>();
            if (!string.IsNullOrWhiteSpace(instruction))
                prefixMessages.Add((Role.System, instruction));

            // RimTalk owns provider, credential, model and request-adapter semantics.
            // Its official OpenAI adapter currently returns one complete response,
            // which is still a valid text chunk for the quest's progressive UI.
            var payload = await Client.GetChatCompletionAsync(prefixMessages, messages);
            SafeChunkCallback(onTextChunkReceived)(payload?.Response);
            return payload;
        }

        /// <summary>
        /// Stream chat completion from OpenAI-compatible APIs with explicit parameters
        /// </summary>
        public async Task<Payload> StreamAsync(
            string baseUrl,
            string model,
            string apiKey,
            Dictionary<string, string> extraHeaders,
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        )
        {
            // Build endpoint URL
            string endpointUrl = FormatEndpointUrl(baseUrl);

            // Build request JSON
            string jsonContent = BuildRequestJson(instruction, messages, model, stream: true);

            var chunk = SafeChunkCallback(onTextChunkReceived);
            var shared = await Task.Run(() => SharedTextAiOrchestrator.Stream(new TextAiRequest
            {
                PrebuiltJson = jsonContent,
                BaseUrl = endpointUrl,
                ApiKey = apiKey,
                ExtraHeaders = extraHeaders,
                UseSharedGameplayCredential = false,
                ApiShape = TextAiApiShape.ChatCompletions,
                Stream = true,
                Model = model,
                Caller = "quests",
                Arbitration = AiRequestMetadata.FromCaller("quests", streaming: true)
            }, ev =>
            {
                if (!string.IsNullOrEmpty(ev.Delta))
                    chunk(ev.Delta);
            }));

            if (!shared.Succeeded)
                throw new Exception(shared.Error ?? "Shared streaming transport failed");

            return new Payload(
                endpointUrl,
                model,
                jsonContent,
                shared.Text,
                0
            );
        }

        #region Private Helper Methods

        private static string FormatEndpointUrl(string baseUrl)
        {
            const string defaultPath = "/v1/chat/completions";

            if (string.IsNullOrEmpty(baseUrl))
                return string.Empty;

            var trimmed = baseUrl.Trim().TrimEnd('/');
            var uri = new Uri(trimmed);

            return (uri.AbsolutePath == "/" || string.IsNullOrEmpty(uri.AbsolutePath.Trim('/')))
                ? trimmed + defaultPath
                : trimmed;
        }

        private static string BuildRequestJson(
            string instruction,
            List<(Role role, string message)> messages,
            string model,
            bool stream
        )
        {
            var normalized = BuildNormalizedMessages(
                instruction,
                messages,
                mergeConsecutiveSameRole: false
            );
            var allMessages = normalized
                .Select(
                    m => new Ustas.RimAI.Communication.Client.OpenAI.Message { Role = m.role, Content = m.content }
                )
                .ToList();

            var request = new OpenAIRequest
            {
                Model = model,
                Messages = allMessages,
                Stream = stream,
                StreamOptions = stream ? new StreamOptions { IncludeUsage = true } : null
            };

            return JsonUtil.SerializeToJson(request);
        }

        private async Task<string> SendRequestAsync(
            string endpointUrl,
            string jsonContent,
            string apiKey,
            Dictionary<string, string> extraHeaders,
            OpenAIStreamHandler streamHandler
        )
        {
            if (string.IsNullOrEmpty(endpointUrl))
            {
                QuestLogger.Error("Endpoint URL is missing.");
                throw new InvalidOperationException("Endpoint URL is missing");
            }

            if (Prefs.DevMode)
            {
                RimAiLog.Info(RimAiLogCategory.Quests, $"[RimAI.Quests] Request URL: {endpointUrl}");
            }

            QuestLogger.Debug($"API request: {endpointUrl}\n{jsonContent}");

            bool isLocal =
                endpointUrl.Contains("localhost")
                || endpointUrl.Contains("127.0.0.1")
                || endpointUrl.Contains("192.168.")
                || endpointUrl.Contains("10.");

            float connectTimeout = isLocal ? 300f : 60f;
            float readTimeout = 60f;
            var http = await SendJsonPostAsync(
                endpointUrl,
                jsonContent,
                apiKey,
                extraHeaders,
                streamHandler.AppendUtf8,
                connectTimeout,
                readTimeout,
                "quests-openai");

            if (http.Cancelled && string.Equals(http.ErrorMessage, "game-exit", StringComparison.Ordinal))
                return null;

            if (http.TimedOut)
                throw new TimeoutException(http.ErrorMessage ?? "Request timed out");

            if (HasTransportError(http))
            {
                ThrowRequestFailed(http, "Request failed");
            }

            QuestLogger.Debug($"API response: \n{streamHandler.GetRawJson()}");
            return streamHandler.GetFullText();
        }

        #endregion
    }
}
