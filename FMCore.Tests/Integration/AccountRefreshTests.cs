using System.Net;
using System.Text;
using FMCore.Authentication;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.AuthenticationTestContext;

namespace FMCore.Tests.Integration;

public sealed class AccountRefreshTests
{
    [Fact]
    public async Task RefreshesWholeChainAndRotatesOnlyTheProtectedRefreshCredential()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        context.ProfileName = "UpdatedPlayer";
        var refreshed = await context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: Ct);
        Assert.Equal(loggedIn.Account.Id, refreshed.Account.Id);
        Assert.Equal("UpdatedPlayer", refreshed.Account.Name);
        Assert.Equal(1, context.Requests.Count(r => r.Path.EndsWith("/devicecode", StringComparison.Ordinal)));
        var request = context.Requests.Single(r => r.Body.Contains("grant_type=refresh_token", StringComparison.Ordinal));
        Assert.Contains("refresh_token=fixture-refresh-1", request.Body);
        Assert.Contains("client_id=" + Options.ClientId, request.Body);
        var payload = Encoding.UTF8.GetString(context.Tokens.Values[loggedIn.Account.Id]);
        Assert.Contains("fixture-refresh-2", payload);
        Assert.DoesNotContain("fixture-minecraft-access", payload);
        Assert.DoesNotContain("fixture-microsoft-access", payload);
        Assert.DoesNotContain("fixture-xbox-token", payload);
        Assert.DoesNotContain("fixture-xsts-token", payload);
    }

    [Fact]
    public async Task UsesUnexpiredInMemorySessionButRefreshesNearExpiryOrAfterRestart()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        var count = context.Requests.Count;
        Assert.Same(loggedIn, await context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct));
        Assert.Equal(count, context.Requests.Count);
        context.Clock.Advance(TimeSpan.FromMinutes(59));
        await context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct);
        Assert.Equal(2, context.OAuthCalls);
        using var reopened = context.Reopen();
        var restored = await reopened.GetLaunchIdentityAsync(cancellationToken: Ct);
        Assert.Equal(loggedIn.Account.Uuid, restored.Account.Uuid);
        Assert.Equal(3, context.OAuthCalls);
    }

    [Fact]
    public async Task RotationSurvivesDownstreamFailureAndNoOldOrOfflineSessionIsReturned()
    {
        using var context = new AuthenticationTestContext();
        await context.Manager.CreateOfflineAccountAsync("OfflinePlayer", Ct);
        var loggedIn = await context.LoginAsync();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Path == "/xsts/authorize"
            ? Json(new { XErr = 2148916233L }, HttpStatusCode.Unauthorized) : null);
        Assert.Equal(AuthenticationErrorCode.XstsRejected, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: Ct))).Code);
        Assert.Contains("fixture-refresh-2", Encoding.UTF8.GetString(context.Tokens.Values[loggedIn.Account.Id]));
        await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct));
        Assert.Contains(context.Requests, r => r.Body.Contains("refresh_token=fixture-refresh-2", StringComparison.Ordinal));
        Assert.Equal(loggedIn.Account.Id, (await context.Manager.GetAccountsAsync(Ct)).SelectedAccountId);
        context.Override = null;
        Assert.True((await context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct)).IsAuthenticated);
    }

    [Fact]
    public async Task RevokedRefreshRequiresInteractiveLoginWithoutDowngrade()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Body.Contains("grant_type=refresh_token", StringComparison.Ordinal)
            ? Json(new { error = "invalid_grant", error_description = "fixture-secret-revoked" }, HttpStatusCode.BadRequest) : null);
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: Ct));
        Assert.Equal(AuthenticationErrorCode.ReauthenticationRequired, error.Code);
        Assert.DoesNotContain("fixture-secret", error.ToString());
        Assert.Equal(AccountKind.Microsoft, Assert.Single((await context.Manager.GetAccountsAsync(Ct)).Accounts).Kind);
    }

    [Fact]
    public async Task MissingSecretIsAnExplicitSignInRequirement()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        context.Tokens.Values.TryRemove(loggedIn.Account.Id, out _);
        Assert.Equal(AuthenticationErrorCode.ReauthenticationRequired, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: Ct))).Code);
    }

    [Fact]
    public async Task RefreshMayOmitNewRefreshTokenAndKeepThePreviousOne()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Body.Contains("grant_type=refresh_token", StringComparison.Ordinal)
            ? Json(new { token_type = "Bearer", access_token = "fixture-new-access", expires_in = 3600 }) : null);
        await context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: Ct);
        Assert.Contains("fixture-refresh-1", Encoding.UTF8.GetString(context.Tokens.Values[loggedIn.Account.Id]));
    }

    [Fact]
    public async Task CancellationDuringRotationStillSavesTheNewCredentialAndDoesNotReturnASession()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        context.Tokens.OnWrite = cancel.Cancel;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: cancel.Token));
        Assert.Contains("fixture-refresh-2", Encoding.UTF8.GetString(context.Tokens.Values[loggedIn.Account.Id]));
        Assert.Equal(1, context.Requests.Count(r => r.Path == "/user/authenticate"));
        context.Tokens.OnWrite = null;
        await context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct);
        Assert.Contains(context.Requests, r => r.Body.Contains("refresh_token=fixture-refresh-2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RefreshCannotChangeTheSavedMinecraftUuid()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        context.ProfileUuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        Assert.Equal(AuthenticationErrorCode.IdentityMismatch, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: Ct))).Code);
        Assert.Equal(loggedIn.Account.Uuid, Assert.Single((await context.Manager.GetAccountsAsync(Ct)).Accounts).Uuid);
        await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task FailedRotationStorageStopsBeforeDownstreamAuthentication()
    {
        using var context = new AuthenticationTestContext();
        var loggedIn = await context.LoginAsync();
        context.Tokens.FailWrite = true;
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.RefreshAsync(loggedIn.Account.Id, cancellationToken: Ct));
        Assert.Equal(AuthenticationErrorCode.StorageError, error.Code);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain("fixture-refresh", error.ToString());
        Assert.Equal(1, context.Requests.Count(r => r.Path == "/user/authenticate"));
    }
}
