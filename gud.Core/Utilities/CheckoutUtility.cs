using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Stores;

namespace gud.Core.Utilities;

public static class CheckoutUtility
{
    public static void Checkout(
        string? headCommitHash,
        string newCommitHash,
        string targetName,
        string repoRoot,
        ObjectRepository repo,
        BranchStore branches,
        RefStore refStore)
    {
        string? oldTreeHash = null;
        if (!string.IsNullOrWhiteSpace(headCommitHash))
        {
            var commit = Commit.Read(repo, headCommitHash);
            oldTreeHash = commit.TreeHash;
        }

        var newTreeHash = Commit.Read(repo, newCommitHash).TreeHash;
        
        WorkingTreeSync.SyncWorkingTree(oldTreeHash, newTreeHash, repoRoot, repo);
        
        if (branches.Exists(targetName))
            refStore.SetBranch(targetName);
        else
            refStore.SetHead(newCommitHash);
    }
}