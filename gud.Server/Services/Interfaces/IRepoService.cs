namespace gud.Server.Services.Interfaces;

public interface IRepoService
{
    bool Exists(string repo);
    IEnumerable<string?> ListRepos();
    Task<(bool Success, string? Error)> CreateRepoAsync(string repo);
}