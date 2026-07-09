using gud.Commands;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.Setup;

public abstract class InitializedTestRepoBase : RepoTestBase
{
    protected CommandAppTester App;

    [SetUp]
    public void SetupInitializedRepo()
    {
        Directory.SetCurrentDirectory(RepoPath);
        var initApp = new CommandAppTester();
        initApp.SetDefaultCommand<InitCommand>();
        initApp.Run();

        App = BuildApp();
    }

    protected abstract CommandAppTester BuildApp();

    [TearDown]
    public void TearDownInitializedRepo()
    {
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }
}