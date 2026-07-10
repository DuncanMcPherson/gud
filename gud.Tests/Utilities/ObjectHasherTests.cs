using System.Text;
using gud.Core.Utilities;

namespace gud.Tests.Utilities;

[TestFixture]
public class ObjectHasherTests
{
    [Test]
    public void SHOULD_ReturnHashedValue_WHEN_ObjectIsHashed()
    {
        const string testStringA = "test";
        const string testStringB = "test";
        var hashA = ObjectHasher.ComputeHash("random", Encoding.UTF8.GetBytes(testStringA));
        var hashB = ObjectHasher.ComputeHash("random", Encoding.UTF8.GetBytes(testStringB));
        Assert.That(hashA, Is.EqualTo(hashB));
    }
}