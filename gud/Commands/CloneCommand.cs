using gud.Core.Repository;
using gud.Core.Services;
using gud.Core.Stores;
using gud.Core.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace gud.Commands;

public class CloneCommand : AsyncCommand<CloneCommand.Settings>
{
    public const string DefaultRemoteName = "origin";
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<url>")]
        public string Url { get; set; }
        
        [CommandArgument(1, "[directory]")]
        public string? Directory { get; set; }
        
        [CommandOption("--api-key")]
        public string? ApiKey { get; set; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            var repoName = settings.Url.Split('/').Last();
            var baseUrl = settings.Url[..^(repoName.Length + 1)];
            var targetDir = settings.Directory ?? repoName;

            if (settings.ApiKey.IsNullOrWhiteSpace())
            {
                AnsiConsole.MarkupLine("[red]Error: [/]Api key is missing and required");
            }

            if (Directory.Exists(targetDir) && Directory.GetFileSystemEntries(targetDir).Length > 0)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] '{targetDir}' already exists and is not empty");
                return 1;
            }

            Directory.CreateDirectory(targetDir);
            var gudPath = Path.Combine(targetDir, ".gud");
            await GudRepository.CreateAsync(gudPath);

            var remotes = new RemoteStore(gudPath);
            remotes.AddRemote(DefaultRemoteName, settings.Url, settings.ApiKey!);

            var client = new GudRemoteClient(baseUrl, repoName, settings.ApiKey!);
            var objectStore = new ObjectStore(gudPath);
            var objects = new ObjectRepository(objectStore);
            var remoteRefs = new RemoteRefStore(gudPath);

            var branches = await client.ListBranchesAsync(); // TODO: implement
            if (branches.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Cloned empty repository[/]");
                return 0;
            }

            var localBranches = new BranchStore(gudPath);
            foreach (var (branchName, commitHash) in branches)
            {
                await remoteRefs.FetchReachable(client, objectStore, objects, commitHash);
                remoteRefs.SetTrackedCommit(DefaultRemoteName, branchName, commitHash);

                localBranches.SetCommit(branchName, commitHash);
            }

            var defaultBranch = branches.ContainsKey("main") ? "main" : branches.Keys.First();
            var refStore = new RefStore(gudPath);
            refStore.SetBranch(defaultBranch);

            CheckoutUtility.Checkout(null, branches[defaultBranch], defaultBranch, targetDir, objects, localBranches, refStore);

            AnsiConsole.MarkupLine($"[green]Cloned into [/]{targetDir}");
            return 0;
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error: [/]{e.Message}");
            return 1;
        }
    }
}