using System.ComponentModel;
using gud.Stores;
using gud.Utilities;
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

        if (string.IsNullOrEmpty(settings.Name) && !settings.Rename)
        {
            foreach (var branch in branches.ListBranches())
                _console.MarkupLine(branch == refStore.CurrentBranchName() ? $"[green]* {branch}[/]" : $"  {branch}");
            return 0;
        }

        if (branches.Exists(settings.Name!))
        {
            _console.MarkupLine($"[red]Error:[/] Branch '{settings.Name}' already exists.");
            return 1;
        }
        var currentBranchName = refStore.CurrentBranchName();
        if (currentBranchName != null && settings.Rename)
        {
            if (string.IsNullOrEmpty(settings.Name))
            {
                _console.MarkupLine($"[red]Error:[/] Branch name cannot be empty");
                return 1;
            }
            
            branches.Rename(currentBranchName, settings.Name);
            refStore.SetBranch(settings.Name);
            _console.MarkupLine($"[green]Branch '{currentBranchName}' renamed to '{settings.Name}'.[/]");
            return 0;
        }

        if (string.IsNullOrEmpty(currentBranchName) && settings.Rename)
        {
            _console.MarkupLine("[red]Error:[/] Cannot rename current branch. No branch checked out");
            return 1;
        }

        var currentCommit = refStore.GetHead();
        if (currentCommit == null)
        {
            _console.MarkupLine("[red]Error:[/] No commits yet - cannot branch");
            return 1;
        }
        
        branches.SetCommit(settings.Name!, currentCommit);
        _console.MarkupLine($"[green]Branch '{settings.Name}' created.[/]");
        return 0;
    }
}