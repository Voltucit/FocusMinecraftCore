using FMCore.Java;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.JavaTestContext;

namespace FMCore.Tests.Unit;

public sealed class JavaDiscoveryTests
{
    [Fact]
    public async Task ChecksHomeThenPathThenCommonRootsAndDeduplicatesCandidates()
    {
        using var directory = new DownloadTestContext();
        var home = Candidate(directory, "home");
        var path = Candidate(directory, "path");
        var common = Candidate(directory, "common/jdk");
        Candidate(directory, "common/unrelated/deeper/jdk");
        var calls = new List<string>();
        var events = new List<JavaDetectionProgress>();
        var options = EmptyOptions with
        {
            JavaHome = directory.At("home"),
            PathDirectories = [Path.GetDirectoryName(home)!, Path.GetDirectoryName(path)!],
            SearchDirectories = [directory.At("common")]
        };
        var manager = new JavaManager(options, (executable, _) =>
        {
            calls.Add(executable);
            return Task.FromResult(Runtime(home: Path.GetDirectoryName(executable)));
        });
        var result = await manager.DetectAsync(new InlineProgress<JavaDetectionProgress>(events.Add), Ct);
        Assert.Equal([home, path, common], calls);
        Assert.Equal(3, result.Runtimes.Count);
        Assert.Empty(result.Issues);
        Assert.False(result.IsTruncated);
        Assert.Equal([0, 1, 2, 3], events.Select(e => e.Completed));
        Assert.All(events, e => Assert.Equal(3, e.Total));
    }

    [Fact]
    public async Task BadCandidateDoesNotHideOtherInstallationsAndFailuresHaveProgress()
    {
        using var directory = new DownloadTestContext();
        var bad = Candidate(directory, "bad");
        var good = Candidate(directory, "good");
        var options = EmptyOptions with { PathDirectories = [Path.GetDirectoryName(bad)!, Path.GetDirectoryName(good)!] };
        var manager = new JavaManager(options, (path, _) => path == bad
            ? throw new JavaException(JavaErrorCode.ExecutionFailed, "synthetic failure", 23) : Task.FromResult(Runtime()));
        var events = new List<JavaDetectionProgress>();
        var result = await manager.DetectAsync(new InlineProgress<JavaDetectionProgress>(events.Add), Ct);
        Assert.Single(result.Runtimes);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(JavaErrorCode.ExecutionFailed, issue.Code);
        Assert.Equal(23, issue.ExitCode);
        Assert.Equal(JavaErrorCode.ExecutionFailed, events[1].ErrorCode);
        Assert.Null(events[2].ErrorCode);
    }

    [Fact]
    public async Task MultipleShimsReportingSameHomeAndVersionProduceOneRuntime()
    {
        using var directory = new DownloadTestContext();
        var a = Candidate(directory, "a");
        var b = Candidate(directory, "b");
        var manager = new JavaManager(EmptyOptions with { PathDirectories = [Path.GetDirectoryName(a)!, Path.GetDirectoryName(b)!] },
            (_, _) => Task.FromResult(Runtime()));
        Assert.Single((await manager.DetectAsync(cancellationToken: Ct)).Runtimes);
    }

    [Fact]
    public async Task CandidateBudgetLimitsExecutionAndReportsTruncation()
    {
        using var directory = new DownloadTestContext();
        var a = Candidate(directory, "a");
        var b = Candidate(directory, "b");
        var calls = 0;
        var manager = new JavaManager(EmptyOptions with { MaxCandidates = 1, PathDirectories = [Path.GetDirectoryName(a)!, Path.GetDirectoryName(b)!] },
            (_, _) => { calls++; return Task.FromResult(Runtime()); });
        var result = await manager.DetectAsync(cancellationToken: Ct);
        Assert.Equal(1, calls);
        Assert.True(result.IsTruncated);
    }

    [Fact]
    public async Task InvalidEntriesAreReportedButMissingCommonDirectoriesAreNormal()
    {
        using var directory = new DownloadTestContext();
        var manager = new JavaManager(EmptyOptions with { JavaHome = "relative", SearchDirectories = [directory.At("not-installed")] });
        var result = await manager.DetectAsync(cancellationToken: Ct);
        Assert.Empty(result.Runtimes);
        Assert.Equal(JavaErrorCode.InvalidPath, Assert.Single(result.Issues).Code);
    }

    [Fact]
    public async Task CancellationStopsDiscoveryWithoutBecomingAJavaFailure()
    {
        using var directory = new DownloadTestContext();
        var path = Candidate(directory, "jdk");
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var manager = new JavaManager(EmptyOptions with { PathDirectories = [Path.GetDirectoryName(path)!] }, (_, token) =>
        { cancellation.Cancel(); token.ThrowIfCancellationRequested(); return Task.FromResult(Runtime()); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.DetectAsync(cancellationToken: cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.ProbeAsync(path, cancellation.Token));
    }

    [Fact]
    public async Task EmptyOverridesDoNotFallBackToMachineEnvironment()
    {
        var result = await new JavaManager(EmptyOptions).DetectAsync(cancellationToken: Ct);
        Assert.Empty(result.Runtimes);
        Assert.Empty(result.Issues);
        Assert.False(result.IsTruncated);
    }

    [Fact]
    public async Task QuotedEnvironmentPathsAndMacBundleLayoutAreSupported()
    {
        using var directory = new DownloadTestContext();
        var executable = Candidate(directory, "bundle/Contents/Home");
        var manager = new JavaManager(EmptyOptions with
        {
            JavaHome = "\"" + directory.At("bundle") + "\"",
            PathDirectories = ["\"" + Path.GetDirectoryName(executable)! + "\""]
        }, (path, _) => Task.FromResult(Runtime() with { ExecutablePath = path }));
        Assert.Equal(executable, Assert.Single((await manager.DetectAsync(cancellationToken: Ct)).Runtimes).ExecutablePath);
    }

    [Fact]
    public async Task CommonDirectoryEnumerationBudgetCountsUnrelatedEntries()
    {
        using var directory = new DownloadTestContext();
        Directory.CreateDirectory(directory.At("root/a"));
        Directory.CreateDirectory(directory.At("root/b"));
        var result = await new JavaManager(EmptyOptions with { MaxCandidates = 1, SearchDirectories = [directory.At("root")] })
            .DetectAsync(cancellationToken: Ct);
        Assert.Empty(result.Runtimes);
        Assert.True(result.IsTruncated);
    }
}
