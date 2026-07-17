namespace gud.Core.Utilities;

public static class ObjectResolver
{
    public static string ResolveHash(string gudPath, string input)
    {
        if (input.Length == 64) return input.ToLowerInvariant();
        if (input.Length < 4) throw new ArgumentException("Short hash must be at least 4 characters");

        var bucketPrefix = input[..2];
        var remainder = input[2..];
        var bucketDir = Path.Combine(gudPath, "objects", bucketPrefix);

        if (!Directory.Exists(bucketDir))
            throw new InvalidOperationException($"No object found matching '{input}'");

        var candidates = Directory.GetFiles(bucketDir)
            .Select(Path.GetFileName)
            .Where(f => f!.StartsWith(remainder, StringComparison.OrdinalIgnoreCase))
            .Select(f => bucketPrefix + f)
            .ToList();

        return candidates.Count switch
        {
            0 => throw new InvalidOperationException($"No object found matching '{input}'"),
            1 => candidates[0],
            _ => throw new InvalidOperationException($"Short hash is '{input}' ambiguous ({candidates.Count} matches)")
        };
    }

    public static int ComputeDisplayLength(string gudPath, int floor = 7)
    {
        var objectsRoot = Path.Combine(gudPath, "objects");
        if (!Directory.Exists(objectsRoot)) return floor;

        var allHashes = Directory.GetDirectories(objectsRoot)
            .SelectMany(bucket => Directory.GetFiles(bucket)
                .Select(f => Path.GetFileName(bucket) + Path.GetFileName(f)))
            .ToList();
        var length = floor;
        while (allHashes.Select(h => h[..Math.Min(length, h.Length)]).Distinct().Count() != allHashes.Count)
            length++;
        return length;
    }
}