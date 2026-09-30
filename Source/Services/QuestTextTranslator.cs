using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimAI.Core.Runtime;
using RimWorld;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Quests.Policy;
using Verse;

namespace Ustas.RimAI.Quests.Services
{
    /// <summary>
    /// Puts an old quest's own text into the language being played.
    ///
    /// A quest description is resolved once, when the quest is generated, and
    /// scribed. So a quest offered before a translation existed keeps the
    /// English it was born with for as long as the save lasts, and no amount
    /// of fixing the language pack touches it - which is what a colonist
    /// reading "has captured 2 prisoners" under a Ukrainian heading actually
    /// means.
    ///
    /// A book in this situation is rebuilt from its rules. A quest cannot be:
    /// RimWorld keeps no slate after generation, only `lastSlateStateDebug`,
    /// which is a dump for a human to read. The text in the save is all there
    /// is. So this translates it rather than regenerating it, once per quest,
    /// and remembers that it has.
    ///
    /// What it will not do is rewrite the quest - that is the description
    /// generator's job. A translation is the same description in another
    /// language, so it is checked for the markup and the paragraphs the game
    /// wrote and refused if either moved.
    /// </summary>
    public static class QuestTextTranslator
    {
        private static readonly HashSet<int> InFlight = new HashSet<int>();

        internal static string Instruction(string language) =>
            "Translate this RimWorld quest text into " + language + ".\n\n"
            + "Keep every <color=...> tag exactly where it is, around exactly the same "
            + "words - they mark names the game colours.\n"
            + "Keep the paragraph breaks.\n"
            + "Keep every number, date and duration as it stands.\n"
            + "Proper names of people and factions stay as they are written.\n"
            + "This is a contract the player reads before accepting, so translate what "
            + "it says and invent nothing: no extra sentences, no explanations, no "
            + "commentary. Answer with the translated text alone.";

        public static void TranslateIfNeeded(Quest quest)
        {
            if (quest == null || quest.hidden)
            {
                return;
            }

            QuestTextTranslationTracker tracker = QuestTextTranslationTracker.Current;
            if (tracker == null || tracker.AlreadyTranslated(quest.id) || InFlight.Contains(quest.id))
            {
                return;
            }

            LoadedLanguage language = LanguageDatabase.activeLanguage;
            if (language == null || language.folderName == "English")
            {
                return;
            }

            bool cyrillic = language.folderName.StartsWith("Ukrainian", StringComparison.Ordinal)
                || language.folderName.StartsWith("Russian", StringComparison.Ordinal);

            string original = quest.description.ToString();
            if (!QuestTextLanguagePolicy.NeedsTranslation(original, cyrillic))
            {
                // Already in the player's language, or a language this cannot
                // judge. Either way it is not ours to touch, and saying so now
                // means never asking about it again.
                tracker.Remember(quest.id);
                return;
            }

            Ask(quest, original, language.FriendlyNameEnglish, cyrillic, tracker);
        }

        private static void Ask(Quest quest, string original, string language, bool cyrillic,
            QuestTextTranslationTracker tracker)
        {
            int questId = quest.id;
            InFlight.Add(questId);

            RimAiBackground.Run(async () =>
            {
                string answer = null;
                try
                {
                    answer = await QuestDescriptionGenerator.TranslateAsync(
                        Instruction(language), original, quest);
                }
                // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY - top of a background task; an unobserved exception here would take the game down.
                catch (Exception exception)
                {
                    RimAiLog.Warning(RimAiLogCategory.Quests,
                        "[RimAI.Quests] Quest text translation failed.", exception: exception);
                }

                string translated = answer?.Trim();
                tracker.RunOnMainThread(() =>
                    Store(questId, original, translated, cyrillic, tracker));
            });
        }

        private static void Store(int questId, string original, string translated, bool cyrillic,
            QuestTextTranslationTracker tracker)
        {
            InFlight.Remove(questId);
            Quest quest = Find.QuestManager?.QuestsListForReading?.Find(q => q.id == questId);
            if (quest == null)
            {
                return;
            }

            string rejected = QuestTextLanguagePolicy.Rejected(original, translated, cyrillic);
            if (rejected != null)
            {
                RimAiLog.Warning(RimAiLogCategory.Quests,
                    "[RimAI.Quests] Quest " + questId + " left untranslated: " + rejected);
                tracker.Remember(questId);
                return;
            }

            // A new quest is also handed to the description generator, whose
            // narrative takes the original's place and is written in the
            // player's language already. If it got there first, the text this
            // translated is gone, and writing the translation would put the
            // original back over the narrative.
            if (!string.Equals(quest.description.ToString(), original, StringComparison.Ordinal))
            {
                tracker.Remember(questId);
                return;
            }

            quest.description = new TaggedString(translated);
            tracker.Remember(questId);
            RimAiLog.Debug(RimAiLogCategory.Quests,
                "[RimAI.Quests] Quest " + questId + " translated into the active language.");
        }
    }
}
