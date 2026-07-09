using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace gud.Stores;

public class ObjectStore(string gudDirectory)
{
    private readonly string _rootFolder = Path.Combine(gudDirectory, "objects");

    public void Write(string hash, byte[] content)
    {
        var (dir, file) = GetPaths(hash);

        Directory.CreateDirectory(dir);
        if (File.Exists(file)) return;

        using var fs = new FileStream(file, FileMode.Create);
        using var compressor = new DeflateStream(fs, CompressionLevel.Optimal);
        compressor.Write(content);
    }
    
    public byte[] Read(string hash)
    {
        var (_, file) = GetPaths(hash);

        if (!File.Exists(file))
            throw new FileNotFoundException($"Object {hash} not found");

        using var fs = new FileStream(file, FileMode.Open);
        using var decompressor = new DeflateStream(fs, CompressionMode.Decompress);
        using var ms = new MemoryStream();
        decompressor.CopyTo(ms);
        return ms.ToArray();
    }

    [ExcludeFromCodeCoverage]
    public bool Exists(string hash) => File.Exists(GetPaths(hash).file);

    private (string dir, string file) GetPaths(string hash)
    {
        var dir = Path.Combine(_rootFolder, hash[..2]);
        var file = Path.Combine(dir, hash[2..]);
        return (dir, file);
    }
}