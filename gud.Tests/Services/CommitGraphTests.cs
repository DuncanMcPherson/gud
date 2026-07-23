using System.Text;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;

namespace gud.Tests.Services;

[TestFixture]
public class CommitGraphTests
{
    private string _gudPath = null!;
    private ObjectRepository _objects = null!;

    [SetUp]
    public void SetUp()
    {
        var root = Path.Combine(Path.GetTempPath(), "gud-graph-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        _gudPath = Path.Combine(root, ".gud");
        Directory.CreateDirectory(Path.Combine(_gudPath, "objects"));
        _objects = new ObjectRepository(new ObjectStore(_gudPath));
    }

    [TearDown]
    public void TearDown()
    {
        var root = Path.GetDirectoryName(_gudPath)!;
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    [Test]
    public void IsAncestor_ReturnsTrue_ForSelf()
    {
        var c = WriteCommit("tree", [], "a", "root");
        Assert.That(CommitGraph.IsAncestor(_objects, c, c), Is.True);
    }

    [Test]
    public void IsAncestor_WalksAllParents()
    {
        // root <- a
        //      <- b
        // merge(a,b) = m
        var root = WriteCommit(EmptyTree(), [], "a", "root");
        var a = WriteCommit(EmptyTree(), [root], "a", "a");
        var b = WriteCommit(EmptyTree(), [root], "a", "b");
        var m = WriteCommit(EmptyTree(), [a, b], "a", "merge");

        Assert.That(CommitGraph.IsAncestor(_objects, b, m), Is.True);
        Assert.That(CommitGraph.IsAncestor(_objects, a, m), Is.True);
        Assert.That(CommitGraph.IsAncestor(_objects, root, m), Is.True);
        Assert.That(CommitGraph.IsAncestor(_objects, m, a), Is.False);
    }

    [Test]
    public void FindMergeBase_ReturnsCommonAncestor()
    {
        var root = WriteCommit(EmptyTree(), [], "a", "root");
        var a = WriteCommit(EmptyTree(), [root], "a", "a");
        var b = WriteCommit(EmptyTree(), [root], "a", "b");

        Assert.That(CommitGraph.FindMergeBase(_objects, a, b), Is.EqualTo(root));
    }

    [Test]
    public void FindMergeBase_ReturnsEqualWhenSame()
    {
        var root = WriteCommit(EmptyTree(), [], "a", "root");
        Assert.That(CommitGraph.FindMergeBase(_objects, root, root), Is.EqualTo(root));
    }

    [Test]
    public void FindMergeBase_ReturnsNull_ForUnrelated()
    {
        var a = WriteCommit(EmptyTree(), [], "a", "a");
        var b = WriteCommit(EmptyTree(), [], "a", "b");
        Assert.That(CommitGraph.FindMergeBase(_objects, a, b), Is.Null);
    }

    private string EmptyTree()
    {
        return _objects.WriteObject(ObjectType.Tree, Tree.SerializeTree(Array.Empty<TreeEntry>()));
    }

    private string WriteCommit(string treeHash, IReadOnlyList<string> parents, string author, string message)
    {
        var sb = new StringBuilder();
        sb.Append("tree ").Append(treeHash).Append('\n');
        foreach (var p in parents)
            sb.Append("parent ").Append(p).Append('\n');
        sb.Append("author ").Append(author).Append('\n');
        sb.Append("timestamp ").Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).Append('\n');
        sb.Append('\n').Append(message);
        return _objects.WriteObject(ObjectType.Commit, Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
