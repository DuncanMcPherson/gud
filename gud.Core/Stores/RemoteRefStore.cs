using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;

namespace gud.Core.Stores;

public class RemoteRefStore(string gudPath)
{
    private readonly string _remoteRefsPath = Path.Combine(gudPath, "refs", "remotes");

    public string? GetTrackedCommit(string remote, string branch)
    {
        var path = Path.Combine(_remoteRefsPath, remote, branch.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path) ? File.ReadAllText(path).Trim() is { Length: > 0 } c ? c : null : null;
    }

    /// <summary>
    /// Resolves a remote-tracking name such as <c>origin/feat/pull</c> to its commit hash.
    /// </summary>
    public string? GetTrackedCommitByName(string trackingName)
    {
        if (string.IsNullOrWhiteSpace(trackingName)) return null;
        var path = Path.Combine(_remoteRefsPath, trackingName.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) return null;
        var content = File.ReadAllText(path).Trim();
        return string.IsNullOrEmpty(content) ? null : content;
    }

    /// <summary>
    /// True when <paramref name="trackingName"/> is a stored remote-tracking ref
    /// (e.g. <c>origin/main</c> or <c>origin/feat/pull</c>).
    /// </summary>
    public bool Exists(string trackingName)
    {
        if (string.IsNullOrWhiteSpace(trackingName)) return false;
        var path = Path.Combine(_remoteRefsPath, trackingName.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path);
    }

    /// <summary>
    /// Splits <c>origin/feat/pull</c> into remote <c>origin</c> and branch <c>feat/pull</c>
    /// when that remote-tracking ref exists on disk.
    /// </summary>
    public bool TrySplitTrackingName(string trackingName, out string remote, out string branch)
    {
        remote = "";
        branch = "";
        if (!Exists(trackingName)) return false;

        var slash = trackingName.IndexOf('/');
        if (slash <= 0 || slash >= trackingName.Length - 1) return false;

        remote = trackingName[..slash];
        branch = trackingName[(slash + 1)..];
        // Verify the split matches on-disk layout (remote is first path segment)
        var expected = GetTrackedCommit(remote, branch);
        return expected is not null;
    }

    public void SetTrackedCommit(string remote, string branch, string commit)
    {
        var path = Path.Combine(_remoteRefsPath, remote, branch.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, commit);
    }

    public async Task<int> FetchReachable(
        GudRemoteClient client,
        ObjectStore objectStore,
        ObjectRepository objects,
        string startCommit)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<(string hash, ObjectType type)>();
        queue.Enqueue((startCommit, ObjectType.Commit));

        var fetchedCount = 0;

        while (queue.Count > 0)
        {
            var (hash, _) = queue.Dequeue();
            if (!visited.Add(hash)) continue;

            byte[] rawContent;
            if (objectStore.Exists(hash))
            {
                rawContent = objects.ReadRawObjectFile(hash);
            }
            else
            {
                rawContent = await client.GetObjectAsync(hash);
                objectStore.Write(hash, rawContent);
                fetchedCount++;
            }

            var (type, content) = ObjectRepository.ParseRaw(rawContent);
            switch (type)
            {
                case ObjectType.Commit:
                    var commit = Commit.Read(content);
                    queue.Enqueue((commit.TreeHash, ObjectType.Tree));
                    foreach (var parent in commit.ParentHashes)
                        queue.Enqueue((parent, ObjectType.Commit));
                    break;
                case ObjectType.Tree:
                    var tree = Tree.Read(content);
                    foreach (var entry in tree.Entries)
                        queue.Enqueue((entry.Hash, entry.Type == TreeEntryType.Blob ? ObjectType.Blob : ObjectType.Tree));
                    break;
                case ObjectType.Blob:
                    // Leaf node, nothing to do
                    break;
            }
        }

        return fetchedCount;
    }
}