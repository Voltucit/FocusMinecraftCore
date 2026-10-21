using FMCore.Installer;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.InstallationTestContext;

namespace FMCore.Tests.Integration;

public sealed class NativeExtractionTests
{
    [Fact]
    public async Task ChangedNativeArchiveIsRejectedBeforeExtraction()
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("native.jar", Zip(("native.dll", "good", null)));
        var changed = await File.ReadAllBytesAsync(context.At(archive.RelativePath), Ct);
        changed[^1] ^= 1;
        await File.WriteAllBytesAsync(context.At(archive.RelativePath), changed, Ct);
        Assert.Equal(InstallationErrorCode.InvalidArchive, (await Assert.ThrowsAsync<InstallationException>(() =>
            new NativeExtractor(context.Root).ExtractAsync([archive], "natives", cancellationToken: Ct))).Code);
        Assert.False(Directory.Exists(context.At("natives")));
    }

    [Fact]
    public async Task DuplicateZipEntriesAreRejectedEvenWithIdenticalContent()
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("duplicate.jar", Zip(("native.dll", "same", null), ("native.dll", "same", null)));
        Assert.Equal(InstallationErrorCode.UnsafePath, (await Assert.ThrowsAsync<InstallationException>(() =>
            new NativeExtractor(context.Root).ExtractAsync([archive], "natives", cancellationToken: Ct))).Code);
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task IgnoresConflictingJarMetadataEvenWhenOfficialExtractExcludesAreAbsent()
    {
        using var context = new InstallationTestContext();
        var first = await context.SaveArchiveAsync("first.jar", Zip(("first.dll", "first", null), ("META-INF/INDEX.LIST", "first metadata", null)));
        var second = await context.SaveArchiveAsync("second.jar", Zip(("second.dll", "second", null), ("META-INF/INDEX.LIST", "second metadata", null)));
        await new NativeExtractor(context.Root).ExtractAsync([first, second], "natives", cancellationToken: Ct);
        Assert.Equal(2, Directory.EnumerateFiles(context.At("natives")).Count());
        Assert.False(Directory.Exists(context.At("natives/META-INF")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task HonorsExcludesAndReplacesStaleFiles()
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("native.jar", Zip(("native.dll", "current", null), ("META-INF/MANIFEST.MF", "skip", null)), ["META-INF/"]);
        Directory.CreateDirectory(context.At("natives"));
        await File.WriteAllTextAsync(context.At("natives/stale.dll"), "old", Ct);
        var target = await new NativeExtractor(context.Root).ExtractAsync([archive], "natives", cancellationToken: Ct);
        Assert.Equal("current", await File.ReadAllTextAsync(Path.Combine(target, "native.dll"), Ct));
        Assert.Single(Directory.EnumerateFileSystemEntries(target));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task IdenticalSharedEntriesAreAllowedButDifferentContentsFail()
    {
        using var context = new InstallationTestContext();
        var a = await context.SaveArchiveAsync("a.jar", Zip(("shared.dll", "identical", null)));
        var b = await context.SaveArchiveAsync("b.jar", Zip(("shared.dll", "identical", null)));
        var extractor = new NativeExtractor(context.Root);
        await extractor.ExtractAsync([a, b], "natives", cancellationToken: Ct);
        b = await context.SaveArchiveAsync("b.jar", Zip(("shared.dll", "different", null)));
        var error = await Assert.ThrowsAsync<InstallationException>(() => extractor.ExtractAsync([a, b], "natives", cancellationToken: Ct));
        Assert.Equal(InstallationErrorCode.ConflictingFiles, error.Code);
        Assert.Equal("identical", await File.ReadAllTextAsync(context.At("natives/shared.dll"), Ct));
        context.AssertNoTemporaryFiles();
    }

    [Theory]
    [InlineData("entry")]
    [InlineData("total")]
    [InlineData("count")]
    public async Task LimitsArchiveExpansion(string limit)
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("large.jar", Zip(("one.dll", "12345", null), ("two.dll", "67890", null)));
        var options = limit switch
        {
            "entry" => new NativeExtractionOptions { MaxEntryBytes = 4, MaxTotalBytes = 8 },
            "total" => new NativeExtractionOptions { MaxEntryBytes = 5, MaxTotalBytes = 8 },
            _ => new NativeExtractionOptions { MaxEntries = 1 }
        };
        var error = await Assert.ThrowsAsync<InstallationException>(() => new NativeExtractor(context.Root, options).ExtractAsync([archive], "natives", cancellationToken: Ct));
        Assert.Equal(InstallationErrorCode.ArchiveLimitExceeded, error.Code);
        Assert.False(Directory.Exists(context.At("natives")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task CorruptArchiveCannotReplaceExistingDirectory()
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("broken.jar", [1, 2, 3]);
        Directory.CreateDirectory(context.At("natives"));
        await File.WriteAllTextAsync(context.At("natives/keep.dll"), "keep", Ct);
        var error = await Assert.ThrowsAsync<InstallationException>(() => new NativeExtractor(context.Root).ExtractAsync([archive], "natives", cancellationToken: Ct));
        Assert.Equal(InstallationErrorCode.InvalidArchive, error.Code);
        Assert.Equal("keep", await File.ReadAllTextAsync(context.At("natives/keep.dll"), Ct));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task CancellationDuringExtractionPreservesPreviousDirectory()
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("native.jar", Zip(("new.dll", "new", null)));
        Directory.CreateDirectory(context.At("natives"));
        await File.WriteAllTextAsync(context.At("natives/keep.dll"), "keep", Ct);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var progress = new InlineProgress<NativeExtractionProgress>(p => { if (p.CompletedArchives == 1) cancel.Cancel(); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new NativeExtractor(context.Root).ExtractAsync([archive], "natives", progress, cancel.Token));
        Assert.Equal("keep", await File.ReadAllTextAsync(context.At("natives/keep.dll"), Ct));
        Assert.False(File.Exists(context.At("natives/new.dll")));
        context.AssertNoTemporaryFiles();
    }
}
