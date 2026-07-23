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

        var gudPath = Path.Combine(root, ".gud");
        var configStore = new ConfigStore(gudPath);
        var mergeState = new MergeState(gudPath);
        var isMergeCommit = mergeState.IsInProgress;

        string message;
        if (!string.IsNullOrWhiteSpace(settings.Message))
        {
            message = settings.Message!;
        }
        else if (isMergeCommit)
        {
            message = mergeState.ReadMergeMessage() ?? "Merge";
        }
        else
        {
            message = _console.Ask<string>("Commit message:");
        }

        var author = configStore.Get("user.name");
        if (string.IsNullOrWhiteSpace(author))
        {
            _console.MarkupLine("[red]Error:[/] Author name is required. Set it in gud config using 'gud config user.name <name>'.");
            return 1;
        }

        var repo = new ObjectRepository(new ObjectStore(gudPath));
        var refStore = new RefStore(gudPath);
        var builder = new CommitBuilder(repo);

        if (isMergeCommit)
        {
            var remaining = FindUnresolvedConflicts(root, mergeState);
            if (remaining.Count > 0)
            {
                _console.MarkupLine("[red]Error:[/] You still have unresolved conflicts:");
                foreach (var path in remaining)
                    _console.MarkupLine($"  [red]{Markup.Escape(path)}[/]");
                _console.MarkupLine("Resolve them, then run [bold]gud commit[/] again.");
                return 1;
            }
        }

        var parentHash = refStore.GetHead();
        List<string> parents;
        if (isMergeCommit)
        {
            var theirs = mergeState.ReadMergeHead();
            if (string.IsNullOrEmpty(parentHash) || string.IsNullOrEmpty(theirs))
            {
                _console.MarkupLine("[red]Error:[/] Invalid merge state (missing HEAD or MERGE_HEAD).");
                return 1;
            }

            parents = [parentHash, theirs];
        }
        else
        {
            parents = string.IsNullOrEmpty(parentHash) ? [] : [parentHash];
        }

        string commitHash;
        try
        {
            commitHash = builder.CommitDirectory(".", parents, author, message);
        }
        catch (InvalidOperationException ex)
        {
            // During a merge, allow committing even if the tree matches the first parent
            // only when we have two parents — rewrite via direct commit if needed.
            if (isMergeCommit && ex.Message.Contains("Working tree clean", StringComparison.Ordinal))
            {
                _console.MarkupLine("[red]Error:[/] Nothing to commit; resolve merge by changing files or abort.");
                return 1;
            }

            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }

        refStore.SetHead(commitHash);
        if (isMergeCommit)
            mergeState.Clear();

        var minDisplayLength = ObjectResolver.ComputeDisplayLength(gudPath);
        _console.MarkupLine($"[green]Committed[/] {commitHash[..minDisplayLength]}");
        return 0;
    }

    private static List<string> FindUnresolvedConflicts(string root, MergeState mergeState)
    {
        var unresolved = new List<string>();
        foreach (var path in mergeState.ReadConflicts())
        {
            var full = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full))
            {
                // Deleted file: treat as resolved if user removed it intentionally
                continue;
            }

            var bytes = File.ReadAllBytes(full);
            if (BlobMerger.ContainsConflictMarkers(bytes))
                unresolved.Add(path);
        }

        return unresolved;
    }
}
