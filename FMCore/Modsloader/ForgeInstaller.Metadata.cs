using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using FMCore.Downloads;
using FMCore.Installer;
using FMCore.Launch;

namespace FMCore.Modsloader;

public sealed partial class ForgeInstaller
{
    private const string ForgeMaven =
        "https://maven.minecraftforge.net/net/minecraftforge/forge/";

    private const string NeoForgeMaven =
        "https://maven.neoforged.net/releases/net/neoforged/neoforge/";

    private static readonly HttpClient MetadataClient = new(
        new HttpClientHandler { AllowAutoRedirect = true })
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 8 * 1024 * 1024
    };

    public static Task<IReadOnlyList<string>> GetVersionsAsync(
        string gameVersion,
        CancellationToken cancellationToken = default)
        => GetVersionsAsync(
            gameVersion, DownloadSource.Official, cancellationToken);

    public static Task<IReadOnlyList<string>> GetVersionsAsync(
        string gameVersion,
        DownloadSource source,
        CancellationToken cancellationToken = default)
    {
        ValidateName(gameVersion);
        ArgumentNullException.ThrowIfNull(source);

        return ReadVersionListAsync(
            ForgeMaven, gameVersion + "-",
            removePrefix: true, source, cancellationToken);
    }

    internal static Task<IReadOnlyList<string>> GetNeoForgeVersionsAsync(
        string gameVersion,
        DownloadSource source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!System.Version.TryParse(gameVersion, out System.Version? minecraft)
            || minecraft.Revision >= 0)
        {
            throw new ArgumentException(
                "NeoForge 版本查询需要正式版编号，例如 1.21.10 或 26.1。",
                nameof(gameVersion));
        }

        int patch = minecraft.Build < 0 ? 0 : minecraft.Build;
        string prefix = minecraft.Major == 1
            ? $"{minecraft.Minor}.{patch}."
            : $"{minecraft.Major}.{minecraft.Minor}.{patch}.";

        return ReadVersionListAsync(
            NeoForgeMaven, prefix, removePrefix: false, source, cancellationToken);
    }

    private static async Task<IReadOnlyList<string>> ReadVersionListAsync(
        string mavenRoot,
        string prefix,
        bool removePrefix,
        DownloadSource downloadSource,
        CancellationToken cancellationToken)
    {
        string url = downloadSource.GetUrl(mavenRoot + "maven-metadata.xml");

        using HttpResponseMessage response = await ReadResponseAsync(
            HttpMethod.Get, url, cancellationToken).ConfigureAwait(false);

        string text = await response.Content.ReadAsStringAsync(
            cancellationToken).ConfigureAwait(false);

        using StringReader source = new(text);
        using XmlReader xml = XmlReader.Create(source, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });

        XDocument document = XDocument.Load(xml);
        XElement? versions = document.Root?
            .Element("versioning")?.Element("versions");

        if (versions is null)
            throw new InvalidDataException("加载器 Maven 清单缺少版本列表。");

        string[] result = versions.Elements("version")
            .Select(item => item.Value.Trim())
            .Where(item => item.StartsWith(prefix, StringComparison.Ordinal))
            .Select(item => removePrefix ? item[prefix.Length..] : item)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(item =>
                System.Version.TryParse(item.Split('-')[0], out System.Version? number)
                    ? number : new System.Version(0, 0))
            .ThenByDescending(item => item, StringComparer.Ordinal)
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();
        return Array.AsReadOnly(result);
    }

    private async Task<DownloadRequest> ReadInstallerRequestAsync(
        CancellationToken cancellationToken)
    {
        string version = _isNeoForge
            ? _forgeVersion
            : _gameVersion + "-" + _forgeVersion;

        string artifact = _isNeoForge ? "neoforge" : "forge";
        string maven = _isNeoForge ? NeoForgeMaven : ForgeMaven;

        string url = maven + Uri.EscapeDataString(version)
                           + "/" + artifact + "-" + Uri.EscapeDataString(version)
                           + "-installer.jar";

        string sha1;

        using (HttpResponseMessage checksum = await ReadResponseAsync(
                   HttpMethod.Get, url + ".sha1",
                   cancellationToken).ConfigureAwait(false))
        {
            string text = await checksum.Content.ReadAsStringAsync(
                cancellationToken).ConfigureAwait(false);

            if (text.Length > 1024)
                throw new InvalidDataException("安装包 SHA-1 响应过长。");

            sha1 = ValidateSha1(text.Trim());
        }

        long size;

        using (HttpResponseMessage head = await ReadResponseAsync(
                   HttpMethod.Head, url, cancellationToken).ConfigureAwait(false))
        {
            if (head.Content.Headers.ContentLength is not long length
                || length <= 0 || length > 64L * 1024 * 1024)
                throw new InvalidDataException("安装包大小无效或超过 64 MiB。");

            size = length;
        }

        return new DownloadRequest
        {
            Url = url,
            FilePath = ArtifactPath(PackageCoordinate + ":installer"),
            Size = size,
            Sha1 = sha1
        };
    }

    private static async Task<HttpResponseMessage> ReadResponseAsync(
        HttpMethod method,
        string url,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, url);
        request.Headers.AcceptEncoding.ParseAdd("identity");

        HttpResponseMessage response;

        try
        {
            response = await MetadataClient.SendAsync(
                request, HttpCompletionOption.ResponseContentRead,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("读取 Forge 元数据超过 30 秒。", error);
        }

        try
        {
            response.EnsureSuccessStatusCode();

            if (response.StatusCode != HttpStatusCode.OK
                || response.Content.Headers.ContentRange is not null
                || response.Content.Headers.ContentEncoding.Any(
                    value => !value.Equals("identity", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Forge 元数据必须返回完整、未压缩的 HTTP 200。");

            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static async Task<byte[]> ReadEntryAsync(
        ZipArchive archive,
        string name,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry entry = archive.GetEntry(name.TrimStart('/'))
            ?? throw new InvalidDataException($"安装包缺少文件：{name}");

        if (entry.Length < 0 || entry.Length > maximumBytes)
            throw new InvalidDataException($"安装包文件超过允许大小：{name}");

        byte[] bytes = new byte[(int)entry.Length];
        await using Stream stream = entry.Open();
        await stream.ReadExactlyAsync(
            bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
        return bytes;
    }

    private static async Task<JsonObject> ReadJsonEntryAsync(
        ZipArchive archive,
        string name,
        CancellationToken cancellationToken)
    {
        byte[] bytes = await ReadEntryAsync(
            archive, name, 2 * 1024 * 1024,
            cancellationToken).ConfigureAwait(false);

        return JsonNode.Parse(bytes) as JsonObject
            ?? throw new InvalidDataException($"安装包中的 {name} 不是 JSON 对象。");
    }

    private (DownloadRequest[] Downloads, ExpectedFile[] Generated) ReadLibraryPlan(
        JsonObject install,
        JsonObject profile,
        DownloadSource source)
    {
        Dictionary<string, ExpectedFile> expected = new(PathSafety.Comparer);
        Dictionary<string, DownloadRequest> downloads = new(PathSafety.Comparer);

        foreach (JsonObject owner in new[] { install, profile })
        {
            if (owner["libraries"] is not JsonArray libraries)
                throw new InvalidDataException("Forge 配置缺少 libraries。");

            foreach (JsonNode? node in libraries)
            {
                if (node is not JsonObject library
                    || library["downloads"] is not JsonObject container
                    || container["artifact"] is not JsonObject artifact)
                {
                    throw new NotSupportedException(
                        "当前 Forge 入口要求依赖提供 downloads.artifact。");
                }

                string coordinate = ReadText(library, "name");
                string relative = ReadText(artifact, "path");
                PathSafety.ValidateWindowsRelativePath(relative);

                if (!PathSafety.Comparer.Equals(
                        relative.Replace('/', Path.DirectorySeparatorChar),
                        LibraryResolver.MavenToPath(coordinate)))
                    throw new InvalidDataException("Forge 依赖坐标与路径不一致。");

                string path = ArtifactPath(coordinate);
                string sha1 = ValidateSha1(ReadText(artifact, "sha1"));

                if (artifact["size"] is not JsonValue sizeValue
                    || !sizeValue.TryGetValue<long>(out long size) || size < 0
                    || artifact["url"] is not JsonValue urlValue
                    || !urlValue.TryGetValue<string>(out string? url) || url is null)
                    throw new InvalidDataException("Forge 依赖大小或 URL 声明无效。");

                ExpectedFile file = new(path, sha1, size);

                if (expected.TryGetValue(path, out ExpectedFile? previous)
                    && (previous.Sha1 != sha1 || previous.Size != size))
                    throw new InvalidDataException("Forge 依赖存在冲突的文件声明。");

                expected[path] = file;

                if (url.Length == 0)
                    continue; // 由处理器生成，不向 Maven 请求空 URL。

                DownloadRequest request = FileDownloader.NormalizeRequest(
                    new DownloadRequest
                    {
                        Url = url,
                        FilePath = path,
                        Sha1 = sha1,
                        Size = size
                    });

                downloads.TryAdd(path, DownloadSourcePolicy.Apply(
                    request, source, replaceInvalidFiles: true));
            }
        }

        return (
            downloads.Values.ToArray(),
            expected.Values.Where(file => !downloads.ContainsKey(file.Path)).ToArray());
    }

    private static string ValidateSha1(string value)
    {
        if (value.Length != 40 || !value.All(Uri.IsHexDigit))
            throw new InvalidDataException("Forge SHA-1 必须为 40 位十六进制字符串。");

        return value.ToLowerInvariant();
    }
}

