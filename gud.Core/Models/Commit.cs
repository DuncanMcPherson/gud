using System.Text;
using gud.Core.Repository;
using gud.Core.Utilities;

namespace gud.Core.Models;

/// <summary>
/// Represents a single commit in a version control system.
/// </summary>
/// <remarks>
/// A commit serves as a snapshot of the repository's state at a particular point in time
/// and contains metadata including author information, the associated tree, parent commit hashes,
/// a commit message, and a unique hash identifier.
/// </remarks>
public sealed class Commit
{
    /// <summary>
    /// Gets the hash of the tree associated with a commit.
    /// The tree hash serves as a reference to the tree object that represents the state of the
    /// file system at the time the commit was created. It is an essential component of the
    /// commit's structure and determines the snapshot of files and directories that are part
    /// of the commit.
    /// </summary>
    public string TreeHash { get; }

    /// <summary>
    /// Gets a read-only list of parent commit hashes associated with this commit.
    /// </summary>
    /// <remarks>
    /// Each parent hash represents a direct predecessor of this commit in the version control history.
    /// For merge commits, this list may contain multiple parent hashes,
    /// whereas for a regular commit, it typically contains only one hash. If this is the initial commit
    /// in a repository, the list will be empty.
    /// </remarks>
    public IReadOnlyList<string> ParentHashes { get; }

    /// <summary>
    /// Gets the author information associated with the commit.
    /// </summary>
    /// <remarks>
    /// The author represents the individual or entity responsible for creating the commit.
    /// This value is typically formatted as a string containing the name and email address
    /// of the author (e.g., "John Doe"). It is used to track the
    /// origin of changes in a repository.
    /// </remarks>
    public string Author { get; }

    /// <summary>
    /// Gets the commit message associated with this commit.
    /// </summary>
    /// <remarks>
    /// The message provides a description or context for the changes introduced
    /// in the commit. It is typically written by the author at the time of commit
    /// creation and forms part of the commit's serialized content.
    /// </remarks>
    public string Message { get; }

    /// <summary>
    /// Gets the timestamp associated with the commit.
    /// </summary>
    /// <remarks>
    /// The timestamp indicates the date and time when the commit was created
    /// and is represented as a <see cref="DateTimeOffset"/>. It is typically stored
    /// as a Unix timestamp within the commit data.
    /// </remarks>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the unique hash identifier for the current commit object.
    /// This hash is computed based on the serialized content of the commit, including
    /// its tree hash, parent hashes, author information, timestamp, and message.
    /// It serves as a cryptographic fingerprint to uniquely identify the commit
    /// and ensure data integrity within the repository.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Represents a commit in a version control system. A commit object encapsulates
    /// the state of the repository at a specific point in time, including the associated
    /// tree, parent commits, author, message, timestamp, and computed hash.
    /// </summary>
    public Commit(string treeHash, IReadOnlyList<string> parentHashes, string author, string message, DateTimeOffset timestamp)
    {
        TreeHash = treeHash;
        ParentHashes = parentHashes;
        Author = author;
        Message = message;
        Timestamp = timestamp;
        Hash = ObjectHasher.ComputeHash("commit", SerializeCommit(this));
    }

    /// Serializes a commit object into a byte array representation.
    /// The serialized representation includes the tree hash, parent hashes, author,
    /// timestamp, and message, encoded in a specific format suitable for storage or
    /// transmission.
    /// <param name="commit">The commit object to be serialized.</param>
    /// <returns>A byte array representing the serialized commit data.</returns>
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

    /// <summary>
    /// Writes the current commit object to the given object repository.
    /// </summary>
    /// <param name="repo">The repository where the commit object will be written.</param>
    public void Write(ObjectRepository repo) => repo.WriteObject(ObjectType.Commit, SerializeCommit(this));

    /// <summary>
    /// Reads a commit object from the specified repository using its hash.
    /// </summary>
    /// <param name="repo">The repository from which the commit object will be read.</param>
    /// <param name="hash">The hash of the commit object to read.</param>
    /// <returns>The commit object corresponding to the specified hash.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the object corresponding to the hash is not of type Commit.</exception>
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

    /// <summary>
    /// Reads a serialized commit from the provided byte array and returns a reconstructed Commit object.
    /// </summary>
    /// <param name="content">The byte array containing the serialized commit data.</param>
    /// <returns>A <see cref="Commit"/> object constructed from the provided serialized data.</returns>
    public static Commit Read(byte[] content)
    {
        var contentStr = Encoding.UTF8.GetString(content);
        var lines = contentStr.Split('\n');
        var skipLines = 0;
        // First line in the commit is always the tree hash.
        var treeHash = lines[skipLines++].Split(' ')[1];
        var parents = lines.Where(l => l.StartsWith("parent ")).Select(l => l.Split(' ')[1]).ToList();
        skipLines += parents.Count;
        var author = lines.Skip(skipLines++).First().Split(' ').Skip(1).Aggregate((a, b) => $"{a} {b}");
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(lines.Skip(skipLines++).First().Split(' ')[1]));
        skipLines++;
        var message = lines.Skip(skipLines).Aggregate((a, b) => a + '\n' + b);
        return new Commit(treeHash, parents, author, message, timestamp);
    }

    /// Returns a string representation of the current Commit object, including its hash,
    /// tree hash, parent hashes, author, timestamp, and message.
    /// <returns>A string detailing the hash, tree hash, parent hashes, author, timestamp, and message of the commit.</returns>
    public override string ToString()
    {
        return $"Commit: {Hash}\n\tTree: {TreeHash}\n\tParents: {string.Join(", ", ParentHashes)}\n\tAuthor: {Author}\n\tTimestamp: {Timestamp}\n\tMessage: {Message}";
    }
}