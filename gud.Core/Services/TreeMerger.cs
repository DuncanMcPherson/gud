namespace gud.Core.Services;

public enum PathMergeStatus
{
    /// <summary>Path is present in the result with a single resolved blob hash.</summary>
    Resolved,
    /// <summary>Path was deleted on the winning side(s).</summary>
    Deleted,
    /// <summary>Both sides changed the path incompatibly.</summary>
    Conflict
}

public sealed class PathMergeResult
{
    public required string Path { get; init; }
    public PathMergeStatus Status { get; init; }
    /// <summary>Resolved blob hash when <see cref="Status"/> is <see cref="PathMergeStatus.Resolved"/>.</summary>
    public string? BlobHash { get; init; }
    public string? OursHash { get; init; }
    public string? TheirsHash { get; init; }
    public string? BaseHash { get; init; }
}

public sealed class TreeMergeResult
{
    /// <summary>Path → blob hash for all non-conflict, non-deleted paths.</summary>
    public required Dictionary<string, string> MergedPaths { get; init; }
    public required IReadOnlyList<PathMergeResult> Conflicts { get; init; }
    public bool HasConflicts => Conflicts.Count > 0;
}

/// <summary>
/// Path-level three-way merge over flattened tree maps (path → blob hash).
/// </summary>
public static class TreeMerger
{
    public static TreeMergeResult Merge(
        IReadOnlyDictionary<string, string> baseMap,
        IReadOnlyDictionary<string, string> oursMap,
        IReadOnlyDictionary<string, string> theirsMap)
    {
        var allPaths = new HashSet<string>(baseMap.Keys);
        allPaths.UnionWith(oursMap.Keys);
        allPaths.UnionWith(theirsMap.Keys);

        var merged = new Dictionary<string, string>();
        var conflicts = new List<PathMergeResult>();

        foreach (var path in allPaths.OrderBy(p => p, StringComparer.Ordinal))
        {
            baseMap.TryGetValue(path, out var bas);
            oursMap.TryGetValue(path, out var ours);
            theirsMap.TryGetValue(path, out var theirs);

            var result = MergePath(path, bas, ours, theirs);
            switch (result.Status)
            {
                case PathMergeStatus.Resolved when result.BlobHash is not null:
                    merged[path] = result.BlobHash;
                    break;
                case PathMergeStatus.Conflict:
                    conflicts.Add(result);
                    break;
                case PathMergeStatus.Deleted:
                    break;
            }
        }

        return new TreeMergeResult { MergedPaths = merged, Conflicts = conflicts };
    }

    private static PathMergeResult MergePath(string path, string? bas, string? ours, string? theirs)
    {
        // Both missing (should not happen for union paths, but safe)
        if (ours is null && theirs is null)
            return Ok(path, null, bas, ours, theirs, deleted: true);

        // Unchanged both sides
        if (ours == theirs)
        {
            if (ours is null)
                return Ok(path, null, bas, ours, theirs, deleted: true);
            return Ok(path, ours, bas, ours, theirs);
        }

        // Only ours changed (or added/deleted) relative to base
        if (theirs == bas)
        {
            if (ours is null)
                return Ok(path, null, bas, ours, theirs, deleted: true);
            return Ok(path, ours, bas, ours, theirs);
        }

        // Only theirs changed relative to base
        if (ours == bas)
        {
            if (theirs is null)
                return Ok(path, null, bas, ours, theirs, deleted: true);
            return Ok(path, theirs, bas, ours, theirs);
        }

        // Both changed differently (includes modify/delete and add/add conflicts)
        return new PathMergeResult
        {
            Path = path,
            Status = PathMergeStatus.Conflict,
            BaseHash = bas,
            OursHash = ours,
            TheirsHash = theirs
        };
    }

    private static PathMergeResult Ok(
        string path, string? blob, string? bas, string? ours, string? theirs, bool deleted = false)
        => new()
        {
            Path = path,
            Status = deleted ? PathMergeStatus.Deleted : PathMergeStatus.Resolved,
            BlobHash = blob,
            BaseHash = bas,
            OursHash = ours,
            TheirsHash = theirs
        };
}
