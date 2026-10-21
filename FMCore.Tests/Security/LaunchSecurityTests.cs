using System.Collections.Concurrent;
using System.Text.Json;
using FMCore.Launch;
using FMCore.Tests.Fixtures;
using FMCore.Tests.Unit;
using static FMCore.Tests.Fixtures.LaunchTestContext;

namespace FMCore.Tests.Security;

public sealed class LaunchSecurityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothOutputStreamsRedactActualLaunchTokenAndLegacySession(bool legacy)
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync(legacy);
        var identity = LaunchArgumentTests.MicrosoftIdentity("fixture-token+/=secret");
        var config = (await context.ConfigAsync()) with { Account = identity };
        await using var launcher = context.Launcher(config);
        var output = new ConcurrentQueue<string>();
        launcher.OutputReceived += (_, e) => output.Enqueue(e.Text);
        launcher.ErrorReceived += (_, e) => output.Enqueue(e.Text);
        var result = await launcher.LaunchAsync(cancellationToken: Ct);
        Assert.True(result.Success);
        await launcher.WaitForExitAsync(Ct);
        Assert.Contains("[REDACTED]", output);
        foreach (var text in output)
        {
            Assert.DoesNotContain(identity.AccessToken, text);
            Assert.DoesNotContain(identity.LegacySession, text);
        }
        Assert.DoesNotContain(identity.AccessToken, JsonSerializer.Serialize(config));
        Assert.DoesNotContain(identity.AccessToken, config.ToString());
        Assert.DoesNotContain(identity.AccessToken, result.ToString());
    }

    [Fact]
    public void EncodedTokensAndOverlappingSessionSecretsAreFiltered()
    {
        var identity = LaunchArgumentTests.MicrosoftIdentity("fixture+/=secret");
        var filtered = new SecretRedactor(identity).Redact($"{identity.AccessToken} {identity.LegacySession} {Uri.EscapeDataString(identity.AccessToken)}");
        Assert.Equal("[REDACTED] [REDACTED] [REDACTED]", filtered);
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("instances/../../escape")]
    public async Task WorkingDirectoryCannotEscapeTheSharedRoot(string directory)
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        var config = await context.ConfigAsync();
        await using var launcher = context.Launcher(config with { GameCore = config.GameCore with { GameDirectory = directory } });
        var result = await launcher.LaunchAsync(cancellationToken: Ct);
        Assert.Equal(LaunchErrorCode.UnsafePath, result.Error!.Code);
        Assert.Null(context.CapturedStart);
    }

    [Fact]
    public async Task ExpiredMicrosoftIdentityFailsRatherThanUsingAnAvailableOfflineAccount()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        var online = LaunchArgumentTests.MicrosoftIdentity("fixture-secret");
        var expired = new FMCore.Authentication.LaunchIdentity(online.Account, online.AccessToken, DateTimeOffset.UtcNow.AddMinutes(-1), online.ClientId, online.Xuid);
        await using var launcher = context.Launcher((await context.ConfigAsync()) with { Account = expired });
        var result = await launcher.LaunchAsync(cancellationToken: Ct);
        Assert.Equal(LaunchErrorCode.AuthenticationError, result.Error!.Code);
        Assert.Null(context.CapturedStart);
        Assert.DoesNotContain("fixture-secret", result.ToString());
    }

    [Theory]
    [InlineData(0, 2048)]
    [InlineData(4096, 2048)]
    [InlineData(512, 2000000)]
    public async Task InvalidMemoryConfigurationIsRejectedBeforeLaunching(int minimum, int maximum)
    {
        using var context = new LaunchTestContext();
        var config = await context.ConfigAsync();
        var error = Assert.Throws<LaunchException>(() => new MinecraftLauncher(config with { Java = config.Java with { MinMemoryMb = minimum, MaxMemoryMb = maximum } }));
        Assert.Equal(LaunchErrorCode.InvalidConfig, error.Code);
    }
}
