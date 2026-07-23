using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class FetchCommand : AsyncCommand<FetchCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[remote]")]
        public string? Remote { get; init; }
        [CommandOption("-b|--branch")]
        public string? Branch { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken ct)
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
        var remotes = new RemoteStore(gudPath);
        var remoteRefs = new RemoteRefStore(gudPath);
        var objectStore = new ObjectStore(gudPath);
        var objects = new ObjectRepository(objectStore);

        if (settings.Remote is null && remotes.ListRemotes().Count() != 1)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Unable to select remote.");
            return 1;
        }
        
        string remoteToUse;
        string url;
        string apiKey;

        var remote = settings.Remote;
        if (remote.IsNullOrWhiteSpace())
        {
            (remoteToUse, url, apiKey) = remotes.ListRemotes().First();
        }
        else
        {
            remoteToUse = remote!;
            var locatedRemote = remotes.GetRemote(remoteToUse);
            if (locatedRemote is null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Unable to find remote '{remoteToUse}'");
                return 1;
            }
            
            url = locatedRemote.Value.Url;
            apiKey = locatedRemote.Value.ApiKey;
        }

        var repo = url.Split('/').Last();
        url = url[..^(repo.Length + 1)];
        var client = new GudRemoteClient(url, repo, apiKey);
        var branch = settings.Branch ?? "main"; // TODO: refactor to assume all branches if none passed

        var serverCommit = await client.GetRefAsync(branch);
        if (serverCommit == null || serverCommit.IsNullOrWhiteSpace())
        {
            AnsiConsole.MarkupLine($"[yellow]Remote branch '{branch}' does not exist.");
            return 0;
        }

        var fetchedCount = await remoteRefs.FetchReachable(client, objectStore, objects, serverCommit);
        remoteRefs.SetTrackedCommit(remoteToUse, branch, serverCommit);
        AnsiConsole.MarkupLine($"[green]Fetched[/] {remoteToUse}/{branch} -> {serverCommit[..ObjectResolver.ComputeDisplayLength(gudPath)]} ({fetchedCount} objects)");
        return 0;
    }
}