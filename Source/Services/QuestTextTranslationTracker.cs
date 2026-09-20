// RimAI.composition: ROOT_OWNED_SINGLETON — one per save, created by RimWorld's own Game.FillComponents, and reached by its only two callers through Current because a GameComponent has no other address.
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Ustas.RimAI.Quests.Services
{
    /// <summary>
    /// Which quests have already been looked at, kept with the save.
    ///
    /// The answer has to survive a reload for the same reason it is worth
    /// having: a translation costs a model call, and a quest's text does not
    /// change once it is written. Without this, every load of an old colony
    /// would pay for every old quest again.
    ///
    /// It records the question rather than the answer - the translated text
    /// goes into quest.description, which RimWorld already scribes.
    /// </summary>
    public class QuestTextTranslationTracker : GameComponent
    {
        private HashSet<int> _translated = new HashSet<int>();

        /// <summary>
        /// Work handed back by a background translation. A quest's description
        /// is game state, so it is written on the main thread and nowhere
        /// else; a GameComponent tick is the main thread by construction,
        /// unlike LongEventHandler.ExecuteWhenFinished, whose queue is also
        /// drained from a long event's own thread.
        /// </summary>
        private readonly List<Action> _pending = new List<Action>();

        public QuestTextTranslationTracker(Game game)
        {
        }

        public static QuestTextTranslationTracker Current =>
            Verse.Current.Game?.GetComponent<QuestTextTranslationTracker>();

        public bool AlreadyTranslated(int questId) => _translated.Contains(questId);

        public void Remember(int questId) => _translated.Add(questId);

        public void RunOnMainThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            lock (_pending)
            {
                _pending.Add(action);
            }
        }

        /// <summary>
        /// How often an old quest is considered. Quest.PostAdded only fires for
        /// a quest being created, and the ones this exists for were created
        /// long ago - so the backlog is walked here instead. Slowly and one at
        /// a time: a colony with thirty old quests should not answer a reload
        /// with thirty model calls at once.
        /// </summary>
        private const int TicksPerSweep = 600;

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % TicksPerSweep == 0)
            {
                SweepOne();
            }

            Action[] due;
            lock (_pending)
            {
                if (_pending.Count == 0)
                {
                    return;
                }

                due = _pending.ToArray();
                _pending.Clear();
            }

            foreach (Action action in due)
            {
                action();
            }
        }

        private void SweepOne()
        {
            List<Quest> quests = Find.QuestManager?.QuestsListForReading;
            if (quests == null)
            {
                return;
            }

            for (int index = 0; index < quests.Count; index++)
            {
                Quest quest = quests[index];
                if (quest != null && !quest.hidden && !_translated.Contains(quest.id))
                {
                    QuestTextTranslator.TranslateIfNeeded(quest);
                    return;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _translated, "questTextTranslated", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _translated ??= new HashSet<int>();
            }
        }
    }
}
