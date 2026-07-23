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

    [Test]
    public void ShouldDeleteBranch()
    {
        const string name = "to-delete";
        Assert.That(App.Run(name).ExitCode, Is.EqualTo(0));
        var branchFilePath = Path.Combine(RepoPath, ".gud", "refs", "heads", name);
        Assert.That(File.Exists(branchFilePath), Is.True);

        var result = App.Run("-d", name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("Deleted branch"));
            Assert.That(result.Output, Does.Contain(name));
            Assert.That(File.Exists(branchFilePath), Is.False);
        }
    }

    [Test]
    public void ShouldErrorWhenDeleteMissingName()
    {
        var result = App.Run("-d");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("Branch name is required"));
        }
    }

    [Test]
    public void ShouldErrorWhenDeleteUnknownBranch()
    {
        var result = App.Run("-d", "does-not-exist");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("does not exist"));
        }
    }

    [Test]
    public void ShouldErrorWhenDeletingCurrentBranch()
    {
        var result = App.Run("-d", "main");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("currently on"));
            Assert.That(File.Exists(Path.Combine(RepoPath, ".gud", "refs", "heads", "main")), Is.True);
        }
    }

    [Test]
    public void ShouldErrorWhenCombiningDeleteAndRename()
    {
        var result = App.Run("-d", "-r", "other");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("Cannot combine"));
        }
    }

    [Test]
    public void ShouldDeleteNestedBranchAndPruneEmptyDirs()
    {
        const string name = "feature/nested-x";
        Assert.That(App.Run(name).ExitCode, Is.EqualTo(0));
        var branchFilePath = Path.Combine(RepoPath, ".gud", "refs", "heads", "feature", "nested-x");
        Assert.That(File.Exists(branchFilePath), Is.True);

        var result = App.Run("-d", name);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0), result.Output);
            Assert.That(File.Exists(branchFilePath), Is.False);
            Assert.That(Directory.Exists(Path.Combine(RepoPath, ".gud", "refs", "heads", "feature")), Is.False);
        }
    }

    [Test]
    public void ShouldNotListDeletedBranch()
    {
        const string name = "gone";
        App.Run(name);
        App.Run("-d", name);

        // Fresh app so output is only the list (tester accumulates prior runs)
        var listApp = new CommandAppTester();
        listApp.SetDefaultCommand<BranchCommand>();
        var result = listApp.Run();
        Assert.That(result.ExitCode, Is.EqualTo(0));
        Assert.That(result.Output, Does.Not.Contain(name));
        Assert.That(result.Output, Does.Contain("main"));
    }
}