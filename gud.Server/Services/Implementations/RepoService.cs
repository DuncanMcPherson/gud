using gud.Server.Repositories.Interfaces;
using gud.Server.Services.Interfaces;

namespace gud.Server.Services.Implementations;

public class RepoService(IRepoRepository repoRepository) : IRepoService
{
    public bool Exists(string repo)
    {
        return repoRepository.Exists(repo);
    }

    public IEnumerable<string?> ListRepos() => repoRepository.ListRepos();

    public async Task<(bool Success, string? Error)> CreateRepoAsync(string repo)
    {
        if (repoRepository.Exists(repo))
            return (false, "Repository already exists");

        try
        {
            await repoRepository.CreateAsync(repo);
            return (true, null);
        }
        catch (Exception)
        {
            return (false, "Failed to create repository");
        }
    }
}