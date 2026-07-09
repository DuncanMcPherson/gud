using gud.Commands;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.Setup;

public abstract class TestRepoWithCommitBase : TestRepoWithConfigBase
{
    private CommandAppTester _commitApp;
    [SetUp]
    public void SetupWithCommit()
    {
        var commitApp = new CommandAppTester();
        var filePath = Path.Combine(RepoPath, "test.txt");
        File.WriteAllText(filePath, Guid.NewGuid().ToString());
        commitApp.SetDefaultCommand<CommitCommand>();
        commitApp.Run("-m", "test commit");
        _commitApp = commitApp;
    }

    protected void AddCommit()
    {
        var newDirPath = Path.Combine(RepoPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(newDirPath);
        var filePath = Path.Combine(newDirPath, "test.txt");
        File.WriteAllText(filePath, Guid.NewGuid().ToString());
        _commitApp.Run("-m", "test 2nd commit");
    }
}