using System.Text;
using gud.Core.Utilities;

namespace gud.Core.Services;

public sealed class BlobMergeResult
{
    public required byte[] Content { get; init; }
    public bool HadConflict { get; init; }
    public bool IsBinary { get; init; }
}

/// <summary>
/// Three-way content merge for blob bytes. Text files use line-level (diff3-style)
/// merge; binary conflicts keep ours content and set <see cref="BlobMergeResult.HadConflict"/>.
/// </summary>
public static class BlobMerger
{
    /// <summary>
    /// Merges blob contents. Text both-changed cases use line-level merge with
    /// regional conflict markers. Binary conflicts keep ours content.
    /// </summary>
    public static BlobMergeResult Merge(
        byte[]? baseContent,
        byte[]? oursContent,
        byte[]? theirsContent,
        string oursLabel = "HEAD",
        string theirsLabel = "theirs")
    {
        // Identical both sides
        if (BytesEqual(oursContent, theirsContent))
        {
            return new BlobMergeResult
            {
                Content = oursContent ?? Array.Empty<byte>(),
                HadConflict = false
            };
        }

        // Only one side differs from base
        if (BytesEqual(theirsContent, baseContent))
        {
            return new BlobMergeResult
            {
                Content = oursContent ?? Array.Empty<byte>(),
                HadConflict = false
            };
        }

        if (BytesEqual(oursContent, baseContent))
        {
            return new BlobMergeResult
            {
                Content = theirsContent ?? Array.Empty<byte>(),
                HadConflict = false
            };
        }

        var oursBytes = oursContent ?? Array.Empty<byte>();
        var theirsBytes = theirsContent ?? Array.Empty<byte>();
        var baseBytes = baseContent ?? Array.Empty<byte>();

        if (IsBinary(oursBytes) || IsBinary(theirsBytes) || IsBinary(baseBytes))
        {
            return new BlobMergeResult
            {
                Content = oursBytes,
                HadConflict = true,
                IsBinary = true
            };
        }

        var baseText = Encoding.UTF8.GetString(baseBytes);
        var oursText = Encoding.UTF8.GetString(oursBytes);
        var theirsText = Encoding.UTF8.GetString(theirsBytes);

        var lineResult = ThreeWayLineMerger.Merge(
            ThreeWayLineMerger.SplitLines(baseText),
            ThreeWayLineMerger.SplitLines(oursText),
            ThreeWayLineMerger.SplitLines(theirsText),
            oursLabel,
            theirsLabel);

        var merged = ThreeWayLineMerger.JoinLines(lineResult.Lines);
        return new BlobMergeResult
        {
            Content = Encoding.UTF8.GetBytes(merged),
            HadConflict = lineResult.HadConflict,
            IsBinary = false
        };
    }

    public static bool ContainsConflictMarkers(string text)
        => text.Contains("<<<<<<< ") && text.Contains("=======") && text.Contains(">>>>>>> ");

    public static bool ContainsConflictMarkers(byte[] content)
    {
        if (IsBinary(content)) return false;
        return ContainsConflictMarkers(Encoding.UTF8.GetString(content));
    }

    public static bool IsBinary(byte[] content)
    {
        if (content.Length == 0) return false;
        // NUL in the first 8KiB is a strong binary signal
        var scan = Math.Min(content.Length, 8192);
        for (var i = 0; i < scan; i++)
        {
            if (content[i] == 0) return true;
        }

        try
        {
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            encoding.GetString(content);
            return false;
        }
        catch (DecoderFallbackException)
        {
            return true;
        }
    }

    private static bool BytesEqual(byte[]? a, byte[]? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.AsSpan().SequenceEqual(b);
    }
}
