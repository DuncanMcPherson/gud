using System.Text;
using gud.Core.Repository;
using gud.Core.Utilities;

namespace gud.Core.Models;

/// <summary>
/// Represents a binary large object (Blob) in the repository system.
/// A Blob contains binary content and a hash derived from the content,
/// ensuring its integrity and uniqueness.
/// </summary>
public sealed class Blob
{
    /// <summary>
    /// Gets the binary content of the Blob.
    /// Represents the raw data stored in the Blob, ensuring
    /// it remains immutable once initialized.
    /// </summary>
    public byte[] Content { get; }

    /// <summary>
    /// Gets the hash of the Blob's content.
    /// The hash is a unique, immutable identifier derived from the binary content of the Blob
    /// using a secure hash algorithm to ensure data integrity and detect tampering.
    /// </summary>
    public string Hash { get; }

    public Blob(byte[] content)
    {
        Content = content;
        Hash = ObjectHasher.ComputeHash("blob", content);
    }

    /// <summary>
    /// Reads a Blob object from the specified repository using its hash.
    /// </summary>
    /// <param name="repo">The repository object from which the Blob will be read.</param>
    /// <param name="hash">The hash of the Blob object to retrieve.</param>
    /// <returns>A Blob object containing the content and hash of the identified object.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the object identified by the specified hash is not a Blob.
    /// </exception>
    public static Blob Read(ObjectRepository repo, string hash)
    {
        var (type, content) = repo.ReadObject(hash);
        return type != ObjectType.Blob ? throw new InvalidOperationException($"{hash} is not a blob") : new Blob(content);
    }

    public void Write(ObjectRepository repo) => repo.WriteObject(ObjectType.Blob, Content);
    
    public override string ToString()
    {
        return $"Blob: {Hash}\n\tContent: {Content.Length} bytes\n\tFileContent: {Encoding.UTF8.GetString(Content)}";
    }
}