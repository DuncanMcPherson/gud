namespace gud.Core.Utilities;

/// <summary>
/// Result of a line-level three-way merge.
/// </summary>
public sealed class LineMergeResult
{
    public required IReadOnlyList<string> Lines { get; init; }
    public bool HadConflict { get; init; }
}

/// <summary>
/// Diff3-style line merge: auto-combine non-overlapping changes from two sides
/// relative to a common base; emit regional conflict markers where they overlap.
/// </summary>
public static class ThreeWayLineMerger
{
    private readonly record struct Region(int BaseStart, int BaseCount, IReadOnlyList<string> SideLines);

    public static LineMergeResult Merge(
        string[] baseLines,
        string[] oursLines,
        string[] theirsLines,
        string oursLabel = "HEAD",
        string theirsLabel = "theirs")
    {
        if (LinesEqual(oursLines, theirsLines))
            return new LineMergeResult { Lines = oursLines, HadConflict = false };

        if (LinesEqual(theirsLines, baseLines))
            return new LineMergeResult { Lines = oursLines, HadConflict = false };

        if (LinesEqual(oursLines, baseLines))
            return new LineMergeResult { Lines = theirsLines, HadConflict = false };

        var oursEdits = MyersDiff.Compute(baseLines, oursLines);
        var theirsEdits = MyersDiff.Compute(baseLines, theirsLines);

        if (oursEdits is null || theirsEdits is null)
        {
            // Defensive fallback: whole-file conflict markers
            return WholeFileConflict(oursLines, theirsLines, oursLabel, theirsLabel);
        }

        var oursRegions = ToRegions(oursEdits);
        var theirsRegions = ToRegions(theirsEdits);

        var output = new List<string>();
        var hadConflict = false;
        var oi = 0;
        var ti = 0;
        var basePos = 0;

        while (basePos < baseLines.Length || oi < oursRegions.Count || ti < theirsRegions.Count)
        {
            var o = oi < oursRegions.Count ? oursRegions[oi] : (Region?)null;
            var t = ti < theirsRegions.Count ? theirsRegions[ti] : (Region?)null;

            var nextO = o?.BaseStart ?? int.MaxValue;
            var nextT = t?.BaseStart ?? int.MaxValue;
            var nextChange = Math.Min(nextO, nextT);

            // Stable base lines before the next change
            if (basePos < baseLines.Length && basePos < nextChange)
            {
                while (basePos < nextChange && basePos < baseLines.Length)
                    output.Add(baseLines[basePos++]);
                continue;
            }

            // Past end of base but trailing pure inserts remain
            if (basePos >= baseLines.Length)
            {
                if (o is null && t is null) break;

                if (o is not null && t is not null && o.Value.BaseStart == t.Value.BaseStart)
                {
                    if (RegionsIdentical(o.Value, t.Value))
                    {
                        output.AddRange(o.Value.SideLines);
                        oi++;
                        ti++;
                    }
                    else
                    {
                        EmitConflict(output, o.Value.SideLines, t.Value.SideLines, oursLabel, theirsLabel);
                        hadConflict = true;
                        oi++;
                        ti++;
                    }
                    continue;
                }

                if (o is not null && (t is null || o.Value.BaseStart <= t.Value.BaseStart))
                {
                    output.AddRange(o.Value.SideLines);
                    oi++;
                    continue;
                }

                output.AddRange(t!.Value.SideLines);
                ti++;
                continue;
            }

            // Change(s) at basePos (nextChange == basePos when we get here with remaining base)
            if (o is not null && t is not null && o.Value.BaseStart == t.Value.BaseStart)
            {
                // Pure insert on one side only at this index — apply insert first
                if (o.Value.BaseCount == 0 && t.Value.BaseCount > 0)
                {
                    output.AddRange(o.Value.SideLines);
                    oi++;
                    continue;
                }

                if (t.Value.BaseCount == 0 && o.Value.BaseCount > 0)
                {
                    output.AddRange(t.Value.SideLines);
                    ti++;
                    continue;
                }

                // Both pure inserts at same point
                if (o.Value.BaseCount == 0 && t.Value.BaseCount == 0)
                {
                    if (RegionsIdentical(o.Value, t.Value))
                    {
                        output.AddRange(o.Value.SideLines);
                    }
                    else
                    {
                        EmitConflict(output, o.Value.SideLines, t.Value.SideLines, oursLabel, theirsLabel);
                        hadConflict = true;
                    }

                    oi++;
                    ti++;
                    continue;
                }

                // Both modify/delete base starting at same index
                if (RegionsIdentical(o.Value, t.Value))
                {
                    output.AddRange(o.Value.SideLines);
                    basePos = o.Value.BaseStart + o.Value.BaseCount;
                    oi++;
                    ti++;
                    continue;
                }

                if (IntervalsOverlap(o.Value, t.Value))
                {
                    hadConflict |= EmitOverlapConflict(
                        output, baseLines, oursRegions, theirsRegions,
                        ref oi, ref ti, ref basePos, oursLabel, theirsLabel);
                    continue;
                }

                // Non-overlapping same start shouldn't happen if both BaseCount > 0
                // Fall through: apply shorter first by treating as conflict cluster
                hadConflict |= EmitOverlapConflict(
                    output, baseLines, oursRegions, theirsRegions,
                    ref oi, ref ti, ref basePos, oursLabel, theirsLabel);
                continue;
            }

            if (o is not null && (t is null || o.Value.BaseStart < t.Value.BaseStart))
            {
                output.AddRange(o.Value.SideLines);
                basePos = o.Value.BaseStart + o.Value.BaseCount;
                oi++;
                continue;
            }

            // Only theirs at this point
            output.AddRange(t!.Value.SideLines);
            basePos = t.Value.BaseStart + t.Value.BaseCount;
            ti++;
        }

        return new LineMergeResult { Lines = output, HadConflict = hadConflict };
    }

