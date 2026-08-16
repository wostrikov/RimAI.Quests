using System;
using Ustas.RimAI.Communication.Client;
using Ustas.RimAI.Communication.Client.OpenAI;
using Ustas.RimAI.Communication.Client.Player2;

namespace Ustas.RimAI.Quests.Services.Streaming
{
    public static class StreamingClientFactory
    {
        public static StreamingClient Create(IAIClient client)
        {
            if (client is OpenAIClient)
            {
                return new OpenAIStreamingClient(client);
            }

            if (client is Player2Client)
            {
                return new Player2StreamingClient(client);
            }

            throw new NotSupportedException(
                $"Client type {client?.GetType().Name ?? "Unknown"} is not supported for streaming"
            );
        }
    }
}
