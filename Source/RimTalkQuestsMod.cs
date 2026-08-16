using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Ustas.RimAI.Core.Modules;
using Verse;

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
        public static RimTalkQuestsMod Instance { get; private set; }
        public static Harmony HarmonyInstance { get; private set; }
        public static QuestSettings Settings { get; private set; }

        public RimTalkQuestsMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<QuestSettings>();
            Ustas.RimAI.Core.Modules.RimAIModuleRegistry.Current.Register(
                new Ustas.RimAI.Core.Modules.RimAIModuleDescriptor(
                    "quests",
                    "RimAI.Quests",
                    "RimAI.Quests",
                    "Quests"));

            Log.Message("[RimAI.Quests] Initializing...");

            // Check if RimTalk is loaded
            if (!ModsConfig.IsActive("ustas.rimai.communication"))
            {
                Log.Error(
                    "[RimAI.Quests] RimTalk is not loaded! This mod requires RimTalk to function."
                );
                return;
            }

            try
            {
                // Apply Harmony patches
                HarmonyInstance = new Harmony("ustas.rimai.quests");
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                Log.Message(
                    "[RimAI.Quests] Successfully initialized with Harmony patches applied."
                );
                Log.Message(
                    "[RimAI.Quests] Attribution: Based on RimTalk by juicy (CC BY-NC-SA 4.0)"
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI.Quests] Failed to initialize: {ex}");
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
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled(
                "Ustas.RimAI.Quests.Settings.EnableAIDescriptions".Translate(),
                ref Settings.enableAIDescriptions,
                "Ustas.RimAI.Quests.Settings.EnableAIDescriptions.Desc".Translate()
            );

            listingStandard.CheckboxLabeled(
                "Ustas.RimAI.Quests.Settings.VerboseDebugLogging".Translate(),
                ref Settings.verboseDebugLogging,
                "Ustas.RimAI.Quests.Settings.VerboseDebugLogging.Desc".Translate()
            );

            listingStandard.CheckboxLabeled(
                "Ustas.RimAI.Quests.Settings.CleanThinkTagsDuringStreaming".Translate(),
                ref Settings.cleanThinkTagsDuringStreaming,
                "Ustas.RimAI.Quests.Settings.CleanThinkTagsDuringStreaming.Desc".Translate()
            );

            listingStandard.Gap();
            listingStandard.Label("Ustas.RimAI.Quests.Settings.CustomQuestInstruction".Translate());
            listingStandard.Label(
                "Ustas.RimAI.Quests.Settings.CustomQuestInstruction.Desc".Translate(),
                -1f
            );

            float textHeight = 150f;
            Rect textAreaRect = listingStandard.GetRect(textHeight);
            Settings.customQuestInstruction = string.IsNullOrWhiteSpace(
                Settings.customQuestInstruction
            )
                ? Constant.GetDefaultQuestInstruction()
                : Settings.customQuestInstruction;
            Settings.customQuestInstruction = Widgets.TextArea(
                textAreaRect,
                Settings.customQuestInstruction
            );

            listingStandard.Gap();

            if (listingStandard.ButtonText("Ustas.RimAI.Quests.Settings.ClearCache".Translate()))
            {
                Services.QuestDescriptionGenerator.ClearCache();
                Messages.Message(
                    "Ustas.RimAI.Quests.Settings.CacheCleared".Translate(),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false
                );
            }

            listingStandard.Gap();
            listingStandard.Label("Ustas.RimAI.Quests.Settings.UsesRimTalkConfig".Translate());
            listingStandard.Label(
                "Ustas.RimAI.Quests.Settings.CurrentlyProcessing".Translate(
                    Services.QuestDescriptionGenerator.ProcessingCount
                )
            );

            listingStandard.End();
        }
    }

    public class QuestSettings : ModSettings
    {
        public bool enableAIDescriptions = true;
        public bool verboseDebugLogging = false;
        public bool cleanThinkTagsDuringStreaming = false;
        public string customQuestInstruction = "";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableAIDescriptions, "enableAIDescriptions", true);
            Scribe_Values.Look(ref verboseDebugLogging, "verboseDebugLogging", false);
            Scribe_Values.Look(
                ref cleanThinkTagsDuringStreaming,
                "cleanThinkTagsDuringStreaming",
                true
            );
            Scribe_Values.Look(ref customQuestInstruction, "customQuestInstruction", "");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && IsUnchangedLegacyDefault(customQuestInstruction))
            {
                customQuestInstruction = Constant.GetDefaultQuestInstruction();
                LongEventHandler.ExecuteWhenFinished(Write);
            }
        }

        private static bool IsUnchangedLegacyDefault(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string normalized = value.Replace("\r\n", "\n").Trim();
            using (var sha = SHA256.Create())
            {
                string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(normalized))).Replace("-", "");
                return string.Equals(hash, "EA578CB442CB6F7675B5815147B859CAD25A69D1362A15F0D068AC2BAEB6E36D", StringComparison.Ordinal);
            }
        }
    }
}
