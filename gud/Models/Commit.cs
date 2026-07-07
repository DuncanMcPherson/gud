using System.Text;
using gud.Repository;
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
    
    public void Write(ObjectRepository repo) => repo.WriteObject(ObjectType.Commit, SerializeCommit(this));

    public static Commit Read(ObjectRepository repo, string hash)
    {
        var (type, content) = repo.ReadObject(hash);
        if (type != ObjectType.Commit) throw new InvalidOperationException($"{hash} is not a commit");
        var contentStr = Encoding.UTF8.GetString(content);
        var parts = contentStr.Split('\n');
        var treeHash = parts[0].Split(' ')[1];
        var parents = parts.Where(p => p.StartsWith("parent ")).Select(p => p.Split(' ')[1]).ToList();
        var author = parts.Where(p => p.StartsWith("author ")).Single().Split(' ').Skip(1).Aggregate((a, b) => a + ' ' + b);
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts.Where(p => p.StartsWith("timestamp ")).Single().Split(' ')[1]));
        // Skip 3 lines for tree hash, author, and timestamp.
        // Skip 1 line for each parent.
        // Skip 1 line for the blank line after the timestamp.
        var partsToSkip = 3 + parents.Count + 1;
        var message = parts.Skip(partsToSkip).Aggregate((a, b) => a + '\n' + b);
        return new Commit(treeHash, parents, author, message, timestamp);
    }

    public override string ToString()
    {
        return $"Commit: {Hash}\n\tTree: {TreeHash}\n\tParents: {string.Join(", ", ParentHashes)}\n\tAuthor: {Author}\n\tTimestamp: {Timestamp}\n\tMessage: {Message}";
    }
}