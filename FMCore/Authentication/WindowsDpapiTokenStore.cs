using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Authentication;

/// <summary>DPAPI CurrentUser, with per-account entropy. No encryption key or plaintext token is written to disk.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsDpapiTokenStore : IAccountTokenStore
{
    private readonly string root;
    public WindowsDpapiTokenStore(string directory)
    {
        try
        {
            root = Path.GetFullPath(directory);
            DownloadPath.CheckLinks(root);
        }
        catch (ArgumentException) { throw Failure(AuthenticationErrorCode.InvalidInput); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException or NotSupportedException)
        { throw Failure(AuthenticationErrorCode.StorageError); }
    }

    public async Task<byte[]?> ReadAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var path = PathFor(accountId);
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > 128 * 1024) throw Failure(AuthenticationErrorCode.StorageCorrupted);
            var encrypted = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var payload = ProtectedData.Unprotect(encrypted, Entropy(accountId), DataProtectionScope.CurrentUser);
            if (cancellationToken.IsCancellationRequested) { CryptographicOperations.ZeroMemory(payload); cancellationToken.ThrowIfCancellationRequested(); }
            return payload;
        }
        catch (CryptographicException) { throw Failure(AuthenticationErrorCode.StorageCorrupted); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException)
        { throw Failure(AuthenticationErrorCode.StorageError); }
    }

    public async Task WriteAsync(Guid accountId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (payload.Length is < 1 or > 64 * 1024) throw Failure(AuthenticationErrorCode.StorageError);
        var plaintext = payload.ToArray();
        try
        {
            var encrypted = ProtectedData.Protect(plaintext, Entropy(accountId), DataProtectionScope.CurrentUser);
            await GameFiles.WriteAtomicAsync(PathFor(accountId), (output, ct) => output.WriteAsync(encrypted, ct).AsTask(), cancellationToken).ConfigureAwait(false);
        }
        catch (CryptographicException) { throw Failure(AuthenticationErrorCode.StorageError); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException)
        { throw Failure(AuthenticationErrorCode.StorageError); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public Task DeleteAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try { File.Delete(PathFor(accountId)); return Task.CompletedTask; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException)
        { throw Failure(AuthenticationErrorCode.StorageError); }
    }

    private string PathFor(Guid id) => id == Guid.Empty ? throw Failure(AuthenticationErrorCode.InvalidInput)
        : GameFiles.Resolve(root, id.ToString("N") + ".dat");
    private static byte[] Entropy(Guid id) => Encoding.UTF8.GetBytes("FMCore.AccountRefresh.v1:" + id.ToString("N"));
    private static AuthenticationException Failure(AuthenticationErrorCode code) => new(code, "无法安全读取、写入或解密账号令牌。");
}
