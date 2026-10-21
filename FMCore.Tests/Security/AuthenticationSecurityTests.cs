using System.Net;
using System.Text;
using System.Text.Json;
using FMCore.Authentication;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.AuthenticationTestContext;

namespace FMCore.Tests.Security;

public sealed class AuthenticationSecurityTests
{
    [Fact]
    public async Task PublicMetadataSessionsAndEventsDoNotSerializeTokens()
    {
        using var context = new AuthenticationTestContext();
        var events = new List<AccountChangedEventArgs>();
        context.Manager.AccountsChanged += (_, change) => events.Add(change);
        var identity = await context.LoginAsync();
        var outputs = new[]
        {
            await File.ReadAllTextAsync(context.At("accounts.json"), Ct),
            JsonSerializer.Serialize(identity), JsonSerializer.Serialize(context.DisplayedCode),
            JsonSerializer.Serialize(events), identity.ToString(), context.DisplayedCode!.ToString()
        };
        foreach (var output in outputs)
            foreach (var secret in new[] { "fixture-refresh", "fixture-minecraft-access", "fixture-microsoft-access", "fixture-device-secret", "MOCK-CODE", "fixture-xsts-token" })
                Assert.DoesNotContain(secret, output);
        Assert.Equal("fixture-minecraft-access", identity.AccessToken); // Explicit in-memory use remains possible for Launch.
    }

