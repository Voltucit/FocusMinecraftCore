using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using FMCore.Downloader;
using FMCore.Versions;

namespace FMCore.Tests.Fixtures;

internal sealed class VersionTestContext : IDisposable
{
    private readonly DownloadTestContext directory = new();
    private readonly HttpClient http;
    internal string Root => directory.Root;
    internal VersionManager Manager { get; }
    internal DelegateHandler Handler { get; }
    internal static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal VersionTestContext(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? respond = null)
    {
        Handler = new(respond ?? ((_, _) => throw new InvalidOperationException("Offline test must not make requests.")));
        http = new(Handler);
        Manager = new(new DownloadManager(http, Root, DownloadTestContext.FastOptions));
    }
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Versions", name));
    internal Task SaveAsync(string json) => Manager.SaveLocalAsync(VersionParser.Parse(json), Ct);
    internal static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK) => DownloadTestContext.Response(json, status);
    internal static string Manifest(params (string Id, string Json)[] versions) => new JsonObject
    {
        ["latest"] = new JsonObject { ["release"] = versions[0].Id, ["snapshot"] = versions[^1].Id },
        ["versions"] = new JsonArray(versions.Select(v => (JsonNode)new JsonObject
        {
            ["id"] = v.Id,
            ["type"] = "release",
            ["url"] = "https://metadata.example.test/" + v.Id,
            ["sha1"] = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(v.Json))),
            ["releaseTime"] = "2024-01-01T00:00:00Z"
        }).ToArray())
    }.ToJsonString();
    internal void AssertNoTemporaryFiles()
    {
        directory.AssertNoParts();
        Assert.Empty(Directory.EnumerateFiles(Root, "*.tmp", SearchOption.AllDirectories));
        var incoming = Path.Combine(Root, ".cache", "metadata", "incoming");
        if (Directory.Exists(incoming)) Assert.Empty(Directory.EnumerateFiles(incoming));
    }
    public void Dispose() { http.Dispose(); directory.Dispose(); }
}
