using System.Security.Cryptography;
using System.Text;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Utilities;

namespace gud.Core.Services;

/// <summary>
/// The CommitBuilder class is responsible for managing the creation of commits
/// within a repository. It provides functionality to commit changes in a directory
/// and check for uncommitted changes in the repository.
/// </summary>
public class CommitBuilder(ObjectRepository repo)
{
    /// <summary>
    /// A constant hash representing an empty Git tree object.
    /// This value is computed using the SHA-256 hash of an empty tree header combined with
    /// empty content. It is used to identify the state of an empty repository or directory
    /// structure in the version control system.
    /// </summary>
    private static readonly string EmptyTreeHash = ComputeEmptyTreeHash();

    /// <summary>
    /// Creates a new commit in the repository by capturing the current state of the specified directory
    /// and associating it with provided metadata such as parent hashes, author, and commit message.
    /// </summary>
    /// <param name="path">The path to the directory to be committed.</param>
    /// <param name="parentHash">A list of parent commit hashes representing the commit's ancestry.</param>
    /// <param name="author">The name of the author creating the commit.</param>
    /// <param name="message">The commit message describing the changes being committed.</param>
    /// <returns>
    /// A string representing the hash of the created commit object.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there are no changes to commit because the working tree is clean,
    /// or if the commit would result in an empty repository with no content.
    /// </exception>
    public string CommitDirectory(string path, IReadOnlyList<string> parentHash, string author, string message)
    {
        var rootPath = GudRepository.RequireRoot();
        var ignoreMatcher = new GudIgnoreMatcher(Path.Combine(rootPath, ".gudignore"));
        var treeHash = WriteTree(path, rootPath, ignoreMatcher);
        if (parentHash.Count > 0)
        {
            var (_, content) = repo.ReadObject(parentHash[0]);
            var commit = Commit.Read(content);
            if (treeHash == commit.TreeHash)
                throw new InvalidOperationException("No content to commit. Working tree clean.");
        }
        if (treeHash == EmptyTreeHash && parentHash.Count == 0)
            throw new InvalidOperationException("No content to commit. No files exist.");
        var commitContent = SerializeCommitFields(treeHash, parentHash, author, message);
        return repo.WriteObject(ObjectType.Commit, commitContent);
    }

    /// <summary>
    /// Determines if there are uncommitted changes in the repository by comparing the current working tree state
    /// with the tree hash of the given HEAD commit.
    /// </summary>
    /// <param name="root">The root directory of the repository.</param>
    /// <param name="headCommit">The hash of the HEAD commit to compare against. Can be null or empty.</param>
    /// <returns>
    /// Returns true if there are uncommitted changes in the repository; otherwise, false.
    /// </returns>
    public bool HasUncommittedChanges(string root, string? headCommit)
    {
        if (string.IsNullOrEmpty(headCommit)) return false;
        var repoRoot = GudRepository.RequireRoot();
        var currentTreeHash = ComputeTreeHash(root, repoRoot, new GudIgnoreMatcher(Path.Combine(repoRoot, ".gudignore")));
        var (_, headContent) = repo.ReadObject(headCommit);
        var commit = Commit.Read(headContent);
        return currentTreeHash != commit.TreeHash;
    }

    /// <summary>
    /// Calculates the cryptographic hash for an empty tree object in the repository.
    /// This method generates a SHA-256 hash based on the standardized format of an empty
    /// Git tree object, which consists of a header specifying the tree type and size,
    /// followed by no content.
    /// </summary>
    /// <returns>
    /// A string representing the hexadecimal-encoded SHA-256 hash of an empty tree object
    /// in lowercase.
    /// </returns>
    private static string ComputeEmptyTreeHash()
    {
        var emptyContent = Array.Empty<byte>();
        var header = "tree 0\0"u8.ToArray();
        var full = header.Concat(emptyContent).ToArray();
        return Convert.ToHexString(SHA256.HashData(full)).ToLowerInvariant();
    }

