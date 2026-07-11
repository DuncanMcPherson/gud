namespace gud.Core.Stores;

public class BranchStore(string gudPath)
{
    private readonly string _headsPath = Path.Join(gudPath, "refs", "heads");

    public IEnumerable<string?> ListBranches()
    {
        if (!Directory.Exists(_headsPath))
            yield break;

        foreach (var file in Directory.GetFiles(_headsPath, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_headsPath, file);
            yield return relative.Replace(Path.DirectorySeparatorChar, '/');
        }
    }

    public bool Exists(string branch) => File.Exists(Path.Join(_headsPath, branch));

    public string? GetCommit(string name)
    {
        var path = ResolvePath(name);
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }

    public string? ResolveTarget(string name)
    {
        if (Exists(name))
            return GetCommit(name)!;

        var objectsPath = Path.Combine(gudPath, "objects", name[..2], name[2..]);
        return File.Exists(objectsPath) ? name : null;
    }
    
    public void Rename(string oldName, string newName)
    {
        var oldPath = ResolvePath(oldName);
        var newPath = ResolvePath(newName);
        File.Move(oldPath, newPath);
    }

    public void SetCommit(string branch, string commit)
    {
        var path = ResolvePath(branch);
        if (Directory.Exists(path))
            throw new InvalidOperationException($"Branch '{branch}' cannot be created because it is a folder");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, commit);
    }
    
    private string ResolvePath(string name) => Path.Combine(_headsPath, name.Replace('/', Path.DirectorySeparatorChar));
}