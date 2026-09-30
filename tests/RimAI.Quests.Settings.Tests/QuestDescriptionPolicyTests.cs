using System.IO;
using Ustas.RimAI.Quests.Policy;
using Xunit;

namespace Ustas.RimAI.Quests.Tests;

public sealed class QuestDescriptionPolicyTests
{
    const string Original = "Deliver 3 steel. Reward: 200 silver.";
    const string Enhancement = "The outlander envoy sounds exhausted, and wants 3 steel for 200 silver.";

    [Fact]
    public void Compose_replaces_the_original_with_the_narrative()
    {
        string text = QuestDescriptionPolicy.Compose(Original, Enhancement);
        Assert.Equal(Enhancement, text);
        Assert.DoesNotContain(Original, text);
    }

    [Fact]
    public void Compose_trims_the_narrative()
    {
        Assert.Equal(Enhancement, QuestDescriptionPolicy.Compose(Original, "\n  " + Enhancement + "  \n"));
    }

    [Fact]
    public void Apply_success_replaces()
    {
        var outcome = QuestDescriptionPolicy.Apply(Original, Enhancement, failed: false);
        Assert.True(outcome.Replaced);
        Assert.False(outcome.Restored);
        Assert.Equal(Enhancement, outcome.Text);
    }

    [Fact]
    public void Apply_failure_restores_original_exactly()
    {
        var outcome = QuestDescriptionPolicy.Apply(Original, Enhancement, failed: true);
        Assert.True(outcome.Restored);
        Assert.False(outcome.Replaced);
        Assert.Equal(Original, outcome.Text);
    }

    [Fact]
    public void Apply_empty_enhancement_keeps_the_original()
    {
        var outcome = QuestDescriptionPolicy.Apply(Original, "  ", failed: false);
        Assert.True(outcome.Restored);
        Assert.Equal(Original, outcome.Text);
        Assert.Equal(Original, QuestDescriptionPolicy.Compose(Original, null!));
    }

    [Fact]
    public void Restore_returns_original_even_when_null()
    {
        Assert.Equal(string.Empty, QuestDescriptionPolicy.Restore(null!));
        Assert.Equal(Original, QuestDescriptionPolicy.Restore(Original));
    }

    [Fact]
    public void Production_generator_writes_once_through_the_policy()
    {
        string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "QuestDescriptionGenerator.cs.src"));
        Assert.Contains("QuestDescriptionPolicy.Compose", source);
        Assert.Contains("RunOnMainThread(() => ApplyDescription(", source);
        Assert.DoesNotContain("ApplyStreamingDisplay", source);
        Assert.DoesNotContain("───", source);
    }
}
