using gud.Core.Services;

namespace gud.Tests.Services;

[TestFixture]
public class TreeMergerTests
{
    [Test]
    public void TakesOurs_WhenOnlyOursChanged()
    {
        var bas = new Dictionary<string, string> { ["f"] = "A" };
        var ours = new Dictionary<string, string> { ["f"] = "B" };
        var theirs = new Dictionary<string, string> { ["f"] = "A" };

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.HasConflicts, Is.False);
        Assert.That(result.MergedPaths["f"], Is.EqualTo("B"));
    }

    [Test]
    public void TakesTheirs_WhenOnlyTheirsChanged()
    {
        var bas = new Dictionary<string, string> { ["f"] = "A" };
        var ours = new Dictionary<string, string> { ["f"] = "A" };
        var theirs = new Dictionary<string, string> { ["f"] = "C" };

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.MergedPaths["f"], Is.EqualTo("C"));
    }

    [Test]
    public void BothSameChange_NoConflict()
    {
        var bas = new Dictionary<string, string> { ["f"] = "A" };
        var ours = new Dictionary<string, string> { ["f"] = "B" };
        var theirs = new Dictionary<string, string> { ["f"] = "B" };

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.MergedPaths["f"], Is.EqualTo("B"));
        Assert.That(result.HasConflicts, Is.False);
    }

    [Test]
    public void BothDifferentChange_Conflict()
    {
        var bas = new Dictionary<string, string> { ["f"] = "A" };
        var ours = new Dictionary<string, string> { ["f"] = "B" };
        var theirs = new Dictionary<string, string> { ["f"] = "C" };

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.HasConflicts, Is.True);
        Assert.That(result.Conflicts[0].Path, Is.EqualTo("f"));
        Assert.That(result.MergedPaths.ContainsKey("f"), Is.False);
    }

    [Test]
    public void AddOnBothSidesDifferent_Conflict()
    {
        var bas = new Dictionary<string, string>();
        var ours = new Dictionary<string, string> { ["f"] = "B" };
        var theirs = new Dictionary<string, string> { ["f"] = "C" };

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.HasConflicts, Is.True);
    }

    [Test]
    public void DeleteOnBoth_Deletes()
    {
        var bas = new Dictionary<string, string> { ["f"] = "A" };
        var ours = new Dictionary<string, string>();
        var theirs = new Dictionary<string, string>();

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.MergedPaths.ContainsKey("f"), Is.False);
        Assert.That(result.HasConflicts, Is.False);
    }

    [Test]
    public void ModifyDelete_Conflict()
    {
        var bas = new Dictionary<string, string> { ["f"] = "A" };
        var ours = new Dictionary<string, string> { ["f"] = "B" };
        var theirs = new Dictionary<string, string>();

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.HasConflicts, Is.True);
    }

    [Test]
    public void IndependentFiles_MergeBoth()
    {
        var bas = new Dictionary<string, string> { ["a"] = "A", ["b"] = "B" };
        var ours = new Dictionary<string, string> { ["a"] = "A2", ["b"] = "B" };
        var theirs = new Dictionary<string, string> { ["a"] = "A", ["b"] = "B2" };

        var result = TreeMerger.Merge(bas, ours, theirs);
        Assert.That(result.HasConflicts, Is.False);
        Assert.That(result.MergedPaths["a"], Is.EqualTo("A2"));
        Assert.That(result.MergedPaths["b"], Is.EqualTo("B2"));
    }
}
