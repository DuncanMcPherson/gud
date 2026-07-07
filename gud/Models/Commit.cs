using System.Text;
using gud.Utilities;

namespace gud.Models;

public sealed class Commit
{
    public string TreeHash { get; }
    public IReadOnlyList<string> ParentHashes { get; }
    public string Author { get; }
    public string Message { get; }
    public DateTimeOffset Timestamp { get; }
    public string Hash { get; }
    
    public Commit(string treeHash, IReadOnlyList<string> parentHashes, string author, string message, DateTimeOffset timestamp)
    {
        TreeHash = treeHash;
        ParentHashes = parentHashes;
        Author = author;
        Message = message;
        Timestamp = timestamp;
        Hash = ObjectHasher.ComputeHash("commit", SerializeCommit(this));
    }
    
    private static byte[] SerializeCommit(Commit commit)
    {
        var sb = new StringBuilder();
        sb.Append("tree ").Append(commit.TreeHash).Append('\n');
        foreach (var parent in commit.ParentHashes)
            sb.Append("parent ").Append(parent).Append('\n');
        sb.Append("author ").Append(commit.Author).Append('\n');
        sb.Append("timestamp ").Append(commit.Timestamp.ToUnixTimeSeconds()).Append('\n');
        sb.Append('\n').Append(commit.Message);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}