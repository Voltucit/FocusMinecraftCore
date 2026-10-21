using System.Net;
using FMCore.Authentication;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.AuthenticationTestContext;

namespace FMCore.Tests.Integration;

public sealed class AuthenticationFailureTests
{
    [Theory]
    [InlineData("/consumers/oauth2/v2.0/devicecode")]
    [InlineData("/consumers/oauth2/v2.0/token")]
    [InlineData("/user/authenticate")]
    [InlineData("/xsts/authorize")]
    [InlineData("/authentication/login_with_xbox")]
    [InlineData("/entitlements/license")]
    [InlineData("/minecraft/profile")]
    public async Task MalformedResponsesAtEveryStageHaveStructuredErrors(string path)
    {
        using var context = new AuthenticationTestContext();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Path == path ? Json(new { unexpected = "fixture-sensitive-body" }) : null);
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync());
        Assert.Equal(AuthenticationErrorCode.InvalidResponse, error.Code);
        Assert.DoesNotContain("fixture-sensitive", error.ToString());
        Assert.Empty((await context.Manager.GetAccountsAsync(Ct)).Accounts);
    }

    [Fact]
    public async Task NetworkTimeoutIsDifferentFromCallerCancellation()
    {
        using var context = new AuthenticationTestContext();
        context.Override = async (_, ct) => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return null; };
        var options = Options with { RequestTimeout = TimeSpan.FromMilliseconds(30) };
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.LoginMicrosoftAsync(options,
            (_, _) => Task.CompletedTask, cancellationToken: Ct));
        Assert.Equal(AuthenticationErrorCode.RequestTimeout, error.Code);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.LoginAsync(ct: cancel.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("YOUR_CLIENT_ID")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task InvalidClientIdsNeverCauseNetworkRequests(string clientId)
    {
        using var context = new AuthenticationTestContext();
        Assert.Equal(AuthenticationErrorCode.InvalidInput, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.LoginMicrosoftAsync(new(clientId), (_, _) => Task.CompletedTask, cancellationToken: Ct))).Code);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task NonJsonHttpFailureDoesNotExposeHtmlOrServerText()
    {
        using var context = new AuthenticationTestContext();
        context.Override = (_, _) => Task.FromResult<HttpResponseMessage?>(new(HttpStatusCode.ServiceUnavailable)
        { Content = new StringContent("<html>fixture-sensitive-server-error</html>") });
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.StatusCode);
        Assert.DoesNotContain("fixture-sensitive", error.ToString());
    }

    [Fact]
    public async Task TokenThatExpiresBeforeProfileReturnsIsNotExposedAsLaunchable()
    {
        using var context = new AuthenticationTestContext();
        context.Override = (request, _) =>
        {
            if (request.Path == "/minecraft/profile") context.Clock.Advance(TimeSpan.FromHours(2));
            return Task.FromResult<HttpResponseMessage?>(null);
        };
        Assert.Equal(AuthenticationErrorCode.ReauthenticationRequired, (await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync())).Code);
        Assert.Empty(context.Tokens.Values);
    }
}
