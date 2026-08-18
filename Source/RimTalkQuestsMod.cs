using HarmonyLib;
using Ustas.RimAI.Core.Handshake;
using Ustas.RimAI.Core.Modules;
using UnityEngine;
using Verse;

namespace Ustas.RimAI.Quests
{
    /// <summary>
    /// Main mod class for RimAI.Quests. Service graph lives in <see cref="QuestsComposition"/>.
    /// </summary>
    public class RimTalkQuestsMod : Mod
    {
        public const string HandshakeModuleVersion = "1.0.0";
        public static RimTalkQuestsMod Instance { get; private set; }
        public static Harmony HarmonyInstance => QuestsComposition.Current.Harmony;
        public static QuestSettings Settings { get; private set; }

        public RimTalkQuestsMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<QuestSettings>();
            RimAiHandshake.TryActivate(
                RimAiHandshakeDescriptor.Current(RimAiModuleIds.Quests, HandshakeModuleVersion, isOptional: true),
                QuestsComposition.Current.Start);
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
