using System.Text;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;

namespace gud.Tests.Services;

[TestFixture]
public class MergeServiceEmptyRepoTests
{
    private string _root = null!;
    private string _gud = null!;
    private ObjectRepository _objects = null!;
    private RefStore _refs = null!;
    private BranchStore _branches = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "gud-empty-merge-" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
        Directory.SetCurrentDirectory(_root);
        _gud = Path.Combine(_root, ".gud");
        Directory.CreateDirectory(Path.Combine(_gud, "objects"));
        Directory.CreateDirectory(Path.Combine(_gud, "refs", "heads"));
        File.WriteAllText(Path.Combine(_gud, "HEAD"), "ref: refs/heads/main\n");
        File.WriteAllText(Path.Combine(_root, ".gudignore"), ".gud/\n");

        _objects = new ObjectRepository(new ObjectStore(_gud));
        _refs = new RefStore(_gud);
        _branches = new BranchStore(_gud);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Test]
    public void Merge_WithNoLocalCommits_InitializesFromTarget()
    {
        var tip = SeedCommit("hello from remote\n", "remote tip");
        var service = new MergeService(_root, _objects, _refs, _branches);

        var result = service.Merge(tip);

        Assert.That(result.Outcome, Is.EqualTo(MergeOutcome.Initialized));
        Assert.That(result.ResultCommitHash, Is.EqualTo(tip));
        Assert.That(_refs.GetHead(), Is.EqualTo(tip));
        Assert.That(File.Exists(Path.Combine(_gud, "refs", "heads", "main")), Is.True);
        Assert.That(File.ReadAllText(Path.Combine(_root, "file.txt")), Is.EqualTo("hello from remote\n"));
    }

    private string SeedCommit(string fileContent, string message)
    {
        var blob = _objects.WriteObject(ObjectType.Blob, Encoding.UTF8.GetBytes(fileContent));
        var tree = _objects.WriteObject(ObjectType.Tree,
            Tree.SerializeTree([new TreeEntry { Name = "file.txt", Hash = blob, Type = TreeEntryType.Blob }]));
        var body = Encoding.UTF8.GetBytes(
            $"tree {tree}\nauthor tester\ntimestamp {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}\n\n{message}");
        return _objects.WriteObject(ObjectType.Commit, body);
    }
}
