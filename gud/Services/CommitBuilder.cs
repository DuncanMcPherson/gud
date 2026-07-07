using System.Text;
using gud.Models;
using gud.Repository;

namespace gud.Services;

public class CommitBuilder(ObjectRepository repo)
{
    public string CommitDirectory(string path, IReadOnlyList<string> parentHash, string author, string message)
    {
        var treeHash = WriteTree(path);
        var commitContent = SerializeCommitFields(treeHash, parentHash, author, message);
        return repo.WriteObject(ObjectType.Commit, commitContent);
    }

    private string WriteTree(string path)
    {
        var entries = (from file in Directory.GetFiles(path) let content = File.ReadAllBytes(file) let hash = repo.WriteObject(ObjectType.Blob, content) select new TreeEntry { Hash = hash, Name = Path.GetFileName(file), Type = TreeEntryType.Blob }).ToList();
        foreach (var subdir in Directory.GetDirectories(path))
        {
            var pathName = Path.GetFileName(subdir);
            if (pathName == ".gud")
                continue;
            var hash = WriteTree(subdir);
            entries.Add(new TreeEntry { Hash = hash, Name = Path.GetFileName(subdir), Type = TreeEntryType.Tree });
        }

        var sortedEntries = entries.OrderBy(e => e.Name).ToList();
        return repo.WriteObject(ObjectType.Tree, Tree.SerializeTree(sortedEntries));
    }

    private static byte[] SerializeCommitFields(string treeHash, IReadOnlyList<string> parentHash, string author, string message)
    {
        var sb = new StringBuilder();
        sb.Append("tree ").Append(treeHash).Append('\n');
        foreach (var parent in parentHash)
            sb.Append("parent ").Append(parent).Append('\n');
        sb.Append("author ").Append(author).Append('\n');
        sb.Append("timestamp ").Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).Append('\n');
        sb.Append('\n').Append(message);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}