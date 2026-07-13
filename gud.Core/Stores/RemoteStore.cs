namespace gud.Core.Stores;

public class RemoteStore(string gudPath)
{
    private readonly string _remotesPath = Path.Combine(gudPath, "remotes");

    public IEnumerable<(string Name, string Url, string ApiKey)> ListRemotes()
    {
        if (!File.Exists(_remotesPath)) yield break;

        foreach (var line in File.ReadAllLines(_remotesPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(' ', 3);
            yield return (parts[0], parts[1], parts.Length > 2 ? parts[2] : "");
        }
    }

    public (string Url, string ApiKey)? GetRemote(string name)
    {
        var match = ListRemotes().FirstOrDefault(r => r.Name == name);
        return match.Name == null ? null : (match.Url, match.ApiKey);
    }

    public void AddRemote(string name, string url, string apiKey = "")
    {
        if (GetRemote(name) != null)
            throw new InvalidOperationException($"Remote '{name}' already exists");
        File.AppendAllText(_remotesPath, $"{name} {url} {apiKey}\n");
    }

    public void RemoveRemote(string name)
    {
        var remaining = ListRemotes().Where(r => r.Name != name)
            .Select(r => $"{r.Name} {r.Url} {r.ApiKey}");
        File.WriteAllLines(_remotesPath, remaining);
    }
}