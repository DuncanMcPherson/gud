using gud.Core.Repository;
using gud.Core.Stores;

namespace gud.Core.Services;

public sealed class FetchResult
{
    public required string RemoteName { get; init; }
    public required string Branch { get; init; }
    public string? TipCommit { get; init; }
    public int ObjectsFetched { get; init; }
    public bool SkippedObjectTransfer { get; init; }
    public bool RemoteBranchMissing { get; init; }
}

/// <summary>
/// Fetches a remote branch tip and its reachable objects, skipping object transfer
/// when the remote tip matches the locally tracked tip and objects already exist.
/// </summary>
public static class FetchService
{
    /// <summary>
    /// Returns true when object download can be skipped: tips match and the tip
    /// commit is already in the local object store.
    /// </summary>
    public static bool ShouldSkipObjectFetch(string? serverTip, string? trackedTip, bool tipObjectExists)
    {
        if (string.IsNullOrWhiteSpace(serverTip)) return false;
        if (string.IsNullOrWhiteSpace(trackedTip)) return false;
        if (!string.Equals(serverTip, trackedTip, StringComparison.OrdinalIgnoreCase)) return false;
        return tipObjectExists;
    }

    public static async Task<FetchResult> FetchAsync(
        string remoteName,
        string baseUrl,
        string repoName,
        string apiKey,
        string branch,
        ObjectStore objectStore,
        ObjectRepository objects,
        RemoteRefStore remoteRefs,
        GudRemoteClient? client = null)
    {
        client ??= new GudRemoteClient(baseUrl, repoName, apiKey);

        var serverTip = await client.GetRefAsync(branch);
        if (string.IsNullOrWhiteSpace(serverTip))
        {
            return new FetchResult
            {
                RemoteName = remoteName,
                Branch = branch,
                RemoteBranchMissing = true
            };
        }

        // Normalize quotes if any
        serverTip = serverTip.Trim().Trim('"');

        var trackedTip = remoteRefs.GetTrackedCommit(remoteName, branch);
        if (ShouldSkipObjectFetch(serverTip, trackedTip, objectStore.Exists(serverTip)))
        {
            return new FetchResult
            {
                RemoteName = remoteName,
                Branch = branch,
                TipCommit = serverTip,
                ObjectsFetched = 0,
                SkippedObjectTransfer = true
            };
        }

        var fetchedCount = await remoteRefs.FetchReachable(client, objectStore, objects, serverTip);
        remoteRefs.SetTrackedCommit(remoteName, branch, serverTip);

        return new FetchResult
        {
            RemoteName = remoteName,
            Branch = branch,
            TipCommit = serverTip,
            ObjectsFetched = fetchedCount,
            SkippedObjectTransfer = false
        };
    }
}
