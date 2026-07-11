using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace gud.Core.Stores;

/// <summary>
/// Represents a storage system for managing and retrieving objects using unique hashes.
/// </summary>
public class ObjectStore(string gudDirectory)
{
    /// <summary>
    /// Represents the root directory where all object data is stored.
    /// This directory serves as the base path for organizing and storing
    /// object files, typically used for managing content associated with
    /// specific hashes in a structured folder hierarchy.
    /// </summary>
    private readonly string _rootFolder = Path.Combine(gudDirectory, "objects");

    /// Writes an object to the object store, compressing its content and organizing it
    /// into the appropriate directory structure based on the hash value.
    /// If the file already exists, the method will not overwrite it.
    /// <param name="hash">
    /// The hash of the content to be written. This is used to determine the directory
    /// and file name for storage.
    /// </param>
    /// <param name="content">
    /// The raw byte array of the content to be stored in the object store.
    /// </param>
    public void Write(string hash, byte[] content)
    {
        var (dir, file) = GetPaths(hash);

        Directory.CreateDirectory(dir);
        if (File.Exists(file)) return;

        using var fs = new FileStream(file, FileMode.Create);
        using var compressor = new DeflateStream(fs, CompressionLevel.Optimal);
        compressor.Write(content);
    }

    /// Reads the content of an object based on its hash.
    /// <param name="hash">
    /// The unique hash identifier of the object to be read.
    /// </param>
    /// <returns>
    /// A byte array containing the decompressed content of the object.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the object corresponding to the given hash cannot be found.
    /// </exception>
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

    /// <summary>
    /// Checks if an object with the specified hash exists in the object store.
    /// </summary>
    /// <param name="hash">The hash of the object to check for existence.</param>
    /// <returns>True if the object exists, otherwise false.</returns>
    [ExcludeFromCodeCoverage]
    public bool Exists(string hash) => File.Exists(GetPaths(hash).file);

    /// <summary>
    /// Determines the directory and file paths for a given hash within the object store.
    /// </summary>
    /// <param name="hash">The hash value used to determine the directory and file paths.</param>
    /// <returns>A tuple containing the directory path and the file path associated with the hash.</returns>
    private (string dir, string file) GetPaths(string hash)
    {
        var dir = Path.Combine(_rootFolder, hash[..2]);
        var file = Path.Combine(dir, hash[2..]);
        return (dir, file);
    }
}