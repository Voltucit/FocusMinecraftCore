using System.Collections.Concurrent;
using FMCore.Authentication;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.LaunchTestContext;

namespace FMCore.Tests.Integration;

public sealed class GameLaunchTests
{
    [Fact]
    public async Task LocalLegacyLaunchCreatesTheNewInstanceResourceLayoutFromVerifiedObjects()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync(legacy: true, virtualAssets: true);
        var config = await context.ConfigAsync();
        await using (var first = context.Launcher(config))
        { Assert.True((await first.LaunchAsync(cancellationToken: Ct)).Success); await first.WaitForExitAsync(Ct); }
        context.Files.Requests.Clear();
        await using var other = context.Launcher(config with { GameCore = config.GameCore with { AutoInstall = false, InstanceId = "fresh" } });
        Assert.True((await other.LaunchAsync(cancellationToken: Ct)).Success);
        await other.WaitForExitAsync(Ct);
        Assert.True(File.Exists(context.Files.At("instances/fresh/resources/sounds/first.ogg")));
        Assert.Empty(context.Files.Requests);
    }
    [Fact]
    public async Task StopTerminatesTheFixtureDescendantTree()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        await using var launcher = context.Launcher(await context.ConfigAsync(), "tree");
        var childId = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        launcher.OutputReceived += (_, e) =>
        {
            if (e.Text.StartsWith("fixture-child:", StringComparison.Ordinal)) childId.TrySetResult(int.Parse(e.Text[14..], System.Globalization.CultureInfo.InvariantCulture));
        };
        Assert.True((await launcher.LaunchAsync(cancellationToken: Ct)).Success);
        var id = await childId.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct);
        using var child = System.Diagnostics.Process.GetProcessById(id);
        await launcher.StopAsync(Ct).WaitAsync(TimeSpan.FromSeconds(15), Ct);
        await child.WaitForExitAsync(Ct).WaitAsync(TimeSpan.FromSeconds(5), Ct);
        Assert.True(child.HasExited);
    }

    [Fact]
    public async Task SelectedAccountManagerCanSupplyAndRefreshAnOnlineIdentity()
    {
        using var context = new LaunchTestContext();
        using var auth = new AuthenticationTestContext();
        auth.Clock.Advance(DateTimeOffset.UtcNow - auth.Clock.GetUtcNow());
        var signedIn = await auth.LoginAsync();
        await context.SeedAsync();
        var config = (await context.ConfigAsync()) with { Account = null, Accounts = auth.Manager };
        await using var launcher = context.Launcher(config);
        Assert.True((await launcher.LaunchAsync(cancellationToken: Ct)).Success);
        await launcher.WaitForExitAsync(Ct);
        Assert.Contains("fixture-minecraft-access", context.CapturedStart!.ArgumentList);
        auth.Clock.Advance(TimeSpan.FromHours(2));
        Assert.True((await launcher.LaunchAsync(cancellationToken: Ct)).Success);
        await launcher.WaitForExitAsync(Ct);
        Assert.Equal(2, auth.OAuthCalls);
        Assert.Equal(signedIn.Account.Id, (await auth.Manager.GetAccountsAsync(Ct)).SelectedAccountId);
    }

    [Fact]
    public async Task FailedAccountRefreshDoesNotStartAnOfflineReplacement()
    {
        using var context = new LaunchTestContext();
        using var auth = new AuthenticationTestContext();
        auth.Clock.Advance(DateTimeOffset.UtcNow - auth.Clock.GetUtcNow());
        await auth.Manager.CreateOfflineAccountAsync("AvailableOffline", Ct);
        var signedIn = await auth.LoginAsync();
        auth.Clock.Advance(TimeSpan.FromHours(2));
        auth.Override = (request, _) => Task.FromResult<System.Net.Http.HttpResponseMessage?>(request.Path.EndsWith("/token", StringComparison.Ordinal)
            ? AuthenticationTestContext.Json(new { error = "invalid_grant" }, System.Net.HttpStatusCode.BadRequest) : null);
        await context.SeedAsync();
        await using var launcher = context.Launcher((await context.ConfigAsync()) with { Account = signedIn, Accounts = auth.Manager });
        var result = await launcher.LaunchAsync(cancellationToken: Ct);
        Assert.Equal(LaunchErrorCode.AuthenticationError, result.Error!.Code);
        Assert.Equal("ReauthenticationRequired", result.Error.DetailCode);
        Assert.Null(context.CapturedStart);
    }

    [Fact]
    public async Task ProcessCreationFailureIsStructuredAndReleasesTheInstanceLease()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        var config = await context.ConfigAsync();
        await using var broken = new MinecraftLauncher(config, context.Files.Downloads, context.Javas, start =>
        { start.FileName = Path.Combine(context.Root, "missing-executable"); return start; });
        var failed = await broken.LaunchAsync(cancellationToken: Ct);
        Assert.Equal(LaunchErrorCode.ProcessStartFailed, failed.Error!.Code);
        await using var usable = context.Launcher(config);
        Assert.True((await usable.LaunchAsync(cancellationToken: Ct)).Success);
        await usable.WaitForExitAsync(Ct);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InstallsStartsDrainsBothStreamsAndReportsRealChildExit(bool legacy)
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync(legacy, virtualAssets: true);
        var config = await context.ConfigAsync();
        await using var launcher = context.Launcher(config, "exit");
        var output = new ConcurrentQueue<string>();
        var errors = new ConcurrentQueue<string>();
        var states = new ConcurrentQueue<LaunchState>();
        var exited = new TaskCompletionSource<GameExitResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        launcher.OutputReceived += (_, e) => output.Enqueue(e.Text);
        launcher.ErrorReceived += (_, e) => errors.Enqueue(e.Text);
        launcher.StateChanged += (_, e) => states.Enqueue(e.State);
        launcher.Exited += (_, e) => exited.TrySetResult(e.Result);
        var progress = new ConcurrentQueue<LaunchProgress>();
        var result = await launcher.LaunchAsync(new InlineProgress<LaunchProgress>(progress.Enqueue), Ct);
        Assert.True(result.Success);
        Assert.Null(result.Error);
        var exit = await launcher.WaitForExitAsync(Ct).WaitAsync(TimeSpan.FromSeconds(15), Ct);
        Assert.Equal(23, exit.ExitCode);
        Assert.Equal(result.ProcessId, exit.ProcessId);
        Assert.Equal(23, launcher.ExitCode);
        Assert.Null(launcher.ProcessId);
        Assert.Equal(LaunchState.Exited, launcher.State);
        Assert.False(exit.StoppedByCaller);
        Assert.Equal(exit, await exited.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct));
        Assert.Contains("fixture-final-line", output);
        Assert.NotEmpty(errors);
        Assert.Equal([LaunchState.Preparing, LaunchState.Starting, LaunchState.Running, LaunchState.Exited], states);
        Assert.Equal(Enum.GetValues<LaunchStage>(), progress.Select(p => p.Stage).Distinct());
        Assert.Equal(Path.Combine(context.Root, "instances", "fixture"), context.CapturedStart!.WorkingDirectory);
        Assert.Contains(context.CapturedStart.ArgumentList, a => a.Contains("assets/virtual", StringComparison.Ordinal) || a.Contains("assets\\virtual", StringComparison.Ordinal) || !legacy);
        Assert.Empty(Directory.EnumerateDirectories(context.Files.At(".fmcore/launch")));
    }

    [Fact]
    public async Task StopTerminatesOwnedProcessAndTheSameLauncherCanBeReused()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        await using var launcher = context.Launcher(await context.ConfigAsync(), "hold");
        for (var attempt = 0; attempt < 2; attempt++)
        {
            Assert.True((await launcher.LaunchAsync(cancellationToken: Ct)).Success);
            Assert.Equal(LaunchErrorCode.AlreadyRunning, (await launcher.LaunchAsync(cancellationToken: Ct)).Error!.Code);
            await launcher.StopAsync(Ct).WaitAsync(TimeSpan.FromSeconds(15), Ct);
            Assert.True((await launcher.WaitForExitAsync(Ct)).StoppedByCaller);
            Assert.Equal(LaunchState.Exited, launcher.State);
        }
    }

    [Fact]
    public async Task SameInstanceIsLockedUntilItsProcessExits()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        var config = await context.ConfigAsync();
        await using var first = context.Launcher(config, "hold");
        await using var second = context.Launcher(config, "hold");
        Assert.True((await first.LaunchAsync(cancellationToken: Ct)).Success);
        Assert.Equal(LaunchErrorCode.InstanceBusy, (await second.LaunchAsync(cancellationToken: Ct)).Error!.Code);
        await first.StopAsync(Ct);
        Assert.True((await second.LaunchAsync(cancellationToken: Ct)).Success);
        await second.StopAsync(Ct);
    }

    [Fact]
    public async Task DifferentIsolatedInstancesCanRunConcurrentlyWithIndependentNatives()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        var config = await context.ConfigAsync();
        await using var first = context.Launcher(config, "hold");
        await using var second = context.Launcher(config with { GameCore = config.GameCore with { InstanceId = "other" } }, "hold");
        Assert.True((await first.LaunchAsync(cancellationToken: Ct)).Success);
        var firstNatives = context.CapturedStart!.ArgumentList.Single(a => a.StartsWith("-Djava.library.path=", StringComparison.Ordinal));
        Assert.True((await second.LaunchAsync(cancellationToken: Ct)).Success);
        Assert.DoesNotContain(firstNatives, context.CapturedStart!.ArgumentList);
        await first.StopAsync(Ct);
        await second.StopAsync(Ct);
    }

    [Fact]
    public async Task CancelingWaitDoesNotTerminateTheRunningGame()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        await using var launcher = context.Launcher(await context.ConfigAsync(), "hold");
        Assert.True((await launcher.LaunchAsync(cancellationToken: Ct)).Success);
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => launcher.WaitForExitAsync(canceled.Token));
        Assert.Equal(LaunchState.Running, launcher.State);
        await launcher.StopAsync(Ct);
    }

    [Fact]
    public async Task StopCanCancelPreparationBeforeAProcessExists()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        var probing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var javas = new JavaManager(JavaTestContext.EmptyOptions, async (_, ct) =>
        { probing.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, ct); return JavaTestContext.Runtime(); });
        await using var launcher = context.Launcher(await context.ConfigAsync(), javas: javas);
        var launching = launcher.LaunchAsync(cancellationToken: Ct);
        await probing.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        await launcher.StopAsync(Ct);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => launching);
        Assert.Equal(LaunchState.Canceled, launcher.State);
        Assert.Null(launcher.ProcessId);
        Assert.Null(context.CapturedStart);
    }

    [Fact]
    public async Task OfflineLaunchUsesVerifiedLocalFilesWithoutNetworkWhenAutoInstallIsDisabled()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        var config = await context.ConfigAsync();
        await using (var initial = context.Launcher(config))
        { Assert.True((await initial.LaunchAsync(cancellationToken: Ct)).Success); await initial.WaitForExitAsync(Ct); }
        context.Files.Requests.Clear();
        await using var local = context.Launcher(config with { GameCore = config.GameCore with { AutoInstall = false } });
        Assert.True((await local.LaunchAsync(cancellationToken: Ct)).Success);
        await local.WaitForExitAsync(Ct);
        Assert.Empty(context.Files.Requests);
        await File.WriteAllTextAsync(context.Files.At("versions/fixture/fixture.jar"), "broken", Ct);
        var failed = await local.LaunchAsync(cancellationToken: Ct);
        Assert.Equal(LaunchErrorCode.MissingFiles, failed.Error!.Code);
        Assert.Empty(context.Files.Requests);
    }

    [Fact]
    public async Task PublicLauncherHonorsAnInvalidExplicitJavaPathWithoutAutomaticFallback()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        await using var launcher = new MinecraftLauncher(await context.ConfigAsync());
        var failed = await launcher.LaunchAsync(cancellationToken: Ct);
        Assert.False(failed.Success);
        Assert.Equal(LaunchErrorCode.JavaError, failed.Error!.Code);
        Assert.Equal(nameof(JavaErrorCode.NotFound), failed.Error.DetailCode);
    }

    [Fact]
    public async Task LargeDualStreamOutputAndFailingSubscribersDoNotBlockExit()
    {
        using var context = new LaunchTestContext();
        await context.SeedAsync();
        await using var launcher = context.Launcher(await context.ConfigAsync(), "flood");
        var oversized = 0;
        var errors = 0;
        launcher.OutputReceived += (_, _) => throw new InvalidOperationException("host handler failure");
        launcher.OutputReceived += (_, e) => { if (e.IsTruncated) Interlocked.Increment(ref oversized); };
        launcher.ErrorReceived += (_, _) => Interlocked.Increment(ref errors);
        launcher.StateChanged += (_, _) => throw new InvalidOperationException("host failure");
        Assert.True((await launcher.LaunchAsync(cancellationToken: Ct)).Success);
        Assert.Equal(0, (await launcher.WaitForExitAsync(Ct).WaitAsync(TimeSpan.FromSeconds(15), Ct)).ExitCode);
        Assert.Equal(1, oversized);
        Assert.True(errors >= 80);
    }
}
