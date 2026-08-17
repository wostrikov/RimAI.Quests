using System;
using System.Collections.Generic;
using Ustas.RimAI.Core.UI;

namespace Ustas.RimAI.Quests;

/// <summary>Stable Quests settings page and field identities for the shared UI shell.</summary>
public static class QuestSettingsCatalog
{
    public static readonly SettingsPageId GeneralPage = SettingsPageId.From("general");
    public static readonly SettingsPageId InstructionPage = SettingsPageId.From("instruction");

    public const string FieldEnable = "enable";
    public const string FieldVerbose = "verbose";
    public const string FieldThinkTags = "think-tags";
    public const string FieldCache = "cache";
    public const string FieldProcessing = "processing";
    public const string FieldSharedConfig = "shared-config";
    public const string FieldInstruction = "instruction";

    public const string EmptyInstructionMessage = "Ustas.RimAI.Quests.Settings.Validation.EmptyInstruction";
    public const string DisabledWarningMessage = "Ustas.RimAI.Quests.Settings.Validation.Disabled";
    public const string LegacyInstructionMessage = "Ustas.RimAI.Quests.Settings.Validation.LegacyInstruction";

    public static IReadOnlyList<SettingsPageDescriptor> CreatePages(string generalTitle, string instructionTitle)
    {
        return new[]
        {
            new SettingsPageDescriptor(GeneralPage, generalTitle, GeneralKeywords),
            new SettingsPageDescriptor(InstructionPage, instructionTitle, InstructionKeywords)
        };
    }

    public static IReadOnlyList<string> GeneralKeywords { get; } =
        new[] { "general", "ai", "debug", "cache", "logging", "enable" };

    public static IReadOnlyList<string> InstructionKeywords { get; } =
        new[] { "instruction", "prompt", "quest", "custom" };

    public static IReadOnlyList<string> KeywordsFor(string fieldId)
    {
        switch (fieldId)
        {
            case FieldEnable:
                return new[] { "enable", "ai", "descriptions", "quest" };
            case FieldVerbose:
                return new[] { "verbose", "debug", "logging" };
            case FieldThinkTags:
                return new[] { "think", "reasoning", "tags", "streaming" };
            case FieldCache:
                return new[] { "cache", "clear" };
            case FieldProcessing:
                return new[] { "processing", "quests", "count" };
            case FieldSharedConfig:
                return new[] { "rimai", "config", "shared", "communication" };
            case FieldInstruction:
                return new[] { "instruction", "prompt", "custom", "quest" };
            default:
                return Array.Empty<string>();
        }
    }

    public static bool IsFieldVisible(string fieldId, SettingsSearchState search)
    {
        if (search == null || search.IsEmpty)
            return true;
        return search.MatchesAny(KeywordsFor(fieldId)) || search.Matches(fieldId);
    }
}
