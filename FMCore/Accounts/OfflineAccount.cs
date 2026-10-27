using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FMCore.Accounts;

/// <summary>离线身份，不表示通过 Microsoft 授权或持有游戏。</summary>
public sealed record OfflineAccount(string Username, Guid Uuid):IMinecraftAccount
{
    public string AccessToken => "0";

    public string UserType => "legacy";

    public string ClientId => Guid.Empty.ToString("D");

    public string? Xuid => null;

    public static OfflineAccount Create(string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        if (!Regex.IsMatch(username, @"\A[A-Za-z0-9_]{1,16}\z", RegexOptions.CultureInvariant))
            throw new ArgumentException("离线用户名需为 1–16 个英文字母、数字或下划线。", nameof(username));
        // 与 Java UUID.nameUUIDFromBytes 一致；Guid 使用网络字节顺序。
        byte[] digest = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
        digest[6] = (byte)((digest[6] & 0x0f) | 0x30);
        digest[8] = (byte)((digest[8] & 0x3f) | 0x80);
        return new OfflineAccount(username, new Guid(digest, bigEndian: true));
    }
}
