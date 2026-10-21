using System.Security.Cryptography;
using System.Text.Json;
using FMCore.Downloader;

namespace FMCore.Authentication;

/// <summary>UI-independent account operations. Switching is for this manager; the persisted default is used by a new manager.</summary>
public sealed class AccountManager : IDisposable
{
    private readonly AccountPersistence persistence;
    private readonly IAccountTokenStore? tokens;
    private readonly HttpClient http;
    private readonly bool ownsHttp;
    private readonly MicrosoftAuthentication microsoft;
    private readonly TimeProvider clock;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, LaunchIdentity> sessions = [];
    private Guid? selected;

    /// <summary>Metadata-only notifications after committed account mutations. UI subscribers marshal updates themselves.</summary>
    public event EventHandler<AccountChangedEventArgs>? AccountsChanged;

    /// <summary>Opens a per-user account directory. Windows defaults to DPAPI; other platforms require a secure token store for online login.</summary>
    public AccountManager(string directory, IAccountTokenStore? tokenStore = null)
        : this(directory, tokenStore, null, TimeProvider.System, null) { }

    internal AccountManager(string directory, IAccountTokenStore? tokenStore, HttpClient? httpClient, TimeProvider clock,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        persistence = new(directory);
        tokens = tokenStore;
        if (tokens == null && OperatingSystem.IsWindows()) tokens = new WindowsDpapiTokenStore(Path.Combine(Path.GetFullPath(directory), "tokens"));
        http = httpClient ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false })
        { Timeout = Timeout.InfiniteTimeSpan };
        ownsHttp = httpClient == null;
        this.clock = clock;
        microsoft = new(http, clock, delay);
    }

    /// <summary>Reads local account metadata, current selection and persisted default without network requests.</summary>
    public Task<AccountSnapshot> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        RunAsync(document => Task.FromResult(Snapshot(document)), cancellationToken);

    /// <summary>Creates/selects an offline account and returns its launch identity. Performs no network requests.</summary>
    public async Task<LaunchIdentity> LoginOfflineAsync(string name, CancellationToken cancellationToken = default)
    {
        var account = await CreateOfflineAccountAsync(name, cancellationToken).ConfigureAwait(false);
        return await GetLaunchIdentityAsync(account.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates/selects a stable OfflinePlayer UUID account without network access; exact repeated names reuse an existing account.</summary>
    public async Task<AccountInfo> CreateOfflineAccountAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!OfflineIdentity.ValidName(name)) throw new AuthenticationException(AuthenticationErrorCode.InvalidInput, "离线昵称须为 1–16 位英文字母、数字或下划线，且不含空格。");
        var changed = await RunAsync(async document =>
        {
            var uuid = OfflineIdentity.CreateUuid(name);
            var existing = document.Accounts.FirstOrDefault(a => a.Kind == AccountKind.Offline && a.Uuid == uuid);
            var account = existing ?? new(Guid.NewGuid(), AccountKind.Offline, name, uuid, false, clock.GetUtcNow(), null);
            if (existing == null)
            {
                CheckCapacity(document);
                document.Accounts.Add(account);
                document = document with { DefaultAccountId = document.DefaultAccountId ?? account.Id };
                await persistence.WriteAsync(document, cancellationToken).ConfigureAwait(false);
            }
            selected = account.Id;
            return (Account: account, Snapshot: Snapshot(document), Change: existing == null ? AccountChangeKind.Added : AccountChangeKind.Selected);
        }, cancellationToken).ConfigureAwait(false);
        Notify(changed.Change, changed.Snapshot);
        return changed.Account;
    }

    /// <summary>Authenticates with the caller's Client ID through device authorization, Xbox/XSTS and Minecraft. Display the short code, then return from the callback promptly.</summary>
    public async Task<LaunchIdentity> LoginMicrosoftAsync(MicrosoftLoginOptions options,
        Func<DeviceCodeInfo, CancellationToken, Task> showDeviceCode, IProgress<AuthenticationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(showDeviceCode);
        options.Validate();
        RequireTokenStore();
        var authenticated = await MicrosoftAuthentication.ParseSafelyAsync(() =>
            microsoft.LoginAsync(options, showDeviceCode, progress, cancellationToken)).ConfigureAwait(false);
        var changed = await RunAsync(async document =>
        {
            var old = document.Accounts.FirstOrDefault(a => a.Kind == AccountKind.Microsoft && a.Uuid == authenticated.Uuid);
            if (old == null) CheckCapacity(document);
            var account = new AccountInfo(old?.Id ?? Guid.NewGuid(), AccountKind.Microsoft, authenticated.Name, authenticated.Uuid, true,
                old?.CreatedAt ?? clock.GetUtcNow(), clock.GetUtcNow());
            await WriteCredentialAsync(account.Id, new(1, account.Uuid, options.ClientId, authenticated.RefreshToken), cancellationToken).ConfigureAwait(false);
            try
            {
                if (old == null) document.Accounts.Add(account);
                else document.Accounts[document.Accounts.IndexOf(old)] = account;
                document = document with { DefaultAccountId = document.DefaultAccountId ?? account.Id };
                await persistence.WriteAsync(document, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (old == null) await DeleteCredentialAsync(account.Id, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            selected = account.Id;
            var identity = Identity(account, authenticated, options.ClientId);
            sessions[account.Id] = identity;
            return (Identity: identity, Snapshot: Snapshot(document), Change: old == null ? AccountChangeKind.Added : AccountChangeKind.Updated);
        }, cancellationToken).ConfigureAwait(false);
        Notify(changed.Change, changed.Snapshot);
        progress?.Report(new(AuthenticationStage.Completed));
        return changed.Identity;
    }

    /// <summary>Selects a local account for this manager without checking or refreshing its credentials.</summary>
    public async Task<AccountInfo> SwitchAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var changed = await RunAsync(document =>
        {
            var account = Find(document, accountId);
            selected = accountId;
            return Task.FromResult((Account: account, Snapshot: Snapshot(document)));
        }, cancellationToken).ConfigureAwait(false);
        Notify(AccountChangeKind.Selected, changed.Snapshot);
        return changed.Account;
    }

    /// <summary>Persists the account used by a newly opened manager; null clears the default without changing an explicit current selection.</summary>
    public async Task SetDefaultAsync(Guid? accountId, CancellationToken cancellationToken = default)
    {
        var snapshot = await RunAsync(async document =>
        {
            if (accountId is Guid id) Find(document, id);
            document = document with { DefaultAccountId = accountId };
            await persistence.WriteAsync(document, cancellationToken).ConfigureAwait(false);
            return Snapshot(document);
        }, cancellationToken).ConfigureAwait(false);
        Notify(AccountChangeKind.DefaultChanged, snapshot);
    }

    /// <summary>Deletes local metadata and protected credentials, returning false for a missing account. Does not revoke server-side consent.</summary>
    public async Task<bool> DeleteAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var changed = await RunAsync(async document =>
        {
            var account = document.Accounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null) return (Removed: false, Snapshot: Snapshot(document));
            // A failed secret deletion keeps the metadata, allowing the caller to retry the same account ID.
            if (account.Kind == AccountKind.Microsoft) await DeleteCredentialAsync(accountId, cancellationToken).ConfigureAwait(false);
            document.Accounts.Remove(account);
            if (document.DefaultAccountId == accountId) document = document with { DefaultAccountId = document.Accounts.FirstOrDefault()?.Id };
            await persistence.WriteAsync(document, cancellationToken).ConfigureAwait(false);
            sessions.Remove(accountId);
            if (selected == accountId) selected = null;
            return (Removed: true, Snapshot: Snapshot(document));
        }, cancellationToken).ConfigureAwait(false);
        if (changed.Removed) Notify(AccountChangeKind.Removed, changed.Snapshot);
        return changed.Removed;
    }

    /// <summary>Forces full online refresh and entitlement/profile checks, or returns the unchanged offline identity. Never downgrades authentication failures.</summary>
    public Task<LaunchIdentity> RefreshAsync(Guid accountId, IProgress<AuthenticationProgress>? progress = null,
        CancellationToken cancellationToken = default) => GetIdentityAsync(accountId, forceRefresh: true, progress, cancellationToken);

    /// <summary>Returns the specified or selected account's credentials, refreshing online sessions near expiry. Secrets remain in memory.</summary>
    public Task<LaunchIdentity> GetLaunchIdentityAsync(Guid? accountId = null, IProgress<AuthenticationProgress>? progress = null,
        CancellationToken cancellationToken = default) => GetIdentityAsync(accountId, forceRefresh: false, progress, cancellationToken);

    private async Task<LaunchIdentity> GetIdentityAsync(Guid? accountId, bool forceRefresh, IProgress<AuthenticationProgress>? progress, CancellationToken ct)
    {
        var result = await RunAsync(async document =>
        {
            var id = accountId ?? Snapshot(document).SelectedAccountId ?? throw NotFound();
            var account = Find(document, id);
            if (account.Kind == AccountKind.Offline) return (Identity: new LaunchIdentity(account, "0", null, null, null), Changed: (AccountSnapshot?)null);
            if (!forceRefresh && sessions.TryGetValue(id, out var cached) && cached.ExpiresAt > clock.GetUtcNow().AddSeconds(60))
                return (Identity: cached, Changed: (AccountSnapshot?)null);
            RequireTokenStore();
            sessions.Remove(id);
            var credential = await ReadCredentialAsync(account, ct).ConfigureAwait(false);
            var authenticated = await MicrosoftAuthentication.ParseSafelyAsync(() => microsoft.RefreshAsync(credential, async rotated =>
            {
                using var commitTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await WriteCredentialAsync(id, credential with { RefreshToken = rotated }, commitTimeout.Token).ConfigureAwait(false);
            }, progress, ct)).ConfigureAwait(false);
            if (authenticated.Uuid != account.Uuid)
                throw new AuthenticationException(AuthenticationErrorCode.IdentityMismatch, "刷新后的 Minecraft 身份与保存的账号不一致。", AuthenticationStage.Profile);
            account = account with { Name = authenticated.Name, LastVerifiedAt = clock.GetUtcNow() };
            document.Accounts[document.Accounts.FindIndex(a => a.Id == id)] = account;
            await persistence.WriteAsync(document, ct).ConfigureAwait(false);
            var identity = Identity(account, authenticated, credential.ClientId);
            sessions[id] = identity;
            return (Identity: identity, Changed: (AccountSnapshot?)Snapshot(document));
        }, ct).ConfigureAwait(false);
        if (result.Changed != null) Notify(AccountChangeKind.Updated, result.Changed);
        progress?.Report(new(AuthenticationStage.Completed));
        return result.Identity;
    }

    private async Task<MicrosoftCredential> ReadCredentialAsync(AccountInfo account, CancellationToken ct)
    {
        var bytes = await SecretAsync(() => RequireTokenStore().ReadAsync(account.Id, ct), ct).ConfigureAwait(false);
        if (bytes == null) throw new AuthenticationException(AuthenticationErrorCode.ReauthenticationRequired, "缺少安全保存的刷新令牌，请重新执行 Microsoft 登录。");
        try
        {
            if (bytes.Length > 64 * 1024) throw AccountPersistence.Corrupt();
            var credential = JsonSerializer.Deserialize<MicrosoftCredential>(bytes, AccountPersistence.JsonOptions) ?? throw AccountPersistence.Corrupt();
            if (credential.SchemaVersion != 1 || credential.Uuid != account.Uuid || !Guid.TryParse(credential.ClientId, out var clientId) || clientId == Guid.Empty ||
                string.IsNullOrWhiteSpace(credential.RefreshToken) || credential.RefreshToken.Length > 32768 || credential.RefreshToken.Any(char.IsControl)) throw AccountPersistence.Corrupt();
            return credential;
        }
        catch (JsonException) { throw AccountPersistence.Corrupt(); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private async Task WriteCredentialAsync(Guid id, MicrosoftCredential credential, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(credential, AccountPersistence.JsonOptions);
        try { await SecretAsync(async () => { await RequireTokenStore().WriteAsync(id, bytes, ct).ConfigureAwait(false); return true; }, ct).ConfigureAwait(false); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private Task<bool> DeleteCredentialAsync(Guid id, CancellationToken ct) => SecretAsync(async () =>
    { await RequireTokenStore().DeleteAsync(id, ct).ConfigureAwait(false); return true; }, ct);

    private static async Task<T> SecretAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try { return await operation().ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (AuthenticationException e)
        { throw new AuthenticationException(e.Code == AuthenticationErrorCode.StorageCorrupted ? e.Code : AuthenticationErrorCode.StorageError, "安全令牌存储操作失败。"); }
        catch (Exception) { throw new AuthenticationException(AuthenticationErrorCode.StorageError, "安全令牌存储操作失败。"); }
    }

    private async Task<T> RunAsync<T>(Func<AccountDocument, Task<T>> operation, CancellationToken ct)
    {
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var lease = persistence.AcquireLease();
            return await operation(await persistence.ReadAsync(ct).ConfigureAwait(false)).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException)
        { throw new AuthenticationException(AuthenticationErrorCode.StorageError, "账号文件操作失败，或存储路径不安全。"); }
        finally { gate.Release(); }
    }

    private AccountSnapshot Snapshot(AccountDocument document)
    {
        var current = selected is Guid id && document.Accounts.Any(a => a.Id == id) ? selected : document.DefaultAccountId ?? document.Accounts.FirstOrDefault()?.Id;
        return new(Array.AsReadOnly(document.Accounts.ToArray()), current, document.DefaultAccountId);
    }
    private IAccountTokenStore RequireTokenStore() => tokens ?? throw new AuthenticationException(AuthenticationErrorCode.StorageUnavailable,
        "此平台尚未配置安全令牌存储，请提供 IAccountTokenStore 实现。");
    private static AccountInfo Find(AccountDocument document, Guid id) => document.Accounts.FirstOrDefault(a => a.Id == id) ?? throw NotFound();
    private static AuthenticationException NotFound() => new(AuthenticationErrorCode.AccountNotFound, "指定账号不存在，或尚未选择账号。");
    private static void CheckCapacity(AccountDocument document)
    { if (document.Accounts.Count >= 256) throw new AuthenticationException(AuthenticationErrorCode.InvalidInput, "最多保存 256 个账号。"); }
    private static LaunchIdentity Identity(AccountInfo account, MicrosoftAuthenticationResult result, string clientId) =>
        new(account, result.AccessToken, result.ExpiresAt, clientId, result.Xuid);

    private void Notify(AccountChangeKind change, AccountSnapshot snapshot)
    {
        if (AccountsChanged == null) return;
        var args = new AccountChangedEventArgs(change, snapshot);
        foreach (EventHandler<AccountChangedEventArgs> handler in AccountsChanged.GetInvocationList())
            try { handler(this, args); }
            catch (Exception) { /* Notification failures must not turn an already committed mutation into an authentication failure. */ }
    }

    /// <summary>Releases resources and cached sessions. Wait for ongoing calls to finish before disposal.</summary>
    public void Dispose()
    {
        sessions.Clear();
        if (ownsHttp) http.Dispose();
        gate.Dispose();
    }
}
