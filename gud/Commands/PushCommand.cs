using gud.Core.Repository;
using gud.Core.Services;
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
        [CommandArgument(0, "[remote]")]
        public string? Remote { get; set; }
        
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

        var commandRemote = settings.Remote;
        string remoteToUse;
        string url;
        string apiKey;
        if (commandRemote is null && remotes.ListRemotes().Count() != 1)
        {
            _console.MarkupLine("[red]Error:[/] No remotes found.");
            return 1;
        } 
        
        if (commandRemote is null)
        {
            (remoteToUse, url, apiKey) = remotes.ListRemotes().First();
        }
        else
        {
            remoteToUse = commandRemote;
            var locatedRemote = remotes.GetRemote(remoteToUse);
            if (locatedRemote is null)
            {
                _console.MarkupLine($"[red]Error:[/] No remote found for {remoteToUse}");
                return 1;
            }

            url = locatedRemote.Value.Url;
            apiKey = locatedRemote.Value.ApiKey;
        }

        var branch = settings.Branch ?? refs.CurrentBranchName();
        if (branch == null)
        {
            _console.MarkupLine("[red]Error:[/] detached HEAD - specify a branch to push");
            return 1;
        }

        var commitToPush = refs.GetCommit(branch);
        if (commitToPush is null)
        {
            _console.MarkupLine("[red]Error:[/] nothing to push - no commits yet");
            return 1;
        }
        
        var repoName = url.Split('/').Last();
        var client = new GudRemoteClient(url[..^(repoName.Length + 1)], repoName, apiKey);
        if (!await client.RepoExistsAsync())
            await client.CreateRepoAsync(repoName);
        
        AnsiConsole.MarkupLine("Collecting objects...");
        var localHashes = ObjectGraphWalker.CollectReachable(objects, commitToPush);
        var toUpload = new List<string>();

        foreach (var hash in localHashes)
        {
            if (!await client.ObjectExistsAsync(hash))
                toUpload.Add(hash);
        }

        if (toUpload.Count == 0)
        {
            _console.MarkupLine("[green]Nothing to push.[/]");
            return 0;
        }
        
        _console.MarkupLine($"[yellow]{toUpload.Count}[/] of [yellow]{localHashes.Count}[/] objects to upload.");
        foreach (var hash in toUpload)
        {
            var rawWithHeader = objects.ReadRawObjectFile(hash);
            await client.PutObjectAsync(hash, rawWithHeader);
        }

        try
        {
            await client.PutRefAsync(branch, commitToPush);
        }
        catch (HttpRequestException ex)
        {
            _console.MarkupLine($"[red]Push rejected:[/] {ex.Message}");
            return 1;
        }
        
        _console.MarkupLine($"[green]Pushed {branch}[/] -> {remoteToUse}/{branch}");
        return 0;
    }
}