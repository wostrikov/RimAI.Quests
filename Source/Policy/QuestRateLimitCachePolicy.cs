using System.Collections.Generic;

namespace Ustas.RimAI.Quests.Policy
{
    public readonly struct QuestGenerationDecision
    {
        public QuestGenerationDecision(bool callProvider, bool useCached, bool skipBusy, bool skipRateLimited)
        {
            CallProvider = callProvider;
            UseCached = useCached;
            SkipBusy = skipBusy;
            SkipRateLimited = skipRateLimited;
        }

        public bool CallProvider { get; }
        public bool UseCached { get; }
        public bool SkipBusy { get; }
        public bool SkipRateLimited { get; }
    }

    /// <summary>
    /// Result cache plus burst/429 admit. A cached enhancement is reused.
    /// In-flight and rate-limited quests do not start another provider call.
    /// </summary>
    public sealed class QuestDescriptionResultCache
    {
        readonly Dictionary<int, string> _results = new Dictionary<int, string>();
        readonly HashSet<int> _rateLimited = new HashSet<int>();

        public bool TryGet(int questId, out string text) => _results.TryGetValue(questId, out text);

        public void Store(int questId, string text)
        {
            if (questId < 0 || string.IsNullOrEmpty(text))
                return;
            _results[questId] = text;
            _rateLimited.Remove(questId);
        }

        public void MarkRateLimited(int questId)
        {
            if (questId < 0)
                return;
            _rateLimited.Add(questId);
        }

        public bool IsRateLimited(int questId) => _rateLimited.Contains(questId);

        public void Clear()
        {
            _results.Clear();
            _rateLimited.Clear();
        }

        public int Count => _results.Count;
    }

    public static class QuestRateLimitCachePolicy
    {
        public static bool IsRateLimitedStatus(int? statusCode) => statusCode == 429;

        public static bool ShouldRetry(int? statusCode) =>
            !IsRateLimitedStatus(statusCode) && statusCode != 401;

        public static QuestGenerationDecision Decide(bool hasCached, bool processing, bool rateLimited)
        {
            if (processing)
                return new QuestGenerationDecision(false, false, true, false);
            if (hasCached)
                return new QuestGenerationDecision(false, true, false, false);
            if (rateLimited)
                return new QuestGenerationDecision(false, false, false, true);
            return new QuestGenerationDecision(true, false, false, false);
        }
    }
}
