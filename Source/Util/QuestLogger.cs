using Ustas.RimAI.Core.Diagnostics;

namespace Ustas.RimAI.Quests.Util;

public static class QuestLogger
{
    public static void Message(object message) =>
        RimAiLog.Info(RimAiLogCategory.Quests, message?.ToString() ?? string.Empty);

    public static void Debug(object message) =>
        RimAiLog.Debug(RimAiLogCategory.Quests, message?.ToString() ?? string.Empty);

    public static void Warning(object message) =>
        RimAiLog.Warning(RimAiLogCategory.Quests, message?.ToString() ?? string.Empty);

    public static void Error(object message) =>
        RimAiLog.Error(RimAiLogCategory.Quests, message?.ToString() ?? string.Empty);

    public static void ErrorOnce(object text, int key) =>
        RimAiLog.ErrorOnce(RimAiLogCategory.Quests, text?.ToString() ?? string.Empty, key);
}
