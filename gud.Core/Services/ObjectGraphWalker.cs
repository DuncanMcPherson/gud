using gud.Core.Models;
using gud.Core.Repository;

namespace gud.Core.Services;

public static class ObjectGraphWalker
{
    public static HashSet<string> CollectReachable(ObjectRepository objects, string commitHash)
    {
        var visited = new HashSet<string>();
        var commitQueue = new Queue<string>();
        commitQueue.Enqueue(commitHash);

        while (commitQueue.Count > 0)
        {
            var current = commitQueue.Dequeue();
            if (!visited.Add(current)) continue;

            var (_, content) = objects.ReadObject(current);
            var commit = Commit.Read(content);
            CollectTree(objects, commit.TreeHash, visited);

            foreach (var parent in commit.ParentHashes)
            {
                commitQueue.Enqueue(parent);
            }
        }

        return visited;
    }

    private static void CollectTree(ObjectRepository objects, string treeHash, HashSet<string> visited)
    {
        if (!visited.Add(treeHash)) return;

        var tree = Tree.Read(objects, treeHash);
        foreach (var entry in tree.Entries)
        {
            if (entry.Type == TreeEntryType.Blob)
                visited.Add(entry.Hash);
            else
                CollectTree(objects, entry.Hash, visited);
        }
    }
}