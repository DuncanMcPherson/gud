using gud.Commands;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.Setup;

public abstract class TestRepoWithConfigBase : InitializedTestRepoBase
{
    [SetUp]
    public void SetupConfig()
    {
        var configApp = new CommandAppTester();
        configApp.SetDefaultCommand<ConfigCommand>();
        configApp.Run("user.name", "test");
    }
}