using System.Diagnostics.CodeAnalysis;

namespace gud.Tests.Setup;

[ExcludeFromCodeCoverage]
public abstract class RepoTestBase
{
    protected string RepoPath;

    [SetUp]
    public void SetUpRepo()
    {
        RepoPath = Path.Combine(Path.GetTempPath(), "gud-test-" + Guid.NewGuid());
        Directory.CreateDirectory(RepoPath);
    }

    [TearDown]
    public void TearDownRepo()
    {
        if (Directory.Exists(RepoPath))
            Directory.Delete(RepoPath, true);
    }
}