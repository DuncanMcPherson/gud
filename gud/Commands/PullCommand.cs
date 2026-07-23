using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class PullCommand : AsyncCommand<PullCommand.Settings>
{
    private readonly IAnsiConsole _console;

    public PullCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[remote]")]
        public string? Remote { get; init; }

        [CommandOption("-b|--branch")]
        public string? Branch { get; init; }
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
            _console.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        var gudPath = Path.Combine(root, ".gud");
        var remotes = new RemoteStore(gudPath);
        var remoteRefs = new RemoteRefStore(gudPath);
        var refs = new RefStore(gudPath);
        var branches = new BranchStore(gudPath);
        var objectStore = new ObjectStore(gudPath);
        var objects = new ObjectRepository(objectStore);
        var config = new ConfigStore(gudPath);

        if (!FetchCommand.TryResolveRemote(remotes, settings.Remote, out var remoteToUse, out var url, out var apiKey, out var error))
        {
            _console.MarkupLine($"[red]Error:[/] {error}");
            return 1;
        }

        var branch = settings.Branch ?? refs.CurrentBranchName();
        if (string.IsNullOrEmpty(branch))
        {
            _console.MarkupLine("[red]Error:[/] detached HEAD - specify a branch with -b/--branch");
            return 1;
        }

        var (baseUrl, repoName) = FetchCommand.SplitRemoteUrl(url);
        var fetchResult = await FetchService.FetchAsync(
            remoteToUse, baseUrl, repoName, apiKey, branch,
            objectStore, objects, remoteRefs);

        if (fetchResult.RemoteBranchMissing)
        {
            _console.MarkupLine($"[red]Error:[/] Remote branch '{branch}' does not exist.");
            return 1;
        }

        var displayLen = ObjectResolver.ComputeDisplayLength(gudPath);
        var shortTip = fetchResult.TipCommit!.Length <= displayLen
            ? fetchResult.TipCommit
            : fetchResult.TipCommit[..displayLen];

        if (fetchResult.SkippedObjectTransfer)
        {
            _console.MarkupLine(
                $"[green]Fetched[/] {remoteToUse}/{branch} -> {shortTip} (0 objects, already up to date with remote)");
        }
        else
        {
            _console.MarkupLine(
                $"[green]Fetched[/] {remoteToUse}/{branch} -> {shortTip} ({fetchResult.ObjectsFetched} objects)");
        }

        var author = config.Get("user.name");
        var mergeMessage = $"Merge branch '{remoteToUse}/{branch}'";
        var mergeService = new MergeService(root, objects, refs, branches);
        var mergeResult = mergeService.Merge(fetchResult.TipCommit!, mergeMessage, author);

        return mergeResult.Outcome switch
        {
            MergeOutcome.AlreadyUpToDate => PrintOk(mergeResult.Message),
            MergeOutcome.FastForward => PrintOk(mergeResult.Message),
            MergeOutcome.Initialized => PrintOk(mergeResult.Message),
            MergeOutcome.MergedClean => PrintMerged(mergeResult),
            MergeOutcome.Conflicts => PrintConflicts(mergeResult),
            _ => PrintError(mergeResult.Message)
        };
    }

    private int PrintOk(string message)
    {
        foreach (var line in message.Split('\n'))
            _console.MarkupLine(Markup.Escape(line));
        return 0;
    }

    private int PrintMerged(MergeResult result)
    {
        var display = result.ResultCommitHash is null
            ? result.Message
            : $"Merged. {result.ResultCommitHash}";
        _console.MarkupLine($"[green]{Markup.Escape(display)}[/]");
        return 0;
    }

    private int PrintConflicts(MergeResult result)
    {
        _console.MarkupLine("[yellow]Automatic merge failed; fix conflicts and then commit the result.[/]");
        foreach (var path in result.ConflictedPaths)
            _console.MarkupLine($"[red]CONFLICT:[/] {Markup.Escape(path)}");
        _console.MarkupLine("After resolving, run [bold]gud commit[/]. To abort, run [bold]gud merge --abort[/].");
        return 1;
    }

    private int PrintError(string message)
    {
        _console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
        return 1;
    }
}
