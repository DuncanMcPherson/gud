using System.Text;
using gud.Core.Models;
using gud.Core.Repository;
using gud.Core.Stores;
using gud.Core.Utilities;

namespace gud.Core.Services;

public enum MergeOutcome
{
    AlreadyUpToDate,
    FastForward,
    MergedClean,
    Conflicts,
    Aborted,
    Failed
}

public sealed class MergeResult
{
    public required MergeOutcome Outcome { get; init; }
    public string? ResultCommitHash { get; init; }
    public IReadOnlyList<string> ConflictedPaths { get; init; } = Array.Empty<string>();
    public required string Message { get; init; }
}

/// <summary>
/// Orchestrates fast-forward and three-way merges into the current HEAD.
/// </summary>
public sealed class MergeService
{
    private readonly ObjectRepository _objects;
    private readonly RefStore _refs;
    private readonly BranchStore _branches;
    private readonly string _root;
    private readonly string _gudPath;
    private readonly MergeState _mergeState;

    public MergeService(string root, ObjectRepository objects, RefStore refs, BranchStore branches)
    {
        _root = root;
        _gudPath = Path.Combine(root, ".gud");
        _objects = objects;
        _refs = refs;
        _branches = branches;
        _mergeState = new MergeState(_gudPath);
    }

    public MergeResult Merge(string target, string? message = null, string? author = null)
    {
        if (_mergeState.IsInProgress)
        {
            return Fail("A merge is already in progress. Resolve conflicts and commit, or run 'gud merge --abort'.");
        }

        var head = _refs.GetHead();
        if (string.IsNullOrEmpty(head))
            return Fail("No commits yet on current branch; nothing to merge into.");

        var targetCommit = _branches.ResolveTarget(target);
        if (string.IsNullOrEmpty(targetCommit))
            return Fail($"'{target}' is not a valid branch or commit.");

        var builder = new CommitBuilder(_objects);
        if (builder.HasUncommittedChanges(_root, head))
            return Fail("You have uncommitted changes. Please commit them before merging.");

        if (head == targetCommit || CommitGraph.IsAncestor(_objects, targetCommit, head))
        {
            return new MergeResult
            {
                Outcome = MergeOutcome.AlreadyUpToDate,
                Message = "Already up to date."
            };
        }

        var mergeMessage = string.IsNullOrWhiteSpace(message)
            ? DefaultMergeMessage(target, targetCommit)
            : message!;

        // Fast-forward
        if (CommitGraph.IsAncestor(_objects, head, targetCommit))
        {
            return FastForward(head, targetCommit);
        }

        // Clean three-way merges create a commit; author is required before we touch the tree.
        if (string.IsNullOrWhiteSpace(author))
            return Fail("Author name is required. Set it with 'gud config user.name <name>'.");

        var mergeBase = CommitGraph.FindMergeBase(_objects, head, targetCommit);
        if (mergeBase is null)
            return Fail("Refusing to merge unrelated histories.");

        var headCommit = Commit.Read(_objects, head);
        var theirsCommit = Commit.Read(_objects, targetCommit);
        var baseCommit = Commit.Read(_objects, mergeBase);

        var baseMap = WorkingTreeStatus.FlattenTree(_objects, baseCommit.TreeHash, "");
        var oursMap = WorkingTreeStatus.FlattenTree(_objects, headCommit.TreeHash, "");
        var theirsMap = WorkingTreeStatus.FlattenTree(_objects, theirsCommit.TreeHash, "");

        var treeMerge = TreeMerger.Merge(baseMap, oursMap, theirsMap);
        var conflicted = new List<string>();
        var resultMap = new Dictionary<string, string>(treeMerge.MergedPaths);
        var workingTreeContent = new Dictionary<string, byte[]>();

        // Materialize resolved paths
        foreach (var (path, blobHash) in resultMap)
        {
            var (_, content) = _objects.ReadObject(blobHash);
            workingTreeContent[path] = content;
        }

        var theirsLabel = _branches.Exists(target) ? target : targetCommit[..Math.Min(7, targetCommit.Length)];

        // Handle path-level conflicts with file-level blob merge / markers
        foreach (var conflict in treeMerge.Conflicts)
        {
            byte[]? baseContent = null;
            byte[]? oursContent = null;
            byte[]? theirsContent = null;

            if (conflict.BaseHash is not null)
            {
                var (_, c) = _objects.ReadObject(conflict.BaseHash);
                baseContent = c;
            }
            if (conflict.OursHash is not null)
            {
                var (_, c) = _objects.ReadObject(conflict.OursHash);
                oursContent = c;
            }
            if (conflict.TheirsHash is not null)
            {
                var (_, c) = _objects.ReadObject(conflict.TheirsHash);
                theirsContent = c;
            }

            // modify/delete: still conflict
            if (conflict.OursHash is null || conflict.TheirsHash is null)
            {
                conflicted.Add(conflict.Path);
                // Prefer keeping remaining side's content (or empty delete)
                if (conflict.OursHash is not null)
                    workingTreeContent[conflict.Path] = oursContent!;
                else if (conflict.TheirsHash is not null)
                    workingTreeContent[conflict.Path] = theirsContent!;
                // if both null somehow, path stays deleted
                continue;
            }

            var blobResult = BlobMerger.Merge(baseContent, oursContent, theirsContent, "HEAD", theirsLabel);
            workingTreeContent[conflict.Path] = blobResult.Content;
            if (blobResult.HadConflict)
            {
                conflicted.Add(conflict.Path);
            }
            else
            {
                // Rare: blob merger resolved what tree thought was conflict (identical after all)
                var hash = _objects.WriteObject(ObjectType.Blob, blobResult.Content);
                resultMap[conflict.Path] = hash;
            }
        }

        // Apply working tree: start from ours tree, then write all final content / deletions
        ApplyWorkingTree(headCommit.TreeHash, workingTreeContent, resultMap, conflicted);

        if (conflicted.Count > 0)
        {
            _mergeState.Write(targetCommit, mergeMessage, conflicted);
            return new MergeResult
            {
                Outcome = MergeOutcome.Conflicts,
                ConflictedPaths = conflicted,
                Message =
                    $"Automatic merge failed; fix conflicts and then commit the result.\n" +
                    $"Conflicted paths:\n{string.Join("\n", conflicted.Select(p => "  " + p))}"
            };
        }

        var treeHash = TreeBuilder.WriteTreeFromFlatMap(_objects, resultMap);
        var commitHash = WriteMergeCommit(treeHash, head, targetCommit, author, mergeMessage);
        _refs.SetHead(commitHash);

        return new MergeResult
        {
            Outcome = MergeOutcome.MergedClean,
            ResultCommitHash = commitHash,
            Message = $"Merge made by the 'ort' strategy.\n{commitHash}"
        };
    }

