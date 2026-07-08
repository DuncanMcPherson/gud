using System.ComponentModel;
using gud.Stores;
using gud.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class BranchCommand : Command<BranchCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[name]")]
        public string? Name { get; set; }
        
        [CommandOption("-r")]
        [Description("Renames the current branch if the new branch name does not already exist")]
        public bool Rename { get; set; }
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
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        var gudPath = Path.Combine(root, ".gud");
        var branches = new BranchStore(gudPath);
        var refStore = new RefStore(gudPath);

        if (string.IsNullOrEmpty(settings.Name))
        {
            foreach (var branch in branches.ListBranches())
                AnsiConsole.MarkupLine(branch == refStore.CurrentBranchName() ? $"[green]* {branch}[/]" : $"  {branch}");
            return 0;
        }

        if (branches.Exists(settings.Name))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Branch '{settings.Name}' already exists.");
            return 1;
        }
        var currentBranchName = refStore.CurrentBranchName();
        if (currentBranchName != null && settings.Rename)
        {
            if (string.IsNullOrEmpty(settings.Name))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Branch name cannot be empty");
                return 1;
            }
            
            branches.Rename(currentBranchName, settings.Name);
            return 0;
        }

        var currentCommit = refStore.GetHead();
        if (currentCommit == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No commits yet - cannot branch");
            return 1;
        }
        
        branches.SetCommit(settings.Name, currentCommit);
        return 0;
    }
}