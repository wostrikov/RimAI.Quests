using System.IO;
using Ustas.RimAI.Quests.Policy;
using Xunit;

namespace Ustas.RimAI.Quests.Tests;

public sealed class QuestAppendPolicyTests
{
    const string Original = "Deliver 3 steel. Reward: 200 silver.";
    const string Enhancement = "The outlander envoy sounds exhausted.";

    [Fact]
    public void Compose_appends_under_separator_and_keeps_original()
    {
        string text = QuestAppendPolicy.Compose(Original, Enhancement);
        Assert.StartsWith(Original, text);
        Assert.Contains(QuestAppendPolicy.Separator, text);
        Assert.EndsWith(Enhancement, text);
        Assert.NotEqual(Enhancement, text);
    }

    [Fact]
    public void Apply_success_does_not_replace_mechanical_description()
    {
        var outcome = QuestAppendPolicy.Apply(Original, Enhancement, failed: false);
        Assert.True(outcome.Appended);
        Assert.False(outcome.Restored);
        Assert.True(outcome.OriginalPreserved);
        Assert.Contains("Reward: 200 silver.", outcome.Text);
        Assert.Contains(Enhancement, outcome.Text);
    }

    [Fact]
    public void Apply_failure_restores_original_exactly()
    {
        var outcome = QuestAppendPolicy.Apply(Original, Enhancement, failed: true);
        Assert.True(outcome.Restored);
        Assert.False(outcome.Appended);
        Assert.Equal(Original, outcome.Text);
    }

    [Fact]
    public void Apply_empty_enhancement_does_not_replace()
    {
        var outcome = QuestAppendPolicy.Apply(Original, "  ", failed: false);
        Assert.True(outcome.Restored);
        Assert.Equal(Original, outcome.Text);
    }

    [Fact]
    public void Restore_returns_original_even_when_null()
    {
        Assert.Equal(string.Empty, QuestAppendPolicy.Restore(string.Empty));
        Assert.Equal(Original, QuestAppendPolicy.Restore(Original));
    }

    [Fact]
    public void Streaming_recompose_from_original_does_not_stack_separators()
    {
        string first = QuestAppendPolicy.Compose(Original, "chunk1");
        string second = QuestAppendPolicy.Compose(Original, "chunk1chunk2");
        Assert.Equal(1, CountSeparators(first));
        Assert.Equal(1, CountSeparators(second));
        Assert.StartsWith(Original, second);
    }

    [Fact]
    public void Mechanical_snapshot_stays_outside_description_text()
    {
        var name = "Trade request";
        var rating = 3;
        var parts = "QuestPart_Choice|Reward_Items";
        var outcome = QuestAppendPolicy.Apply(Original, Enhancement, failed: false);
        Assert.Equal("Trade request", name);
        Assert.Equal(3, rating);
        Assert.Equal("QuestPart_Choice|Reward_Items", parts);
        Assert.DoesNotContain(name, outcome.Text);
        Assert.DoesNotContain(parts, outcome.Text);
    }

    [Fact]
    public void Production_generator_uses_append_policy()
    {
        string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "QuestDescriptionGenerator.cs.src"));
        Assert.Contains("QuestAppendPolicy.Compose", source);
        Assert.Contains("QuestAppendPolicy.Restore", source);
        Assert.DoesNotContain("originalDescription + \"\\n\\n───────────\\n\\n\"", source);
    }

    static int CountSeparators(string text)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(QuestAppendPolicy.Separator, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += QuestAppendPolicy.Separator.Length;
        }
        return count;
    }
}
