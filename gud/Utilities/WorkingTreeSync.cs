using System.Diagnostics.CodeAnalysis;
using gud.Models;
using gud.Repository;

namespace gud.Utilities;

[ExcludeFromCodeCoverage]
public static class WorkingTreeSync
{
    public static void SyncWorkingTree(string? oldTreeHash, string newTreeHash, string path, ObjectRepository repo)
    {
        var oldEntries = !string.IsNullOrEmpty(oldTreeHash) ? ReadTreeEntries(oldTreeHash, repo) : new List<TreeEntry>();
        var newEntries = ReadTreeEntries(newTreeHash, repo);
        
        var oldByName = oldEntries.ToDictionary(e => e.Name);
        var newByName = newEntries.ToDictionary(e => e.Name);
        
        // remove files/dirs that existed before but don't exist in target
        foreach (var name in oldByName.Keys.Except(newByName.Keys))
        {
            var fullPath = Path.Combine(path, name);
            if (Directory.Exists(fullPath)) Directory.Delete(fullPath, true);
            else if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        foreach (var entry in newEntries)
        {
            var fullPath = Path.Combine(path, entry.Name);
            if (entry.Type == TreeEntryType.Blob)
            {
                if (oldByName.TryGetValue(entry.Name, out var oldEntry) && oldEntry.Hash == entry.Hash)
                    continue; // File was not changed, skip write

                var dir = Path.GetDirectoryName(fullPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var (_, content) = repo.ReadObject(entry.Hash);
                File.WriteAllBytes(fullPath, content);
            }
            else
            {
                var oldChildHash = oldByName.TryGetValue(entry.Name, out var oe) && oe.Type == TreeEntryType.Tree
                    ? oe.Hash
                    : null;
                SyncWorkingTree(oldChildHash, entry.Hash, fullPath, repo);
            }
        }
    }

    private static List<TreeEntry> ReadTreeEntries(string hash, ObjectRepository repo)
    {
        var tree = Tree.Read(repo, hash);
        return tree.Entries.ToList();
    }
}