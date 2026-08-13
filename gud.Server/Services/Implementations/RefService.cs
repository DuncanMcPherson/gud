using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using gud.Server.Repositories.Interfaces;
using gud.Server.Services.Interfaces;

namespace gud.Server.Services.Implementations;

public class RefService(IRepoRepository repoRepository) : IRefService
{
    public bool RepoExists(string repo) => repoRepository.Exists(repo);

    public Dictionary<string, string?> ListBranches(string repo)
    {
        var branches = new BranchStore(repoRepository.GetGudPath(repo));
        return branches.ListBranches().ToDictionary(b => b!, b => branches.GetCommit(b!));
    }

    public string? GetBranchCommit(string repo, string branch)
    {
        return new BranchStore(repoRepository.GetGudPath(repo)).GetCommit(branch);
    }

    public (bool Success, string? Error) UpdateBranch(string repo, string branch, string newCommit)
    {
        var gudPath = repoRepository.GetGudPath(repo);
        var branches = new BranchStore(gudPath);
        var objects = new ObjectRepository(new ObjectStore(gudPath));

        var currentCommit = branches.GetCommit(branch);

        if (currentCommit != null && !CommitGraph.IsAncestor(objects, currentCommit, newCommit))
            return (false,
                $"Rejected: {ObjectResolver.ShortHash(newCommit)} is not a fast-forward of {ObjectResolver.ShortHash(currentCommit)}");
        branches.SetCommit(branch, newCommit);
        return (true, null);
    }
}