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
}