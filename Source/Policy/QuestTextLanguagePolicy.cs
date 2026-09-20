using System;
using System.Text.RegularExpressions;

namespace Ustas.RimAI.Quests.Policy
{
    /// <summary>
    /// Whether a quest's stored text is still in the language it was written
    /// in rather than the one being played, and what may be done about it.
    ///
    /// A quest's description is resolved once, when the quest is generated,
    /// and scribed. RimWorld keeps no slate afterwards - only
    /// `lastSlateStateDebug`, a string dump - so the text cannot be resolved
    /// again from the rules the way a book's can. What is in the save is all
    /// there is, which is why this is a translation rather than a
    /// regeneration.
    ///
    /// The test is deliberately blunt: a run of Latin words outside the
    /// markup. A Ukrainian description carries Latin only inside tags and
    /// names, so three of them in a row is somebody's English sentence.
    /// Guessing wrong costs one model call and changes nothing else.
    /// </summary>
    public static class QuestTextLanguagePolicy
    {
        /// <summary>Markup and names, which are not evidence of a language.</summary>
        private static readonly Regex Markup = new Regex(
            @"<[^>]*>|\{[^}]*\}|\[[^\]]*\]", RegexOptions.Compiled);

        private static readonly Regex LatinRun = new Regex(
            @"\b[A-Za-z]{2,}\b(?:[^\p{L}]+\b[A-Za-z]{2,}\b){2,}", RegexOptions.Compiled);

        private static readonly Regex Cyrillic = new Regex(@"\p{IsCyrillic}", RegexOptions.Compiled);

        /// <summary>
        /// True when this text reads as a language other than the one the
        /// player is in, and is therefore worth asking about.
        /// </summary>
        public static bool NeedsTranslation(string text, bool playingCyrillicLanguage)
        {
            if (string.IsNullOrWhiteSpace(text) || !playingCyrillicLanguage)
            {
                return false;
            }

            string bare = Markup.Replace(text, " ");
            return LatinRun.IsMatch(bare);
        }

        /// <summary>
        /// None when the answer may replace the original, otherwise why not.
        ///
        /// What must survive is everything the game will read back: the colour
        /// tags it wrote, and the line breaks that separate the paragraphs a
        /// quest is laid out in. The answer also has to actually be in the
        /// target language - a model that hands the English back has told us
        /// it had nothing to do.
        /// </summary>
        public static string Rejected(string original, string answer, bool expectCyrillic)
        {
            if (string.IsNullOrWhiteSpace(answer))
            {
                return "empty";
            }

            if (CountTags(original) != CountTags(answer))
            {
                return "the markup tags differ";
            }

            if (expectCyrillic && !Cyrillic.IsMatch(answer))
            {
                return "left in the original language";
            }

            int originalBreaks = CountBreaks(original);
            int answerBreaks = CountBreaks(answer);
            if (answerBreaks < originalBreaks / 2)
            {
                return "the paragraphs were flattened";
            }

            return null;
        }

        private static int CountTags(string text) =>
            text == null ? 0 : new Regex("<[^>]*>").Matches(text).Count;

        private static int CountBreaks(string text) =>
            text == null ? 0 : new Regex("\n\n").Matches(text).Count;
    }
}
