using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Tests.Fixtures;

internal sealed class InstallationTestContext : IDisposable
{
    private readonly DownloadTestContext directory = new();
    private readonly HttpClient http;
    internal string Root => directory.Root;
    internal DownloadManager Downloads { get; }
    internal VersionManager Versions { get; }
    internal GameInstaller Installer { get; }
    internal ConcurrentDictionary<string, byte[]> Bodies { get; } = new(StringComparer.Ordinal);
    internal ConcurrentDictionary<string, int> Requests { get; } = new(StringComparer.Ordinal);
    internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Override { get; set; }
    internal static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static InstallationOptions Options => new() { Context = new("windows", "amd64", "10.0"), CachePolicy = VersionCachePolicy.CacheOnly };
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Installer", name));
    internal string At(string relative) => directory.At(relative.Replace('/', Path.DirectorySeparatorChar));

    internal InstallationTestContext()
    {
        http = new(new DelegateHandler(async (request, ct) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.AddOrUpdate(path, 1, (_, count) => count + 1);
            if (Override != null) return await Override(request, ct);
            return Response(path);
        }));
        Downloads = new(http, Root, DownloadTestContext.FastOptions);
        Versions = new(Downloads);
        Installer = new(Downloads, Versions);
    }

    internal HttpResponseMessage Response(string path) => Bodies.TryGetValue(path, out var bytes)
        ? new(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }
        : new(System.Net.HttpStatusCode.NotFound);

    internal async Task<JsonObject> SeedAsync(bool virtualAssets = false, bool mapToResources = false)
    {
        var content = Encoding.UTF8.GetBytes("fixture asset");
        var hash = Hash(content);
        var emptyHash = Hash([]);
        var index = new JsonObject
        {
            ["virtual"] = virtualAssets,
            ["map_to_resources"] = mapToResources,
            ["objects"] = new JsonObject
            {
                ["sounds/first.ogg"] = new JsonObject { ["hash"] = hash, ["size"] = content.Length },
                ["sounds/alias.ogg"] = new JsonObject { ["hash"] = hash, ["size"] = content.Length },
                ["empty.txt"] = new JsonObject { ["hash"] = emptyHash, ["size"] = 0 }
            }
        };
        Bodies[$"/{hash[..2]}/{hash}"] = content;
        Bodies[$"/{emptyHash[..2]}/{emptyHash}"] = [];
        var jar = Zip(("fixture.dll", "native bytes", null), ("META-INF/MANIFEST.MF", "excluded", null));
        var document = new JsonObject
        {
            ["id"] = "fixture",
            ["type"] = "release",
            ["mainClass"] = "net.minecraft.client.main.Main",
            ["downloads"] = new JsonObject { ["client"] = Artifact("client.jar", Encoding.UTF8.GetBytes("client bytes")) },
            ["assetIndex"] = Artifact("index.json", Encoding.UTF8.GetBytes(index.ToJsonString())),
            ["logging"] = new JsonObject
            {
                ["client"] = new JsonObject
                { ["argument"] = "-Dlog4j.configurationFile=${path}", ["file"] = Artifact("client.xml", Encoding.UTF8.GetBytes("log config")) }
            },
            ["libraries"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "example:both:1", ["downloads"] = new JsonObject
                    {
                        ["artifact"] = Artifact("both.jar", Encoding.UTF8.GetBytes("classpath bytes")),
                        ["classifiers"] = new JsonObject { ["natives-windows-64"] = Artifact("natives.jar", jar) }
                    },
                    ["natives"] = new JsonObject { ["windows"] = "natives-windows-${arch}" },
                    ["extract"] = new JsonObject { ["exclude"] = new JsonArray("META-INF/") }
                },
                new JsonObject
                {
                    ["name"] = "example:linux-only:1", ["downloads"] = new JsonObject { ["artifact"] = Artifact("linux.jar", [1, 2, 3]) },
                    ["rules"] = new JsonArray { new JsonObject { ["action"] = "allow", ["os"] = new JsonObject { ["name"] = "linux" } } }
                }
            }
        };
        document["assetIndex"]!["id"] = "fixture-assets";
        document["logging"]!["client"]!["file"]!["id"] = "fixture-log.xml";
        await SaveAsync(document);
        return document;
    }

    internal Task SaveAsync(JsonObject document) => Versions.SaveLocalAsync(VersionParser.Parse(document.ToJsonString()), Ct);
    internal JsonObject Artifact(string path, byte[] bytes)
    {
        Bodies["/" + path] = bytes;
        return new JsonObject { ["url"] = "https://fixture.example.test/" + path, ["sha1"] = Hash(bytes), ["size"] = bytes.Length };
    }
    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
    internal static byte[] Zip(params (string Name, string Content, int? Attributes)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var (name, content, attributes) in entries)
            {
                var entry = archive.CreateEntry(name);
                if (attributes.HasValue) entry.ExternalAttributes = attributes.Value;
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        return stream.ToArray();
    }
    internal async Task<NativeArchive> SaveArchiveAsync(string name, byte[] bytes, IReadOnlyList<string>? excludes = null)
    {
        var path = "libraries/" + name;
        Directory.CreateDirectory(Path.GetDirectoryName(At(path))!);
        await File.WriteAllBytesAsync(At(path), bytes, Ct);
        return new(path, Hash(bytes), bytes.Length, excludes ?? []);
    }
    internal void AssertNoTemporaryFiles()
    {
        directory.AssertNoParts();
        Assert.Empty(Directory.EnumerateFiles(Root, "*.tmp", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateDirectories(Root, ".fmcore-natives-*", SearchOption.AllDirectories));
    }
    public void Dispose() { http.Dispose(); directory.Dispose(); }
}
