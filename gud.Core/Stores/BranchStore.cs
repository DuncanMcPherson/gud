namespace gud.Core.Stores;

/// <summary>
/// Represents a store for managing branches in a version control system.
/// </summary>
public class BranchStore(string gudPath)
{
    /// <summary>
    /// Represents the file system path to the "refs/heads" directory within the repository structure.
    /// This directory stores branch references for the version control system. Each branch name corresponds
    /// to a file in this directory, and the content of the file typically contains the commit hash the branch points to.
    /// The value of this variable is initialized relative to the repository root path provided in the constructor.
    /// It is used for various branch operations such as listing existing branches, checking branch existence,
    /// and reading or updating branch references.
    /// </summary>
    private readonly string _headsPath = Path.Join(gudPath, "refs", "heads");

    /// <summary>
    /// Retrieves a list of branch names from the repository.
    /// Branch names are derived from the relative paths of files
    /// located in the "refs/heads" directory within the repository.
    /// If the directory does not exist, an empty enumerable is returned.
    /// </summary>
    /// <return>
    /// An enumerable of branch names as strings, each representing
    /// the relative path with directory separators replaced by '/'.
    /// </return>
    public IEnumerable<string?> ListBranches()
    {
        if (!Directory.Exists(_headsPath))
            yield break;

        foreach (var file in Directory.GetFiles(_headsPath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_headsPath, file);
            yield return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
    }

    /// Determines whether a branch with the specified name exists in the branch store.
    /// <param name="branch">The name of the branch to check for existence.</param>
    /// <returns>True if the branch exists; otherwise, false.</returns>
    public bool Exists(string branch) => File.Exists(Path.Join(_headsPath, branch));

    /// <summary>
    /// Retrieves the commit hash associated with the specified branch name.
    /// </summary>
    /// <param name="name">The name of the branch whose commit hash is to be retrieved.</param>
    /// <returns>
    /// The commit hash as a string if the branch exists and is associated with a commit;
    /// otherwise, null if the branch does not exist or is not associated with a commit.
    /// </returns>
    public string? GetCommit(string name)
    {
        var path = ResolvePath(name);
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }

    /// <summary>
    /// Resolves a target name to its corresponding commit hash or verifies if it represents a valid object.
    /// </summary>
    /// <param name="name">
    /// The target name to resolve, which can be a branch or an object hash.
    /// </param>
    /// <returns>
    /// A commit hash if the target is a valid branch or object, or <c>null</c> if the target cannot be resolved.
    /// </returns>
    public string? ResolveTarget(string name)
    {
        if (Exists(name))
            return GetCommit(name)!;

        var objectsPath = Path.Combine(gudPath, "objects", name[..2], name[2..]);
        return File.Exists(objectsPath) ? name : null;
    }

    /// <summary>
    /// Renames an existing branch to a new name within the branch store.
    /// </summary>
    /// <param name="oldName">The name of the existing branch to rename.</param>
    /// <param name="newName">The new name to assign to the branch.</param>
    /// <exception cref="FileNotFoundException">Thrown if the branch specified by <paramref name="oldName"/> does not exist.</exception>
    /// <exception cref="IOException">Thrown if an error occurs while renaming the branch.</exception>
    public void Rename(string oldName, string newName)
    {
        var oldPath = ResolvePath(oldName);
        var newPath = ResolvePath(newName);
        File.Move(oldPath, newPath);
    }

    /// <summary>
    /// Sets the commit hash for the specified branch. If the branch does not exist,
    /// it creates the branch and writes the commit hash to its reference file.
    /// </summary>
    /// <param name="branch">
    /// The name of the branch for which the commit hash should be set.
    /// An exception is thrown if the name refers to an existing folder.
    /// </param>
    /// <param name="commit">
    /// The commit hash to associate with the specified branch.
    /// This hash represents the state of the branch.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified branch name resolves to an existing directory.
    /// </exception>
    public void SetCommit(string branch, string commit)
    {
        var path = ResolvePath(branch);
        if (Directory.Exists(path))
            throw new InvalidOperationException($"Branch '{branch}' cannot be created because it is a folder");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, commit);
    }

    /// <summary>
    /// Resolves the full file system path for a given branch name by combining it with the base
    /// heads directory and replacing forward slashes with the platform's directory separator.
    /// </summary>
    /// <param name="name">The name of the branch whose path is to be resolved.</param>
    /// <returns>The resolved full file system path for the specified branch.</returns>
    private string ResolvePath(string name) => Path.Combine(_headsPath, name.Replace('/', Path.DirectorySeparatorChar));
}