    /// <summary>
    /// Split text into lines the same way as <c>DiffCommand</c> (<c>Split('\n')</c>).
    /// A trailing newline yields a final empty segment so <see cref="JoinLines"/> round-trips.
    /// </summary>
    public static string[] SplitLines(string text) => text.Split('\n');

    /// <summary>
    /// Join lines with <c>\n</c> (inverse of <see cref="SplitLines"/>).
    /// </summary>
    public static string JoinLines(IReadOnlyList<string> lines) => string.Join('\n', lines);

    private static List<Region> ToRegions(List<DiffEdit> edits)
    {
        var regions = new List<Region>();
        var baseIndex = 0;
        var i = 0;
        while (i < edits.Count)
        {
            if (edits[i].Type == EditType.Equal)
            {
                baseIndex++;
                i++;
                continue;
            }

            var baseStart = baseIndex;
            var baseCount = 0;
            var side = new List<string>();
            while (i < edits.Count && edits[i].Type != EditType.Equal)
            {
                if (edits[i].Type == EditType.Delete)
                {
                    baseCount++;
                    baseIndex++;
                }
                else
                {
                    side.Add(edits[i].Line);
                }

                i++;
            }

            regions.Add(new Region(baseStart, baseCount, side));
        }

        return regions;
    }

    private static bool EmitOverlapConflict(
        List<string> output,
        string[] baseLines,
        List<Region> oursRegions,
        List<Region> theirsRegions,
        ref int oi,
        ref int ti,
        ref int basePos,
        string oursLabel,
        string theirsLabel)
    {
        var o = oursRegions[oi];
        var t = theirsRegions[ti];
        var windowStart = Math.Min(o.BaseStart, t.BaseStart);
        var windowEnd = Math.Max(o.BaseStart + o.BaseCount, t.BaseStart + t.BaseCount);

        // Expand cluster while either side has a region overlapping the window
        var changed = true;
        while (changed)
        {
            changed = false;
            for (var i = oi; i < oursRegions.Count; i++)
            {
                var r = oursRegions[i];
                if (r.BaseStart > windowEnd) break;
                if (RegionTouchesWindow(r, windowStart, windowEnd))
                {
                    var end = r.BaseStart + r.BaseCount;
                    if (end > windowEnd)
                    {
                        windowEnd = end;
                        changed = true;
                    }

                    if (r.BaseStart < windowStart)
                    {
                        windowStart = r.BaseStart;
                        changed = true;
                    }
                }
            }

            for (var i = ti; i < theirsRegions.Count; i++)
            {
                var r = theirsRegions[i];
                if (r.BaseStart > windowEnd) break;
                if (RegionTouchesWindow(r, windowStart, windowEnd))
                {
                    var end = r.BaseStart + r.BaseCount;
                    if (end > windowEnd)
                    {
                        windowEnd = end;
                        changed = true;
                    }

                    if (r.BaseStart < windowStart)
                    {
                        windowStart = r.BaseStart;
                        changed = true;
                    }
                }
            }
        }

        // Emit any stable base lines before the conflict window (should already be at windowStart)
        while (basePos < windowStart && basePos < baseLines.Length)
            output.Add(baseLines[basePos++]);

        var oursSide = new List<string>();
        var theirsSide = new List<string>();

        while (oi < oursRegions.Count && RegionTouchesWindow(oursRegions[oi], windowStart, windowEnd))
        {
            oursSide.AddRange(oursRegions[oi].SideLines);
            oi++;
        }

        while (ti < theirsRegions.Count && RegionTouchesWindow(theirsRegions[ti], windowStart, windowEnd))
        {
            theirsSide.AddRange(theirsRegions[ti].SideLines);
            ti++;
        }

        if (SideLinesEqual(oursSide, theirsSide))
        {
            output.AddRange(oursSide);
            basePos = windowEnd;
            return false;
        }

        EmitConflict(output, oursSide, theirsSide, oursLabel, theirsLabel);
        basePos = windowEnd;
        return true;
    }

