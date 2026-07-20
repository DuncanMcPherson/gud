using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class StatusCommand : Command<StatusCommand.Settings>
{
    private readonly IAnsiConsole _console;
    
    public StatusCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken ct)
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
        var objects = new ObjectRepository(new ObjectStore(gudPath));
        var refs = new RefStore(gudPath);
        var ignore = new GudIgnoreMatcher(Path.Combine(root, ".gudignore"));

        var headCommit = refs.GetHead();
        string? headTreeHash = null;
        if (!headCommit.IsNullOrWhiteSpace())
        {
            var commit = Commit.Read(objects, headCommit!);
            headTreeHash = commit.TreeHash;
        }

        var changes = WorkingTreeStatus.Compute(root, headTreeHash, objects, ignore);

        foreach (var statusEntry in changes.OrderBy(c => c.Path))
        {
            var (color, label) = statusEntry.ChangeType switch
            {
                ChangeType.Added => ("green", "added"),
                ChangeType.Deleted => ("red", "deleted"),
                ChangeType.Modified => ("yellow", "modified"),
                _ => ("grey", "unknown")
            };
            
            _console.MarkupLine($"[{color}]{label}:[/] {statusEntry.Path}");
        }

        return 0;
    }
}