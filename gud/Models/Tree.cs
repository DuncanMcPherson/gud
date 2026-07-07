using System.Text;
using gud.Repository;
using gud.Utilities;

namespace gud.Models;

public sealed class Tree
{
    public IReadOnlyList<TreeEntry> Entries { get; }
    public string Hash { get; }

    public Tree(IEnumerable<TreeEntry> entries)
    {
        Entries = entries.OrderBy(e => e.Name).ToList();
        Hash = ObjectHasher.ComputeHash("tree", SerializeTree(Entries));
    }

    public static byte[] SerializeTree(IReadOnlyList<TreeEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var entry in entries)
            sb.Append(entry.Type).Append(' ')
                .Append(entry.Hash).Append(' ')
                .Append(entry.Name).Append('\n');
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

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

    public void Write(ObjectRepository repo)
    {
        repo.WriteObject(ObjectType.Tree, SerializeTree(Entries));
    }
    
    public override string ToString()
    {
        return $"Tree: {Hash}\n\tEntries:\n\t\t{string.Join(",\n\t\t", Entries)}";
    }
}

public sealed class TreeEntry
{
    public string Name { get; init; }
    public string Hash { get; init; }
    public TreeEntryType Type { get; init; }
    
    public override string ToString()
    {
        return $"TreeEntry: {Name}\n\tType: {Type}\n\tHash: {Hash}";
    }
}

public enum TreeEntryType
{
    Blob,
    Tree
}