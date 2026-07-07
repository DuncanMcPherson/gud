namespace gud.Stores;

public class ConfigStore
{
    private readonly string _configPath;
    private readonly Dictionary<string, string> _values = new();

    public ConfigStore(string gudDirectory)
    {
        _configPath = Path.Combine(gudDirectory, "config");
        if (File.Exists(_configPath))
        {
            foreach (var line in File.ReadAllLines(_configPath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    _values[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
    }

    public string? Get(string key)
    {
        return _values.GetValueOrDefault(key);
    }

    public void Set(string key, string value)
    {
        _values[key] = value;
        File.WriteAllLines(_configPath, _values.Select(kv => $"{kv.Key} = {kv.Value}"));
    }
}