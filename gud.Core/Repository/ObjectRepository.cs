using System.Security.Cryptography;
using System.Text;
using gud.Core.Stores;

namespace gud.Core.Repository;

/// <summary>
/// Represents a repository for storing and retrieving objects within a content-based storage system.
/// This class handles the serialization and deserialization of objects, using their type and binary content,
/// and leverages an underlying store for physical storage operations based on their calculated hashes.
/// </summary>
public class ObjectRepository(ObjectStore store)
{
    /// <summary>
    /// Writes an object to the object repository, calculates its hash using SHA-256,
    /// and stores it in the object store.
    /// </summary>
    /// <param name="type">The type of the object being written (e.g., Blob, Tree, Commit).</param>
    /// <param name="rawContent">The raw binary content of the object.</param>
    /// <returns>The SHA-256 hash of the written object as a lowercase hexadecimal string.</returns>
    public string WriteObject(ObjectType type, byte[] rawContent)
    {
        var typeStr = type.ToString().ToLowerInvariant();
        var header = Encoding.UTF8.GetBytes($"{typeStr} {rawContent.Length}\0");
        var full = header.Concat(rawContent).ToArray();

        var hash = Convert.ToHexString(SHA256.HashData(full)).ToLowerInvariant();
        store.Write(hash, full);
        return hash;
    }

    /// Reads an object from the object repository using its hash.
    /// <param name="hash">
    /// The hash of the object to be retrieved. This is a unique identifier for the object stored in the repository.
    /// </param>
    /// <returns>
    /// A tuple containing the type of the object and its content. The type is represented as an <see cref="ObjectType"/> and
    /// the content is provided as a byte array.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the object cannot be read or if the provided hash is invalid.
    /// </exception>
    public (ObjectType Type, byte[] Content) ReadObject(string hash)
    {
        var raw = store.Read(hash);
        var nullIndex = Array.IndexOf(raw, (byte)'\0');
        var headerString = Encoding.UTF8.GetString(raw, 0, nullIndex);
        var parts = headerString.Split(' ');
        var type = ParseType(parts[0]);
        var content = raw[(nullIndex + 1)..];
        return (type, content);
    }

    /// <summary>
    /// Parses a string representation of an object type into the corresponding <see cref="ObjectType"/> enumeration value.
    /// </summary>
    /// <param name="typeStr">The string representation of the object type to parse. Supported values are "blob", "tree", and "commit".</param>
    /// <returns>The parsed <see cref="ObjectType"/> value corresponding to the input string.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if the input string does not match any supported object types.</exception>
    private ObjectType ParseType(string typeStr)
    {
        return typeStr switch
        {
            "blob" => ObjectType.Blob,
            "tree" => ObjectType.Tree,
            "commit" => ObjectType.Commit,
            _ => throw new IndexOutOfRangeException()
        };
    }
}

/// <summary>
/// Represents a commit object in the repository.
/// A commit ties a tree (snapshot of the file system) to its parent commit(s),
/// identifying changes, the author, a timestamp, and an associated commit message.
/// </summary>
public enum ObjectType
{
    /// <summary>
    /// Represents a binary large object (blob) in the object repository.
    /// </summary>
    /// <remarks>
    /// A blob is a basic unit of storage in the repository that contains raw binary content.
    /// It is used for storing file data.
    /// </remarks>
    Blob,

    /// <summary>
    /// Represents a hierarchical structure of objects or entities within the repository.
    /// </summary>
    /// <remarks>
    /// The Tree type is typically used to model a collection of references to other objects in the
    /// repository, such as blobs and other trees, enabling the representation of directory-like
    /// structures. It allows the organization of objects into meaningful groupings.
    /// </remarks>
    Tree,

    /// <summary>
    /// Represents a commit object in the object repository.
    /// </summary>
    /// <remarks>
    /// A commit serves as a snapshot of changes in the repository. It links the current state
    /// of a tree object to one or more parent commits, providing metadata such as the author's
    /// information, a timestamp, and a commit message describing the changes made.
    /// </remarks>
    Commit
}