using System.Diagnostics;
using System.Net;
using System.Text.Json.Serialization;

namespace FMCore.Authentication;

public enum AccountKind { Offline, Microsoft }
public enum AuthenticationStage { DeviceAuthorization, WaitingForAuthorization, MicrosoftToken, XboxUserToken, XstsToken, MinecraftToken, Entitlements, Profile, Completed }
public enum AccountChangeKind { Added, Updated, Selected, DefaultChanged, Removed }
public enum AuthenticationErrorCode
{
    InvalidInput, AccountNotFound, NetworkError, RequestTimeout, InvalidResponse, AuthorizationDeclined,
    DeviceCodeExpired, ClientRejected, MicrosoftRejected, XboxRejected, XstsRejected, MinecraftRejected,
    GameNotOwned, ProfileMissing, ReauthenticationRequired, IdentityMismatch,
    StorageUnavailable, StorageError, StorageCorrupted, StoreBusy
}

public sealed class AuthenticationException(AuthenticationErrorCode code, string message,
    AuthenticationStage? stage = null, HttpStatusCode? statusCode = null, long? xboxError = null) : Exception(message)
{
    public AuthenticationErrorCode Code { get; } = code;
    public AuthenticationStage? Stage { get; } = stage;
    public HttpStatusCode? StatusCode { get; } = statusCode;
    public long? XboxError { get; } = xboxError;
}

/// <summary>Public metadata only. Entitlement and verification time describe the last successful online authentication.</summary>
public sealed record AccountInfo(Guid Id, AccountKind Kind, string Name, Guid Uuid, bool HasGameEntitlement,
    DateTimeOffset CreatedAt, DateTimeOffset? LastVerifiedAt);
public sealed record AccountSnapshot(IReadOnlyList<AccountInfo> Accounts, Guid? SelectedAccountId, Guid? DefaultAccountId);
public sealed record AuthenticationProgress(AuthenticationStage Stage);
public sealed class AccountChangedEventArgs(AccountChangeKind change, AccountSnapshot snapshot) : EventArgs
{
    public AccountChangeKind Change { get; } = change;
    public AccountSnapshot Snapshot { get; } = snapshot;
}

public sealed record MicrosoftLoginOptions(string ClientId)
{
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    internal void Validate()
    {
        if (!Guid.TryParse(ClientId, out var id) || id == Guid.Empty || RequestTimeout <= TimeSpan.Zero || RequestTimeout > TimeSpan.FromMinutes(2))
            throw new AuthenticationException(AuthenticationErrorCode.InvalidInput, "请提供自己的有效应用 Client ID 和合理的请求超时。");
    }
}

[DebuggerDisplay("Microsoft device authorization challenge")]
public sealed class DeviceCodeInfo
{
    [JsonIgnore] public string UserCode { get; }
    public Uri VerificationUri { get; }
    public DateTimeOffset ExpiresAt { get; }
    internal DeviceCodeInfo(string userCode, Uri verificationUri, DateTimeOffset expiresAt)
    { UserCode = userCode; VerificationUri = verificationUri; ExpiresAt = expiresAt; }
    public override string ToString() => "Microsoft device authorization challenge";
}

/// <summary>Short-lived launch credentials. Never write this object or its token to application configuration or logs.</summary>
[DebuggerDisplay("Launch identity ({Account.Kind})")]
public sealed class LaunchIdentity
{
    public AccountInfo Account { get; }
    public string Uuid => Account.Uuid.ToString("N");
    public string UserType => Account.Kind == AccountKind.Microsoft ? "msa" : "legacy";
    public bool IsAuthenticated => Account.Kind == AccountKind.Microsoft;
    public string? ClientId { get; }
    public string? Xuid { get; }
    public DateTimeOffset? ExpiresAt { get; }
    [JsonIgnore] public string AccessToken { get; }
    [JsonIgnore] public string LegacySession => IsAuthenticated ? $"token:{AccessToken}:{Uuid}" : "0";
    internal LaunchIdentity(AccountInfo account, string token, DateTimeOffset? expiresAt, string? clientId, string? xuid)
    { Account = account; AccessToken = token; ExpiresAt = expiresAt; ClientId = clientId; Xuid = xuid; }
    public override string ToString() => $"{Account.Kind} launch identity";
}
