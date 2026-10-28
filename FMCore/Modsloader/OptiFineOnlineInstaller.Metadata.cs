using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Downloads;

namespace FMCore.Modsloader;

public sealed partial class OptiFineOnlineInstaller
{
    private const string CatalogRoot = "https://bmclapi2.bangbang93.com/optifine/";

    private static readonly HttpClient MetadataClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 1024 * 1024
    };

    public static Task<IReadOnlyList<OptiFineInstallVersion>> GetVersionsAsync(
        string gameVersion,
        CancellationToken cancellationToken = default)
        => GetVersionsAsync(gameVersion, DownloadSource.BmclApi, cancellationToken);

    public static async Task<IReadOnlyList<OptiFineInstallVersion>> GetVersionsAsync(
        string gameVersion,
        DownloadSource source,
        CancellationToken cancellationToken = default)
    {
        ValidateName(gameVersion);
        ValidateSource(source);
        cancellationToken.ThrowIfCancellationRequested();

        string catalogUrl = source.GetUrl(CatalogRoot + Uri.EscapeDataString(gameVersion));
        JsonNode? data;

        try
        {
            using HttpResponseMessage response = await MetadataClient.GetAsync(
                catalogUrl,
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode != HttpStatusCode.OK
                || response.Content.Headers.ContentRange is not null)
                throw new InvalidDataException("OptiFine 列表必须返回完整的 HTTP 200。");

            data = JsonNode.Parse(await response.Content.ReadAsStringAsync(
                cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("读取 OptiFine 列表超过 30 秒。", error);
        }

        if (data is not JsonArray entries)
            throw new InvalidDataException("OptiFine 版本列表必须是数组。");

        Dictionary<string, OptiFineInstallVersion> result = new(StringComparer.Ordinal);

        foreach (JsonNode? node in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node is not JsonObject entry)
                throw new InvalidDataException("OptiFine 版本项必须是对象。");

            string game = ReadText(entry, "mcversion");
            string type = ReadText(entry, "type");
            string patch = ReadText(entry, "patch");
            string fileName = ReadText(entry, "filename");

            if (game != gameVersion
                || !Regex.IsMatch(type, @"\AHD_U(?:_[A-Za-z0-9]+)*\z")
                || !Regex.IsMatch(patch, @"\A[A-Za-z0-9_]+\z"))
                throw new InvalidDataException("OptiFine 版本声明与查询不匹配。");

            ValidateName(fileName);

            if (!fileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("OptiFine 文件名必须是 JAR。");

            string edition = type + "_" + patch;
            string url = CatalogRoot
                + Uri.EscapeDataString(game) + "/"
                + Uri.EscapeDataString(type) + "/"
                + Uri.EscapeDataString(patch);

            if (!result.TryAdd(edition, new OptiFineInstallVersion(
                    game, edition, fileName, url)))
                throw new InvalidDataException("OptiFine 列表包含重复版本。");
        }

        return Array.AsReadOnly(result.Values
            .OrderBy(item => item.IsPreview)
            .ThenByDescending(item => Regex.Replace(
                item.Edition, @"\d+", match => match.Value.PadLeft(10, '0')),
                StringComparer.Ordinal)
            .ToArray());
    }

    private static string ReadText(JsonObject node, string key)
        => node[key] is JsonValue value
            && value.TryGetValue<string>(out string? text)
            && !string.IsNullOrWhiteSpace(text)
                ? text
                : throw new InvalidDataException($"OptiFine 元数据缺少 {key}。");
}

