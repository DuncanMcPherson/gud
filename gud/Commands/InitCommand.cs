using gud.Core.Utilities;
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
        var gudPath = await GudRepository.CreateAsync(Directory.GetCurrentDirectory());
        
        _console.MarkupLine($"[green]Initialized empty gud repository[/] in {gudPath}");
        return 0;
    }
}