    [Theory]
    [InlineData("https://evil.example.test/login")]
    [InlineData("http://microsoft.com/devicelogin")]
    [InlineData("https://microsoft.com@evil.example.test/")]
    [InlineData("https://microsoft.com:8443/devicelogin")]
    [InlineData("https://microsoft.com/devicelogin?token=fixture-secret")]
    public async Task UntrustedVerificationUrisAreNotDisplayed(string uri)
    {
        using var context = new AuthenticationTestContext();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Path.EndsWith("/devicecode", StringComparison.Ordinal)
            ? Json(new { device_code = "fixture-secret", user_code = "MOCK", verification_uri = uri, expires_in = 900, interval = 1 }) : null);
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync());
        Assert.Equal(AuthenticationErrorCode.InvalidResponse, error.Code);
        Assert.Null(context.DisplayedCode);
        Assert.DoesNotContain("fixture-secret", error.ToString());
        Assert.Single(context.Requests);
    }

    [Fact]
    public async Task RedirectResponseIsRejectedWithoutSendingCredentialsToAnotherEndpoint()
    {
        using var context = new AuthenticationTestContext();
        context.Override = (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            response.Headers.Location = new Uri("https://evil.example.test/receive");
            return Task.FromResult<HttpResponseMessage?>(response);
        };
        Assert.Equal(AuthenticationErrorCode.InvalidResponse, (await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync())).Code);
        Assert.Single(context.Requests);
        Assert.DoesNotContain(context.Requests, r => r.Host == "evil.example.test");
    }

    [Fact]
    public async Task XboxUserHashMismatchCannotReachMinecraftServices()
    {
        using var context = new AuthenticationTestContext();
        context.Override = (request, _) => Task.FromResult<HttpResponseMessage?>(request.Path == "/xsts/authorize"
            ? Json(new { Token = "fixture-xsts-token", DisplayClaims = new { xui = new[] { new { uhs = "another-user" } } } }) : null);
        Assert.Equal(AuthenticationErrorCode.IdentityMismatch, (await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync())).Code);
        Assert.DoesNotContain(context.Requests, r => r.Host == "api.minecraftservices.com");
    }

    [Fact]
    public async Task SensitiveTransportExceptionsAreSanitized()
    {
        using var context = new AuthenticationTestContext();
        context.Override = (_, _) => throw new HttpRequestException("fixture-secret-network-error");
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync());
        Assert.Equal(AuthenticationErrorCode.NetworkError, error.Code);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain("fixture-secret", error.ToString());
    }

    [Fact]
    public async Task SensitiveDisplayCallbackExceptionsAreSanitized()
    {
        using var context = new AuthenticationTestContext();
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.LoginMicrosoftAsync(Options,
            (code, _) => throw new InvalidOperationException(code.UserCode), cancellationToken: Ct));
        Assert.Equal(AuthenticationErrorCode.InvalidInput, error.Code);
        Assert.DoesNotContain("MOCK-CODE", error.ToString());
    }

    [Fact]
    public async Task SecretCredentialCannotBeSwappedBetweenMicrosoftProfiles()
    {
        using var context = new AuthenticationTestContext();
        var first = await context.LoginAsync();
        context.ProfileUuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        context.ProfileName = "OtherPlayer";
        var second = await context.LoginAsync();
        context.Tokens.Values[first.Account.Id] = context.Tokens.Values[second.Account.Id].ToArray();
        Assert.Equal(AuthenticationErrorCode.StorageCorrupted, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.RefreshAsync(first.Account.Id, cancellationToken: Ct))).Code);
    }

    [Fact]
    public async Task LinkedMetadataPathCannotWriteOutsideAccountRoot()
    {
        using var context = new AuthenticationTestContext();
        using var outside = new DownloadTestContext();
        var link = context.At("linked");
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(link, outside.Root);
        try
        {
            Assert.Equal(AuthenticationErrorCode.StorageError,
                Assert.Throws<AuthenticationException>(() => new AccountManager(link, context.Tokens)).Code);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside.Root));
        }
        finally { Directory.Delete(link); }
        await context.Manager.CreateOfflineAccountAsync("Player", Ct);
    }

    [Fact]
    public void InvalidAccountDirectoryHasASanitizedStructuredError()
    {
        var error = Assert.Throws<AuthenticationException>(() => new AccountManager("\0fixture-secret-path"));
        Assert.Equal(AuthenticationErrorCode.InvalidInput, error.Code);
        Assert.DoesNotContain("fixture-secret", error.ToString());
    }

    [Fact]
    public async Task UnexpectedTokenFieldInPublicConfigIsRejectedWithoutReflectingItsValue()
    {
        using var context = new AuthenticationTestContext();
        await File.WriteAllTextAsync(context.At("accounts.json"), """{"schemaVersion":1,"accounts":[],"defaultAccountId":null,"refreshToken":"fixture-secret-config"}""", Ct);
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.GetAccountsAsync(Ct));
        Assert.Equal(AuthenticationErrorCode.StorageCorrupted, error.Code);
        Assert.DoesNotContain("fixture-secret", error.ToString());
    }

    [Fact]
    public async Task RealDpapiStoreEncryptsPayloadAndBindsItToItsAccountId()
    {
        if (!OperatingSystem.IsWindows()) return; // Windows-specific encryption is exercised on Windows CI.
        using var directory = new DownloadTestContext();
        var store = new WindowsDpapiTokenStore(directory.Root);
        var id = Guid.NewGuid();
        var bytes = Encoding.UTF8.GetBytes("fixture-refresh-secret-encryption");
        await store.WriteAsync(id, bytes, Ct);
        var ciphertext = await File.ReadAllBytesAsync(directory.At(id.ToString("N") + ".dat"), Ct);
        Assert.DoesNotContain("fixture-refresh-secret", Encoding.UTF8.GetString(ciphertext));
        Assert.NotEqual(bytes, ciphertext);
        Assert.Equal(bytes, await new WindowsDpapiTokenStore(directory.Root).ReadAsync(id, Ct));
        var other = Guid.NewGuid();
        await File.WriteAllBytesAsync(directory.At(other.ToString("N") + ".dat"), ciphertext, Ct);
        IAccountTokenStore portableStore = store;
        var error = await Assert.ThrowsAsync<AuthenticationException>(() => portableStore.ReadAsync(other, Ct));
        Assert.Equal(AuthenticationErrorCode.StorageCorrupted, error.Code);
        await store.DeleteAsync(id, Ct);
        Assert.Null(await store.ReadAsync(id, Ct));
    }

    [Fact]
    public async Task DefaultWindowsTokenStorageKeepsMockCredentialsOutOfPublicFiles()
    {
        using var context = new AuthenticationTestContext(useDefaultStore: true);
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(AuthenticationErrorCode.StorageUnavailable, (await Assert.ThrowsAsync<AuthenticationException>(() => context.LoginAsync())).Code);
            Assert.Empty(context.Requests);
            return;
        }
        var identity = await context.LoginAsync();
        await context.Manager.RefreshAsync(identity.Account.Id, cancellationToken: Ct);
        var files = Directory.EnumerateFiles(context.Root, "*", SearchOption.AllDirectories).ToArray();
        Assert.Contains(files, path => path.EndsWith(".dat", StringComparison.Ordinal));
        foreach (var path in files)
        {
            var text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path, Ct));
            Assert.DoesNotContain("fixture-refresh", text);
            Assert.DoesNotContain("fixture-minecraft-access", text);
        }
        await context.Manager.DeleteAsync(identity.Account.Id, Ct);
        Assert.Empty(Directory.EnumerateFiles(context.At("tokens")));
    }
}
