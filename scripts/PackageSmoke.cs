using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FMCore.Authentication;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

// 此文件由 Verify-Package.ps1 复制到独立 PackageReference 调用方；不进入核心包。
var root = Path.GetFullPath(args[0]);
Directory.CreateDirectory(root);
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
var ct = timeout.Token;
var payload = Encoding.UTF8.GetBytes("FMCore package consumer fixture");
var index = Encoding.UTF8.GetBytes("{\"objects\":{}}");
using var handler = new FixtureHandler(payload, index);
using var http = new HttpClient(handler);
var downloads = new DownloadManager(http, root);
var request = new DownloadRequest(new("https://fixture.invalid/file"), "downloads/one.bin")
{
    ExpectedHash = Convert.ToHexString(SHA256.HashData(payload)),
    HashAlgorithm = DownloadHashAlgorithm.Sha256,
    ExpectedSize = payload.Length
};
await downloads.DownloadAsync(request, cancellationToken: ct);
await downloads.DownloadManyAsync([request with { RelativePath = "downloads/two.bin" },
    request with { RelativePath = "downloads/three.bin" }], cancellationToken: ct);
Require((await downloads.VerifyAsync(request, ct)).State == FileVerificationState.Valid, "download hash");

var versions = new VersionManager(downloads);
var metadata = VersionParser.Parse(JsonSerializer.Serialize(new
{
    id = "package-fixture", type = "release", mainClass = "fixture.Main",
    javaVersion = new { component = "jre-legacy", majorVersion = 8 },
    downloads = new { client = new { url = "https://fixture.invalid/client", sha1 = Sha1(payload), size = payload.Length } },
    assetIndex = new { id = "package-fixture", url = "https://fixture.invalid/index", sha1 = Sha1(index), size = index.Length },
    libraries = Array.Empty<object>(), minecraftArguments = "--username ${auth_player_name}"
}));
await versions.SaveLocalAsync(metadata, ct);
var resolved = await versions.ResolveAsync(metadata.Id, VersionCachePolicy.CacheOnly, ct);
Require(resolved.Metadata.Java?.MajorVersion == 8, "metadata Java requirement");
Require((await GameCoreUtil.GetGameCoresAsync(root, ct)).Count == 1, "local inventory");
Require((await GameCoreUtil.GetGameCoreAsync(metadata.Id, root, ct)).Id == metadata.Id, "single version");

var java8 = new JavaRuntime(Path.Combine(root, "fixture-java"), root, JavaVersion.Parse("1.8.0_482"), JavaArchitecture.X64, "fixture");
var java21 = java8 with { Version = JavaVersion.Parse("21.0.1") };
Require(new JavaManager().Select([java21, java8], metadata,
    new() { TargetArchitecture = JavaArchitecture.X64 }).Version.Major == 8, "strict Java selection");
Require((await JavaUtil.GetJavasAsync(new() { JavaHome = null, PathDirectories = [], SearchDirectories = [] }, cancellationToken: ct)).Count == 0,
    "bounded Java discovery");

var installer = new GameInstaller(downloads, versions);
var options = new InstallationOptions { CachePolicy = VersionCachePolicy.CacheOnly, Context = java8.GetRuleContext() };
var installed = await installer.InstallAsync(metadata.Id, options, cancellationToken: ct);
Require(installed.DownloadedFiles == 2, "fixture installation");
Require((await installer.VerifyAsync(metadata.Id, options, cancellationToken: ct)).IsValid, "installation verification");
var beforeRepair = handler.Requests;
Require((await installer.RepairAsync(metadata.Id, options, cancellationToken: ct)).DownloadedFiles == 0 && handler.Requests == beforeRepair,
    "repair reuses complete files");

using var accounts = new AccountManager(Path.Combine(root, "accounts"));
var first = await accounts.LoginOfflineAsync("Notch", ct);
Require(first.Uuid == "b50ad385829d3141a2167e7d7539ba7f" && !first.IsAuthenticated, "offline UUID and identity");
var second = await accounts.LoginOfflineAsync("Second", ct);
await accounts.SwitchAsync(first.Account.Id, ct);
await accounts.SetDefaultAsync(first.Account.Id, ct);
Require((await accounts.GetLaunchIdentityAsync(cancellationToken: ct)).Account.Id == first.Account.Id, "account selection");
await accounts.RefreshAsync(first.Account.Id, cancellationToken: ct);
Require(await accounts.DeleteAsync(second.Account.Id, ct), "account deletion");
Require((await accounts.GetAccountsAsync(ct)).Accounts.Count == 1, "account persistence");

await using var launcher = new MinecraftLauncher(new()
{
    GameCore = new() { RootDirectory = root, VersionId = "absent-fixture", AutoInstall = false },
    Account = first
});
var failure = await launcher.LaunchAsync(cancellationToken: ct);
Require(!failure.Success && failure.Error?.Code == LaunchErrorCode.VersionError && launcher.ProcessId == null,
    "public launch structured failure");
await launcher.StopAsync(ct);
using var canceled = new CancellationTokenSource();
canceled.Cancel();
try { await launcher.LaunchAsync(cancellationToken: canceled.Token); throw new Exception("Cancellation was ignored."); }
catch (OperationCanceledException) { }

Console.WriteLine(JsonSerializer.Serialize(new
{
    PackageReferenceOnly = true, Modules = 6, DownloadHash = true, LocalVersion = true,
    StrictJavaSelection = true, SimulatedInstall = true, RepairRequests = handler.Requests - beforeRepair,
    OfflineAccounts = true, LaunchFailureAndCancellation = true,
    RealMicrosoftLogin = false, RealGameLaunch = false
}));

static string Sha1(byte[] bytes) => Convert.ToHexString(SHA1.HashData(bytes));
static void Require(bool condition, string behavior) { if (!condition) throw new Exception("Package API check failed: " + behavior); }

sealed class FixtureHandler(byte[] payload, byte[] index) : HttpMessageHandler
{
    private int requests;
    public int Requests => Volatile.Read(ref requests);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref requests);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(request.RequestUri!.AbsolutePath == "/index" ? index : payload),
            RequestMessage = request
        });
    }
}
