using FMCore.Authentication;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.AuthenticationTestContext;

namespace FMCore.Tests.Unit;

public sealed class OfflineAccountTests
{
    [Theory]
    [InlineData("Notch", "b50ad385-829d-3141-a216-7e7d7539ba7f")]
    [InlineData("Steve", "5627dd98-e6be-3c21-b8a8-e92344183641")]
    [InlineData("notch", "42653081-a90e-3475-b3d6-3550cdb43f8e")]
    [InlineData("玩家", "511a6918-f669-3b23-ba72-decf60a9cb59")]
    public void UsesJavaCompatibleUuidV3AndUtf8ByteOrder(string name, string uuid) =>
        Assert.Equal(Guid.Parse(uuid), OfflineIdentity.CreateUuid(name));

    [Fact]
    public async Task OfflineCreationPersistenceAndLaunchIdentityNeverUseTheNetworkOrTokenStore()
    {
        using var context = new AuthenticationTestContext();
        var account = await context.Manager.CreateOfflineAccountAsync("Steve", Ct);
        Assert.Equal(AccountKind.Offline, account.Kind);
        Assert.False(account.HasGameEntitlement);
        var identity = await context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct);
        Assert.Equal(account, identity.Account);
        Assert.Equal("0", identity.AccessToken);
        Assert.Equal("0", identity.LegacySession);
        Assert.Equal("legacy", identity.UserType);
        Assert.False(identity.IsAuthenticated);
        Assert.Equal("5627dd98e6be3c21b8a8e92344183641", identity.Uuid);
        using var reopened = new AccountManager(context.Root);
        var restored = Assert.Single((await reopened.GetAccountsAsync(Ct)).Accounts);
        Assert.Equal(account, restored);
        Assert.Empty(context.Requests);
        Assert.Empty(context.Tokens.Values);
    }

    [Theory]
    [InlineData("")]
    [InlineData("with space")]
    [InlineData(" Steve")]
    [InlineData("Steve ")]
    [InlineData("abcdefghijklmnopq")]
    [InlineData("bad/name")]
    [InlineData("bad\nname")]
    public async Task InvalidNicknameHasStructuredErrorAndIsNotSilentlyNormalized(string name)
    {
        using var context = new AuthenticationTestContext();
        Assert.Equal(AuthenticationErrorCode.InvalidInput, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.CreateOfflineAccountAsync(name, Ct))).Code);
        Assert.Empty((await context.Manager.GetAccountsAsync(Ct)).Accounts);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task RecreatingExactNicknameReusesAccountButCaseIsSignificant()
    {
        using var context = new AuthenticationTestContext();
        var first = await context.Manager.CreateOfflineAccountAsync("Steve", Ct);
        var same = await context.Manager.CreateOfflineAccountAsync("Steve", Ct);
        var different = await context.Manager.CreateOfflineAccountAsync("steve", Ct);
        Assert.Equal(first.Id, same.Id);
        Assert.NotEqual(first.Uuid, different.Uuid);
        Assert.Equal(2, (await context.Manager.GetAccountsAsync(Ct)).Accounts.Count);
        await context.Manager.RefreshAsync(first.Id, cancellationToken: Ct);
        Assert.Empty(context.Requests);
    }
}
