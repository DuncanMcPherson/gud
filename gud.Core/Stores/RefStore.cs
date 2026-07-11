namespace gud.Core.Stores;

/// <summary>
/// Provides functionality to manage references and branches in a repository by interacting
/// with internal storage related to the repository's HEAD and branch references.
/// </summary>
public class RefStore(string gudDirectory)
{
    /// <summary>
    /// Represents the file system path to the `HEAD` reference within the specified repository directory.
    /// This path is used to read and write the content of the `HEAD` file, which may point to a branch
    /// reference or contain a specific commit hash.
    /// </summary>
    private readonly string _headPath = Path.Combine(gudDirectory, "HEAD");

    /// Retrieves the current commit hash or branch reference pointed to by the HEAD.
    /// If the HEAD points to a branch reference, this method resolves the reference file
    /// and returns its content, representing the latest commit hash on that branch. If the
    /// reference file does not exist, null is returned. If the HEAD directly contains a
    /// commit hash, that value is returned as-is.
    /// <returns>
    /// The commit hash or the resolved referral pointed to by the HEAD, or null if
    /// the branch reference file is missing.
    /// </returns>
    public string? GetHead()
    {
        var headContent = File.ReadAllText(_headPath).Trim();

        if (headContent.StartsWith("ref: "))
        {
            var refPath = Path.Combine(gudDirectory, headContent[5..]);
            return File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : null;
        }
        
        return headContent;
    }

    /// <summary>
    /// Retrieves the name of the current branch if one is checked out, based on the "HEAD" file.
    /// </summary>
    /// <returns>
    /// The name of the current branch, or null if the repository is in a detached HEAD state or the branch name cannot be determined.
    /// </returns>
    public string? CurrentBranchName()
    {
        var headContent = File.ReadAllText(_headPath).Trim();
        
        if (headContent.StartsWith("ref: refs/heads/"))
        {
            return headContent["ref: refs/heads/".Length..];
        }

        return null;
    }

    /// <summary>
    /// Updates the HEAD reference to point to the specified branch.
    /// </summary>
    /// <param name="branch">The name of the branch to set as the current branch.</param>
    public void SetBranch(string branch)
    {
        File.WriteAllText(_headPath, $"ref: refs/heads/{branch}");
    }

    /// <summary>
    /// Updates the HEAD reference to point to the specified commit hash.
    /// If HEAD refers to a branch, it updates the corresponding branch reference.
    /// Otherwise, it directly updates the HEAD file with the provided commit hash.
    /// </summary>
    /// <param name="hash">The commit hash to set as the new HEAD.</param>
    public void SetHead(string hash)
    {
        var headContent = File.ReadAllText(_headPath).Trim();

        if (headContent.StartsWith("ref: "))
        {
            var refPath = Path.Combine(gudDirectory, headContent[5..]);
            Directory.CreateDirectory(Path.GetDirectoryName(refPath)!);
            File.WriteAllText(refPath, hash);
        }
        else
        {
            File.WriteAllText(_headPath, hash);
        }
    }
}