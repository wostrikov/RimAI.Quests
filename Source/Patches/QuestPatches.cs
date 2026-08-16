using System;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace Ustas.RimAI.Quests.Patches
{
    /// <summary>
    /// Harmony patch to intercept quest addition and generate AI descriptions.
    ///
    /// This patch hooks into RimWorld's quest system to provide dynamic, context-aware
    /// quest descriptions using Ustas.RimAI.Communication's AI integration.
    /// Generates descriptions when quest is first added, so they're ready when player views them.
    /// </summary>
    [HarmonyPatch(typeof(Quest), nameof(Quest.PostAdded))]
    public static class QuestPostAddedPatch
    {
        /// <summary>
        /// Postfix patch that runs after a quest is added to the game
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Quest __instance)
        {
            try
            {
                if (__instance == null || !RimTalkQuestsMod.Settings.enableAIDescriptions)
                    return;

                // Skip hidden quests
                if (__instance.hidden)
                    return;

                // Check if RimTalk is properly configured
                if (!Services.QuestDescriptionGenerator.IsAIServiceAvailable())
                {
                    if (Prefs.DevMode)
                        Log.Warning(
                            "[RimAI.Quests] AI service not available. Make sure RimTalk is configured with an API key."
                        );
                    return;
                }

                // Generate AI description when quest is first added
                // This happens before player sees it, so description will be ready
                Services.QuestDescriptionGenerator.GenerateQuestDescriptionAsync(__instance);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimAI.Quests] Error in quest post added patch: {ex}");
            }
        }
    }
}
