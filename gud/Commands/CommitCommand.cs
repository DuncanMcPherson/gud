using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class CommitCommand : AsyncCommand<CommitCommand.Settings>
{
    private readonly IAnsiConsole _console;
    
    public CommitCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-m|--message")]
        public string? Message { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        string root;
        try
        {
            root = GudRepository.RequireRoot();
        }
        catch (InvalidOperationException ex)
        {
            _console.MarkupLine($"[red]{ex.Message}[/]");
            return 1;
        }
        var configStore = new ConfigStore(Path.Combine(root, ".gud"));
        var message = string.IsNullOrWhiteSpace(settings.Message)
            ? _console.Ask<string>("Commit message:")
            : settings.Message!;
        var author = configStore.Get("user.name");
        if (string.IsNullOrWhiteSpace(author))
        {
            _console.MarkupLine("[red]Error:[/] Author name is required. Set it in gud config using 'gud config user.name <name>'.");
            return 1;
        }

        var repo = new ObjectRepository(new ObjectStore(Path.Combine(root, ".gud")));
        var refStore = new RefStore(Path.Combine(root, ".gud"));
        var builder = new CommitBuilder(repo);

        var parentHash = refStore.GetHead();
        var parents = string.IsNullOrEmpty(parentHash) ? Array.Empty<string>() : new[] { parentHash };

        var commitHash = builder.CommitDirectory(".", parents, author, message);
        refStore.SetHead(commitHash);
        var minDisplayLength = ObjectResolver.ComputeDisplayLength(root);
        _console.MarkupLine($"[green]Committed[/] {commitHash[..minDisplayLength]}");
        return 0;
    }
}