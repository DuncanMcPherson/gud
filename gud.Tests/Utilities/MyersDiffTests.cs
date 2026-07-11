using gud.Core.Utilities;

namespace gud.Tests.Utilities;

[TestFixture]
public class MyersDiffTests
{
    [Test]
    public void SHOULD_ReturnCorrectEdits_WHEN_SingleInsertion()
    {
        var a = new[] { "the", "quick", "fox" };
        var b = new[] { "the", "quick", "brown", "fox" };

        var result = MyersDiff.Compute(a, b);
        
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Count(e => e.Type == EditType.Insert), Is.EqualTo(1));
            Assert.That(result.Single(e => e.Type == EditType.Insert).Line, Is.EqualTo("brown"));
        });
    }
    
    [Test]
    public void SHOULD_ReturnCorrectEdits_WHEN_SingleDeletion()
    {
        var b = new[] { "the", "quick", "fox" };
        var a = new[] { "the", "quick", "brown", "fox" };

        var result = MyersDiff.Compute(a, b);
        
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Count(e => e.Type == EditType.Delete), Is.EqualTo(1));
            Assert.That(result.Single(e => e.Type == EditType.Delete).Line, Is.EqualTo("brown"));
        });
    }
}