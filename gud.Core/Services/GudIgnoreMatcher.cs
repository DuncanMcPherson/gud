using System.Text.RegularExpressions;

namespace gud.Core.Services;

public class GudIgnoreMatcher
{
    private readonly List<(string Pattern, bool Negated)> _rules = [];

    public GudIgnoreMatcher(string gudignorePath)
    {
        if (!File.Exists(gudignorePath))
            return;

        foreach (var line in File.ReadAllLines(gudignorePath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;
            
            var negated = trimmed.StartsWith('!');
            var pattern = negated ? trimmed[1..] : trimmed;
            _rules.Add((pattern, negated));
        }
    }

    public bool IsIgnored(string relativePath)
    {
        var name = Path.GetFileName(relativePath);
        var ignored = false;

        foreach (var (pattern, negated) in _rules)
        {
            if (IsMatch(name, pattern) || IsMatch(relativePath, pattern))
                ignored = !negated;
        }

        return ignored;
    }

    private static bool IsMatch(string path, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";
        
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}