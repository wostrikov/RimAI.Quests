using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ustas.RimAI.Communication.Client;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Quests.Util;
using UnityEngine.Networking;
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

        protected static UnityWebRequest CreateJsonPostRequest(
            string url,
            string jsonContent,
            DownloadHandler downloadHandler,
            string apiKey = null,
            Dictionary<string, string> extraHeaders = null
        )
        {
            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonContent));
            request.downloadHandler = downloadHandler;
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            }

            if (extraHeaders != null)
            {
                foreach (var header in extraHeaders)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            return request;
        }

        protected static async Task<bool> AwaitStreamingResponseAsync(
            UnityWebRequest webRequest,
            float connectTimeoutSeconds,
            float readTimeoutSeconds,
            Func<float, string> connectTimeoutMessageFactory,
            Func<float, string> readTimeoutMessageFactory
        )
        {
            var asyncOp = webRequest.SendWebRequest();

            float inactivityTimer = 0f;
            ulong lastBytes = 0;

            while (!asyncOp.isDone)
            {
                if (Current.Game == null)
                {
                    return false;
                }

                await Task.Delay(100);

                ulong currentBytes = webRequest.downloadedBytes;
                bool hasStartedReceiving = currentBytes > 0;

                if (currentBytes > lastBytes)
                {
                    inactivityTimer = 0f;
                    lastBytes = currentBytes;
                }
                else
                {
                    inactivityTimer += 0.1f;
                }

                if (!hasStartedReceiving && inactivityTimer > connectTimeoutSeconds)
                {
                    webRequest.Abort();
                    throw new TimeoutException(connectTimeoutMessageFactory(connectTimeoutSeconds));
                }

                if (hasStartedReceiving && inactivityTimer > readTimeoutSeconds)
                {
                    webRequest.Abort();
                    throw new TimeoutException(readTimeoutMessageFactory(readTimeoutSeconds));
                }
            }

            return true;
        }

        protected static bool HasTransportError(UnityWebRequest webRequest)
        {
            return webRequest.result == UnityWebRequest.Result.ConnectionError
                || webRequest.result == UnityWebRequest.Result.ProtocolError;
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
            UnityWebRequest webRequest,
            string logPrefix,
            Action onBeforeThrow = null
        )
        {
            onBeforeThrow?.Invoke();
            string errorMsg = webRequest.error;
            QuestLogger.Error($"{logPrefix}: {webRequest.responseCode} - {errorMsg}");
            throw new Exception($"{logPrefix}: {errorMsg}");
        }
    }
}