    /// <summary>
    /// Computes a hash representing the tree structure and the content within a directory,
    /// excluding paths ignored by the specified ignore matcher.
    /// </summary>
    /// <param name="path">The directory path for which the tree hash is computed.</param>
    /// <param name="rootPath">The root path of the repository, used to calculate relative paths.</param>
    /// <param name="ignoreMatcher">An instance of <see cref="GudIgnoreMatcher"/> to determine which paths should be ignored.</param>
    /// <returns>A hash string representing the directory's tree structure and content.</returns>
    private string ComputeTreeHash(string path, string rootPath, GudIgnoreMatcher ignoreMatcher)
    {
        var entries = (from file in Directory.GetFiles(path)
            let relativePath = Path.GetRelativePath(rootPath, file)
            where !ignoreMatcher.IsIgnored(relativePath)
            let content = File.ReadAllBytes(file)
            let hash = ObjectHasher.ComputeHash("blob", content)
            select new TreeEntry { Hash = hash, Name = Path.GetFileName(file), Type = TreeEntryType.Blob }).ToList();
        entries.AddRange(
            from subdir in Directory.GetDirectories(path)
            where !subdir.EndsWith(".gud")
            let relativeSubdirPath = Path.GetRelativePath(rootPath, subdir)
            where !ignoreMatcher.IsIgnored(relativeSubdirPath)
                let hash = ComputeTreeHash(subdir, rootPath, ignoreMatcher)
                    select new TreeEntry{Name = Path.GetFileName(subdir), Hash = hash, Type = TreeEntryType.Tree});
        var sortedEntries = entries.OrderBy(e => e.Name).ToList();
        return ObjectHasher.ComputeHash("tree", Tree.SerializeTree(sortedEntries));
    }

    /// <summary>
    /// Recursively creates a tree object representation for a given directory, including its
    /// files and subdirectories, while respecting defined ignore patterns.
    /// </summary>
    /// <param name="path">The absolute path of the directory to process.</param>
    /// <param name="rootPath">The root directory of the repository, used for relative path resolution.</param>
    /// <param name="ignoreMatcher">An instance of <c>GudIgnoreMatcher</c>, used to identify files or directories to ignore based on configured patterns.</param>
    /// <returns>A SHA-1 hash of the created tree object, representing the directory structure.</returns>
    private string WriteTree(string path, string rootPath, GudIgnoreMatcher ignoreMatcher)
    {
        var entries = (
            from file in Directory.GetFiles(path)
            let relativePath = Path.GetRelativePath(rootPath, file)
            where !ignoreMatcher.IsIgnored(relativePath)
            let content = File.ReadAllBytes(file)
            let hash = repo.WriteObject(ObjectType.Blob, content)
            select new TreeEntry
            {
                Hash = hash, 
                Name = Path.GetFileName(file),
                Type = TreeEntryType.Blob
            }).ToList();
        entries.AddRange(
            from subdir in Directory.GetDirectories(path)
            let relativeSubdirPath = Path.GetRelativePath(rootPath, subdir)
            where !subdir.EndsWith(".gud")
            where !ignoreMatcher.IsIgnored(relativeSubdirPath)
                let hash = WriteTree(subdir, rootPath, ignoreMatcher)
                    select new TreeEntry{Name = Path.GetFileName(subdir), Hash = hash, Type = TreeEntryType.Tree});

        var sortedEntries = entries.OrderBy(e => e.Name).ToList();
        return repo.WriteObject(ObjectType.Tree, Tree.SerializeTree(sortedEntries));
    }

    /// <summary>
    /// Serializes commit information fields into a byte array format suitable for storage in the repository.
    /// </summary>
    /// <param name="treeHash">The hash representing the root of the file tree being committed.</param>
    /// <param name="parentHash">A list of hashes representing the parent commits.</param>
    /// <param name="author">The author of the commit, including name and email.</param>
    /// <param name="message">The commit message describing the changes.</param>
    /// <returns>A byte array containing the serialized commit fields.</returns>
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