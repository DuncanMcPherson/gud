using System.Diagnostics.CodeAnalysis;

namespace gud.Utilities;

public static class GudRepository
{
    public static string? FindRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".gud")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    [ExcludeFromCodeCoverage]
    public static string RequireRoot()
    {
        var root = FindRoot(Directory.GetCurrentDirectory());
        return root ?? throw new InvalidOperationException("Not a gud repository");
    }
}