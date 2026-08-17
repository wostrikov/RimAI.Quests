using System.Collections.Generic;
using Ustas.RimAI.Core.UI;

namespace Ustas.RimAI.Quests;

/// <summary>Defaults, reset, and validation for Quests settings. No Verse types.</summary>
public static class QuestSettingsPolicy
{
    public static string ResolveInstruction(string value, string defaultInstruction)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultInstruction ?? "" : value;
    }

    public static SettingsValidationResult Validate(QuestSettingsModel model)
    {
        if (model == null)
            return SettingsValidationResult.Valid;
        var issues = new List<SettingsValidationIssue>();
        if (!model.EnableAIDescriptions)
        {
            issues.Add(new SettingsValidationIssue(
                SettingsValidationSeverity.Warning,
                QuestSettingsCatalog.DisabledWarningMessage,
                QuestSettingsCatalog.FieldEnable,
                QuestSettingsCatalog.GeneralPage));
        }

        if (string.IsNullOrWhiteSpace(model.CustomQuestInstruction))
        {
            issues.Add(new SettingsValidationIssue(
                SettingsValidationSeverity.Warning,
                QuestSettingsCatalog.EmptyInstructionMessage,
                QuestSettingsCatalog.FieldInstruction,
                QuestSettingsCatalog.InstructionPage));
        }
        else if (QuestSettingsLegacy.IsUnchangedLegacyDefault(model.CustomQuestInstruction))
        {
            issues.Add(new SettingsValidationIssue(
                SettingsValidationSeverity.Warning,
                QuestSettingsCatalog.LegacyInstructionMessage,
                QuestSettingsCatalog.FieldInstruction,
                QuestSettingsCatalog.InstructionPage));
        }

        return SettingsValidationResult.FromIssues(issues);
    }

    public static QuestSettingsModel ApplyReset(QuestSettingsModel current, SettingsResetRequest request)
    {
        var next = current?.Clone() ?? QuestSettingsModel.Default();
        if (request == null)
            return next;
        var resetGeneral = request.Scope == SettingsResetScope.All ||
                           IsPage(request, QuestSettingsCatalog.GeneralPage) ||
                           IsSection(request, "general");
        var resetInstruction = request.Scope == SettingsResetScope.All ||
                               IsPage(request, QuestSettingsCatalog.InstructionPage) ||
                               IsSection(request, "instruction");
        if (resetGeneral)
        {
            var defaults = QuestSettingsModel.Default();
            next.EnableAIDescriptions = defaults.EnableAIDescriptions;
            next.VerboseDebugLogging = defaults.VerboseDebugLogging;
            next.CleanThinkTagsDuringStreaming = defaults.CleanThinkTagsDuringStreaming;
        }

        if (resetInstruction)
            next.CustomQuestInstruction = QuestSettingsModel.Default().CustomQuestInstruction;
        return next;
    }

    static bool IsPage(SettingsResetRequest request, SettingsPageId pageId) =>
        request.Scope == SettingsResetScope.Page &&
        request.PageId.HasValue &&
        request.PageId.Value.Equals(pageId);

    static bool IsSection(SettingsResetRequest request, string sectionId) =>
        request.Scope == SettingsResetScope.Section &&
        string.Equals(request.SectionId, sectionId, System.StringComparison.Ordinal);
}
