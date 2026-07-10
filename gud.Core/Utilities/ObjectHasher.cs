using System.Security.Cryptography;
using System.Text;

namespace gud.Core.Utilities;

public static class ObjectHasher
{
    public static string ComputeHash(string type, byte[] content)
    {
        var header = ComputeHeader(type, content.Length);
        var combined = header.Concat(content).ToArray();
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(combined)).ToLowerInvariant();
    }

    public static byte[] ComputeHeader(string type, int length)
    {
        return Encoding.UTF8.GetBytes($"{type} {length}\0");
    }
}