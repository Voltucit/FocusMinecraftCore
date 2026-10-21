using System.Text.Json;
using System.Text.Json.Serialization;
using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Authentication;

internal sealed record AccountDocument(int SchemaVersion, List<AccountInfo> Accounts, Guid? DefaultAccountId);

internal sealed class AccountPersistence
{
    private readonly string root;
    internal static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16,
        Converters = { new JsonStringEnumConverter<AccountKind>(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    internal AccountPersistence(string directory)
    {
        try
        {
            root = Path.GetFullPath(directory);
            DownloadPath.CheckLinks(root);
        }
        catch (ArgumentException) { throw new AuthenticationException(AuthenticationErrorCode.InvalidInput, "账号存储目录无效。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException or NotSupportedException)
        { throw new AuthenticationException(AuthenticationErrorCode.StorageError, "账号存储目录不可用，或存储路径不安全。"); }
    }

    internal FileStream AcquireLease()
    {
        var path = GameFiles.Resolve(root, "accounts.lock");
        GameFiles.CreateDirectory(root);
        try { return new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException e) when ((e.HResult & 0xffff) is 32 or 33 or 11)
        { throw new AuthenticationException(AuthenticationErrorCode.StoreBusy, "账号存储正在被另一个操作使用。"); }
    }

    internal async Task<AccountDocument> ReadAsync(CancellationToken ct)
    {
        var path = GameFiles.Resolve(root, "accounts.json");
        if (!File.Exists(path)) return new(1, [], null);
        if (new FileInfo(path).Length > 1024 * 1024) throw Corrupt();
        try
        {
            var document = JsonSerializer.Deserialize<AccountDocument>(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false), JsonOptions) ?? throw Corrupt();
            if (document.SchemaVersion != 1 || document.Accounts == null || document.Accounts.Count > 256 ||
                document.Accounts.Select(a => a.Id).Distinct().Count() != document.Accounts.Count ||
                document.Accounts.Select(a => (a.Kind, a.Uuid)).Distinct().Count() != document.Accounts.Count ||
                document.DefaultAccountId is Guid id && !document.Accounts.Any(a => a.Id == id)) throw Corrupt();
            foreach (var account in document.Accounts)
            {
                if (account.Id == Guid.Empty || account.Uuid == Guid.Empty || !Enum.IsDefined(account.Kind) ||
                    !OfflineIdentity.ValidName(account.Name) || account.CreatedAt == default ||
                    account.Kind == AccountKind.Offline && (account.HasGameEntitlement || account.LastVerifiedAt != null || account.Uuid != OfflineIdentity.CreateUuid(account.Name)) ||
                    account.Kind == AccountKind.Microsoft && (!account.HasGameEntitlement || account.LastVerifiedAt == null)) throw Corrupt();
            }
            return document;
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or NotSupportedException or NullReferenceException)
        { throw Corrupt(); }
    }

    internal Task WriteAsync(AccountDocument document, CancellationToken ct) => GameFiles.WriteAtomicAsync(
        GameFiles.Resolve(root, "accounts.json"), (output, token) => JsonSerializer.SerializeAsync(output, document, JsonOptions, token), ct);

    internal static AuthenticationException Corrupt() => new(AuthenticationErrorCode.StorageCorrupted, "账号存储格式无效或已损坏。");
}
