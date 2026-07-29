using System.Text;
using gud.Commands;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Stores;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.IntegrationTests;

[TestFixture]
public class CheckoutRemoteTrackingTests : TestRepoWithCommitBase
{
    protected override CommandAppTester BuildApp()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<CheckoutCommand>();
        return app;
    }

    [Test]
    public void ShouldCheckoutRemoteTrackingRef_CreatesLocalBranch()
    {
        var gud = Path.Combine(RepoPath, ".gud");
        var objects = new ObjectRepository(new ObjectStore(gud));
        var tip = SeedFeatureCommit(objects, "feature content\n");

        var remoteRefs = new RemoteRefStore(gud);
        remoteRefs.SetTrackedCommit("origin", "feat/pull", tip);

        // Resolve path works for nested remote branch
        Assert.That(new BranchStore(gud).ResolveTarget("origin/feat/pull"), Is.EqualTo(tip));

        var result = App.Run("origin/feat/pull");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0), result.Output);
            Assert.That(result.Output, Does.Contain("feat/pull"));
            Assert.That(File.Exists(Path.Combine(gud, "refs", "heads", "feat", "pull")), Is.True);
            Assert.That(new RefStore(gud).CurrentBranchName(), Is.EqualTo("feat/pull"));
            Assert.That(new RefStore(gud).GetHead(), Is.EqualTo(tip));
            Assert.That(File.ReadAllText(Path.Combine(RepoPath, "feature.txt")), Is.EqualTo("feature content\n"));
        }
    }

    [Test]
    public void ShouldResolveOriginMain_RemoteTracking()
    {
        var gud = Path.Combine(RepoPath, ".gud");
        var head = new RefStore(gud).GetHead()!;
        new RemoteRefStore(gud).SetTrackedCommit("origin", "main", head);

        Assert.That(new BranchStore(gud).ResolveTarget("origin/main"), Is.EqualTo(head));
    }

    private static string SeedFeatureCommit(ObjectRepository objects, string content)
    {
        var blob = objects.WriteObject(ObjectType.Blob, Encoding.UTF8.GetBytes(content));
        var tree = objects.WriteObject(ObjectType.Tree,
            Tree.SerializeTree([new TreeEntry { Name = "feature.txt", Hash = blob, Type = TreeEntryType.Blob }]));
        var body = Encoding.UTF8.GetBytes(
            $"tree {tree}\nauthor t\ntimestamp {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}\n\nfeat");
        return objects.WriteObject(ObjectType.Commit, body);
    }
}
