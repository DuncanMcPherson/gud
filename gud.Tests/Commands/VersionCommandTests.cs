using gud.Commands;
using gud.Utilities;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Testing;

namespace gud.Tests.Commands;

[TestFixture]
public class VersionCommandTests
{
    [Test]
    public void AppVersion_Get_ReturnsNonEmptyVersion()
    {
        var version = AppVersion.Get();
        Assert.That(version, Is.Not.Null.And.Not.Empty);
        Assert.That(version, Does.Match(@"\d+\.\d+"));
        Assert.That(version, Does.Not.Contain('+'));
    }

    [Test]
    public void VersionCommand_PrintsVersion()
    {
        var app = new CommandAppTester();
        app.SetDefaultCommand<VersionCommand>();
        var result = app.Run();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output.Trim(), Is.EqualTo(AppVersion.Get()));
            Assert.That(result.Output, Does.Match(@"\d+\.\d+"));
        }
    }

    [Test]
    public void VersionFlag_PrintsVersion()
    {
        var app = new CommandAppTester();
        app.Configure(config =>
        {
            config.SetApplicationName("gud");
            config.SetApplicationVersion(AppVersion.Get());
            config.AddCommand<VersionCommand>("version");
        });

        var result = app.Run("--version");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.Output, Does.Match(@"\d+\.\d+"));
        }
    }
}
