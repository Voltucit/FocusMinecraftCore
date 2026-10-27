namespace FMCore.Accounts;

public sealed record MicrosoftLoginCode(
    string UserCode,
    Uri VerificationUri,
    DateTimeOffset ExpiresAt,
    string Message);

public sealed class MicrosoftAccount : IMinecraftAccount
{
    public string Username { get; }

    public Guid Uuid { get; }

    public string AccessToken { get; }

    public string UserType => "msa";

    public string ClientId { get; }

    public string? Xuid { get; }

    public DateTimeOffset ExpiresAt { get; }

    internal MicrosoftAccount(
        string username,
        Guid uuid,
        string accessToken,
        string clientId,
        string? xuid,
        DateTimeOffset expiresAt)
    {
        Username = username;
        Uuid = uuid;
        AccessToken = accessToken;
        ClientId = clientId;
        Xuid = xuid;
        ExpiresAt = expiresAt;
    }

    public override string ToString()
    {
        return $"{Username} ({Uuid:N})";
    }
}
