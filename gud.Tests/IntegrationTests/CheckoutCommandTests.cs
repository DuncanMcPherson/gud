using gud.Commands;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.IntegrationTests;

[TestFixture]
public class CheckoutCommandTests : TestRepoWithBranchBase
{
    protected override CommandAppTester BuildApp()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<CheckoutCommand>();
        return app;
    }

    [Test]
    public void ShouldCheckoutBranch()
    {
        var result = App.Run(NewBranchName);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain(NewBranchName));
        }
    }
    
    [Test]
    public void ShouldShowErrorAndExitWhenUncommittedChangesExist()
    {
        var newFilePath = Path.Combine(RepoPath, "new-file.txt");
        File.WriteAllText(newFilePath, "new content");
        var result = App.Run(NewBranchName);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.Contain("Error: You have uncommitted changes. Please commit them before checking out a new branch."));
        }
    }
}