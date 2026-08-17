using Verse;

namespace Ustas.RimAI.Quests;

public class QuestSettings : ModSettings
{
    public bool enableAIDescriptions = true;
    public bool verboseDebugLogging = false;
    public bool cleanThinkTagsDuringStreaming = false;
    public string customQuestInstruction = "";

    public QuestSettingsModel ToModel()
    {
        return new QuestSettingsModel
        {
            EnableAIDescriptions = enableAIDescriptions,
            VerboseDebugLogging = verboseDebugLogging,
            CleanThinkTagsDuringStreaming = cleanThinkTagsDuringStreaming,
            CustomQuestInstruction = customQuestInstruction ?? ""
        };
    }

    public void CopyFrom(QuestSettingsModel model)
    {
        if (model == null)
            return;
        enableAIDescriptions = model.EnableAIDescriptions;
        verboseDebugLogging = model.VerboseDebugLogging;
        cleanThinkTagsDuringStreaming = model.CleanThinkTagsDuringStreaming;
        customQuestInstruction = model.CustomQuestInstruction ?? "";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref enableAIDescriptions, "enableAIDescriptions", true);
        Scribe_Values.Look(ref verboseDebugLogging, "verboseDebugLogging", false);
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
