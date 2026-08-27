using Verse;

namespace Ustas.RimAI.Quests;

public class QuestSettings : ModSettings
{
    public bool enableAIDescriptions = true;
    public bool cleanThinkTagsDuringStreaming = false;
    public string customQuestInstruction = "";

    public QuestSettingsModel ToModel()
    {
        return new QuestSettingsModel
        {
            EnableAIDescriptions = enableAIDescriptions,
            CleanThinkTagsDuringStreaming = cleanThinkTagsDuringStreaming,
            CustomQuestInstruction = customQuestInstruction ?? ""
        };
    }

    public void CopyFrom(QuestSettingsModel model)
    {
        if (model == null)
            return;
        enableAIDescriptions = model.EnableAIDescriptions;
        cleanThinkTagsDuringStreaming = model.CleanThinkTagsDuringStreaming;
        customQuestInstruction = model.CustomQuestInstruction ?? "";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref enableAIDescriptions, "enableAIDescriptions", true);
        Scribe_Values.Look(
            ref cleanThinkTagsDuringStreaming,
            "cleanThinkTagsDuringStreaming",
            true);
        Scribe_Values.Look(ref customQuestInstruction, "customQuestInstruction", "");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && QuestSettingsLegacy.IsUnchangedLegacyDefault(customQuestInstruction))
        {
            customQuestInstruction = Constant.GetDefaultQuestInstruction();
            LongEventHandler.ExecuteWhenFinished(Write);
        }
    }
}
