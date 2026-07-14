using System.Net;
using System.Net.Http.Json;

namespace gud.Core.Services;

public class GudRemoteClient
{
    private readonly HttpClient _client;
    private readonly string? _repoName;

    public GudRemoteClient(string baseUrl, string? repoName, string apiKey)
    {
        _repoName = repoName;
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<bool> RepoExistsAsync()
    {
        var response = await _client.GetAsync($"repos/{_repoName}");
        return response.IsSuccessStatusCode;
    }

    public async Task CreateRepoAsync(string repo)
    {
        var response = await _client.PostAsync($"/repos/{repo}", null);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
            throw new HttpRequestException($"Failed to create repo '{repo}': {response.StatusCode}");
    }

    public async Task<bool> ObjectExistsAsync(string hash)
    {
        var response = await _client.GetAsync($"repos/{_repoName}/objects/{hash}/exists");
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]> GetObjectAsync(string hash)
    {
        var response = await _client.GetAsync($"/repos/{_repoName}/objects/{hash}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task PutObjectAsync(string hash, byte[] content)
    {
        var response = await _client.PostAsync($"/repos/{_repoName}/objects/{hash}", new ByteArrayContent(content));
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetRefAsync(string branch)
    {
        var response = await _client.GetAsync($"/repos/{_repoName}/refs/heads/{branch}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadAsStringAsync()).Trim('"');
    }

    public async Task PutRefAsync(string branch, string newCommit)
    {
        var response = await _client.PutAsJsonAsync($"/repos/{_repoName}/refs/heads/{branch}", new { NewCommit = newCommit });
        response.EnsureSuccessStatusCode();
    }
}