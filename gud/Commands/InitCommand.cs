using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class InitCommand : AsyncCommand<InitCommand.Settings>
{
    private readonly IAnsiConsole _console;
    
    public InitCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var gudPath = Path.Combine(Directory.GetCurrentDirectory(), ".gud");

        if (Directory.Exists(gudPath))
        {
            _console.MarkupLine("[red]Error:[/] Repository already initialized here");
            return 1;
        }

        Directory.CreateDirectory(Path.Combine(gudPath, "objects"));
        Directory.CreateDirectory(Path.Combine(gudPath, "refs", "heads"));

        await File.WriteAllTextAsync(Path.Combine(gudPath, "HEAD"), "ref: refs/heads/main\n", cancellationToken);
        
        _console.MarkupLine($"[green]Initialized empty gud repository[/] in {gudPath}");
        return 0;
    }
}