    public MergeResult Abort()
    {
        if (!_mergeState.IsInProgress)
            return Fail("There is no merge in progress.");

        var head = _refs.GetHead();
        if (string.IsNullOrEmpty(head))
        {
            _mergeState.Clear();
            return Fail("Cannot abort: HEAD is missing.");
        }

        var headCommit = Commit.Read(_objects, head);
        // Rebuild working tree from HEAD only (pre-merge tip was not moved)
        WorkingTreeSync.SyncWorkingTree(null, headCommit.TreeHash, _root, _objects);

        // Remove files that exist only as untracked merge leftovers: re-sync from empty old
        // Sync with old=null already writes full tree; delete extras that were conflict-only adds.
        // Walk disk and remove tracked-by-neither: simpler approach — flatten HEAD and delete files
        // not in HEAD that we may have written. For abort correctness, also remove any path that
        // was in MERGE_CONFLICTS or appeared only from theirs.
        CleanupWorkingTreeToMatch(headCommit.TreeHash);

        _mergeState.Clear();
        return new MergeResult
        {
            Outcome = MergeOutcome.Aborted,
            Message = "Merge aborted."
        };
    }

    private MergeResult FastForward(string oldHead, string targetCommit)
    {
        var oldCommit = Commit.Read(_objects, oldHead);
        var newCommit = Commit.Read(_objects, targetCommit);
        WorkingTreeSync.SyncWorkingTree(oldCommit.TreeHash, newCommit.TreeHash, _root, _objects);
        _refs.SetHead(targetCommit);
        return new MergeResult
        {
            Outcome = MergeOutcome.FastForward,
            ResultCommitHash = targetCommit,
            Message = $"Fast-forward\n{oldHead[..Math.Min(7, oldHead.Length)]}..{targetCommit[..Math.Min(7, targetCommit.Length)]}"
        };
    }

