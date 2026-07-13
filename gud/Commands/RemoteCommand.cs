using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class RemoteCommand(IAnsiConsole console) : Command<RemoteCommand.Settings>
{
    private readonly IAnsiConsole _console = console;
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        public string Name { get; set; }
        
        [CommandArgument(1, "[url]")]
        public string Url { get; set; }
        
        [CommandArgument(2, "[apiKey]")]
        public string ApiKey { get; set; }
        
        [CommandOption("-r|--remove")]
        public bool Remove { get; set; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        string root;
        try
        {
            root = GudRepository.RequireRoot();
        }
        catch (InvalidOperationException ex)
        {
            _console.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        var remotes = new RemoteStore(Path.Combine(root, ".gud"));

        if (settings.Remove)
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                _console.MarkupLine($"[red]Error:[/] Name is required");
                return 1;
            }
            remotes.RemoveRemote(settings.Name);
            return 0;
        }

        if (settings.Name.IsNullOrWhiteSpace())
        {
            foreach (var (name, url, _) in remotes.ListRemotes())
                _console.MarkupLine($"[yellow]{name}[/] {url}");
            return 0;
        }
        
        remotes.AddRemote(settings.Name, settings.Url, settings.ApiKey);
        return 0;
    }
}