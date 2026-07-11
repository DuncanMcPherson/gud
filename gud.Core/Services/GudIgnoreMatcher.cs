using System.Text.RegularExpressions;

namespace gud.Core.Services;

/// <summary>
/// Provides functionality to match file and directory paths against a set of rules
/// defined in a .gudignore file. Supports pattern-based matching similar to .gitignore
/// files, including negated patterns.
/// </summary>
public class GudIgnoreMatcher
{
    /// <summary>
    /// A collection of pattern rules used to determine whether specific files or directories
    /// should be ignored. Each rule consists of a pattern and a negation flag, indicating if
    /// the rule excludes or includes matching paths.
    /// </summary>
    private readonly List<(string Pattern, bool Negated)> _rules = [];

    /// <summary>
    /// Provides functionality to parse and evaluate rules defined in a `.gudignore` file.
    /// </summary>
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

    /// <summary>
    /// Determines whether a specified file or directory path should be ignored
    /// based on matching rules defined in the ignore patterns.
    /// </summary>
    /// <param name="relativePath">The relative path of the file or directory to evaluate.</param>
    /// <returns>
    /// A boolean value indicating whether the specified path is ignored.
    /// Returns true if the path matches an ignore pattern and is not negated; otherwise, false.
    /// </returns>
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

    /// <summary>
    /// Determines whether the specified path matches the given pattern.
    /// </summary>
    /// <param name="path">The path to be evaluated against the pattern.</param>
    /// <param name="pattern">The pattern to match the path against. Supports wildcard characters such as '*' and '?'.</param>
    /// <returns>
    /// True if the path matches the specified pattern, otherwise false.
    /// </returns>
    private static bool IsMatch(string path, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".") + "$";
        
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}