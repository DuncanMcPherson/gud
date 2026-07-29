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
        var refs = new RefStore(gudPath);
        var objectStore = new ObjectStore(gudPath);
        var objects = new ObjectRepository(objectStore);

        if (!TryResolveRemote(remotes, settings.Remote, out var remoteToUse, out var url, out var apiKey, out var error))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {error}");
            return 1;
        }

        var branch = settings.Branch ?? refs.CurrentBranchName() ?? "main";
        var (baseUrl, repoName) = SplitRemoteUrl(url);

        var result = await FetchService.FetchAsync(
            remoteToUse, baseUrl, repoName, apiKey, branch,
            objectStore, objects, remoteRefs);

        if (result.RemoteBranchMissing)
        {
            AnsiConsole.MarkupLine($"[yellow]Remote branch '{branch}' does not exist.[/]");
            return 0;
        }

        var displayLen = ObjectResolver.ComputeDisplayLength(gudPath);
        var shortTip = result.TipCommit!.Length <= displayLen
            ? result.TipCommit
            : result.TipCommit[..displayLen];

        if (result.SkippedObjectTransfer)
        {
            AnsiConsole.MarkupLine(
                $"[green]Fetched[/] {remoteToUse}/{branch} -> {shortTip} (0 objects, already up to date with remote)");
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"[green]Fetched[/] {remoteToUse}/{branch} -> {shortTip} ({result.ObjectsFetched} objects)");
        }

        return 0;
    }

    internal static bool TryResolveRemote(
        RemoteStore remotes,
        string? remoteArg,
        out string remoteName,
        out string url,
        out string apiKey,
        out string error)
    {
        remoteName = "";
        url = "";
        apiKey = "";
        error = "";

        if (remoteArg is null)
        {
            var list = remotes.ListRemotes().ToList();
            if (list.Count != 1)
            {
                error = list.Count == 0 ? "No remotes found." : "Unable to select remote.";
                return false;
            }

            (remoteName, url, apiKey) = list[0];
            return true;
        }

        var located = remotes.GetRemote(remoteArg);
        if (located is null)
        {
            error = $"Unable to find remote '{remoteArg}'";
            return false;
        }

        remoteName = remoteArg;
        url = located.Value.Url;
        apiKey = located.Value.ApiKey;
        return true;
    }

    internal static (string BaseUrl, string RepoName) SplitRemoteUrl(string url)
    {
        var repoName = url.Split('/').Last();
        var baseUrl = url[..^(repoName.Length + 1)];
        return (baseUrl, repoName);
    }
}
