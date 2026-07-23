using gud.Core.Models;
using gud.Core.Repository;

namespace gud.Core.Services;

/// <summary>
/// Builds nested tree objects from a flat path → blob-hash map.
/// </summary>
public static class TreeBuilder
{
    /// <summary>
    /// Writes tree objects for the given flat map and returns the root tree hash.
    /// Paths use forward slashes (e.g. "dir/file.txt"). Empty map yields an empty tree.
    /// </summary>
    public static string WriteTreeFromFlatMap(ObjectRepository repo, IReadOnlyDictionary<string, string> pathToBlobHash)
    {
        // Nested structure: directory path "" is root; each node maps name -> (blob hash | nested dict)
        var root = new DirNode();
        foreach (var (path, blobHash) in pathToBlobHash)
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var node = root;
            for (var i = 0; i < parts.Length - 1; i++)
            {
                if (!node.Dirs.TryGetValue(parts[i], out var child))
                {
                    child = new DirNode();
                    node.Dirs[parts[i]] = child;
                }
                node = child;
            }

            node.Files[parts[^1]] = blobHash;
        }

        return WriteNode(repo, root);
    }

    private static string WriteNode(ObjectRepository repo, DirNode node)
    {
        var entries = new List<TreeEntry>();

        foreach (var (name, hash) in node.Files)
            entries.Add(new TreeEntry { Name = name, Hash = hash, Type = TreeEntryType.Blob });

        foreach (var (name, child) in node.Dirs)
        {
            var childHash = WriteNode(repo, child);
            entries.Add(new TreeEntry { Name = name, Hash = childHash, Type = TreeEntryType.Tree });
        }

        var sorted = entries.OrderBy(e => e.Name).ToList();
        return repo.WriteObject(ObjectType.Tree, Tree.SerializeTree(sorted));
    }

    private sealed class DirNode
    {
        public Dictionary<string, string> Files { get; } = new();
        public Dictionary<string, DirNode> Dirs { get; } = new();
    }
}
