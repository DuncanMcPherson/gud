using gud.Tests.Setup;
using gud.Core.Utilities;

namespace gud.Tests.Utilities;

[TestFixture]
public class GudRepositoryTests : RepoTestBase
{
    [TestFixture]
    public class FindRoot : GudRepositoryTests
    {
        [Test]
        public void SHOULD_ReturnNull_WHEN_RepositoryIsNotFound()
        {
            var root = GudRepository.FindRoot(RepoPath);
            Assert.That(root, Is.Null);
        }
        
        [Test]
        public void SHOULD_ReturnRootPath_WHEN_RepositoryIsFound()
        {
            Directory.CreateDirectory(Path.Join(RepoPath, ".gud"));
            var root = GudRepository.FindRoot(RepoPath);
            Assert.That(root, Is.EqualTo(RepoPath));
        }
    }
}