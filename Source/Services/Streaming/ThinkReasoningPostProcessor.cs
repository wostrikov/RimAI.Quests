using System.Text;
using Ustas.RimAI.Quests.Policy;

namespace Ustas.RimAI.Quests.Services.Streaming
{
    /// <summary>
    /// Post-processes streamed text and removes closed think/reasoning blocks.
    /// Unclosed blocks are preserved until completion so "thinking" can be shown temporarily.
    /// </summary>
    public class ThinkReasoningPostProcessor
    {
        private readonly StringBuilder _rawBuffer = new StringBuilder();
        private string _cachedProcessed = string.Empty;
        private bool _dirty = true;

        public void AppendChunk(string chunk)
        {
            if (string.IsNullOrEmpty(chunk))
            {
                return;
            }

            _rawBuffer.Append(chunk);
            _dirty = true;
        }

        public string GetRawText()
        {
            return _rawBuffer.ToString();
        }

        public string GetProcessedText()
        {
            if (!_dirty)
            {
                return _cachedProcessed;
            }

            string processed = QuestResponseSanitizePolicy.StripThinkBlocks(_rawBuffer.ToString());
            _cachedProcessed = processed;
            _dirty = false;
            return _cachedProcessed;
        }

        public string ProcessFinal(string fullText)
        {
            _rawBuffer.Clear();
            _rawBuffer.Append(fullText ?? string.Empty);
            _dirty = true;
            return GetProcessedText();
        }
    }
}
