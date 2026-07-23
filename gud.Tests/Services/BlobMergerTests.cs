using System.Text;
using gud.Core.Services;

namespace gud.Tests.Services;

[TestFixture]
public class BlobMergerTests
{
    [Test]
    public void SameBothSides_NoConflict()
    {
        var content = Encoding.UTF8.GetBytes("hello\n");
        var result = BlobMerger.Merge(content, content, content);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(result.Content, Is.EqualTo(content));
    }

    [Test]
    public void OnlyOursChanged_TakesOurs()
    {
        var bas = Encoding.UTF8.GetBytes("base\n");
        var ours = Encoding.UTF8.GetBytes("ours\n");
        var result = BlobMerger.Merge(bas, ours, bas);
        Assert.That(result.HadConflict, Is.False);
        Assert.That(Encoding.UTF8.GetString(result.Content), Is.EqualTo("ours\n"));
    }

    [Test]
    public void BothChanged_WritesMarkers()
    {
        var bas = Encoding.UTF8.GetBytes("base\n");
        var ours = Encoding.UTF8.GetBytes("ours\n");
        var theirs = Encoding.UTF8.GetBytes("theirs\n");
        var result = BlobMerger.Merge(bas, ours, theirs, "HEAD", "feature");
        Assert.That(result.HadConflict, Is.True);
        var text = Encoding.UTF8.GetString(result.Content);
        Assert.That(text, Does.Contain("<<<<<<< HEAD"));
        Assert.That(text, Does.Contain("ours"));
        Assert.That(text, Does.Contain("======="));
        Assert.That(text, Does.Contain("theirs"));
        Assert.That(text, Does.Contain(">>>>>>> feature"));
        Assert.That(BlobMerger.ContainsConflictMarkers(result.Content), Is.True);
    }

    [Test]
    public void BinaryConflict_KeepsOurs()
    {
        var bas = new byte[] { 1, 2, 0, 3 };
        var ours = new byte[] { 9, 0, 9 };
        var theirs = new byte[] { 8, 0, 8 };
        var result = BlobMerger.Merge(bas, ours, theirs);
        Assert.That(result.HadConflict, Is.True);
        Assert.That(result.IsBinary, Is.True);
        Assert.That(result.Content, Is.EqualTo(ours));
    }
}
