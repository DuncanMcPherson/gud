using gud.Core.Models;
using gud.Core.Repository;

namespace gud.Core.Services;

public static class ObjectGraphWalker
{
    public static HashSet<string> CollectReachable(ObjectRepository objects, string commitHash, HashSet<string>? seed = null)
    {
        var visited = seed != null ? new HashSet<string>(seed) : new HashSet<string>();
        var newlyFound = new HashSet<string>();
        var commitQueue = new Queue<string>();
        commitQueue.Enqueue(commitHash);

        while (commitQueue.Count > 0)
        {
            var current = commitQueue.Dequeue();
            if (!visited.Add(current)) continue;
            newlyFound.Add(current);

            var commit = Commit.Read(objects, current);
            CollectTree(objects, commit.TreeHash, visited, newlyFound);

            foreach (var parent in commit.ParentHashes)
            {
                commitQueue.Enqueue(parent);
            }
        }

        return newlyFound;
    }

    private static void CollectTree(ObjectRepository objects, string treeHash, HashSet<string> visited, HashSet<string> newlyFound)
    {
        if (!visited.Add(treeHash)) return;
        newlyFound.Add(treeHash);

        var tree = Tree.Read(objects, treeHash);
        foreach (var entry in tree.Entries)
        {
            if (entry.Type == TreeEntryType.Blob)
            {
                if (visited.Add(entry.Hash))
                    newlyFound.Add(entry.Hash);
            }
            else
                CollectTree(objects, entry.Hash, visited, newlyFound);
        }
    }
}