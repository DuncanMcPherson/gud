using gud.Commands;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.IntegrationTests;

[TestFixture]
public class InitCommandTests : RepoTestBase
{
    private string _originalPath;
    private CommandAppTester _app;
    
    [SetUp]
    public void Setup()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<InitCommand>();
        _app = app;
        _originalPath = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(RepoPath);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.SetCurrentDirectory(_originalPath);
    }

    [Test]
    public void ShouldCreateRepo()
    {
        var result = _app.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Directory.Exists(RepoPath), Is.True);
            Assert.That(Directory.Exists(Path.Combine(RepoPath, ".gud")), Is.True);
            Assert.That(result.ExitCode, Is.EqualTo(0));
        }
    }

    [Test]
    public void ShouldFailToCreate()
    {
        Directory.CreateDirectory(Path.Join(RepoPath, ".gud"));
        var result = _app.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
        }
    }
}