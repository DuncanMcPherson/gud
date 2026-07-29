using System.Text;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;

namespace gud.Tests.Services;

[TestFixture]
public class FetchServiceTests
{
    [Test]
    public void ShouldSkipObjectFetch_WhenTipsEqualAndObjectExists()
    {
        Assert.That(FetchService.ShouldSkipObjectFetch("abc", "abc", tipObjectExists: true), Is.True);
    }

    [Test]
    public void ShouldSkipObjectFetch_WhenTipsEqualButObjectMissing()
    {
        Assert.That(FetchService.ShouldSkipObjectFetch("abc", "abc", tipObjectExists: false), Is.False);
    }

    [Test]
    public void ShouldSkipObjectFetch_WhenTipsDiffer()
    {
        Assert.That(FetchService.ShouldSkipObjectFetch("abc", "def", tipObjectExists: true), Is.False);
    }

    [Test]
    public void ShouldSkipObjectFetch_WhenNoTrackedTip()
    {
        Assert.That(FetchService.ShouldSkipObjectFetch("abc", null, tipObjectExists: true), Is.False);
    }

    [Test]
    public void ShouldSkipObjectFetch_CaseInsensitiveTipCompare()
    {
        Assert.That(FetchService.ShouldSkipObjectFetch("ABC", "abc", tipObjectExists: true), Is.True);
    }

    [Test]
    public async Task FetchReachable_ReusesLocalObjects_WithoutCallingGetObject()
    {
        var root = Path.Combine(Path.GetTempPath(), "gud-fetch-" + Guid.NewGuid());
        var gud = Path.Combine(root, ".gud");
        Directory.CreateDirectory(Path.Combine(gud, "objects"));
        try
        {
            var store = new ObjectStore(gud);
            var objects = new ObjectRepository(store);

            // Seed a minimal commit graph locally
            var blob = objects.WriteObject(ObjectType.Blob, Encoding.UTF8.GetBytes("hi"));
            var tree = objects.WriteObject(ObjectType.Tree,
                Tree.SerializeTree([new TreeEntry { Name = "f.txt", Hash = blob, Type = TreeEntryType.Blob }]));
            var commitContent = Encoding.UTF8.GetBytes(
                $"tree {tree}\nauthor t\ntimestamp {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}\n\nmsg");
            var commit = objects.WriteObject(ObjectType.Commit, commitContent);

            var remoteRefs = new RemoteRefStore(gud);
            // Client that throws if any object download is attempted
            var client = new ThrowingRemoteClient();

            var count = await remoteRefs.FetchReachable(client, store, objects, commit);
            Assert.That(count, Is.EqualTo(0));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    /// <summary>
    /// Minimal stand-in: GudRemoteClient methods are non-virtual, so we use a
    /// subclass only if needed — here we pass a real client base and override
    /// via a local test double by constructing with invalid URL and ensuring
    /// FetchReachable never hits the network when all objects exist.
    /// </summary>
    private sealed class ThrowingRemoteClient : GudRemoteClient
    {
        public ThrowingRemoteClient() : base("http://127.0.0.1:1", "repo", "key")
        {
        }

        // Cannot override GetObjectAsync (non-virtual). Local-only path must never call it.
        // This type documents intent; the test fails if FetchReachable tries HTTP (connection error).
    }
}