    private static bool RegionTouchesWindow(Region r, int windowStart, int windowEnd)
    {
        if (r.BaseCount == 0)
            return r.BaseStart >= windowStart && r.BaseStart <= windowEnd;
        return r.BaseStart < windowEnd && r.BaseStart + r.BaseCount > windowStart;
    }

    private static bool IntervalsOverlap(Region a, Region b)
    {
        if (a.BaseCount == 0 && b.BaseCount == 0)
            return a.BaseStart == b.BaseStart;
        if (a.BaseCount == 0 || b.BaseCount == 0)
            return false;
        return a.BaseStart < b.BaseStart + b.BaseCount && b.BaseStart < a.BaseStart + a.BaseCount;
    }

    private static bool RegionsIdentical(Region a, Region b)
        => a.BaseStart == b.BaseStart
           && a.BaseCount == b.BaseCount
           && SideLinesEqual(a.SideLines, b.SideLines);

    private static bool SideLinesEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private static void EmitConflict(
        List<string> output,
        IReadOnlyList<string> oursSide,
        IReadOnlyList<string> theirsSide,
        string oursLabel,
        string theirsLabel)
    {
        output.Add($"<<<<<<< {oursLabel}");
        output.AddRange(oursSide);
        output.Add("=======");
        output.AddRange(theirsSide);
        output.Add($">>>>>>> {theirsLabel}");
    }

    private static LineMergeResult WholeFileConflict(
        string[] oursLines, string[] theirsLines, string oursLabel, string theirsLabel)
    {
        var output = new List<string>
        {
            $"<<<<<<< {oursLabel}"
        };
        output.AddRange(oursLines);
        output.Add("=======");
        output.AddRange(theirsLines);
        output.Add($">>>>>>> {theirsLabel}");
        return new LineMergeResult { Lines = output, HadConflict = true };
    }

    private static bool LinesEqual(string[] a, string[] b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
