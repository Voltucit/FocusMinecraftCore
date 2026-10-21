using System.Diagnostics;
using System.Text;
using FMCore.Java;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.JavaTestContext;

namespace FMCore.Tests.Integration;

public sealed class JavaProcessTests
{
    [Fact]
    public async Task DrainsBothRealProcessPipesWithoutDeadlock()
    {
        var result = await JavaProcess.RunAsync(JavaProcessFixture.Start("streams"), TimeSpan.FromSeconds(10), 200_000, Ct);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(new string('O', 102400), result.StandardOutput);
        Assert.Equal(new string('E', 102400), result.StandardError);
    }

    [Fact]
    public async Task PreservesExitCodeForProbeToInterpret()
    {
        var result = await JavaProcess.RunAsync(JavaProcessFixture.Start("exit"), TimeSpan.FromSeconds(10), 1024, Ct);
        Assert.Equal(23, result.ExitCode);
        Assert.Equal("synthetic-sensitive-value", result.StandardError);
    }

    [Fact]
    public async Task PreservesUtf8CharactersInRedirectedOutput()
    {
        var start = JavaProcessFixture.Start("unicode");
        start.StandardOutputEncoding = Encoding.UTF8;
        var output = await JavaProcess.RunAsync(start, TimeSpan.FromSeconds(10), 1024, Ct);
        Assert.Equal("Java 目录/运行时", output.StandardOutput);
    }

    [Fact]
    public async Task ExcessiveOutputFailsWithBoundedCapture()
    {
        var error = await Assert.ThrowsAsync<JavaException>(() => JavaProcess.RunAsync(
            JavaProcessFixture.Start("streams"), TimeSpan.FromSeconds(10), 1024, Ct));
        Assert.Equal(JavaErrorCode.OutputLimitExceeded, error.Code);
        Assert.DoesNotContain(new string('O', 100), error.ToString());
    }

    [Fact]
    public async Task TimeoutTerminatesTheOwnedProbeProcess()
    {
        using var directory = new DownloadTestContext();
        var pidFile = directory.At("timeout.pid");
        var error = await Assert.ThrowsAsync<JavaException>(() => JavaProcess.RunAsync(
            JavaProcessFixture.Start("hang", pidFile), TimeSpan.FromSeconds(3), 1024, Ct));
        Assert.Equal(JavaErrorCode.ProbeTimeout, error.Code);
        AssertNotRunning(int.Parse(await File.ReadAllTextAsync(pidFile, Ct)));
    }

    [Fact]
    public async Task CallerCancellationTerminatesProbeAndKeepsCancellationSemantics()
    {
        using var directory = new DownloadTestContext();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        var pidFile = directory.At("cancel.pid");
        var running = JavaProcess.RunAsync(JavaProcessFixture.Start("hang", pidFile), TimeSpan.FromSeconds(15), 1024, cancellation.Token);
        try
        {
            while (!File.Exists(pidFile) || new FileInfo(pidFile).Length == 0) await Task.Delay(20, deadline.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
            AssertNotRunning(int.Parse(await File.ReadAllTextAsync(pidFile, Ct)));
        }
        finally { cancellation.Cancel(); try { await running; } catch (OperationCanceledException) { } }
    }

    [Fact]
    public async Task NonExecutableFileIsReportedWithoutLeakingNativeErrors()
    {
        using var directory = new DownloadTestContext();
        var path = Candidate(directory, "broken");
        var error = await Assert.ThrowsAsync<JavaException>(() => new JavaManager(EmptyOptions).ProbeAsync(path, Ct));
        Assert.Equal(JavaErrorCode.ExecutionFailed, error.Code);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain(path, error.ToString());
    }

    private static void AssertNotRunning(int pid)
    {
        try { using var process = Process.GetProcessById(pid); Assert.True(process.HasExited); }
        catch (ArgumentException) { }
    }
}
