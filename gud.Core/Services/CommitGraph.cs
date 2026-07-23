using gud.Core.Models;
using gud.Core.Repository;

namespace gud.Core.Services;

/// <summary>
/// Commit DAG helpers: multi-parent ancestry and merge-base discovery.
/// </summary>
public static class CommitGraph
{
    /// <summary>
    /// Returns true if <paramref name="ancestor"/> is reachable by walking all parents
    /// from <paramref name="descendant"/> (inclusive of equal hashes).
    /// </summary>
    public static bool IsAncestor(ObjectRepository objects, string ancestor, string descendant)
    {
        if (ancestor == descendant) return true;

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(descendant);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;
            if (current == ancestor) return true;

            var commit = Commit.Read(objects, current);
            foreach (var parent in commit.ParentHashes)
                queue.Enqueue(parent);
        }

        return false;
    }

    /// <summary>
    /// Finds a merge base of two commits: BFS from <paramref name="b"/> until a commit
    /// that is an ancestor of <paramref name="a"/> (including <paramref name="a"/> itself).
    /// When multiple bases exist, returns the first found (no recursive multi-base strategy).
    /// Returns null if the histories are unrelated.
    /// </summary>
    public static string? FindMergeBase(ObjectRepository objects, string a, string b)
    {
        if (a == b) return a;

        var ancestorsOfA = CollectAncestors(objects, a);
        if (ancestorsOfA.Contains(b)) return b;

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(b);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;
            if (ancestorsOfA.Contains(current)) return current;

            var commit = Commit.Read(objects, current);
            foreach (var parent in commit.ParentHashes)
                queue.Enqueue(parent);
        }

        return null;
    }

    /// <summary>
    /// Collects the set of all commits reachable from <paramref name="commitHash"/>
    /// including itself, walking every parent edge.
    /// </summary>
    public static HashSet<string> CollectAncestors(ObjectRepository objects, string commitHash)
    {
        var ancestors = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(commitHash);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!ancestors.Add(current)) continue;

            var commit = Commit.Read(objects, current);
            foreach (var parent in commit.ParentHashes)
                queue.Enqueue(parent);
        }

        return ancestors;
    }
}
