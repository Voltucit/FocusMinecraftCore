using System.Diagnostics;
using System.Net;
using FMCore.Downloader;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.DownloadTestContext;

namespace FMCore.Tests.Security;

public sealed class DownloadSecurityTests
{
    [Theory]
    [InlineData("../outside")]
    [InlineData("a/../../outside")]
    [InlineData("a\\..\\outside")]
    [InlineData("/rooted")]
    [InlineData("C:\\outside")]
    [InlineData("\\\\server\\share\\file")]
    [InlineData("file:stream")]
    [InlineData("CON.txt")]
    [InlineData("LPT1.log")]
    [InlineData("a//file")]
    [InlineData("a/./file")]
    [InlineData("trailing. /file")]
    [InlineData("trailing./file")]
    public async Task RejectsUnsafeDestinationsBeforeNetworkAccess(string relativePath)
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) => throw new InvalidOperationException("No request expected."));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root).DownloadAsync(Request(relativePath), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.UnsafePath, error.Code);
        Assert.Equal(0, handler.Calls);
        Assert.Empty(Directory.EnumerateFileSystemEntries(context.Root));
    }

    [Theory]
    [InlineData("http://example.test/file")]
    [InlineData("file:///C:/file")]
    [InlineData("https://user:secret@example.test/file")]
    [InlineData("relative")]
    public async Task RejectsInsecureNonLoopbackAndCredentialBearingUrls(string source)
    {
        using var context = new DownloadTestContext();
        using var http = new HttpClient(new DelegateHandler((_, _) => throw new InvalidOperationException("No request expected.")));
        var request = Request() with { Source = new(source, UriKind.RelativeOrAbsolute) };
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root).DownloadAsync(request, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.InvalidRequest, error.Code);
        Assert.DoesNotContain("secret", error.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DownloadHashAlgorithm.Sha1, "abc")]
    [InlineData(DownloadHashAlgorithm.Sha256, "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData((DownloadHashAlgorithm)99, null)]
    public async Task RejectsInvalidHashMetadata(DownloadHashAlgorithm algorithm, string? hash)
    {
        using var context = new DownloadTestContext();
        using var http = new HttpClient(new DelegateHandler((_, _) => throw new InvalidOperationException("No request expected.")));
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root).DownloadAsync(Request() with { HashAlgorithm = algorithm, ExpectedHash = hash }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.InvalidRequest, error.Code);
    }

    [Fact]
    public async Task DoesNotExposeSignedUrlsOrUnderlyingHttpExceptionMessages()
    {
        using var context = new DownloadTestContext();
        const string secret = "private-access-token";
        var request = Request() with { Source = new("https://downloads.example.test/file?token=" + secret) };
        using var http = new HttpClient(new DelegateHandler((_, _) => throw new HttpRequestException("Failed URL: " + request.Source)));
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root, FastOptions).DownloadAsync(request, cancellationToken: TestContext.Current.CancellationToken));
        Assert.DoesNotContain(secret, request.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(secret, error.ToString(), StringComparison.Ordinal);
        Assert.Null(error.InnerException);
    }

    [Fact]
    public async Task RejectsAnInsecureRedirectDestination()
    {
        using var context = new DownloadTestContext();
        using var http = new HttpClient(new DelegateHandler((_, _) =>
        {
            var response = Response("ok");
            response.RequestMessage = new(HttpMethod.Get, "http://external.example.test/file");
            return Task.FromResult(response);
        }));
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root, FastOptions).DownloadAsync(Request(), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.InvalidRequest, error.Code);
    }

    [Fact]
    public async Task RefusesDirectoryLinksOutsideTheRoot()
    {
        using var context = new DownloadTestContext();
        using var outside = new DownloadTestContext();
        var link = context.At("link");
        CreateDirectoryLinkOrSkip(link, outside.Root);
        try
        {
            using var http = new HttpClient(new DelegateHandler((_, _) => throw new InvalidOperationException("No request expected.")));
            var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root).DownloadAsync(Request("link/outside"), cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(DownloadErrorCode.UnsafePath, error.Code);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside.Root));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public void RefusesALinkedRootDirectory()
    {
        using var context = new DownloadTestContext();
        using var outside = new DownloadTestContext();
        var link = context.At("root-link");
        CreateDirectoryLinkOrSkip(link, outside.Root);
        try
        {
            using var http = new HttpClient();
            var error = Assert.Throws<DownloadException>(() => new DownloadManager(http, link));
            Assert.Equal(DownloadErrorCode.UnsafePath, error.Code);
        }
        finally { Directory.Delete(link); }
    }

    internal static void CreateDirectoryLinkOrSkip(string link, string target)
    {
        try { Directory.CreateSymbolicLink(link, target); }
        catch (Exception error) when (error is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            // Windows junctions exercise the same reparse-point boundary without requiring symlink privileges.
            if (!OperatingSystem.IsWindows() || (link + target).IndexOfAny(['&', '|', '<', '>', '^', '%', '!', '"', '\r', '\n']) >= 0)
                Assert.Skip("The current environment cannot create directory links.");
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"))
            {
                Arguments = $"/d /v:off /c mklink /J \"{link}\" \"{target}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(start)!;
            if (!process.WaitForExit(5000)) { process.Kill(true); Assert.Skip("Creating a test junction timed out."); }
            if (process.ExitCode != 0) Assert.Skip("The current environment cannot create test junctions.");
        }
    }
}
