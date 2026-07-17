namespace gud.Core.Stores;

public class RemoteRefStore(string gudPath)
{
    private readonly string _remoteRefsPath = Path.Combine(gudPath, "refs", "remotes");

    public string? GetTrackedCommit(string remote, string branch)
    {
        var path = Path.Combine(_remoteRefsPath, remote, branch.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path) ? File.ReadAllText(path).Trim() is { Length: > 0 } c ? c : null : null;
    }

    public void SetTrackedCommit(string remote, string branch, string commit)
    {
        var path = Path.Combine(_remoteRefsPath, remote, branch.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, commit);
    }
}