namespace gud.Server.Services.Interfaces;

public interface IObjectService
{
    bool RepoExists(string repo);
    bool ObjectExists(string repo, string hash);
    byte[]? ReadObject(string repo, string hash);
    (bool Success, string? Error) WriteObject(string repo, string hash, byte[] data);
}