using System.Text;
using gud.Stores;

namespace gud.Repository;

public class ObjectRepository(ObjectStore store)
{
    public void WriteObject(ObjectType type, string hash, byte[] rawContent)
    {
        var header = Encoding.UTF8.GetBytes($"{type.ToString().ToLowerInvariant()} {rawContent.Length}\0");
        var full = header.Concat(rawContent).ToArray();
        store.Write(hash, full);
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