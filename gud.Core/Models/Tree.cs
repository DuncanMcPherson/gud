using System.Text;
using gud.Core.Repository;
using gud.Core.Utilities;

namespace gud.Core.Models;

/// <summary>
/// Represents a tree object in the version control system, containing a collection of tree entries and a computed hash.
/// </summary>
public sealed class Tree
{
    /// <summary>
    /// Gets the collection of entries contained within the tree.
    /// Each entry represents a file or subdirectory in the tree with associated metadata,
    /// including its name, hash, and type (blob or tree).
    /// </summary>
    /// <remarks>
    /// The entries are sorted by their names in ascending order. This property is immutable
    /// and cannot be modified after the Tree object is initialized.
    /// </remarks>
    public IReadOnlyList<TreeEntry> Entries { get; }

    /// <summary>
    /// Gets the computed hash of the tree, uniquely identifying its contents.
    /// The hash is generated using the tree's serialized content and the object hasher mechanism.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Represents a collection of tree entries with a unique hash used to identify the tree object.
    /// </summary>
    public Tree(IEnumerable<TreeEntry> entries)
    {
        Entries = entries.OrderBy(e => e.Name).ToList();
        Hash = ObjectHasher.ComputeHash("tree", SerializeTree(Entries));
    }

    /// <summary>
    /// Serializes a list of tree entries into a byte array representation.
    /// </summary>
    /// <param name="entries">The list of tree entries to serialize.</param>
    /// <returns>A byte array containing the serialized tree entries.</returns>
    public static byte[] SerializeTree(IReadOnlyList<TreeEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var entry in entries)
            sb.Append(entry.Type).Append(' ')
                .Append(entry.Hash).Append(' ')
                .Append(entry.Name).Append('\n');
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Reads a tree object from the repository using its hash and reconstructs the tree structure.
    /// </summary>
    /// <param name="repo">The repository from which the tree object will be read.</param>
    /// <param name="hash">The hash identifying the tree object to be read.</param>
    /// <returns>The reconstructed <see cref="Tree"/> object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the hash does not represent a tree object.</exception>
    public static Tree Read(ObjectRepository repo, string hash)
    {
        var (type, content) = repo.ReadObject(hash);
        if (type != ObjectType.Tree) throw new InvalidOperationException($"{hash} is not a tree");
        var contentStr = Encoding.UTF8.GetString(content);
        var entryStrs = contentStr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var entries = (from entryStr in entryStrs
            select entryStr.Split(' ')
            into parts
            let entryType = Enum.Parse<TreeEntryType>(parts[0])
            let entryHash = parts[1]
            let entryName = parts[2]
            select new TreeEntry { Name = entryName, Hash = entryHash, Type = entryType }).ToList();
        return new Tree(entries);
    }

    /// <summary>
    /// Writes the current <see cref="Tree"/> object to the specified repository.
    /// </summary>
    /// <param name="repo">
    /// The object repository where the <see cref="Tree"/> will be written.
    /// </param>
    public void Write(ObjectRepository repo)
    {
        repo.WriteObject(ObjectType.Tree, SerializeTree(Entries));
    }

    /// <summary>
    /// Returns a string representation of the Tree object, including its hash and entries.
    /// </summary>
    /// <returns>
    /// A string that represents the current Tree instance, containing its hash and the list of entries.
    /// </returns>
    public override string ToString()
    {
        return $"Tree: {Hash}\n\tEntries:\n\t\t{string.Join(",\n\t\t", Entries)}";
    }
}

/// <summary>
/// Represents an individual entry in a tree structure, corresponding to a file or directory.
/// </summary>
public sealed class TreeEntry
{
    /// <summary>
    /// Gets the name of the tree entry. The name typically represents the file or directory name
    /// associated with this tree entry in the repository structure.
    /// </summary>
    /// <remarks>
    /// For entries representing files, this is the file name (without any path information).
    /// For entries representing directories, this is the directory name.
    /// It is used to differentiate and order entries within a tree structure.
    /// </remarks>
    public string Name { get; init; }

    /// <summary>
    /// Represents the computed hash value associated with a specific tree entry.
    /// The hash is a string identifier derived from the content or structure of
    /// the corresponding file or directory, ensuring uniqueness and immutability
    /// for versioning and tracking changes.
    /// </summary>
    public string Hash { get; init; }

    /// <summary>
    /// Represents the type of a tree entry in a version control system.
    /// This property specifies whether the entry is a `Blob` (file) or a `Tree` (directory).
    /// </summary>
    public TreeEntryType Type { get; init; }

    /// Returns a string representation of the Tree instance, including its hash
    /// and list of entries.
    /// <returns>A string containing the hash of the tree and its entries.</returns>
    public override string ToString()
    {
        return $"TreeEntry: {Name}\n\tType: {Type}\n\tHash: {Hash}";
    }
}

/// <summary>
/// Represents the type of a tree entry in a version control system.
/// </summary>
/// <remarks>
/// Tree entries can either represent a file or a directory within a hierarchical structure.
/// The type of the entry helps determine its behavior when interacting with different parts of the system.
/// </remarks>
public enum TreeEntryType
{
    /// <summary>
    /// Represents a blob entry in a tree, typically corresponding to a file in the repository.
    /// Blob entries store the hash of the file's content and are used to track the state
    /// of individual files within a repository's structure.
    /// </summary>
    Blob,

    /// <summary>
    /// Represents a directory entry in a tree object within a version control system.
    /// This enum value identifies an entry of type "Tree", which refers to another tree object.
    /// Entries of this type are used to model hierarchical structures,
    /// enabling representation of nested directories or submodules within a repository.
    /// </summary>
    Tree
}