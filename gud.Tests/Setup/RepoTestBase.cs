using System.Diagnostics.CodeAnalysis;

namespace gud.Tests.Setup;

[ExcludeFromCodeCoverage]
public abstract class RepoTestBase
{
    protected string RepoPath = null!;

    [SetUp]
    public void SetUpRepo()
    {
        if (RepoPath is null)
        {
            RepoPath = Path.Combine(Path.GetTempPath(), "gud-test-" + Guid.NewGuid());
            Directory.CreateDirectory(RepoPath);
        }
    }

    [TearDown]
    public void TearDownRepo()
    {
        if (Directory.Exists(RepoPath))
            Directory.Delete(RepoPath, true);
        RepoPath = null;
    }
}