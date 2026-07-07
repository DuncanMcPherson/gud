using System.Security.Cryptography;
using System.Text;

namespace gud.Utilities;

public static class ObjectHasher
{
    public static string ComputeHash(string type, byte[] content)
    {
        var header = Encoding.UTF8.GetBytes($"{type} {content.Length}\0");
        var combined = header.Concat(content).ToArray();
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(combined)).ToLowerInvariant();
    }
}