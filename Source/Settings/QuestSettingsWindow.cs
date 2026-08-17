using RimWorld;
using UnityEngine;
using Ustas.RimAI.Core.UI;
using Ustas.RimAI.RimWorld.UI;
using Verse;

namespace Ustas.RimAI.Quests;

/// <summary>Quests settings renderer. Persistent data stays on QuestSettings.</summary>
public static class QuestSettingsWindow
{
    static SettingsUiState _state;
    static Vector2 _scrollPosition;

    public static void Draw(Rect inRect, QuestSettings settings)
    {
        if (settings == null)
            return;

        EnsureState(settings);
        var content = SettingsShellRenderer.DrawChrome(
            inRect,
            _state,
            SettingsShellLabels.FromKeyedTranslations(),
            out _state,
            out var resetRequest);
        if (resetRequest != null)
        {
            settings.CopyFrom(QuestSettingsPolicy.ApplyReset(settings.ToModel(), resetRequest));
            _state = _state.WithValidation(QuestSettingsPolicy.Validate(settings.ToModel()));
        }

        settings.customQuestInstruction = QuestSettingsPolicy.ResolveInstruction(
            settings.customQuestInstruction,
            Constant.GetDefaultQuestInstruction());
        _state = _state.WithValidation(QuestSettingsPolicy.Validate(settings.ToModel()));

        var search = _state.Search;
        var viewHeight = EstimateHeight(_state.Navigation.CurrentPage, search);
        SettingsShellRenderer.BeginScroll(content, ref _scrollPosition, viewHeight);
        var listing = new Listing_Standard();
        listing.Begin(new Rect(0f, 0f, content.width - 16f, viewHeight));
        if (_state.Navigation.CurrentPage.Equals(QuestSettingsCatalog.InstructionPage))
            DrawInstructionPage(listing, settings, search);
        else
            DrawGeneralPage(listing, settings, search);
        listing.End();
        SettingsShellRenderer.EndScroll();
    }

    static void EnsureState(QuestSettings settings)
    {
        var pages = QuestSettingsCatalog.CreatePages(
            "Ustas.RimAI.Quests.Settings.PageGeneral".Translate(),
            "Ustas.RimAI.Quests.Settings.PageInstruction".Translate());
        if (_state == null)
        {
            _state = new SettingsUiState(
                SettingsNavigationState.Create(pages),
                SettingsSearchState.Empty,
                QuestSettingsPolicy.Validate(settings.ToModel()));
            return;
        }

        _state = _state.WithNavigation(SettingsNavigationState.Create(pages, _state.Navigation.CurrentPage));
    }

    static void DrawGeneralPage(Listing_Standard listing, QuestSettings settings, SettingsSearchState search)
    {
        var any = false;
        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldEnable, search))
        {
            any = true;
            listing.CheckboxLabeled(
                "Ustas.RimAI.Quests.Settings.EnableAIDescriptions".Translate(),
                ref settings.enableAIDescriptions,
                "Ustas.RimAI.Quests.Settings.EnableAIDescriptions.Desc".Translate());
        }

        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldVerbose, search))
        {
            any = true;
            listing.CheckboxLabeled(
                "Ustas.RimAI.Quests.Settings.VerboseDebugLogging".Translate(),
                ref settings.verboseDebugLogging,
                "Ustas.RimAI.Quests.Settings.VerboseDebugLogging.Desc".Translate());
        }

        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldThinkTags, search))
        {
            any = true;
            listing.CheckboxLabeled(
                "Ustas.RimAI.Quests.Settings.CleanThinkTagsDuringStreaming".Translate(),
                ref settings.cleanThinkTagsDuringStreaming,
                "Ustas.RimAI.Quests.Settings.CleanThinkTagsDuringStreaming.Desc".Translate());
        }

        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldCache, search))
        {
            any = true;
            listing.Gap();
            if (listing.ButtonText("Ustas.RimAI.Quests.Settings.ClearCache".Translate()))
            {
                Services.QuestDescriptionGenerator.ClearCache();
                Messages.Message(
                    "Ustas.RimAI.Quests.Settings.CacheCleared".Translate(),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false);
            }
        }

        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldSharedConfig, search))
        {
            any = true;
            listing.Gap();
            listing.Label("Ustas.RimAI.Quests.Settings.UsesRimTalkConfig".Translate());
        }

        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldProcessing, search))
        {
            any = true;
            listing.Label(
                "Ustas.RimAI.Quests.Settings.CurrentlyProcessing".Translate(
                    Services.QuestDescriptionGenerator.ProcessingCount));
        }

        if (!any)
            listing.Label("RimAI.Settings.NoSearchMatches".Translate());
    }

    static void DrawInstructionPage(Listing_Standard listing, QuestSettings settings, SettingsSearchState search)
    {
        if (!QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldInstruction, search))
        {
            listing.Label("RimAI.Settings.NoSearchMatches".Translate());
            return;
        }

        listing.Label("Ustas.RimAI.Quests.Settings.CustomQuestInstruction".Translate());
        listing.Label("Ustas.RimAI.Quests.Settings.CustomQuestInstruction.Desc".Translate(), -1f);
        Rect textAreaRect = listing.GetRect(150f);
        settings.customQuestInstruction = Widgets.TextArea(textAreaRect, settings.customQuestInstruction);
    }

    static float EstimateHeight(SettingsPageId page, SettingsSearchState search)
    {
        if (page.Equals(QuestSettingsCatalog.InstructionPage))
            return 240f;
        var rows = 0;
        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldEnable, search)) rows++;
        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldVerbose, search)) rows++;
        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldThinkTags, search)) rows++;
        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldCache, search)) rows += 2;
        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldSharedConfig, search)) rows++;
        if (QuestSettingsCatalog.IsFieldVisible(QuestSettingsCatalog.FieldProcessing, search)) rows++;
        if (rows == 0)
            return 80f;
        return 40f + rows * 32f;
    }
}
