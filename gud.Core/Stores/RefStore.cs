namespace gud.Core.Stores;

public class RefStore(string gudDirectory)
{
    private readonly string _headPath = Path.Combine(gudDirectory, "HEAD");

    public string? GetHead()
    {
        var headContent = File.ReadAllText(_headPath).Trim();

        if (headContent.StartsWith("ref: "))
        {
            var refPath = Path.Combine(gudDirectory, headContent[5..]);
            return File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : null;
        }
        
        return headContent;
    }

    public string? CurrentBranchName()
    {
        var headContent = File.ReadAllText(_headPath).Trim();
        
        if (headContent.StartsWith("ref: refs/heads/"))
        {
            return headContent["ref: refs/heads/".Length..];
        }

        return null;
    }

    public void SetBranch(string branch)
    {
        File.WriteAllText(_headPath, $"ref: refs/heads/{branch}");
    }

    public void SetHead(string hash)
    {
        var headContent = File.ReadAllText(_headPath).Trim();

        if (headContent.StartsWith("ref: "))
        {
            var refPath = Path.Combine(gudDirectory, headContent[5..]);
            Directory.CreateDirectory(Path.GetDirectoryName(refPath)!);
            File.WriteAllText(refPath, hash);
        }
        else
        {
            File.WriteAllText(_headPath, hash);
        }
    }
}