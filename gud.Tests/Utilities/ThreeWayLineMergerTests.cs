using gud.Core.Utilities;

namespace gud.Tests.Utilities;

[TestFixture]
public class ThreeWayLineMergerTests
{
    [Test]
    public void IndependentLineEdits_AutoMerge()
    {
        var bas = Lines("a", "b", "c", "d");
        var ours = Lines("A", "b", "c", "d");
        var theirs = Lines("a", "b", "c", "D");

        var result = ThreeWayLineMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(result.Lines, Is.EqualTo(Lines("A", "b", "c", "D")));
    }

    [Test]
    public void SameLineDifferent_RegionalConflict()
    {
        var bas = Lines("prefix", "middle", "suffix");
        var ours = Lines("prefix", "ours-mid", "suffix");
        var theirs = Lines("prefix", "theirs-mid", "suffix");

        var result = ThreeWayLineMerger.Merge(bas, ours, theirs, "HEAD", "feature");
        Assert.That(result.HadConflict, Is.True);
        Assert.That(result.Lines[0], Is.EqualTo("prefix"));
        Assert.That(result.Lines[^1], Is.EqualTo("suffix"));
        Assert.That(result.Lines, Does.Contain("<<<<<<< HEAD"));
        Assert.That(result.Lines, Does.Contain("ours-mid"));
        Assert.That(result.Lines, Does.Contain("======="));
        Assert.That(result.Lines, Does.Contain("theirs-mid"));
        Assert.That(result.Lines, Does.Contain(">>>>>>> feature"));
        // Markers should not wrap the whole file as first line
        Assert.That(result.Lines[0], Is.Not.EqualTo("<<<<<<< HEAD"));
    }

    [Test]
    public void BothAppendIdentical_NoConflict()
    {
        var bas = Lines("a", "b");
        var ours = Lines("a", "b", "c");
        var theirs = Lines("a", "b", "c");

        var result = ThreeWayLineMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(result.Lines, Is.EqualTo(Lines("a", "b", "c")));
    }

    [Test]
    public void BothAppendDifferent_ConflictAtEof()
    {
        var bas = Lines("a", "b");
        var ours = Lines("a", "b", "ours");
        var theirs = Lines("a", "b", "theirs");

        var result = ThreeWayLineMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.True);
        Assert.That(result.Lines, Does.Contain("<<<<<<< HEAD"));
        Assert.That(result.Lines, Does.Contain("ours"));
        Assert.That(result.Lines, Does.Contain("theirs"));
    }

    [Test]
    public void BothDeleteSameBlock_Resolved()
    {
        var bas = Lines("a", "drop", "b");
        var ours = Lines("a", "b");
        var theirs = Lines("a", "b");

        var result = ThreeWayLineMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(result.Lines, Is.EqualTo(Lines("a", "b")));
    }

    [Test]
    public void EditTopAndDeleteBottom_CleanMerge()
    {
        var bas = Lines("top", "mid", "bottom");
        var ours = Lines("TOP", "mid", "bottom");
        var theirs = Lines("top", "mid");

        var result = ThreeWayLineMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(result.Lines, Is.EqualTo(Lines("TOP", "mid")));
    }

    [Test]
    public void EmptyBase_BothDifferent_Conflict()
    {
        var bas = Array.Empty<string>();
        var ours = Lines("ours");
        var theirs = Lines("theirs");

        var result = ThreeWayLineMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.True);
    }

    [Test]
    public void EmptyBase_BothSame_NoConflict()
    {
        var bas = Array.Empty<string>();
        var side = Lines("same");
        var result = ThreeWayLineMerger.Merge(bas, side, side);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(result.Lines, Is.EqualTo(side));
    }

    [Test]
    public void SplitJoin_RoundTripsTrailingNewline()
    {
        const string text = "a\nb\n";
        var lines = ThreeWayLineMerger.SplitLines(text);
        Assert.That(ThreeWayLineMerger.JoinLines(lines), Is.EqualTo(text));
    }

    [Test]
    public void OnlyOursChanged_TakesOurs()
    {
        var bas = Lines("a");
        var ours = Lines("A");
        var theirs = Lines("a");
        var result = ThreeWayLineMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(result.Lines, Is.EqualTo(ours));
    }

    private static string[] Lines(params string[] lines) => lines;
}
