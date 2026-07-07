using System.Text;
using gud.Models;
using gud.Repository;
using gud.Stores;
using gud.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class LogCommand : AsyncCommand<LogCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
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
        var repo = new ObjectRepository(new ObjectStore(Path.Combine(root, ".gud")));
        var refStore = new RefStore(Path.Combine(root, ".gud"));

        var current = refStore.GetHead();
        if (string.IsNullOrEmpty(current))
        {
            AnsiConsole.MarkupLine("[yellow]No commits yet[/]");
            return 0;
        }

        while (!string.IsNullOrEmpty(current))
        {
            var (_, content) = repo.ReadObject(current);
            var (parents, author, message, date) = DeserializeCommit(content);
            
            AnsiConsole.MarkupLine($"[yellow]{current[..8]}[/] {message.Split('\n')[0]} [grey]({author}, {date:g})[/]");
            
            current = parents.FirstOrDefault();
        }
        return 0;
    }

    private (IReadOnlyList<string> parents, string author, string message, DateTimeOffset date) DeserializeCommit(
        byte[] content)
    {
        var commitStr = Encoding.UTF8.GetString(content);
        var parts = commitStr.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var skipCount = 1;
        var parents = parts.Where(p => p.StartsWith("parent ")).Select(p => p.Split(' ')[1]).ToList();
        skipCount += parents.Count;
        var author = parts.Skip(skipCount++).First().Split(' ').Skip(1).Aggregate((a, b) => $"{a} {b}");
        var date = DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts.Skip(skipCount++).First().Split(' ')[1]));
        var message = parts.Skip(skipCount).Aggregate((a, b) => a + '\n' + b);
        return (parents, author, message, date);
    }
}