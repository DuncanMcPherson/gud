using gud.Core.Utilities;
using gud.Server.Repositories.Interfaces;

namespace gud.Server.Repositories.Implementations;

public class RepoRepository : IRepoRepository
{
    private readonly string _reposRoot;

    public RepoRepository(IConfiguration config)
    {
        _reposRoot = (string.IsNullOrWhiteSpace(config["ReposRoot"]) ? "./repos" : config["ReposRoot"])!;
    }

    public string GetGudPath(string repo)
    {
        return Path.Combine(_reposRoot, repo, ".gud");
    }

    public bool Exists(string repo) => GudRepository.Exists(GetGudPath(repo));

    public IEnumerable<string?> ListRepos()
    {
        if (!Directory.Exists(_reposRoot))
            return [];

        return Directory.GetDirectories(_reposRoot)
            .Where(dir => Directory.Exists(Path.Combine(dir, ".gud")))
            .Select(Path.GetFileName);
    }

    public Task CreateAsync(string repo)
    {
        return GudRepository.CreateAsync(Path.Combine(_reposRoot, repo));
    }
}