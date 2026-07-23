using System.ComponentModel;
using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class MergeCommand : Command<MergeCommand.Settings>
{
    private readonly IAnsiConsole _console;

    public MergeCommand(IAnsiConsole console)
    {
        _console = console;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[target]")]
        [Description("Branch or commit to merge into the current branch")]
        public string? Target { get; init; }

        [CommandOption("-m|--message <MESSAGE>")]
        [Description("Merge commit message")]
        public string? Message { get; init; }

        [CommandOption("--abort")]
        [Description("Abort an in-progress merge")]
        public bool Abort { get; init; }

        public override ValidationResult Validate()
        {
            if (Abort && !string.IsNullOrEmpty(Target))
                return ValidationResult.Error("Do not specify a target with --abort.");
            if (!Abort && string.IsNullOrEmpty(Target))
                return ValidationResult.Error("Specify a branch or commit to merge, or use --abort.");
            return base.Validate();
        }
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
        var objects = new ObjectRepository(new ObjectStore(gudPath));
        var refs = new RefStore(gudPath);
        var branches = new BranchStore(gudPath);
        var service = new MergeService(root, objects, refs, branches);

        if (settings.Abort)
        {
            var abortResult = service.Abort();
            if (abortResult.Outcome == MergeOutcome.Failed)
            {
                _console.MarkupLine($"[red]Error:[/] {Markup.Escape(abortResult.Message)}");
                return 1;
            }

            _console.MarkupLine($"[green]{Markup.Escape(abortResult.Message)}[/]");
            return 0;
        }

        var config = new ConfigStore(gudPath);
        var author = config.Get("user.name");

        var result = service.Merge(settings.Target!, settings.Message, author);
        return result.Outcome switch
        {
            MergeOutcome.AlreadyUpToDate => PrintOk(result.Message),
            MergeOutcome.FastForward => PrintOk(result.Message),
            MergeOutcome.MergedClean => PrintMerged(result),
            MergeOutcome.Conflicts => PrintConflicts(result),
            _ => PrintError(result.Message)
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