    private void ApplyWorkingTree(
        string oursTreeHash,
        Dictionary<string, byte[]> workingTreeContent,
        Dictionary<string, string> resultMap,
        List<string> conflicted)
    {
        var oursMap = WorkingTreeStatus.FlattenTree(_objects, oursTreeHash, "");

        // Paths to delete: in ours but not in final content
        foreach (var path in oursMap.Keys)
        {
            if (!workingTreeContent.ContainsKey(path))
            {
                var full = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full)) File.Delete(full);
            }
        }

        foreach (var (path, content) in workingTreeContent)
        {
            var full = Path.Combine(_root, path.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllBytes(full, content);

            // Store conflict marker blobs only on disk; clean resolved blobs already in resultMap
            if (conflicted.Contains(path) && !resultMap.ContainsKey(path))
            {
                // leave out of resultMap — commit will re-scan working tree
            }
        }

        // Prune empty directories left after deletes
        // (best-effort; not critical)
    }

    private void CleanupWorkingTreeToMatch(string treeHash)
    {
        var expected = WorkingTreeStatus.FlattenTree(_objects, treeHash, "");
        var ignore = new GudIgnoreMatcher(Path.Combine(_root, ".gudignore"));

        void Walk(string dir)
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                var relative = Path.GetRelativePath(_root, file).Replace(Path.DirectorySeparatorChar, '/');
                if (ignore.IsIgnored(relative)) continue;
                if (!expected.ContainsKey(relative))
                    File.Delete(file);
            }

            foreach (var sub in Directory.GetDirectories(dir))
            {
                if (Path.GetFileName(sub) == ".gud") continue;
                var relative = Path.GetRelativePath(_root, sub).Replace(Path.DirectorySeparatorChar, '/');
                if (ignore.IsIgnored(relative)) continue;
                Walk(sub);
                if (Directory.Exists(sub) && !Directory.EnumerateFileSystemEntries(sub).Any())
                    Directory.Delete(sub);
            }
        }

        Walk(_root);

        // Ensure expected files match blob content
        WorkingTreeSync.SyncWorkingTree(null, treeHash, _root, _objects);
    }

    private string WriteMergeCommit(
        string treeHash, string ours, string theirs, string author, string message)
    {
        var sb = new StringBuilder();
        sb.Append("tree ").Append(treeHash).Append('\n');
        sb.Append("parent ").Append(ours).Append('\n');
        sb.Append("parent ").Append(theirs).Append('\n');
        sb.Append("author ").Append(author).Append('\n');
        sb.Append("timestamp ").Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).Append('\n');
        sb.Append('\n').Append(message);
        return _objects.WriteObject(ObjectType.Commit, Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private string DefaultMergeMessage(string target, string targetCommit)
    {
        if (_branches.Exists(target))
            return $"Merge branch '{target}'";
        var shortHash = targetCommit[..Math.Min(7, targetCommit.Length)];
        return $"Merge commit '{shortHash}'";
    }

    private static MergeResult Fail(string message) => new()
    {
        Outcome = MergeOutcome.Failed,
        Message = message
    };
}
