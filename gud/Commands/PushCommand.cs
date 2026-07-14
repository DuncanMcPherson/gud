using gud.Core.Repository;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class PushCommand : AsyncCommand<PushCommand.Settings>
{
    private readonly IAnsiConsole _console;
    
    public PushCommand(IAnsiConsole console)
    {
        _console = console;
    }
    
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[origin]")]
        public string? Origin { get; set; }
        
        [CommandArgument(1, "[branch]")]
        public string? BranchArg { get; set; }
        
        [CommandOption("-b|--branch <BRANCH>")]
        public string? BranchOption { get; set; }

        public string? Branch => BranchOption ?? BranchArg;

        public override ValidationResult Validate()
        {
            if (BranchOption is not null && BranchArg is not null)
                return ValidationResult.Error("Specify branch through either the flag or the argument");
            return base.Validate();
        }
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
            _console.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        var gudPath = Path.Combine(root, ".gud");
        var remotes = new RemoteStore(gudPath);
        var refs = new RefStore(gudPath);
        var objects = new ObjectRepository(new ObjectStore(gudPath));
        
        return 0;
    }
}