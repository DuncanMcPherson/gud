namespace gud.Server.Repositories.Interfaces;

public interface IRepoRepository
{
    string GetGudPath(string repo);
    bool Exists(string repo);
    IEnumerable<string?> ListRepos();
    Task CreateAsync(string repo);
}