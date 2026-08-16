using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkQuests
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

            Log.Message("[RimTalk-Quests] Initializing...");

            // Check if RimTalk is loaded
            if (!ModsConfig.IsActive("cj.rimtalk"))
            {
                Log.Error(
                    "[RimTalk-Quests] RimTalk is not loaded! This mod requires RimTalk to function."
                );
                return;
            }

            try
            {
                // Apply Harmony patches
                HarmonyInstance = new Harmony("rimtalk.quests");
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                Log.Message(
                    "[RimTalk-Quests] Successfully initialized with Harmony patches applied."
                );
                Log.Message(
                    "[RimTalk-Quests] Attribution: Based on RimTalk by juicy (CC BY-NC-SA 4.0)"
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk-Quests] Failed to initialize: {ex}");
            }
        }

        public override string SettingsCategory()
        {
            return Content?.Name ?? "RimAI.Quests";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            AccessTools.TypeByName("Ustas.RimAI.Core.Modules.RimAISettingsNavigation")
                ?.GetMethod("Open")
                ?.Invoke(null, new object[] { "quests", null });
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.CheckboxLabeled(
                "RimTalkQuests.Settings.EnableAIDescriptions".Translate(),
                ref Settings.enableAIDescriptions,
                "RimTalkQuests.Settings.EnableAIDescriptions.Desc".Translate()
            );

            listingStandard.CheckboxLabeled(
                "RimTalkQuests.Settings.VerboseDebugLogging".Translate(),
                ref Settings.verboseDebugLogging,
                "RimTalkQuests.Settings.VerboseDebugLogging.Desc".Translate()
            );

            listingStandard.CheckboxLabeled(
                "RimTalkQuests.Settings.CleanThinkTagsDuringStreaming".Translate(),
                ref Settings.cleanThinkTagsDuringStreaming,
                "RimTalkQuests.Settings.CleanThinkTagsDuringStreaming.Desc".Translate()
            );

            listingStandard.Gap();
            listingStandard.Label("RimTalkQuests.Settings.CustomQuestInstruction".Translate());
            listingStandard.Label(
                "RimTalkQuests.Settings.CustomQuestInstruction.Desc".Translate(),
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

            if (listingStandard.ButtonText("RimTalkQuests.Settings.ClearCache".Translate()))
            {
                Services.QuestDescriptionGenerator.ClearCache();
                Messages.Message(
                    "RimTalkQuests.Settings.CacheCleared".Translate(),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false
                );
            }

            listingStandard.Gap();
            listingStandard.Label("RimTalkQuests.Settings.UsesRimTalkConfig".Translate());
            listingStandard.Label(
                "RimTalkQuests.Settings.CurrentlyProcessing".Translate(
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
