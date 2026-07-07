using gud.Repository;
using gud.Services;
using gud.Stores;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class CommitCommand : AsyncCommand<CommitCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-m|--message")]
        public string? Message { get; set; }
        
        [CommandOption("--author")]
        public string? Author { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
    {
        var message = string.IsNullOrWhiteSpace(settings.Message)
            ? AnsiConsole.Ask<string>("Commit message:")
            : settings.Message!;
        
        var author = string.IsNullOrWhiteSpace(settings.Author)
            ? AnsiConsole.Ask<string>("Author name:")
            : settings.Author!;

        var repo = new ObjectRepository(new ObjectStore(".gud"));
        var refStore = new RefStore(".gud");
        var builder = new CommitBuilder(repo);

        var parentHash = refStore.GetHead();
        var parents = string.IsNullOrEmpty(parentHash) ? Array.Empty<string>() : new[] { parentHash };

        var commitHash = builder.CommitDirectory(".", parents, author, message);
        refStore.SetHead(commitHash);
        AnsiConsole.MarkupLine($"[green]Committed[/] {commitHash[..8]}");
        return 0;
    }
}