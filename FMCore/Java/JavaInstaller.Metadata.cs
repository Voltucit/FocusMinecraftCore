using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Installer;

namespace FMCore.Java;

public sealed partial class JavaInstaller
{
    private const string CatalogUrl =
        "https://launchermeta.mojang.com/v1/products/java-runtime/" +
        "2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json";

    private const int MaximumMetadataBytes =
        8 * 1024 * 1024;

    private static readonly HttpClient MetadataClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = MaximumMetadataBytes
    };

    private (
        DownloadRequest[] Requests,
        string[] Directories) ReadPlan(
            JsonObject manifest,
            CancellationToken cancellationToken)
    {
        if (manifest["files"] is not JsonObject entries)
        {
            throw new InvalidDataException(
                "Java 文件清单缺少 files 对象。");
        }

        List<DownloadRequest> requests = [];
        List<string> directories = [];

        HashSet<string> targets =
            new(PathSafety.Comparer);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PathSafety.ValidateWindowsRelativePath(entry.Key);

            string target = PathSafety.Resolve(
                Path.Combine(
                    _installDirectory,
                    entry.Key.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));

            if (!PathSafety.IsWithin(
                    target,
                    _installDirectory))
            {
                throw new InvalidDataException(
                    $"Java 文件路径越出安装目录：{entry.Key}");
            }

            if (!targets.Add(target))
            {
                throw new InvalidDataException(
                    $"Java 清单存在重复目标：{entry.Key}");
            }

            if (entry.Value is not JsonObject file)
            {
                throw new InvalidDataException(
                    $"Java 文件声明无效：{entry.Key}");
            }

            string type = ReadText(file, "type");

            if (type == "directory")
            {
                if (File.Exists(target))
                {
                    throw new IOException(
                        $"Java 目录路径被文件占用：{target}");
                }

                directories.Add(target);
                continue;
            }

            if (type != "file")
            {
                throw new NotSupportedException(
                    $"当前不支持 Java 清单中的 {type} 项：{entry.Key}");
            }

            if (file["downloads"]?["raw"]
                is not JsonObject raw)
            {
                throw new InvalidDataException(
                    $"Java 文件缺少原始下载声明：{entry.Key}");
            }

            requests.Add(FileDownloader.NormalizeRequest(
                new DownloadRequest
                {
                    Url = RequireHttps(
                        ReadText(raw, "url")),

                    FilePath = target,
                    Size = ReadSize(raw),
                    Sha1 = ReadText(raw, "sha1"),

                    // 不覆盖已有的不匹配文件。
                    ReplaceInvalidFiles = false
                }));
        }

        HashSet<string> fileTargets = new(
            requests.Select(request => request.FilePath),
            PathSafety.Comparer);

        // 防止文件同时被声明为其他目标的父目录。
        foreach (string target in targets)
        {
            for (
                string? parent = Path.GetDirectoryName(target);
                parent is not null
                    && PathSafety.IsWithin(
                        parent,
                        _installDirectory);
                parent = Path.GetDirectoryName(parent))
            {
                if (fileTargets.Contains(parent))
                {
                    throw new InvalidDataException(
                        $"Java 清单中的文件与目录冲突：{parent}");
                }
            }
        }

        string javaPath = PathSafety.Resolve(
            Path.Combine(
                _installDirectory,
                "bin",
                "java.exe"));

        if (!fileTargets.Contains(javaPath))
        {
            throw new InvalidDataException(
                "Java 文件清单缺少 bin/java.exe。");
        }

        return (
            requests.ToArray(),
            directories.ToArray());
    }

    private static async Task<JsonObject> ReadJsonAsync(
        string url,
        CancellationToken cancellationToken,
        JsonObject? declaration = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        url = RequireHttps(url);

        long expectedSize = 0;
        string? expectedSha1 = null;

        if (declaration is not null)
        {
            expectedSize = ReadSize(declaration);
            expectedSha1 = ReadText(declaration, "sha1");

            if (expectedSize <= 0
                || expectedSize > MaximumMetadataBytes
                || expectedSha1.Length != 40
                || !expectedSha1.All(Uri.IsHexDigit))
            {
                throw new InvalidDataException(
                    "Java 文件清单的大小或 SHA-1 声明无效。");
            }
        }

        try
        {
            using HttpRequestMessage request =
                new(HttpMethod.Get, url);

            request.Headers.AcceptEncoding.ParseAdd("identity");

            using HttpResponseMessage response =
                await MetadataClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            bool encoded =
                response.Content.Headers.ContentEncoding.Any(
                    value => !value.Equals(
                        "identity",
                        StringComparison.OrdinalIgnoreCase));

            if (response.StatusCode != HttpStatusCode.OK
                || response.Content.Headers.ContentRange is not null
                || encoded)
            {
                throw new InvalidDataException(
                    "Java 元数据必须返回完整、未压缩的 HTTP 200 响应。");
            }

            byte[] bytes =
                await response.Content.ReadAsByteArrayAsync(
                    cancellationToken).ConfigureAwait(false);

            if (declaration is not null)
            {
                string actualSha1 = Convert.ToHexString(
                    SHA1.HashData(bytes));

                if (bytes.LongLength != expectedSize
                    || !actualSha1.Equals(
                        expectedSha1,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "Java 文件清单的大小或 SHA-1 校验失败。");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return JsonNode.Parse(bytes) as JsonObject
                ?? throw new InvalidDataException(
                    "Java 元数据必须是 JSON 对象。");
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "读取 Java 元数据超过 30 秒。",
                error);
        }
    }

    private static string ReadText(
        JsonObject value,
        string key)
    {
        string? text = value[key]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException(
                $"Java 元数据缺少有效的 {key}。");
        }

        return text;
    }

    private static long ReadSize(JsonObject value)
    {
        if (value["size"] is not JsonValue field
            || !field.TryGetValue<long>(out long size)
            || size < 0)
        {
            throw new InvalidDataException(
                "Java 元数据缺少有效的文件大小。");
        }

        return size;
    }

    private static string RequireHttps(string value)
    {
        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out Uri? url)
            || url is null
            || url.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException(
                "Java 下载地址必须是完整的 HTTPS 地址。");
        }

        return url.AbsoluteUri;
    }
}
