namespace Ustas.RimAI.Quests;

/// <summary>Renderer-neutral snapshot of quest settings. Persistence stays on QuestSettings.</summary>
public sealed class QuestSettingsModel
{
    public bool EnableAIDescriptions { get; set; } = true;
    public bool VerboseDebugLogging { get; set; }
    public bool CleanThinkTagsDuringStreaming { get; set; }
    public string CustomQuestInstruction { get; set; } = "";

    public static QuestSettingsModel Default() => new();

    public QuestSettingsModel Clone()
    {
        return new QuestSettingsModel
        {
            EnableAIDescriptions = EnableAIDescriptions,
            VerboseDebugLogging = VerboseDebugLogging,
            CleanThinkTagsDuringStreaming = CleanThinkTagsDuringStreaming,
            CustomQuestInstruction = CustomQuestInstruction ?? ""
        };
    }
}
