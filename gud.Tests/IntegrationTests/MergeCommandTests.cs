using gud.Commands;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Stores;
using gud.Core.Utilities;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.IntegrationTests;

[TestFixture]
public class MergeCommandTests : TestRepoWithConfigBase
{
    private CommandAppTester _commitApp = null!;
    private CommandAppTester _branchApp = null!;
    private CommandAppTester _checkoutApp = null!;
    private CommandAppTester _mergeApp = null!;

    protected override CommandAppTester BuildApp()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<MergeCommand>();
        return app;
    }

    [SetUp]
    public void SetUpMergeHarness()
    {
        _commitApp = new CommandAppTester();
        _commitApp.SetDefaultCommand<CommitCommand>();
        _branchApp = new CommandAppTester();
        _branchApp.SetDefaultCommand<BranchCommand>();
        _checkoutApp = new CommandAppTester();
        _checkoutApp.SetDefaultCommand<CheckoutCommand>();
        _mergeApp = App;

        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "base\n");
        _commitApp.Run("-m", "initial");
    }

    [Test]
    public void FastForward_AdvancesBranchAndWorkingTree()
    {
        _branchApp.Run("feature");
        _checkoutApp.Run("feature");
        File.WriteAllText(Path.Combine(RepoPath, "feature.txt"), "feature\n");
        _commitApp.Run("-m", "on feature");

        _checkoutApp.Run("main");
        var result = _mergeApp.Run("feature");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Output, Does.Contain("Fast-forward").IgnoreCase);
        Assert.That(File.Exists(Path.Combine(RepoPath, "feature.txt")), Is.True);
        Assert.That(File.ReadAllText(Path.Combine(RepoPath, "feature.txt")), Is.EqualTo("feature\n"));
    }

    [Test]
    public void AlreadyUpToDate()
    {
        _branchApp.Run("feature");
        var result = _mergeApp.Run("feature");
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Output, Does.Contain("Already up to date"));
    }

    [Test]
    public void CleanThreeWay_CreatesMergeCommitWithTwoParents()
    {
        _branchApp.Run("feature");
        _checkoutApp.Run("feature");
        File.WriteAllText(Path.Combine(RepoPath, "feature-only.txt"), "f\n");
        _commitApp.Run("-m", "feature change");

        _checkoutApp.Run("main");
        File.WriteAllText(Path.Combine(RepoPath, "main-only.txt"), "m\n");
        _commitApp.Run("-m", "main change");

        var result = _mergeApp.Run("feature");
        Assert.That(result.ExitCode, Is.EqualTo(0), result.Output);
        Assert.That(File.Exists(Path.Combine(RepoPath, "feature-only.txt")), Is.True);
        Assert.That(File.Exists(Path.Combine(RepoPath, "main-only.txt")), Is.True);

        var gud = Path.Combine(RepoPath, ".gud");
        var refs = new RefStore(gud);
        var head = refs.GetHead()!;
        var commit = Commit.Read(new ObjectRepository(new ObjectStore(gud)), head);
        Assert.That(commit.ParentHashes, Has.Count.EqualTo(2));
        Assert.That(new MergeState(gud).IsInProgress, Is.False);
    }

    [Test]
    public void ConflictingEdits_LeaveMergeStateAndMarkers()
    {
        _branchApp.Run("feature");
        _checkoutApp.Run("feature");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "feature version\n");
        _commitApp.Run("-m", "feature edit");

        _checkoutApp.Run("main");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "main version\n");
        _commitApp.Run("-m", "main edit");

        var result = _mergeApp.Run("feature");
        Assert.That(result.ExitCode, Is.EqualTo(1), result.Output);
        Assert.That(result.Output, Does.Contain("CONFLICT"));

        var gud = Path.Combine(RepoPath, ".gud");
        Assert.That(new MergeState(gud).IsInProgress, Is.True);
        var content = File.ReadAllText(Path.Combine(RepoPath, "base.txt"));
        Assert.That(content, Does.Contain("<<<<<<<"));
        Assert.That(content, Does.Contain("main version"));
        Assert.That(content, Does.Contain("feature version"));
    }

    [Test]
    public void ResolveConflict_ThenCommit_ClearsMergeState()
    {
        _branchApp.Run("feature");
        _checkoutApp.Run("feature");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "feature version\n");
        _commitApp.Run("-m", "feature edit");

        _checkoutApp.Run("main");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "main version\n");
        _commitApp.Run("-m", "main edit");

        Assert.That(_mergeApp.Run("feature").ExitCode, Is.EqualTo(1));

        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "resolved\n");
        var commitResult = _commitApp.Run("-m", "merge resolve");
        Assert.That(commitResult.ExitCode, Is.EqualTo(0), commitResult.Output);

        var gud = Path.Combine(RepoPath, ".gud");
        Assert.That(new MergeState(gud).IsInProgress, Is.False);
        var head = new RefStore(gud).GetHead()!;
        var commit = Commit.Read(new ObjectRepository(new ObjectStore(gud)), head);
        Assert.That(commit.ParentHashes, Has.Count.EqualTo(2));
        Assert.That(File.ReadAllText(Path.Combine(RepoPath, "base.txt")), Is.EqualTo("resolved\n"));
    }

    [Test]
    public void Abort_RestoresPreMergeTree()
    {
        _branchApp.Run("feature");
        _checkoutApp.Run("feature");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "feature version\n");
        _commitApp.Run("-m", "feature edit");

        _checkoutApp.Run("main");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "main version\n");
        _commitApp.Run("-m", "main edit");
        var before = File.ReadAllText(Path.Combine(RepoPath, "base.txt"));

        Assert.That(_mergeApp.Run("feature").ExitCode, Is.EqualTo(1));
        var abort = _mergeApp.Run("--abort");
        Assert.That(abort.ExitCode, Is.EqualTo(0), abort.Output);
        Assert.That(new MergeState(Path.Combine(RepoPath, ".gud")).IsInProgress, Is.False);
        Assert.That(File.ReadAllText(Path.Combine(RepoPath, "base.txt")), Is.EqualTo(before));
    }

    [Test]
    public void DirtyWorkingTree_RefusesMerge()
    {
        _branchApp.Run("feature");
        File.WriteAllText(Path.Combine(RepoPath, "dirty.txt"), "nope");
        var result = _mergeApp.Run("feature");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Output, Does.Contain("uncommitted").IgnoreCase);
    }

    [Test]
    public void Checkout_RefusedDuringMerge()
    {
        _branchApp.Run("feature");
        _checkoutApp.Run("feature");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "feature version\n");
        _commitApp.Run("-m", "feature edit");

        _checkoutApp.Run("main");
        File.WriteAllText(Path.Combine(RepoPath, "base.txt"), "main version\n");
        _commitApp.Run("-m", "main edit");
        Assert.That(_mergeApp.Run("feature").ExitCode, Is.EqualTo(1));

        var checkout = _checkoutApp.Run("feature");
        Assert.That(checkout.ExitCode, Is.EqualTo(1));
        Assert.That(checkout.Output, Does.Contain("merge is in progress").IgnoreCase);
    }
}
