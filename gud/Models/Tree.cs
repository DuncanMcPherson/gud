using System.Text;
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
    
    private static byte[] SerializeTree(IReadOnlyList<TreeEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var entry in entries)
            sb.Append(entry.Type).Append(' ')
                .Append(entry.Hash).Append(' ')
                .Append(entry.Name).Append('\n');
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

public sealed class TreeEntry
{
    public string Name { get; init; }
    public string Hash { get; init; }
    public TreeEntryType Type { get; init; }
}

public enum TreeEntryType
{
    Blob,
    Tree
}