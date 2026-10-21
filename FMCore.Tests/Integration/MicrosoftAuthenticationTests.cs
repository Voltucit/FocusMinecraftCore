using System.Net;
using System.Text.Json;
using FMCore.Authentication;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.AuthenticationTestContext;

namespace FMCore.Tests.Integration;

/// <summary>Protocol integration with a mock handler only. No Microsoft account or public service is used.</summary>
public sealed class MicrosoftAuthenticationTests
{
    [Fact]
    public async Task AuthenticatesTheCompleteChainWithCallerClientIdAndCorrectTokenBoundaries()
    {
        using var context = new AuthenticationTestContext();
        var progress = new List<AuthenticationProgress>();
        var identity = await context.LoginAsync(new InlineProgress<AuthenticationProgress>(progress.Add));
        Assert.True(identity.IsAuthenticated);
        Assert.Equal(AccountKind.Microsoft, identity.Account.Kind);
        Assert.True(identity.Account.HasGameEntitlement);
        Assert.Equal("MockPlayer", identity.Account.Name);
        Assert.Equal("123456781234123412341234567890ab", identity.Uuid);
        Assert.Equal("msa", identity.UserType);
        Assert.Equal("fixture-minecraft-access", identity.AccessToken);
        Assert.Equal(Options.ClientId, identity.ClientId);
        Assert.Equal("1234567890123", identity.Xuid);
        Assert.Equal("MOCK-CODE", context.DisplayedCode!.UserCode);
        Assert.Equal(new Uri("https://microsoft.com/devicelogin"), context.DisplayedCode.VerificationUri);
        Assert.Equal(Enum.GetValues<AuthenticationStage>(), progress.Select(p => p.Stage));
        var requests = context.Requests.ToArray();
        Assert.Equal(7, requests.Length);
        Assert.Contains("client_id=" + Options.ClientId, requests[0].Body);
        Assert.Contains("XboxLive.signin+offline_access", requests[0].Body);
        using var xbox = JsonDocument.Parse(requests[2].Body);
        Assert.Equal("d=fixture-microsoft-access", xbox.RootElement.GetProperty("Properties").GetProperty("RpsTicket").GetString());
        using var xsts = JsonDocument.Parse(requests[3].Body);
        Assert.Equal("RETAIL", xsts.RootElement.GetProperty("Properties").GetProperty("SandboxId").GetString());
        Assert.Equal("rp://api.minecraftservices.com/", xsts.RootElement.GetProperty("RelyingParty").GetString());
        using var minecraft = JsonDocument.Parse(requests[4].Body);
        Assert.Equal("XBL3.0 x=fixture-uhs;fixture-xsts-token", minecraft.RootElement.GetProperty("identityToken").GetString());
        Assert.All(requests.Take(5), request => Assert.Null(request.Authorization));
        Assert.All(requests.Skip(5), request => Assert.Equal("Bearer fixture-minecraft-access", request.Authorization));
        Assert.All(requests, request => Assert.DoesNotContain("password", request.Body, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PendingAndSlowDownObservePollingIntervals()
    {
        using var context = new AuthenticationTestContext();
        context.PollErrors.Enqueue("authorization_pending");
        context.PollErrors.Enqueue("slow_down");
        await context.LoginAsync();
        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6)], context.Delays);
        Assert.Equal(3, context.OAuthCalls);
    }

    [Theory]
    [InlineData("authorization_declined", AuthenticationErrorCode.AuthorizationDeclined)]
    [InlineData("access_denied", AuthenticationErrorCode.AuthorizationDeclined)]
    [InlineData("expired_token", AuthenticationErrorCode.DeviceCodeExpired)]
    [InlineData("invalid_client", AuthenticationErrorCode.ClientRejected)]
    [InlineData("invalid_scope", AuthenticationErrorCode.ClientRejected)]
    public async Task OAuthDenialsDoNotCreateAnAccount(string response, AuthenticationErrorCode expected)
    {
        using var context = new AuthenticationTestContext();
        context.PollErrors.Enqueue(response);
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync());
        Assert.Equal(expected, error.Code);
        Assert.DoesNotContain("fixture-sensitive", error.ToString());
        Assert.Empty((await context.Manager.GetAccountsAsync(Ct)).Accounts);
        Assert.Empty(context.Tokens.Values);
    }

    [Theory]
    [InlineData("/user/authenticate", AuthenticationErrorCode.XboxRejected)]
    [InlineData("/xsts/authorize", AuthenticationErrorCode.XstsRejected)]
    [InlineData("/authentication/login_with_xbox", AuthenticationErrorCode.MinecraftRejected)]
    public async Task ServiceDenialsRemainExplicitFailures(string path, AuthenticationErrorCode expected)
    {
        using var context = new AuthenticationTestContext();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Path == path
            ? Json(new { XErr = 2148916238L, Message = "fixture-secret-service-message" }, HttpStatusCode.Unauthorized) : null);
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync());
        Assert.Equal(expected, error.Code);
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Equal(2148916238L, error.XboxError);
        Assert.DoesNotContain("fixture-secret", error.ToString());
        Assert.Empty(context.Tokens.Values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrUnrelatedEntitlementsCannotBeTreatedAsOwnership(bool unrelated)
    {
        using var context = new AuthenticationTestContext();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Path == "/entitlements/license"
            ? Json(new { items = unrelated ? new[] { new { name = "unrelated_product" } } : [] }) : null);
        Assert.Equal(AuthenticationErrorCode.GameNotOwned, (await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync())).Code);
        Assert.DoesNotContain(context.Requests, r => r.Path == "/minecraft/profile");
        Assert.Empty((await context.Manager.GetAccountsAsync(Ct)).Accounts);
    }

    [Fact]
    public async Task MissingProfileIsDifferentFromMissingEntitlement()
    {
        using var context = new AuthenticationTestContext();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Path == "/minecraft/profile" ? Json(new { error = "NOT_FOUND" }, HttpStatusCode.NotFound) : null);
        Assert.Equal(AuthenticationErrorCode.ProfileMissing, (await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync())).Code);
        Assert.Empty(context.Tokens.Values);
    }

    [Fact]
    public async Task DeviceCodeExpiresDuringDisplayWithoutPolling()
    {
        using var context = new AuthenticationTestContext();
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.LoginMicrosoftAsync(Options, (_, _) =>
        { context.Clock.Advance(TimeSpan.FromMinutes(16)); return Task.CompletedTask; }, cancellationToken: Ct));
        Assert.Equal(AuthenticationErrorCode.DeviceCodeExpired, error.Code);
        Assert.Equal(0, context.OAuthCalls);
    }

    [Fact]
    public async Task CallerCanCancelWhileWaitingForAuthorization()
    {
        using var context = new AuthenticationTestContext();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        context.OnDelay = _ => cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.LoginAsync(ct: cancel.Token));
        Assert.Empty((await context.Manager.GetAccountsAsync(Ct)).Accounts);
        Assert.Empty(context.Tokens.Values);
    }

    [Fact]
    public async Task RepeatedLoginUpdatesSameMicrosoftAccountRatherThanDuplicatingIt()
    {
        using var context = new AuthenticationTestContext();
        var first = await context.LoginAsync();
        context.ProfileName = "RenamedPlayer";
        var second = await context.LoginAsync();
        Assert.Equal(first.Account.Id, second.Account.Id);
        Assert.Equal("RenamedPlayer", second.Account.Name);
        Assert.Single((await context.Manager.GetAccountsAsync(Ct)).Accounts);
    }
}
