namespace gud.Core.Stores;

/// <summary>
/// A class that manages configuration key-value pairs stored in a text file.
/// </summary>
public class ConfigStore
{
    /// <summary>
    /// Represents the file path of the configuration file used to store and retrieve key-value pairs.
    /// This variable is initialized with a path derived from the specified directory and
    /// is used to read and write configuration data.
    /// </summary>
    private readonly string _configPath;

    /// <summary>
    /// A dictionary that stores key-value pairs representing configuration settings.
    /// Used internally to manage and access configuration data within the ConfigStore.
    /// </summary>
    private readonly Dictionary<string, string> _values = new();

    /// <summary>
    /// Represents a configuration storage system for managing key-value pairs within a configuration file.
    /// </summary>
    /// <remarks>
    /// The configuration is stored and read from a file named "config" located in a specific directory.
    /// This class provides functionality to retrieve, update, and enumerate configuration entries.
    /// </remarks>
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

    /// Retrieves the value associated with the specified key from the configuration store.
    /// <param name="key">The key for the configuration setting to retrieve.</param>
    /// <return>The value associated with the specified key, or <c>null</c> if the key does not exist in the configuration store.</return>
    public string? Get(string key)
    {
        return _values.GetValueOrDefault(key);
    }

    /// <summary>
    /// Sets the specified key-value pair in the configuration store and writes the updated configuration
    /// to the underlying storage.
    /// </summary>
    /// <param name="key">The key to set or update in the configuration store.</param>
    /// <param name="value">The value to associate with the specified key.</param>
    public void Set(string key, string value)
    {
        _values[key] = value;
        File.WriteAllLines(_configPath, _values.Select(kv => $"{kv.Key} = {kv.Value}"));
    }

    /// Retrieves all key-value pairs from the configuration store.
    /// This method iterates over the underlying dictionary of configuration values
    /// and returns an enumerable collection of tuples containing keys and their corresponding values.
    /// <returns>An enumerable collection of key-value pairs stored in the configuration.</returns>
    public IEnumerable<(string key, string value)> All()
    {
        return _values.Select(kv => (kv.Key, kv.Value));
    }
}