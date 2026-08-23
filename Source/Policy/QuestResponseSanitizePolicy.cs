using System.Text.RegularExpressions;

namespace Ustas.RimAI.Quests.Policy
{
    /// <summary>
    /// Authoritative LLM-output sanitize: strip think/reasoning blocks and
    /// keep RimWorld rich-text such as color tags.
    /// </summary>
    public static class QuestResponseSanitizePolicy
    {
        static readonly Regex[] ClosedBlockPatterns =
        {
            new Regex(
                @"<\s*(think|thought|thinking|reasoning|analysis)\b[^>]*>.*?<\s*/\s*\1\s*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
            ),
            new Regex(
                @"\[(think|thought|thinking|reasoning|analysis)\].*?\[/\1\]",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
            ),
            new Regex(
                @"```\s*(think|thought|thinking|reasoning|analysis)[^\n]*\n.*?```",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
            )
        };

        public static string StripThinkBlocks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? string.Empty;

            string processed = text;
            foreach (var regex in ClosedBlockPatterns)
                processed = regex.Replace(processed, string.Empty);
            return processed;
        }

        public static bool PreservesRichText(string sanitized)
        {
            return !string.IsNullOrEmpty(sanitized)
                && sanitized.IndexOf("<color", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
