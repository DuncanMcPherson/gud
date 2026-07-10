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
        [Description("The branch to check out")]
        public string Target { get; init; } = null!;
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken _)
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
        var builder = new CommitBuilder(repo);

        var targetCommit = branches.ResolveTarget(settings.Target);
        if (string.IsNullOrEmpty(targetCommit))
        {
            _console.MarkupLine($"[red]Error:[/] '{settings.Target}' is not a valid branch or commit");
            return 1;
        }

        var headCommit = refStore.GetHead();

        if (builder.HasUncommittedChanges(root, headCommit))
        {
            _console.MarkupLine($"[red]Error:[/] You have uncommitted changes. Please commit them before checking out a new branch."); // Note: this message will need to be updated once we have stashing
            return 1;
        }

        string? oldTreeHash = null;
        if (headCommit != null)
        {
            var (_, headContent) = repo.ReadObject(headCommit);
            var committedHead = Commit.Read(headContent);
            oldTreeHash = committedHead.TreeHash;
        }
        
        var (_, targetContent) = repo.ReadObject(targetCommit);
        var committedTarget = Commit.Read(targetContent);
        var newTreeHash = committedTarget.TreeHash;
        
        WorkingTreeSync.SyncWorkingTree(oldTreeHash, newTreeHash, root, repo);
        
        if (branches.Exists(settings.Target))
            refStore.SetBranch(settings.Target);
        else
            refStore.SetHead(targetCommit);
        _console.MarkupLine($"[green]Switched to[/] {settings.Target}");
        return 0;
    }
}