namespace FMCore.Accounts;

public sealed record YggdrasilProfile(Guid Uuid, string Name);

public sealed class YggdrasilAccount : IMinecraftAccount
{
    public Uri ApiRoot { get; }
    public string LoginName { get; }
    public string Username { get; }
    public Guid Uuid { get; }
    public string AccessToken { get; }
    public string ClientToken { get; }

    public string UserType => "mojang";
    public string ClientId => Guid.Empty.ToString("D");
    public string? Xuid => null;

    internal YggdrasilAccount(
        Uri apiRoot,
        string loginName,
        YggdrasilProfile profile,
        string accessToken,
        string clientToken)
    {
        ApiRoot = apiRoot;
        LoginName = loginName;
        Username = profile.Name;
        Uuid = profile.Uuid;
        AccessToken = accessToken;
        ClientToken = clientToken;
    }

    public override string ToString()
        => $"{Username} ({Uuid:N})";
}
