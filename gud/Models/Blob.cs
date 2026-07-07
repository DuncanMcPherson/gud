using gud.Repository;
using gud.Utilities;

namespace gud.Models;

public sealed class Blob
{
    public byte[] Content { get; }
    public string Hash { get; }

    public Blob(byte[] content)
    {
        Content = content;
        Hash = ObjectHasher.ComputeHash("blob", content);
    }

    public static Blob Read(ObjectRepository repo, string hash)
    {
        var (type, content) = repo.ReadObject(hash);
        return type != ObjectType.Blob ? throw new InvalidOperationException($"{hash} is not a blob") : new Blob(content);
    }

    public void Write(ObjectRepository repo) => repo.WriteObject(ObjectType.Blob, Content);
}