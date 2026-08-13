using gud.Server.Services.Interfaces;
using System.Security.Cryptography;
using gud.Core.Stores;
using gud.Server.Repositories.Interfaces;

namespace gud.Server.Services.Implementations;

public class ObjectService : IObjectService
{
    private readonly IRepoRepository _repoRepository;

    public ObjectService(IRepoRepository repoRepository)
    {
        _repoRepository = repoRepository;
    }

    public bool RepoExists(string repo)
    {
        return _repoRepository.Exists(repo);
    }

    public bool ObjectExists(string repo, string hash)
    {
        return new ObjectStore(_repoRepository.GetGudPath(repo)).Exists(hash);
    }

    public byte[]? ReadObject(string repo, string hash)
    {
        try
        {
            return new ObjectStore(_repoRepository.GetGudPath(repo)).Read(hash);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public (bool Success, string? Error) WriteObject(string repo, string hash, byte[] content)
    {
        var actualHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        if (actualHash != hash.ToLowerInvariant())
            return (false, $"Content hash mismatch: expected {hash}, got {actualHash}");
        
        new ObjectStore(_repoRepository.GetGudPath(repo)).Write(hash, content);
        return (true, null);
    }
}