using System.Security.Cryptography;
using System.Text;

namespace FMCore.Authentication;

public static class OfflineIdentity
{
    /// <summary>Java UUID.nameUUIDFromBytes(UTF8("OfflinePlayer:" + name)); case and whitespace are not normalized.</summary>
    public static Guid CreateUuid(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        // UUIDv3 is defined by the Minecraft offline protocol; MD5 is not used to protect credentials.
        var hash = MD5.HashData(new UTF8Encoding(false, true).GetBytes("OfflinePlayer:" + name));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash, bigEndian: true);
    }

    internal static bool ValidName(string? name) => name is { Length: >= 1 and <= 16 } &&
        name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
}
