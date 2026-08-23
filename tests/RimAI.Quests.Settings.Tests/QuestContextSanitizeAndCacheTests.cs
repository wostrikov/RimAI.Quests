using System.IO;
using Ustas.RimAI.Quests.Policy;
using Xunit;

namespace Ustas.RimAI.Quests.Tests;

public sealed class QuestContextSanitizeAndCacheTests
{
    [Fact]
    public void Assemble_includes_type_rating_rewards_wealth_weather_factions()
    {
        string prompt = QuestContextBundlePolicy.Assemble(new QuestContextBundle
        {
            Title = "Trade request",
            Description = "Bring <color=#D09B61>steel</color>.",
            Type = "TradeRequest",
            Challenge = "2",
            RewardsBlock = QuestContextBundlePolicy.RewardsMarker + "\n  Option 1: 200 silver",
            Weather = "Clear",
            Wealth = "modest",
            FactionsBlock = QuestContextBundlePolicy.FactionsMarker + "\nFrom: Outlander"
        });

        Assert.True(QuestContextBundlePolicy.HasRequiredFacets(prompt));
        Assert.Contains("TradeRequest", prompt);
        Assert.Contains("200 silver", prompt);
        Assert.Contains("Clear", prompt);
        Assert.Contains("Outlander", prompt);
    }

    [Fact]
    public void Sanitize_strips_think_and_keeps_color_tags()
    {
        const string raw =
            "<think>secret plan</think>Deliver <color=#D09B61>steel</color> [reasoning]no[/reasoning]";
        string sanitized = QuestResponseSanitizePolicy.StripThinkBlocks(raw);
        Assert.DoesNotContain("secret plan", sanitized);
        Assert.DoesNotContain("no", sanitized);
        Assert.Contains("<color=#D09B61>steel</color>", sanitized);
        Assert.True(QuestResponseSanitizePolicy.PreservesRichText(sanitized));
    }

    [Fact]
    public void Cache_reuses_result_and_blocks_429_burst()
    {
        var cache = new QuestDescriptionResultCache();
        Assert.True(QuestRateLimitCachePolicy.IsRateLimitedStatus(429));
        Assert.False(QuestRateLimitCachePolicy.ShouldRetry(429));

        var first = QuestRateLimitCachePolicy.Decide(false, false, false);
        Assert.True(first.CallProvider);

        cache.Store(7, "The envoy is tired.");
        Assert.True(cache.TryGet(7, out var hit));
        Assert.Equal("The envoy is tired.", hit);

        var cached = QuestRateLimitCachePolicy.Decide(true, false, false);
        Assert.True(cached.UseCached);
        Assert.False(cached.CallProvider);

        var busy = QuestRateLimitCachePolicy.Decide(false, true, false);
        Assert.True(busy.SkipBusy);
        Assert.False(busy.CallProvider);

        cache.MarkRateLimited(8);
        var limited = QuestRateLimitCachePolicy.Decide(cache.TryGet(8, out _), false, cache.IsRateLimited(8));
        Assert.True(limited.SkipRateLimited);
        Assert.False(limited.CallProvider);
    }

    [Fact]
    public void Generator_consumes_the_three_policies()
    {
        string generator = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "QuestDescriptionGenerator.cs.src"));
        Assert.Contains("QuestContextBundlePolicy.Assemble", generator);
        Assert.Contains("QuestRateLimitCachePolicy.Decide", generator);
        Assert.Contains("_results.Store", generator);
        Assert.Contains("MarkRateLimited", generator);

        string processor = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "ThinkReasoningPostProcessor.cs.src"));
        Assert.Contains("QuestResponseSanitizePolicy.StripThinkBlocks", processor);
    }
}
