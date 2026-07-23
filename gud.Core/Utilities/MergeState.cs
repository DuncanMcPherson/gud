namespace gud.Core.Utilities;

/// <summary>
/// On-disk merge-in-progress state under <c>.gud/</c>.
/// </summary>
public sealed class MergeState(string gudDirectory)
{
    private readonly string _mergeHead = Path.Combine(gudDirectory, "MERGE_HEAD");
    private readonly string _mergeMsg = Path.Combine(gudDirectory, "MERGE_MSG");
    private readonly string _mergeConflicts = Path.Combine(gudDirectory, "MERGE_CONFLICTS");

    public bool IsInProgress => File.Exists(_mergeHead);

    public string? ReadMergeHead()
        => File.Exists(_mergeHead) ? File.ReadAllText(_mergeHead).Trim() : null;

    public string? ReadMergeMessage()
        => File.Exists(_mergeMsg) ? File.ReadAllText(_mergeMsg) : null;

    public IReadOnlyList<string> ReadConflicts()
    {
        if (!File.Exists(_mergeConflicts)) return Array.Empty<string>();
        return File.ReadAllLines(_mergeConflicts)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }

    public void Write(string theirsCommit, string message, IEnumerable<string> conflictedPaths)
    {
        File.WriteAllText(_mergeHead, theirsCommit + "\n");
        File.WriteAllText(_mergeMsg, message);
        var paths = conflictedPaths.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        File.WriteAllText(_mergeConflicts, paths.Length == 0 ? "" : string.Join("\n", paths) + "\n");
    }

    public void Clear()
    {
        if (File.Exists(_mergeHead)) File.Delete(_mergeHead);
        if (File.Exists(_mergeMsg)) File.Delete(_mergeMsg);
        if (File.Exists(_mergeConflicts)) File.Delete(_mergeConflicts);
    }
}
