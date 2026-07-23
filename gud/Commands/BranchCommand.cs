using System.ComponentModel;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class BranchCommand : Command<BranchCommand.Settings>
{
    private readonly IAnsiConsole _console;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        public string? Name { get; set; }

        [CommandOption("-r")]
        [Description("Renames the current branch if the new branch name does not already exist")]
        public bool Rename { get; set; }

        [CommandOption("-d|--delete")]
        [Description("Deletes the named branch")]
        public bool Delete { get; set; }
    }

    public BranchCommand(IAnsiConsole console)
    {
        _console = console;
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
        var branches = new BranchStore(gudPath);
        var refStore = new RefStore(gudPath);

        if (settings.Delete && settings.Rename)
        {
            _console.MarkupLine("[red]Error:[/] Cannot combine --delete and rename.");
            return 1;
        }

        if (settings.Delete)
            return DeleteBranch(settings.Name, branches, refStore, gudPath);

        if (settings.Rename)
            return RenameBranch(settings.Name, branches, refStore);

        if (string.IsNullOrEmpty(settings.Name))
        {
            foreach (var branch in branches.ListBranches())
                _console.MarkupLine(branch == refStore.CurrentBranchName() ? $"[green]* {branch}[/]" : $"  {branch}");
            return 0;
        }

        if (branches.Exists(settings.Name))
        {
            _console.MarkupLine($"[red]Error:[/] Branch '{settings.Name}' already exists.");
            return 1;
        }

        var currentCommit = refStore.GetHead();
        if (currentCommit == null)
        {
            _console.MarkupLine("[red]Error:[/] No commits yet - cannot branch");
            return 1;
        }

        branches.SetCommit(settings.Name, currentCommit);
        _console.MarkupLine($"[green]Branch '{settings.Name}' created.[/]");
        return 0;
    }

    private int DeleteBranch(string? name, BranchStore branches, RefStore refStore, string gudPath)
    {
        if (string.IsNullOrEmpty(name))
        {
            _console.MarkupLine("[red]Error:[/] Branch name is required for delete.");
            return 1;
        }

        if (!branches.Exists(name))
        {
            _console.MarkupLine($"[red]Error:[/] Branch '{name}' does not exist.");
            return 1;
        }

        if (refStore.CurrentBranchName() == name)
        {
            _console.MarkupLine($"[red]Error:[/] Cannot delete the branch you are currently on ('{name}').");
            return 1;
        }

        var tip = branches.GetCommit(name);
        branches.Delete(name);

        if (!string.IsNullOrEmpty(tip))
        {
            var displayLen = ObjectResolver.ComputeDisplayLength(gudPath);
            var shortHash = tip.Length <= displayLen ? tip : tip[..displayLen];
            _console.MarkupLine($"[green]Deleted branch '{name}'[/] (was {shortHash}).");
        }
        else
        {
            _console.MarkupLine($"[green]Deleted branch '{name}'.[/]");
        }

        return 0;
    }

    private int RenameBranch(string? newName, BranchStore branches, RefStore refStore)
    {
        if (string.IsNullOrEmpty(newName))
        {
            _console.MarkupLine("[red]Error:[/] Branch name cannot be empty");
            return 1;
        }

        var currentBranchName = refStore.CurrentBranchName();
        if (currentBranchName == null)
        {
            _console.MarkupLine("[red]Error:[/] Cannot rename current branch. No branch checked out");
            return 1;
        }

        if (branches.Exists(newName))
        {
            _console.MarkupLine($"[red]Error:[/] Branch '{newName}' already exists.");
            return 1;
        }

        branches.Rename(currentBranchName, newName);
        refStore.SetBranch(newName);
        _console.MarkupLine($"[green]Branch '{currentBranchName}' renamed to '{newName}'.[/]");
        return 0;
    }
}
