namespace gud.Stores;

public class RefStore(string gudDirectory)
{
    private readonly string _headPath = Path.Combine(gudDirectory, "HEAD");
    
    public string? GetHead()  => File.Exists(_headPath) ? File.ReadAllText(_headPath).Trim() : null;
    
    public void SetHead(string hash) => File.WriteAllText(_headPath, hash);
}