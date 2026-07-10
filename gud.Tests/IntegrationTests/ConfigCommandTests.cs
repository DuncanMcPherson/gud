using gud.Commands;
using gud.Tests.Setup;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.IntegrationTests;

[TestFixture]
public class ConfigCommandTests : InitializedTestRepoBase
{
    protected override CommandAppTester BuildApp()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<ConfigCommand>();
        return app;
    }

    [Test]
    public void SHOULD_SetValueInConfigFile()
    {
        App.Run("user.name", "Duncan");
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), ".gud", "config");
        var contents = File.ReadAllText(configPath);
        Assert.That(contents, Is.EqualTo($"user.name = Duncan{Environment.NewLine}"));
    }

    [Test]
    public void SHOULD_ReadValueFromFile_WHEN_FileExistsWithKey()
    {
        App.Run("user.name", "Duncan");
        var result = App.Run("user.name");
        Assert.That(result.Output, Is.EqualTo("user.name set to Duncan\nDuncan"));
    }

    [Test]
    public void SHOULD_ListAllStoredValues()
    {
        App.Run("user.name", "Duncan");
        var result = App.Run("-l");
        Assert.That(result.Output, Is.EqualTo("user.name set to Duncan\nuser.name = Duncan"));
    }

    [Test]
    public void ShouldPrintKeyError_WhenNoArgs()
    {
        var result = App.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Is.EqualTo("Error: Key is required."));
        }
    }
    
    [Test]
    public void ShouldPrintKeyError_WhenNoValue()
    {
        var result = App.Run("user.name");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Is.EqualTo("Warning: Key 'user.name' does not exist."));
        }
    }
}