using Verse;

namespace Ustas.RimAI.Quests
{
    public static class Constant
    {
        public static string GetLegacyEnglishQuestInstruction(string languageName)
        {
            return $@"You are enhancing a RimWorld quest description.
Your task is NOT to summarize or rewrite mechanically,
but to add narrative weight and implied motivation.

Writing goals:
1. Expand vague quest elements into short in-universe narrative.
2. Avoid making the quest feel like a pure reward transaction.
3. Emphasize uncertainty, intention, or quiet tension where appropriate.
4. Match RimWorld's restrained, grounded sci-fi tone (no epic fantasy).

Constraints:
- Write in {languageName}
- Write 2–3 short paragraphs.
- Do NOT invent new gameplay mechanics or outcomes.
- Do NOT contradict the original quest text.
- Subtext is preferred over explicit exposition.
- The visitor should feel like a person with intent, not loot.
- PRESERVE all <color> tags from the original quest description exactly as they appear.
- When mentioning highlighted elements (names, items, numbers), use the same <color> tags.

Use the current scene and faction context when relevant,
but do not repeat raw data (dates, stats) directly.";
        }

        public static string GetDefaultQuestInstruction()
        {
            string languageName = LanguageDatabase.activeLanguage?.info?.friendlyNameNative
                ?? "Українська";
            return $@"Ти доповнюєш опис завдання RimWorld.
Твоє завдання — НЕ підсумовувати й не переписувати текст механічно,
а додати йому оповідної ваги та неявної мотивації.

Цілі тексту:
1. Розгорни нечіткі елементи завдання в коротку внутрішньосвітову оповідь.
2. Не зводь завдання до простої угоди за винагороду.
3. Де доречно, підкреслюй невизначеність, намір або приховану напругу.
4. Дотримуйся стриманого, приземленого науково-фантастичного тону RimWorld (без епічного фентезі).

Обмеження:
- Пиши мовою: {languageName}.
- Напиши 2–3 короткі абзаци.
- НЕ вигадуй нових ігрових механік або наслідків.
- НЕ супереч оригінальному тексту завдання.
- Віддавай перевагу підтексту, а не прямому поясненню.
- Відвідувач має сприйматися як особа з власним наміром, а не як здобич.
- ЗБЕРЕЖИ всі теги <color> з оригінального опису завдання точно в незмінному вигляді.
- Згадуючи виділені елементи (імена, предмети, числа), використовуй ті самі теги <color>.

Коли доречно, використовуй контекст поточної сцени та фракції,
але не повторюй безпосередньо сирі дані (дати, характеристики).";
        }
    }
}
