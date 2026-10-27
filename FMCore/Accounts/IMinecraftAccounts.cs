namespace FMCore.Accounts;

public interface IMinecraftAccount
{
    string Username { get; }

    Guid Uuid { get; }

    string AccessToken { get; }

    string UserType { get; }

    string ClientId { get; }

    string? Xuid { get; }
}
