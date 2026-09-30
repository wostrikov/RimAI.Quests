namespace Ustas.RimAI.Quests.Policy
{
    public sealed class QuestDescriptionOutcome
    {
        public string Text = string.Empty;
        public bool Replaced;
        public bool Restored;
    }

    /// <summary>
    /// Authoritative quest-description mutation: the AI narrative takes the
    /// original's place, and the original stays when there is no narrative.
    /// Mechanical quest fields stay outside this type.
    ///
    /// This used to append the narrative under a separator and keep the
    /// original above it. The owner read that as every quest telling its
    /// story twice - once as the game wrote it, sometimes half in English,
    /// and once again in other words - which is what it was. The narrative is
    /// asked to carry every fact of the original for that reason.
    /// </summary>
    public static class QuestDescriptionPolicy
    {
        public static string Compose(string original, string enhancement)
        {
            if (string.IsNullOrWhiteSpace(enhancement))
                return original ?? string.Empty;
            return enhancement.Trim();
        }

        public static string Restore(string original)
        {
            return original ?? string.Empty;
        }

        public static QuestDescriptionOutcome Apply(string original, string enhancement, bool failed)
        {
            original = original ?? string.Empty;
            if (failed || string.IsNullOrWhiteSpace(enhancement))
            {
                return new QuestDescriptionOutcome
                {
                    Text = Restore(original),
                    Replaced = false,
                    Restored = true
                };
            }

            return new QuestDescriptionOutcome
            {
                Text = Compose(original, enhancement),
                Replaced = true,
                Restored = false
            };
        }
    }
}
