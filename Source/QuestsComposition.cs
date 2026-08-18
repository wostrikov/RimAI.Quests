using System;
using System.Reflection;
using HarmonyLib;
using Ustas.RimAI.Core.Composition;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Core.Handshake;
using Ustas.RimAI.Core.Modules;
using Verse;

namespace Ustas.RimAI.Quests;

/// <summary>
/// Module composition root for RimAI.Quests. Owns Harmony when Communication is active.
/// </summary>
public sealed class QuestsComposition : IRimAiModuleComposition
{
    public static QuestsComposition Current { get; } = new();

    public string ModuleId => RimAiModuleIds.Quests;

    public bool IsStarted { get; private set; }

    public Harmony Harmony { get; private set; }

    public void Start()
    {
        if (IsStarted)
            return;

        RimAIModuleRegistry.Current.Register(
            new RimAIModuleDescriptor(
                "quests",
                "RimAI.Quests",
                "RimAI.Quests",
                "Quests"));

        RimAiLog.Info(RimAiLogCategory.Quests, "[RimAI.Quests] Initializing...");

        if (!ModsConfig.IsActive("ustas.rimai.communication"))
        {
            RimAiLog.Info(
                RimAiLogCategory.Quests,
                "[RimAI.Quests] Communication is not loaded; quest AI patches were not applied.");
            IsStarted = true;
            return;
        }

        Harmony = new Harmony("ustas.rimai.quests");
        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        RimAiLog.Info(
            RimAiLogCategory.Quests,
            "[RimAI.Quests] Successfully initialized with Harmony patches applied.");
        IsStarted = true;
    }

    public void Stop()
    {
        IsStarted = false;
    }
}
