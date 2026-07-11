using System.Diagnostics.CodeAnalysis;
using gud.Core.Models;
using gud.Core.Repository;

namespace gud.Core.Utilities;

/// <summary>
/// Provides functionality for synchronizing a working tree with a specified target state.
/// </summary>
[ExcludeFromCodeCoverage]
public static class WorkingTreeSync
{
    /// <summary>
    /// Synchronizes the working tree at the specified path with the target tree state defined
    /// by the provided hashes, adding, removing, or updating files and directories as necessary.
    /// </summary>
    /// <param name="oldTreeHash">
    /// The hash of the previous tree state. Can be null if there is no existing state.
    /// </param>
    /// <param name="newTreeHash">
    /// The hash of the target tree to synchronize with.
    /// </param>
    /// <param name="path">
    /// The root directory path for the working tree to be synchronized.
    /// </param>
    /// <param name="repo">
    /// The repository instance used to access object data and interact with the file system.
    /// </param>
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

    /// <summary>
    /// Reads the entries of a tree object identified by the specified hash from the given object repository.
    /// </summary>
    /// <param name="hash">The hash of the tree object to read.</param>
    /// <param name="repo">The repository from which the tree object is retrieved.</param>
    /// <returns>A list of <see cref="TreeEntry"/> objects contained within the tree.</returns>
    private static List<TreeEntry> ReadTreeEntries(string hash, ObjectRepository repo)
    {
        var tree = Tree.Read(repo, hash);
        return tree.Entries.ToList();
    }
}