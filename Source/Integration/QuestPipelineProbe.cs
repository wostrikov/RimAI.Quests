using System;
using System.Collections.Generic;
using RimWorld;
using Ustas.RimAI.Core.TestDriver;
using Ustas.RimAI.Quests.Policy;
using Ustas.RimAI.Quests.Services;
using Verse;

namespace Ustas.RimAI.Quests.Integration;

/// <summary>
/// Deterministic TestDriver fixture for quest-panel streaming writes.
/// Injects chunks through <see cref="QuestDescriptionGenerator.ApplyStreamingDisplay"/>
/// and does not call a paid provider.
/// </summary>
public static class QuestPipelineProbe
{
    public static void Register()
    {
        TestDriverModuleOperations.Register(
            TestDriverCommandNames.ProbeQuests,
            (request, _) => new TestDriverDelegateOperation(() => Run(request)));
    }

    static TestDriverProgress Run(TestDriverRequest request)
    {
        if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null)
            return TestDriverProgress.Failed("probe_quests requires a loaded game");

        var mode = request.Arguments.GetString("mode", "stream_fixture");
        var correlationId = request.Arguments.GetString("correlationId", request.RequestId);
        if (!string.Equals(mode, "stream_fixture", StringComparison.OrdinalIgnoreCase))
            return TestDriverProgress.Failed("mode must be stream_fixture");

        return StreamFixture(correlationId);
    }

    static TestDriverProgress StreamFixture(string correlationId)
    {
        bool created = false;
        var quest = PickQuest();
        if (quest == null)
        {
            quest = CreateTemporaryQuest();
            created = quest != null;
        }
        if (quest == null)
            return TestDriverProgress.Failed("no quest available");

        string original = quest.description.ToString();
        var samples = new List<TestDriverJsonWriter>();
        var chunks = new[] { "Alpha ", "Alpha Beta ", "Alpha Beta Gamma" };
        int previous = original.Length;
        bool grew = true;
        bool originalPreserved = true;
        string firstError = null;

        try
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                QuestDescriptionGenerator.ApplyStreamingDisplay(quest, original, chunks[i]);
                string now = quest.description.ToString();
                bool chunkGrew = now.Length > previous;
                bool preserved = now.StartsWith(original, StringComparison.Ordinal)
                    && now.IndexOf(QuestAppendPolicy.Separator, StringComparison.Ordinal) >= original.Length;
                grew = grew && chunkGrew;
                originalPreserved = originalPreserved && preserved;
                samples.Add(new TestDriverJsonWriter()
                    .Integer("index", i)
                    .Integer("length", now.Length)
                    .Flag("grew", chunkGrew)
                    .Flag("originalPreserved", preserved));
                previous = now.Length;
            }

            TryOpenQuestTab();
        }
        catch (InvalidOperationException ex)
        {
            firstError = ex.GetType().Name + ": " + ex.Message;
        }
        catch (ArgumentException ex)
        {
            firstError = ex.GetType().Name + ": " + ex.Message;
        }
        finally
        {
            if (created)
                RemoveTemporaryQuest(quest);
            else
                quest.description = new TaggedString(original);
        }

        return TestDriverProgress.Completed(new TestDriverJsonWriter()
            .Text("mode", "stream_fixture")
            .Text("correlationId", correlationId)
            .Integer("questId", quest.id)
            .Text("questName", quest.name)
            .Integer("originalLength", original.Length)
            .Integer("chunkCount", chunks.Length)
            .Flag("descriptionGrew", grew)
            .Flag("originalPreserved", originalPreserved)
            .Flag("questTabOpen", Find.MainTabsRoot?.OpenTab?.defName == "Quests")
            .Flag("createdTemporaryQuest", created)
            .Flag("restored", created || quest.description.ToString() == original)
            .Flag("EXCEPTION_PRESENT", firstError != null)
            .Text("firstError", firstError)
            .Flag("paused", Find.TickManager?.Paused ?? true)
            .Integer("ticksGame", Find.TickManager?.TicksGame ?? 0)
            .ObjectArray("chunks", samples));
    }

    static Quest PickQuest()
    {
        var quests = Find.QuestManager?.QuestsListForReading;
        if (quests == null)
            return null;
        for (int i = 0; i < quests.Count; i++)
        {
            var quest = quests[i];
            if (quest != null && !string.IsNullOrWhiteSpace(quest.description))
                return quest;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i] != null)
                return quests[i];
        }

        return null;
    }

    static Quest CreateTemporaryQuest()
    {
        var quest = new Quest
        {
            name = "RimAIProbeStream",
            appearanceTick = Find.TickManager?.TicksGame ?? 0
        };
        quest.description = new TaggedString("Original probe description.");
        Find.QuestManager.Add(quest);
        return quest;
    }

    static void RemoveTemporaryQuest(Quest quest)
    {
        if (quest == null || Find.QuestManager == null)
            return;
        Find.QuestManager.Remove(quest);
    }

    static void TryOpenQuestTab()
    {
        var def = DefDatabase<MainButtonDef>.GetNamedSilentFail("Quests");
        if (def == null || Find.MainTabsRoot == null)
            return;
        Find.MainTabsRoot.SetCurrentTab(def, playSound: false);
    }
}
