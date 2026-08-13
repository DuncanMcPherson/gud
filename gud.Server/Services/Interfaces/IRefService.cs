namespace gud.Server.Services.Interfaces;

public interface IRefService
{
    bool RepoExists(string repo);
    Dictionary<string, string?> ListBranches(string repo);
    string? GetBranchCommit(string repo, string branch);
    (bool Success, string? Error) UpdateBranch(string repo, string branch, string newCommit);
}