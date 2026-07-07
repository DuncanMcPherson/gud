using gud.Repository;
using gud.Services;
using gud.Stores;
using gud.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class CommitCommand : AsyncCommand<CommitCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-m|--message")]
        public string? Message { get; set; }
        
        [Obsolete("Set user.name and user.email in gud config instead")]
        [CommandOption("--author")]
        public string? Author { get; set; }
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
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            return 1;
        }
        var configStore = new ConfigStore(Path.Combine(root, ".gud"));
        var message = string.IsNullOrWhiteSpace(settings.Message)
            ? AnsiConsole.Ask<string>("Commit message:")
            : settings.Message!;
        var author = configStore.Get("user.name");
        if (string.IsNullOrWhiteSpace(author) || !string.IsNullOrWhiteSpace(settings.Author))
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] '--author' is deprecated and will be removed in a future version.");
            author = settings.Author ?? AnsiConsole.Ask<string>("Author name:");
        }

        var repo = new ObjectRepository(new ObjectStore(Path.Combine(root, ".gud")));
        var refStore = new RefStore(Path.Combine(root, ".gud"));
        var builder = new CommitBuilder(repo);

        var parentHash = refStore.GetHead();
        var parents = string.IsNullOrEmpty(parentHash) ? Array.Empty<string>() : new[] { parentHash };

        var commitHash = builder.CommitDirectory(".", parents, author, message);
        refStore.SetHead(commitHash);
        AnsiConsole.MarkupLine($"[green]Committed[/] {commitHash[..8]}");
        return 0;
    }
}