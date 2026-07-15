using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;

namespace gud.Core.Utilities;

public enum ChangeType { Added, Modified, Deleted }

public record StatusEntry(ChangeType ChangeType, string Path);

public static class WorkingTreeStatus
{
    public static List<StatusEntry> Compute(string root, string? headTreeHash, ObjectRepository objects,
        GudIgnoreMatcher ignore)
    {
        var results = new List<StatusEntry>();
        var oldEntries = string.IsNullOrEmpty(headTreeHash)
            ? new Dictionary<string, string>()
            : FlattenTree(objects, headTreeHash, "");

        var seenPaths = new HashSet<string>();
        WalkDirectory(root, root, ignore, oldEntries, seenPaths, results, objects);

        foreach (var (path, _) in oldEntries)
        {
            if (!seenPaths.Contains(path))
                results.Add(new StatusEntry(ChangeType.Deleted, path));
        }
        return results;
    }
    
    private static void WalkDirectory(
        string dir,
        string root,
        GudIgnoreMatcher ignore,
        Dictionary<string, string> oldEntries,
        HashSet<string> seenPaths,
        List<StatusEntry> results,
        ObjectRepository objects)
    {
        foreach (var file in Directory.GetFiles(dir))
        {
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (ignore.IsIgnored(relative)) continue;

            seenPaths.Add(relative);
            var actualHash = ObjectHasher.ComputeHash("blob", File.ReadAllBytes(file));
            if (!oldEntries.TryGetValue(relative, out var oldHash))
                results.Add(new StatusEntry(ChangeType.Added, relative));
            else if (oldHash != actualHash)
                results.Add(new StatusEntry(ChangeType.Modified, relative));
        }

        foreach (var subdir in Directory.GetDirectories(dir))
        {
            if (Path.GetFileName(subdir) == ".gud") continue;
            var relative = Path.GetRelativePath(root, subdir).Replace(Path.DirectorySeparatorChar, '/');
            if (ignore.IsIgnored(relative)) continue;

            WalkDirectory(subdir, root, ignore, oldEntries, seenPaths, results, objects);
        }
    }

    private static Dictionary<string, string> FlattenTree(ObjectRepository objects, string treeHash, string? prefix)
    {
        var result = new Dictionary<string, string>();
        var tree = Tree.Read(objects, treeHash);

        foreach (var entry in tree.Entries)
        {
            var path = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}/{entry.Name}";
            if (entry.Type == TreeEntryType.Blob)
                result[path] = entry.Hash;
            else
                foreach (var kvp in FlattenTree(objects, entry.Hash, path))
                    result[kvp.Key] = kvp.Value;
        }

        return result;
    }
}