using System.Text;

namespace gud.Core.Services;

public sealed class BlobMergeResult
{
    public required byte[] Content { get; init; }
    public bool HadConflict { get; init; }
    public bool IsBinary { get; init; }
}

/// <summary>
/// File-level three-way content merge. When both sides change a text file differently,
/// emits whole-file Git-style conflict markers.
/// </summary>
public static class BlobMerger
{
    /// <summary>
    /// Merges blob contents. For conflicts, writes markers with ours / theirs full content.
    /// Binary conflicts keep ours content and set <see cref="BlobMergeResult.HadConflict"/>.
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

        // Only one side differs from base — tree merger usually handles this, but be complete
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

        if (IsBinary(oursBytes) || IsBinary(theirsBytes) ||
            (baseContent is not null && IsBinary(baseContent)))
        {
            return new BlobMergeResult
            {
                Content = oursBytes,
                HadConflict = true,
                IsBinary = true
            };
        }

        var oursText = Encoding.UTF8.GetString(oursBytes);
        var theirsText = Encoding.UTF8.GetString(theirsBytes);

        // Normalize so markers are clean; preserve trailing newline if either side had one
        var marker = new StringBuilder();
        marker.Append("<<<<<<< ").Append(oursLabel).Append('\n');
        marker.Append(oursText);
        if (oursText.Length > 0 && !oursText.EndsWith('\n'))
            marker.Append('\n');
        marker.Append("=======\n");
        marker.Append(theirsText);
        if (theirsText.Length > 0 && !theirsText.EndsWith('\n'))
            marker.Append('\n');
        marker.Append(">>>>>>> ").Append(theirsLabel).Append('\n');

        return new BlobMergeResult
        {
            Content = Encoding.UTF8.GetBytes(marker.ToString()),
            HadConflict = true,
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

        // Invalid UTF-8
        try
        {
            Encoding.UTF8.GetString(content);
            // Also reject if decoder would replace — use strict
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
