using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Ustas.RimAI.Core.Handshake;
using Ustas.RimAI.Core.Modules;
using UnityEngine;
using Verse;
using Ustas.RimAI.Core.Diagnostics;

namespace Ustas.RimAI.Quests
{
    /// <summary>
    /// Main mod class for RimTalk-Quests
    ///
    /// This mod is a derivative work based on RimTalk by juicy, licensed under CC BY-NC-SA 4.0.
    /// It reuses RimTalk's AI model configuration and API calling functionality to generate
    /// dynamic quest descriptions.
    /// </summary>
    [StaticConstructorOnStartup]
    public class RimTalkQuestsMod : Mod
    {
        public const string HandshakeModuleVersion = "1.0.0";
        public static RimTalkQuestsMod Instance { get; private set; }
        public static Harmony HarmonyInstance { get; private set; }
        public static QuestSettings Settings { get; private set; }

        public RimTalkQuestsMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<QuestSettings>();
            RimAiHandshake.TryActivate(
                RimAiHandshakeDescriptor.Current(RimAiModuleIds.Quests, HandshakeModuleVersion, isOptional: true),
                Activate);
        }

        static void Activate()
        {
            Ustas.RimAI.Core.Modules.RimAIModuleRegistry.Current.Register(
                new Ustas.RimAI.Core.Modules.RimAIModuleDescriptor(
                    "quests",
                    "RimAI.Quests",
                    "RimAI.Quests",
                    "Quests"));

            RimAiLog.Info(RimAiLogCategory.Quests, "[RimAI.Quests] Initializing...");

            if (!ModsConfig.IsActive("ustas.rimai.communication"))
            {
                RimAiLog.Info(RimAiLogCategory.Quests, 
                    "[RimAI.Quests] Communication is not loaded; quest AI patches were not applied."
                );
                return;
            }

            try
            {
                HarmonyInstance = new Harmony("ustas.rimai.quests");
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                RimAiLog.Info(RimAiLogCategory.Quests, 
                    "[RimAI.Quests] Successfully initialized with Harmony patches applied."
                );
            }
            catch (Exception ex)
            {
                RimAiLog.Error(RimAiLogCategory.Quests, $"[RimAI.Quests] Failed to initialize: {ex}");
                throw;
            }
        }

        public override string SettingsCategory()
        {
            return Content?.Name ?? "RimAI.Quests";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RimAISettingsNavigation.Open("quests");
            base.DoSettingsWindowContents(inRect);
            QuestSettingsWindow.Draw(inRect, Settings);
        }
    }
}
