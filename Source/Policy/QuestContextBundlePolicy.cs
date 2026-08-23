using System.Text;

namespace Ustas.RimAI.Quests.Policy
{
    public sealed class QuestContextBundle
    {
        public string? Title;
        public string? Description;
        public string? Type;
        public string? Challenge;
        public string? RewardsBlock;
        public string? Weather;
        public string? Wealth;
        public string? SceneBlock;
        public string? FactionsBlock;
    }

    /// <summary>
    /// Authoritative quest-prompt facets: type, rating, rewards, wealth,
    /// weather, and factions. Hosts collect Verse values; this type owns the
    /// assembled prompt shape.
    /// </summary>
    public static class QuestContextBundlePolicy
    {
        public const string TypeMarker = "Type:";
        public const string ChallengeMarker = "Challenge:";
        public const string RewardsMarker = "--- Quest Rewards ---";
        public const string WeatherMarker = "Weather:";
        public const string WealthMarker = "Wealth:";
        public const string FactionsMarker = "--- Faction Context ---";

        public static bool HasRequiredFacets(string prompt)
        {
            return Contains(prompt, TypeMarker)
                && Contains(prompt, ChallengeMarker)
                && Contains(prompt, RewardsMarker)
                && Contains(prompt, WeatherMarker)
                && Contains(prompt, WealthMarker)
                && Contains(prompt, FactionsMarker);
        }

        public static string Assemble(QuestContextBundle bundle)
        {
            bundle ??= new QuestContextBundle();
            var sb = new StringBuilder();
            sb.AppendLine("Quest Title: " + (bundle.Title ?? string.Empty));
            sb.AppendLine("Quest Description: " + (bundle.Description ?? string.Empty));
            sb.AppendLine();

            if (!string.IsNullOrEmpty(bundle.Type))
                sb.AppendLine(TypeMarker + " " + bundle.Type);
            if (!string.IsNullOrEmpty(bundle.Challenge))
                sb.AppendLine(ChallengeMarker + " " + bundle.Challenge);
            if (!string.IsNullOrEmpty(bundle.RewardsBlock))
            {
                sb.AppendLine();
                sb.Append(bundle.RewardsBlock.TrimEnd());
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(bundle.SceneBlock))
            {
                sb.AppendLine();
                sb.Append(bundle.SceneBlock.TrimEnd());
                sb.AppendLine();
            }
            else
            {
                if (!string.IsNullOrEmpty(bundle.Weather))
                    sb.AppendLine(WeatherMarker + " " + bundle.Weather);
                if (!string.IsNullOrEmpty(bundle.Wealth))
                    sb.AppendLine(WealthMarker + " " + bundle.Wealth);
            }

            if (!string.IsNullOrEmpty(bundle.FactionsBlock))
            {
                sb.AppendLine();
                sb.Append(bundle.FactionsBlock.TrimEnd());
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static bool Contains(string text, string marker) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(marker, System.StringComparison.Ordinal) >= 0;
    }
}
