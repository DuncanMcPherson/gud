using gud.Models;
using gud.Repository;
using gud.Services;
using gud.Stores;

namespace gud;

internal static class Program
{
    private static void Main(string[] args)
    {
        var cPath = Directory.GetCurrentDirectory();
        var gudPath = Path.Combine(cPath, ".gud");
        var commitHash = args[0];
        var objStore = new ObjectStore(gudPath);
        var repo = new ObjectRepository(objStore);
        var commit = Commit.Read(repo, commitHash);
        Console.WriteLine(commit);
        var tree = Tree.Read(repo, commit.TreeHash);
        Console.WriteLine(tree);
        DeserializeTree(repo, tree);
    }

    private static void DeserializeTree(ObjectRepository repo, Tree tree)
    {
        foreach (var entry in tree.Entries)
        {
            switch (entry.Type)
            {
                case TreeEntryType.Tree:
                    var child = Tree.Read(repo, entry.Hash);
                    Console.WriteLine(child);
                    DeserializeTree(repo, child);
                    break;
                case TreeEntryType.Blob:
                    var blob = Blob.Read(repo, entry.Hash);
                    Console.WriteLine(blob);
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}