using System;

namespace Ustas.RimAI.Quests.Policy
{
    public sealed class QuestAppendOutcome
    {
        public string Text = string.Empty;
        public bool Appended;
        public bool Restored;
        public bool OriginalPreserved;
    }

    /// <summary>
    /// Authoritative quest-description mutation: append AI narrative under a
    /// separator, never replace the original, restore the original on failure.
    /// Mechanical quest fields stay outside this type.
    /// </summary>
    public static class QuestAppendPolicy
    {
        public const string Separator = "───────────";

        public static string Compose(string original, string enhancement)
        {
            original = original ?? string.Empty;
            if (string.IsNullOrWhiteSpace(enhancement))
                return original;
            return original + "\n\n" + Separator + "\n\n" + enhancement.TrimEnd();
        }

        public static string Restore(string original)
        {
            return original ?? string.Empty;
        }

        public static QuestAppendOutcome Apply(string original, string enhancement, bool failed)
        {
            original = original ?? string.Empty;
            if (failed || string.IsNullOrWhiteSpace(enhancement))
            {
                return new QuestAppendOutcome
                {
                    Text = Restore(original),
                    Appended = false,
                    Restored = true,
                    OriginalPreserved = true
                };
            }

            string text = Compose(original, enhancement);
            return new QuestAppendOutcome
            {
                Text = text,
                Appended = true,
                Restored = false,
                OriginalPreserved = text.StartsWith(original, StringComparison.Ordinal)
                    && text.IndexOf(Separator, StringComparison.Ordinal) >= original.Length
            };
        }
    }
}
