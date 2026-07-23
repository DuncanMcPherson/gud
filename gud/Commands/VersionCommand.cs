using gud.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class VersionCommand : Command<VersionCommand.Settings>
{
    private readonly IAnsiConsole _console;

    public VersionCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Match Spectre's --version output (version string only)
        _console.WriteLine(AppVersion.Get());
        return 0;
    }
}
