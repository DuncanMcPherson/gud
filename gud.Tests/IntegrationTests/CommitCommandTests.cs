using gud.Commands;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;
using Spectre.Console.Testing;

namespace gud.Tests.IntegrationTests;

[TestFixture]
public class CommitCommandTests : TestRepoWithConfigBase
{
    protected override CommandAppTester BuildApp()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<CommitCommand>();
        return app;
    }

    [Test]
    public void ShouldNotPromptForMessage()
    {
        var filePath = Path.Combine(RepoPath, "test.txt");
        File.WriteAllText(filePath, "test");
        var result = App.Run("-m", "testMessage");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Not.Contain("Commit message:"));
        }
    }

    [Test]
    public void ShouldPromptForMessage()
    {
        var filePath = Path.Combine(RepoPath, "test.txt");
        File.WriteAllText(filePath, "test");
        var console = new TestConsole();
        console.Profile.Capabilities.Interactive = true;
        console.Input.PushTextWithEnter("testCommit");
        var app = new CommandAppTester(console: console);
        app.SetDefaultCommand<CommitCommand>();
        var result = app.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("Commit message:"));
        }
    }

    [Test]
    public void ShouldPrintErrorAndReturnCode1WhenNotConfigured()
    {
        // Arrange
        // We need to delete the config file to get this to work
        File.Delete(Path.Combine(RepoPath, ".gud", "config"));
        var result = App.Run("-m", "test");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("Error:"));
        }
    }

    [Test]
    public void ShouldAlertOfEmptyTreeAndExit()
    {
        var result = App.Run("-m", "test");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("No files exist."));
        }
    }

    [Test]
    public void ShouldAlertOfCleanWorkingTreeAndExit()
    {
        var filePath = Path.Combine(RepoPath, "test.txt");
        File.WriteAllText(filePath, "test");
        App.Run("-m", "test");
        var result = App.Run("-m", "test");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("Working tree clean."));
        }
    }

    [Test]
    public void ShouldCommitChangesToFile()
    {
        var filePath = Path.Combine(RepoPath, "test.txt");
        File.WriteAllText(filePath, "test");
        App.Run("-m", "test");
        File.WriteAllText(filePath, "Testing a second commit");
        var result = App.Run("-m", "test");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("Committed"));
        }
    }

    [Test]
    public void ShouldCommitFilesAndSubdirectories()
    {
        var filePath = Path.Combine(RepoPath, "test.txt");
        File.WriteAllText(filePath, "test");
        var dirPath = Path.Combine(RepoPath, "testdir");
        Directory.CreateDirectory(dirPath);
        var filePath2 = Path.Combine(dirPath, "test2.txt");
        File.WriteAllText(filePath2, "test");
        var result = App.Run("-m", "test");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("Committed"));
        }
    }
}