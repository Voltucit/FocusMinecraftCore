using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Modsloader;

public sealed partial class FabricInstaller
{
    private const string LoaderMetadataRoot =
        "https://meta.fabricmc.net/v2/versions/loader/";

    private static readonly HttpClient MetadataClient = new(
        new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip
                | DecompressionMethods.Deflate
                | DecompressionMethods.Brotli
        })
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 1024 * 1024
    };

    public static Task<IReadOnlyList<FabricLoaderVersion>> GetLoaderVersionsAsync(
        string gameVersion,
        CancellationToken cancellationToken = default)
        => GetLoaderVersionsAsync(
            gameVersion, DownloadSource.Official, cancellationToken);

    public static async Task<IReadOnlyList<FabricLoaderVersion>>
        GetLoaderVersionsAsync(
            string gameVersion,
            DownloadSource source,
            CancellationToken cancellationToken = default)
    {
        ValidateName(gameVersion, nameof(gameVersion));
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        JsonNode data = await ReadMetadataAsync(
            Uri.EscapeDataString(gameVersion),
            source, cancellationToken).ConfigureAwait(false);

        if (data is not JsonArray entries)
            throw new InvalidDataException("Fabric Loader 列表必须是数组。");

        List<FabricLoaderVersion> result = [];
        HashSet<string> versions = new(StringComparer.Ordinal);

        foreach (JsonNode? node in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node is not JsonObject entry
                || entry["loader"] is not JsonObject loader
                || loader["stable"] is not JsonValue stableValue
                || !stableValue.TryGetValue<bool>(out bool stable))
            {
                throw new InvalidDataException("Fabric Loader 版本项无效。");
            }

            string version = ReadText(loader, "version");

            if (!versions.Add(version))
                throw new InvalidDataException("Fabric Loader 列表包含重复版本。");

            result.Add(new FabricLoaderVersion(version, stable));
        }

        return result.AsReadOnly();
    }

    private async Task<JsonObject> ReadProfileAsync(
        CancellationToken cancellationToken)
    {
        string relativePath =
            Uri.EscapeDataString(_gameVersion) + "/"
            + Uri.EscapeDataString(_loaderVersion) + "/profile/json";

        JsonNode data = await ReadMetadataAsync(
            relativePath, Source, cancellationToken).ConfigureAwait(false);

        if (data is not JsonObject profile
            || ReadText(profile, "inheritsFrom") != _gameVersion
            || profile["libraries"] is not JsonArray libraries)
        {
            throw new InvalidDataException("Fabric 客户端配置与所选游戏不匹配。");
        }

        _ = ReadText(profile, "mainClass");
        bool foundLoader = false;

        foreach (JsonNode? node in libraries)
        {
            if (node is not JsonObject library)
                throw new InvalidDataException("Fabric 依赖项必须是对象。");

            if (ReadText(library, "name")
                == $"net.fabricmc:fabric-loader:{_loaderVersion}")
            {
                foundLoader = true;
            }
        }

        if (!foundLoader)
            throw new InvalidDataException("Fabric 配置没有声明所选 Loader。");

        // 使用调用方选择的实例 ID，保留对原版的继承。
        profile["id"] = VersionId;
        return profile;
    }

    private static async Task<JsonNode> ReadMetadataAsync(
        string relativePath,
        DownloadSource source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using HttpResponseMessage response = await MetadataClient.GetAsync(
                source.GetUrl(LoaderMetadataRoot + relativePath),
                HttpCompletionOption.ResponseContentRead,
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new InvalidDataException("Fabric 元数据未返回 HTTP 200。");

            string text = await response.Content.ReadAsStringAsync(
                cancellationToken).ConfigureAwait(false);

            return JsonNode.Parse(text)
                ?? throw new InvalidDataException("Fabric 元数据不能为空。");
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("读取 Fabric 元数据超过 30 秒。", error);
        }
    }

    private static void CheckExistingProfile(
        string directory,
        string jsonPath,
        JsonObject expected)
    {
        if (!File.Exists(jsonPath))
            return;

        JsonObject actual = MinecraftVersionReader.ReadVersionJson(directory);
        JsonObject left = actual.DeepClone().AsObject();
        JsonObject right = expected.DeepClone().AsObject();

        // 时间字段不决定加载内容，复用时允许这些显示时间不同。
        foreach (string key in new[] { "time", "releaseTime" })
        {
            left.Remove(key);
            right.Remove(key);
        }

        if (!JsonNode.DeepEquals(left, right))
        {
            throw new IOException(
                "已有实例配置与所选 Fabric 配置不同，文件已保留；"
                + "请换一个 versionId。");
        }
    }

    private static async Task WriteProfileAsync(
        string directory,
        string jsonPath,
        JsonObject profile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(jsonPath))
        {
            CheckExistingProfile(directory, jsonPath, profile);
            return;
        }

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(
            profile,
            new JsonSerializerOptions { WriteIndented = true });

        Directory.CreateDirectory(directory);

        string temporary = Path.Combine(
            directory, $".fabric-profile-{Guid.NewGuid():N}.tmp");

        bool created = false;

        try
        {
            await using (FileStream stream = new(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true))
            {
                created = true;
                await stream.WriteAsync(
                    bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(
                    cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, jsonPath, overwrite: false);
        }
        catch (Exception error)
        {
            if (created)
            {
                try
                {
                    File.Delete(temporary);
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "写入 Fabric 配置失败，临时文件清理也失败。",
                        error,
                        cleanupError);
                }
            }

            throw;
        }
    }

    private static string ReadText(JsonObject data, string key)
        => MinecraftVersionReader.ReadRequiredString(
            data, key, $"Fabric 元数据缺少有效的 {key}。");
}

