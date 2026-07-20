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