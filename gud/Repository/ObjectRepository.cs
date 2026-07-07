using System.Security.Cryptography;
using System.Text;
using gud.Stores;
using gud.Utilities;

namespace gud.Repository;

public class ObjectRepository(ObjectStore store)
{
    public string WriteObject(ObjectType type, byte[] rawContent)
    {
        var typeStr = type.ToString().ToLowerInvariant();
        var header = Encoding.UTF8.GetBytes($"{typeStr} {rawContent.Length}\0");
        var full = header.Concat(rawContent).ToArray();

        var hash = Convert.ToHexString(SHA256.HashData(full)).ToLowerInvariant();
        store.Write(hash, full);
        return hash;
    }

    public (ObjectType Type, byte[] Content) ReadObject(string hash)
    {
        var raw = store.Read(hash);
        var nullIndex = Array.IndexOf(raw, (byte)'\0');
        var headerString = Encoding.UTF8.GetString(raw, 0, nullIndex);
        var parts = headerString.Split(' ');
        var type = Enum.Parse<ObjectType>(parts[0]);
        var content = raw[(nullIndex + 1)..];
        return (type, content);
    }
}

public enum ObjectType
{
    Blob,
    Tree,
    Commit
}