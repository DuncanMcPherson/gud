using gud.Commands;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.IntegrationTests;

[TestFixture]
public class BranchCommandTests : TestRepoWithCommitBase
{
    protected override CommandAppTester BuildApp()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<BranchCommand>();
        return app;
    }
    
    [Test]
    public void ShouldPrintBranches()
    {
        var result = App.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("* main"));
        }
    }

    [Test]
    public void ShouldRenameCurrentBranch()
    {
        var result = App.Run("-r", "master");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("master"));
            var branchFilePath = Path.Combine(RepoPath, ".gud", "refs", "heads", "master");
            Assert.That(File.Exists(branchFilePath), Is.True);
        }
    }

    [Test]
    public void ShouldPrintErrorWhenBranchAlreadyExists()
    {
        var result = App.Run("main");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("Branch 'main' already exists"));
        }
    }

    [Test]
    public void ShouldPrintErrorWhenNameIsNotProvidedWithRename()
    {
        var result = App.Run("-r");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("Branch name cannot be empty"));
        }
    }

    [Test]
    public void ShouldCreateNewBranchWhenNoErrors()
    {
        var newBranchName = Guid.NewGuid().ToString();
        var result = App.Run(newBranchName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain(newBranchName));
            var branchFilePath = Path.Combine(RepoPath, ".gud", "refs", "heads", newBranchName);
            Assert.That(File.Exists(branchFilePath), Is.True);
        }
    }
}