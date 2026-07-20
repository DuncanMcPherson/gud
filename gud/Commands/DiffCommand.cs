using System.Text;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using gud.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class DiffCommand : Command<DiffCommand.Settings>
{
    public class Settings : CommandSettings
    {
        // Empty for now
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
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            return 1;
        }

        var gudPath = Path.Combine(root, ".gud");
        var repo = new ObjectRepository(new ObjectStore(gudPath));
        var refs = new RefStore(gudPath);
        var ignore = new GudIgnoreMatcher(Path.Combine(root, ".gudignore"));

        var headCommit = refs.GetHead();
        string? treeHash = null;
        if (!headCommit.IsNullOrWhiteSpace())
        {
            var commit = Commit.Read(repo, headCommit!);
            treeHash = commit.TreeHash;
        }

        var changes = WorkingTreeStatus.Compute(root, treeHash, repo, ignore);
        var modified = changes.Where(x => x.ChangeType == ChangeType.Modified);

        foreach (var change in modified)
        {
            var fullPath = Path.Combine(root, change.Path);
            var newLines = File.ReadAllLines(fullPath);
            var oldBlobHash = WorkingTreeStatus.FlattenTree(repo, treeHash!, "")[change.Path];
            var (_, oldContent) = repo.ReadObject(oldBlobHash);
            var oldLines = Encoding.UTF8.GetString(oldContent).Split('\n');
            DiffRenderer.RenderFileDiff(change.Path, oldLines, newLines);
            AnsiConsole.WriteLine();
        }

        foreach (var change in changes.Where(c => c.ChangeType != ChangeType.Modified))
        {
            var label = change.ChangeType == ChangeType.Added ? "new file" : "deleted";
            AnsiConsole.MarkupLine($"[bold]diff --gud a/{change.Path} b/{change.Path}[/] {label}");
        }
        return 0;
    }
}