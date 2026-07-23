using System.ComponentModel;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class CheckoutCommand : Command<CheckoutCommand.Settings>
{
    private readonly IAnsiConsole _console;

    public CheckoutCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<target>")]
        [Description("Local branch, remote-tracking ref (e.g. origin/feat/x), or commit")]
        public string Target { get; init; } = null!;
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

        var gudPath = Path.Combine(root, ".gud");
        var repo = new ObjectRepository(new ObjectStore(gudPath));
        var refStore = new RefStore(gudPath);
        var branches = new BranchStore(gudPath);
        var remoteRefs = new RemoteRefStore(gudPath);
        var builder = new CommitBuilder(repo);

        if (new MergeState(gudPath).IsInProgress)
        {
            _console.MarkupLine("[red]Error:[/] Cannot checkout while a merge is in progress. Commit or run 'gud merge --abort'.");
            return 1;
        }

        var targetCommit = branches.ResolveTarget(settings.Target);
        if (string.IsNullOrEmpty(targetCommit))
        {
            _console.MarkupLine($"[red]Error:[/] '{settings.Target}' is not a valid branch, remote-tracking ref, or commit");
            return 1;
        }

        var headCommit = refStore.GetHead();

        if (builder.HasUncommittedChanges(root, headCommit))
        {
            _console.MarkupLine("[red]Error:[/] You have uncommitted changes. Please commit them before checking out a new branch.");
            return 1;
        }

        string? oldTreeHash = null;
        if (headCommit != null)
        {
            var headContent = repo.ReadObject(headCommit).Content;
            var committedHead = Commit.Read(headContent);
            oldTreeHash = committedHead.TreeHash;
        }

        var targetContent = repo.ReadObject(targetCommit).Content;
        var committedTarget = Commit.Read(targetContent);
        var newTreeHash = committedTarget.TreeHash;

        WorkingTreeSync.SyncWorkingTree(oldTreeHash, newTreeHash, root, repo);

        if (branches.Exists(settings.Target))
        {
            refStore.SetBranch(settings.Target);
            _console.MarkupLine($"[green]Switched to[/] {settings.Target}");
            return 0;
        }

        // Remote-tracking ref: origin/feat/pull → create local feat/pull when missing
        if (remoteRefs.TrySplitTrackingName(settings.Target, out _, out var remoteBranch))
        {
            if (!branches.Exists(remoteBranch))
            {
                branches.SetCommit(remoteBranch, targetCommit);
                refStore.SetBranch(remoteBranch);
                _console.MarkupLine($"[green]Switched to a new branch[/] '{remoteBranch}'");
                return 0;
            }

            // Local branch already exists: stay detached at the remote-tracking tip (git-like)
            refStore.SetHead(targetCommit);
            _console.MarkupLine($"[green]Switched to[/] {settings.Target} [grey](detached HEAD)[/]");
            return 0;
        }

        // Bare commit / short hash
        refStore.SetHead(targetCommit);
        _console.MarkupLine($"[green]Switched to[/] {settings.Target}");
        return 0;
    }
}
