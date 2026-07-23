using System.Text;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;

namespace gud.Tests.Services;

[TestFixture]
public class TreeBuilderTests
{
    [Test]
    public void RoundTripsFlatMap()
    {
        var root = Path.Combine(Path.GetTempPath(), "gud-tree-" + Guid.NewGuid());
        var gud = Path.Combine(root, ".gud");
        Directory.CreateDirectory(Path.Combine(gud, "objects"));
        try
        {
            var objects = new ObjectRepository(new ObjectStore(gud));
            var h1 = objects.WriteObject(ObjectType.Blob, Encoding.UTF8.GetBytes("one"));
            var h2 = objects.WriteObject(ObjectType.Blob, Encoding.UTF8.GetBytes("two"));
            var map = new Dictionary<string, string>
            {
                ["a.txt"] = h1,
                ["dir/b.txt"] = h2
            };

            var treeHash = TreeBuilder.WriteTreeFromFlatMap(objects, map);
            var flat = WorkingTreeStatus.FlattenTree(objects, treeHash, "");
            Assert.That(flat, Is.EquivalentTo(map));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
