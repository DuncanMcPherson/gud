using System.Diagnostics.CodeAnalysis;

namespace gud.Core.Utilities;

/// <summary>
/// Provides utility methods to interact with a Gud repository structure,
/// including locating the repository root directory and validating its existence.
/// </summary>
public static class GudRepository
{
    /// <summary>
    /// Traverses the directory structure starting from the specified path to locate the root of
    /// a ".gud" repository. A ".gud" repository is identified by the presence of a folder named ".gud".
    /// </summary>
    /// <param name="startPath">The path to start searching from.</param>
    /// <returns>
    /// Returns the full path to the root of the ".gud" repository if found; otherwise, returns null.
    /// </returns>
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

    /// Throws an exception if the current directory is not within a gud repository.
    /// This method checks for the root directory of the repository starting from the
    /// current working directory. If no root directory is found, it throws an
    /// InvalidOperationException with an appropriate message.
    /// <returns>The path of the root directory of the gud repository if found.</returns>
    [ExcludeFromCodeCoverage]
    public static string RequireRoot()
    {
        var root = FindRoot(Directory.GetCurrentDirectory());
        return root ?? throw new InvalidOperationException("Not a gud repository");
    }
}