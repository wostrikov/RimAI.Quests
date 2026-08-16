using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ustas.RimAI.Communication.Client;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Core.Net;
using Ustas.RimAI.Quests.Util;
using Verse;

namespace Ustas.RimAI.Quests.Services.Streaming
{
    /// <summary>
    /// Base class for provider-specific streaming clients.
    /// </summary>
    public abstract class StreamingClient
    {
        protected StreamingClient(IAIClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected IAIClient Client { get; }

        public abstract Task<Payload> StreamFromSettingsAsync(
            string instruction,
            List<(Role role, string message)> messages,
            Action<string> onTextChunkReceived
        );

        protected static Action<string> SafeChunkCallback(Action<string> callback)
        {
            return chunk =>
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    callback?.Invoke(chunk);
                }
            };
        }

        protected static async Task<HttpTransportResponse> SendJsonPostAsync(
            string url,
            string jsonContent,
            string apiKey,
            Dictionary<string, string> extraHeaders,
            Action<string> onUtf8Chunk,
            float connectTimeoutSeconds,
            float readTimeoutSeconds,
            string correlationId
        )
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(apiKey))
                headers["Authorization"] = $"Bearer {apiKey}";
            if (extraHeaders != null)
            {
                foreach (var header in extraHeaders)
                    headers[header.Key] = header.Value;
            }

            using var cts = new CancellationTokenSource();
            var send = SharedHttpTransport.Current.SendAsync(
                new HttpTransportRequest
                {
                    Method = "POST",
                    Url = url,
                    Headers = headers,
                    Body = jsonContent,
                    ContentType = "application/json",
                    TimeoutMilliseconds = (int)((connectTimeoutSeconds + readTimeoutSeconds) * 1000),
                    FirstByteTimeoutMilliseconds = (int)(connectTimeoutSeconds * 1000),
                    IdleTimeoutMilliseconds = (int)(readTimeoutSeconds * 1000),
                    CorrelationId = correlationId
                },
                onUtf8Chunk,
                cts.Token);

            while (!send.IsCompleted)
            {
                if (Current.Game == null)
                {
                    cts.Cancel();
                    return HttpTransportResponse.Fail(
                        HttpTransportErrorKind.Cancelled,
                        "game-exit",
                        SharedHttpTransport.Current.Kind);
                }

                await Task.Delay(100);
            }

            return await send;
        }

        protected static bool HasTransportError(HttpTransportResponse response)
        {
            return response == null
                || !response.Succeeded
                || response.ErrorKind == HttpTransportErrorKind.NetworkFailure
                || response.ErrorKind == HttpTransportErrorKind.HttpFailure
                || response.ErrorKind == HttpTransportErrorKind.Timeout;
        }

        protected static List<(string role, string content)> BuildNormalizedMessages(
            string instruction,
            List<(Role role, string message)> messages,
            bool mergeConsecutiveSameRole
        )
        {
            var normalized = new List<(string role, string content)>();

            if (!string.IsNullOrEmpty(instruction))
            {
                normalized.Add(("system", instruction));
            }

            if (messages == null || messages.Count == 0)
            {
                return normalized;
            }

            foreach (var message in messages)
            {
                string role = message.role == Role.User ? "user" : "assistant";
                string content = message.message ?? string.Empty;

                if (
                    mergeConsecutiveSameRole
                    && normalized.Count > 0
                    && normalized[normalized.Count - 1].role == role
                )
                {
                    var last = normalized[normalized.Count - 1];
                    normalized[normalized.Count - 1] = (last.role, last.content + "\n\n" + content);
                }
                else
                {
                    normalized.Add((role, content));
                }
            }

            return normalized;
        }

        protected static void ThrowRequestFailed(
            HttpTransportResponse response,
            string logPrefix,
            Action onBeforeThrow = null
        )
        {
            onBeforeThrow?.Invoke();
            string errorMsg = response?.ErrorMessage;
            QuestLogger.Error($"{logPrefix}: {response?.StatusCode} - {errorMsg}");
            throw new Exception($"{logPrefix}: {errorMsg}");
        }
    }
}
