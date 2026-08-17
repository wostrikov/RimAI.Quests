using Ustas.RimAI.Core.UI;
using Xunit;

namespace Ustas.RimAI.Quests.Tests;

public sealed class QuestSettingsPolicyTests
{
    [Fact]
    public void Defaults_match_new_settings_fields()
    {
        var model = QuestSettingsModel.Default();
        Assert.True(model.EnableAIDescriptions);
        Assert.False(model.VerboseDebugLogging);
        Assert.False(model.CleanThinkTagsDuringStreaming);
        Assert.Equal("", model.CustomQuestInstruction);
    }

    [Fact]
    public void Mapping_clone_does_not_alias_instruction_state()
    {
        var original = new QuestSettingsModel { CustomQuestInstruction = "keep" };
        var clone = original.Clone();
        clone.CustomQuestInstruction = "changed";
        clone.EnableAIDescriptions = false;
        Assert.Equal("keep", original.CustomQuestInstruction);
        Assert.True(original.EnableAIDescriptions);
    }

    [Fact]
    public void Resolve_instruction_uses_default_for_empty_query()
    {
        Assert.Equal("DEFAULT", QuestSettingsPolicy.ResolveInstruction("  ", "DEFAULT"));
        Assert.Equal("kept", QuestSettingsPolicy.ResolveInstruction("kept", "DEFAULT"));
    }

    [Fact]
    public void Validate_disabled_and_empty_instruction_are_warnings()
    {
        var result = QuestSettingsPolicy.Validate(new QuestSettingsModel
        {
            EnableAIDescriptions = false,
            CustomQuestInstruction = " "
        });
        Assert.True(result.IsValid);
        Assert.True(result.HasWarnings);
        Assert.Equal(2, result.Issues.Count);
        Assert.Contains(result.Issues, issue => issue.Message == QuestSettingsCatalog.DisabledWarningMessage);
        Assert.Contains(result.Issues, issue => issue.Message == QuestSettingsCatalog.EmptyInstructionMessage);
    }

    [Fact]
    public void Validate_enabled_custom_instruction_is_valid()
    {
        var result = QuestSettingsPolicy.Validate(new QuestSettingsModel
        {
            EnableAIDescriptions = true,
            CustomQuestInstruction = "Write two paragraphs."
        });
        Assert.True(result.IsValid);
        Assert.False(result.HasWarnings);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Page_reset_does_not_clear_the_other_page()
    {
        var current = new QuestSettingsModel
        {
            EnableAIDescriptions = false,
            VerboseDebugLogging = true,
            CleanThinkTagsDuringStreaming = true,
            CustomQuestInstruction = "custom"
        };
        var page = QuestSettingsPolicy.ApplyReset(
            current,
            SettingsResetRequest.Page(QuestSettingsCatalog.GeneralPage));
        Assert.True(page.EnableAIDescriptions);
        Assert.False(page.VerboseDebugLogging);
        Assert.False(page.CleanThinkTagsDuringStreaming);
        Assert.Equal("custom", page.CustomQuestInstruction);
        Assert.False(current.EnableAIDescriptions);
        Assert.Equal("custom", current.CustomQuestInstruction);

        var instruction = QuestSettingsPolicy.ApplyReset(
            current,
            SettingsResetRequest.Page(QuestSettingsCatalog.InstructionPage));
        Assert.False(instruction.EnableAIDescriptions);
        Assert.Equal("", instruction.CustomQuestInstruction);
    }

    [Fact]
    public void All_reset_restores_defaults_without_mutating_source()
    {
        var current = new QuestSettingsModel
        {
            EnableAIDescriptions = false,
            VerboseDebugLogging = true,
            CustomQuestInstruction = "custom"
        };
        var next = QuestSettingsPolicy.ApplyReset(current, SettingsResetRequest.All());
        Assert.True(next.EnableAIDescriptions);
        Assert.False(next.VerboseDebugLogging);
        Assert.Equal("", next.CustomQuestInstruction);
        Assert.False(current.EnableAIDescriptions);
        Assert.Equal("custom", current.CustomQuestInstruction);
    }

    [Fact]
    public void Search_hides_unrelated_fields()
    {
        var search = SettingsSearchState.FromQuery("cache");
        Assert.True(QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldCache, search));
        Assert.False(QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldEnable, search));
        Assert.True(QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldEnable, SettingsSearchState.Empty));
    }

    [Fact]
    public void Pages_are_stable_and_searchable()
    {
        var pages = QuestSettingsCatalog.CreatePages("General", "Instruction");
        var navigation = SettingsNavigationState.Create(pages);
        Assert.Equal("general", navigation.CurrentPage.Value);
        Assert.Equal(QuestSettingsCatalog.InstructionPage, pages[1].Id);
        Assert.True(SettingsSearchState.FromQuery("prompt").MatchesPage(pages[1]));
    }

    [Fact]
    public void Legacy_hash_still_identifies_the_known_english_default()
    {
        Assert.Equal(
            "EA578CB442CB6F7675B5815147B859CAD25A69D1362A15F0D068AC2BAEB6E36D",
            QuestSettingsLegacy.UnchangedLegacyDefaultHash);
        Assert.False(QuestSettingsLegacy.IsUnchangedLegacyDefault(""));
        Assert.False(QuestSettingsLegacy.IsUnchangedLegacyDefault("custom"));
    }
}
