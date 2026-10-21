using System.Text.Json.Nodes;
using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.InstallationTestContext;

namespace FMCore.Tests.Security;

public sealed class InstallationSecurityTests
{
    [Theory]
    [InlineData("../escape")]
    [InlineData("/absolute")]
    [InlineData("C:/outside")]
    [InlineData("sounds/../../escape")]
    [InlineData("CON.txt")]
    [InlineData("sounds\\escape")]
    public void RejectsUnsafeAssetNames(string name)
    {
        var json = new JsonObject { ["objects"] = new JsonObject { [name] = new JsonObject { ["hash"] = new string('a', 40), ["size"] = 1 } } };
        Assert.Throws<AssetException>(() => AssetIndex.Parse(json.ToJsonString()));
    }

    [Theory]
    [InlineData("../escape.dll")]
    [InlineData("/escape.dll")]
    [InlineData("C:/escape.dll")]
    [InlineData("a/../../escape.dll")]
    [InlineData("a\\..\\escape.dll")]
    [InlineData("NUL.dll")]
    [InlineData("same.dll|stream")]
    public async Task UnsafeZipEntriesCannotEscapeOrReplaceExistingNativeDirectory(string name)
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("unsafe.jar", Zip(("good.dll", "new", null), (name, "unsafe", null)));
        Directory.CreateDirectory(context.At("natives"));
        await File.WriteAllTextAsync(context.At("natives/previous.dll"), "previous", Ct);
        var error = await Assert.ThrowsAsync<InstallationException>(() => new NativeExtractor(context.Root).ExtractAsync([archive], "natives", cancellationToken: Ct));
        Assert.Equal(InstallationErrorCode.UnsafePath, error.Code);
        Assert.Equal("previous", await File.ReadAllTextAsync(context.At("natives/previous.dll"), Ct));
        Assert.False(File.Exists(context.At("natives/good.dll")));
        context.AssertNoTemporaryFiles();
    }

    [Theory]
    [InlineData(unchecked((int)0xa1ff0000))]
    [InlineData(unchecked((int)0x21ff0000))]
    [InlineData((int)FileAttributes.ReparsePoint)]
    public async Task RejectsArchiveLinksAndSpecialFiles(int attributes)
    {
        using var context = new InstallationTestContext();
        var archive = await context.SaveArchiveAsync("link.jar", Zip(("link.dll", "target", attributes)));
        var error = await Assert.ThrowsAsync<InstallationException>(() => new NativeExtractor(context.Root).ExtractAsync([archive], "natives", cancellationToken: Ct));
        Assert.Equal(InstallationErrorCode.UnsafePath, error.Code);
        Assert.False(Directory.Exists(context.At("natives")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task RejectsSymbolicLinkInLegacyLayoutDestination()
    {
        using var context = new InstallationTestContext();
        using var outside = new DownloadTestContext();
        await context.SeedAsync(false, true);
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(context.At("resources"), outside.Root);
        try
        {
            var error = await Assert.ThrowsAsync<DownloadException>(() => context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct));
            Assert.Equal(DownloadErrorCode.UnsafePath, error.Code);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside.Root));
        }
        finally { Directory.Delete(context.At("resources")); }
    }

    [Fact]
    public async Task RejectsLinkedNativeDestinationBeforeDeletingOrReplacingAnything()
    {
        using var context = new InstallationTestContext();
        using var outside = new DownloadTestContext();
        var archive = await context.SaveArchiveAsync("native.jar", Zip(("native.dll", "safe", null)));
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(context.At("natives"), outside.Root);
        try
        {
            await Assert.ThrowsAsync<DownloadException>(() => new NativeExtractor(context.Root).ExtractAsync([archive], "natives", cancellationToken: Ct));
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside.Root));
        }
        finally { Directory.Delete(context.At("natives")); }
    }
}
