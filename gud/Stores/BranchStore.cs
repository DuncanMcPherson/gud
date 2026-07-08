namespace gud.Stores;

public class BranchStore(string gudPath)
{
    private readonly string _headsPath = Path.Join(gudPath, "refs", "heads");

    public IEnumerable<string?> ListBranches()
    {
        return Directory.Exists(_headsPath) ? Directory.GetFiles(_headsPath).Select(Path.GetFileName) : Enumerable.Empty<string>();
    }

    public bool Exists(string branch) => File.Exists(Path.Join(_headsPath, branch));

    public string? GetCommit(string name)
    {
        var path = Path.Join(_headsPath, name);
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }
    
    public void Rename(string oldName, string newName)
    {
        var oldPath = Path.Join(_headsPath, oldName);
        var newPath = Path.Join(_headsPath, newName);
        File.Move(oldPath, newPath);
    }

    public void SetCommit(string branch, string commit)
    {
        var path = Path.Join(_headsPath, branch);
        File.WriteAllText(path, commit);
    }
}