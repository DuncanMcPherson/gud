using gud.Stores;
using gud.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class ConfigCommand : AsyncCommand<ConfigCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[key]")]
        public string? Key { get; set; }
        [CommandArgument(1, "[value]")]
        public string? Value { get; set; }
        
        [CommandOption("-l|--list")]
        public bool List { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        string root;
        try
        {
            root = GudRepository
                .RequireRoot(); // Eventually, we won't need to be in a repo, but for now all config is repo specific
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"Failed to get repo root: {ex.Message}");
            return 1;
        }

        var configStore = new ConfigStore(root);

        if (settings.List)
        {
            foreach (var (key, value) in configStore.All())
            {
                AnsiConsole.MarkupLine($"[grey]{key}[/] = {value}");
            }

            return 0;
        }

        if (string.IsNullOrEmpty(settings.Key))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Key is required.");
            return 1;
        }

        if (string.IsNullOrEmpty(settings.Value))
        {
            var existing = configStore.Get(settings.Key);
            if (existing == null)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Key '{settings.Key}' does not exist.");
                return 1;
            }
            AnsiConsole.WriteLine(existing);
            return 0;
        }
        
        configStore.Set(settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]{settings.Key}[/] set to [grey]{settings.Value}[/]");
        return 0;
    }
}