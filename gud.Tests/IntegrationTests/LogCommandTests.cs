using gud.Commands;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.IntegrationTests;

public class LogCommandTests : TestRepoWithCommitBase
{
    protected override CommandAppTester BuildApp()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<LogCommand>();
        return app;
    }
    
    [Test]
    public void ShouldPrintCommitMessage()
    {
        var result = App.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("test commit"));
        }
    }

    [Test]
    public void ShouldPrint2CommitMessages()
    {
        AddCommit();
        var result = App.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Contain("test 2nd commit"));
            Assert.That(result.Output, Does.Contain("test commit"));
        }
    }
}