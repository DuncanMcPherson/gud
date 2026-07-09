using gud.Commands;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.Setup;

public abstract class TestRepoWithBranchBase : TestRepoWithCommitBase
{
    protected string NewBranchName;
    [SetUp]
    public void SetUpWithBranch()
    {
        var branchApp = new CommandAppTester();
        branchApp.SetDefaultCommand<BranchCommand>();
        NewBranchName = Guid.NewGuid().ToString();
        branchApp.Run(NewBranchName);
    }
}