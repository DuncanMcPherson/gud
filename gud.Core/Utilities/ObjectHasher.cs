using System.Security.Cryptography;
using System.Text;

namespace gud.Core.Utilities;

/// <summary>
/// Provides utility methods for calculating SHA-256 hashes of objects.
/// </summary>
public static class ObjectHasher
{
    /// <summary>
    /// Computes the SHA-256 hash of a content block by combining a header with the content.
    /// The header is based on the provided type and content length.
    /// </summary>
    /// <param name="type">The type identifier for the content, used to generate the header.</param>
    /// <param name="content">The byte array of content to be hashed.</param>
    /// <returns>A lowercase hexadecimal string representation of the computed SHA-256 hash.</returns>
    public static string ComputeHash(string type, byte[] content)
    {
        var header = ComputeHeader(type, content.Length);
        var combined = header.Concat(content).ToArray();
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(combined)).ToLowerInvariant();
    }

    /// <summary>
    /// Constructs a header in the form of a UTF-8 encoded byte array using the specified type and length.
    /// </summary>
    /// <param name="type">The type of the content, represented as a string.</param>
    /// <param name="length">The length of the content, represented as an integer.</param>
    /// <returns>A byte array representing the computed header.</returns>
    public static byte[] ComputeHeader(string type, int length)
    {
        return Encoding.UTF8.GetBytes($"{type} {length}\0");
    }
}