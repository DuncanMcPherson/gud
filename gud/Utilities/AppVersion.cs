using System.Reflection;

namespace gud.Utilities;

/// <summary>
/// Resolves the running CLI's display version from assembly metadata
/// (stamped by release CI as InformationalVersion).
/// </summary>
public static class AppVersion
{
    public static string Get()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var info = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(info))
            return StripSourceRevision(info);

        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "0.0.0";
    }

    /// <summary>
    /// SDK may append <c>+commit</c> metadata; drop it for cleaner --version output.
    /// Pre-release tags (e.g. <c>-dev.4</c>) are kept.
    /// </summary>
    private static string StripSourceRevision(string informationalVersion)
    {
        var plus = informationalVersion.IndexOf('+');
        return plus >= 0 ? informationalVersion[..plus] : informationalVersion;
    }
}
