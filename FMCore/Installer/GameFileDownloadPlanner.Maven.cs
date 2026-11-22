using System.Net;
using System.Net.Http;
using System.Text;
using FMCore.Downloads;
using FMCore.Launch;
using FMCore.Logger;


namespace FMCore.Installer;

internal static partial class GameFileDownloadPlanner
{
    private const int MaximumChecksumBytes = 1024;

    private static readonly HttpClient MetadataClient = new()
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan
    };

    internal static async Task<IReadOnlyList<GameDownloadFile>>
        ReadResolvedAsync(
            string versionId,
            string minecraftRoot,
            string architecture,
            bool reuseIncompleteLegacyLibraries,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GameDownloadFile> files =
            await ReadAsync(
                versionId,
                minecraftRoot,
                architecture,
                cancellationToken).ConfigureAwait(false);

        GameDownloadFile[] resolved =
            new GameDownloadFile[files.Count];

        for (int index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            GameDownloadFile file = files[index];

            if (file.Request is not null || file.IsLocallyGenerated)
            {
                resolved[index] = file;
                continue;
            }

            if (reuseIncompleteLegacyLibraries && file.IsLegacyLibrary)
            {
                var issue = await GameFileChecker.InspectFileAsync(
                    file.Name, file.FilePath, file.DeclaredSize, file.DeclaredSha1,
                    generated: false, cancellationToken).ConfigureAwait(false);
                if (issue is null or { Kind: GameFileIssueKind.MetadataMissing })
                {
                    resolved[index] = file with { ReuseWithoutDownloadRequest = true };
                    continue;
                }
            }

            resolved[index] = file.MavenUrl is null ? file
                : await ResolveMavenFileAsync(file, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Array.AsReadOnly(resolved);
    }

    private static async Task<GameDownloadFile>
        ResolveMavenFileAsync(
            GameDownloadFile file,
            CancellationToken cancellationToken)
    {
        string url = file.MavenUrl
            ?? throw new InvalidOperationException(
                "待查询依赖缺少 Maven 下载地址。");

        // 单个依赖的所有元数据请求共用 30 秒。
        using CancellationTokenSource deadline =
            new(TimeSpan.FromSeconds(30));

        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                deadline.Token);

        CancellationToken token = linked.Token;

        async Task<DownloadRequest> ReadRequestAsync(string artifactUrl)
        {
            // 已有声明优先，只查询缺少的字段。
            string sha1 = file.DeclaredSha1
                ?? await ReadMavenSha1Async(artifactUrl, token).ConfigureAwait(false);

            long size = file.DeclaredSize
                ?? await ReadMavenSizeAsync(artifactUrl, token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            return FileDownloader.NormalizeRequest(new DownloadRequest
            {
                Url = artifactUrl,
                FilePath = file.FilePath,
                Size = size,
                Sha1 = sha1
            });
        }

        try
        {
            DownloadRequest request;
            try
            {
                request = await ReadRequestAsync(url).ConfigureAwait(false);
            }
            catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.NotFound
                && GetLegacyForgeUniversalUrl(file.Name, url) is { } universalUrl)
            {
                request = await ReadRequestAsync(universalUrl).ConfigureAwait(false);
            }

            return file with
            {
                Request = request,
                MissingFields = Array.Empty<string>(),
                DeclaredSha1 = request.Sha1,
                DeclaredSize = request.Size
            };
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested
                  && deadline.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"读取依赖 {file.Name} 的 Maven 元数据超时。",
                error);
        }
        catch (HttpRequestException error)
        {
            throw new HttpRequestException(
                $"读取依赖 {file.Name} 的 Maven 元数据失败：" +
                error.Message,
                error,
                error.StatusCode);
        }
    }

    internal static string? GetLegacyForgeUniversalUrl(string name, string url)
    {
        const string prefix = "net.minecraftforge:forge:";
        if (!name.StartsWith(prefix, StringComparison.Ordinal)) return null;
        var coordinateVersion = name.AsSpan(prefix.Length);
        var separator = coordinateVersion.IndexOf('-');
        if (separator < 1 || coordinateVersion.Contains(':') || coordinateVersion.Contains('@')
            || !Version.TryParse(coordinateVersion[..separator], out var minecraft)
            || minecraft.Major != 1 || minecraft.Minor > 12) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0
            || !uri.Host.Equals("maven.minecraftforge.net", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath != "/" + LibraryResolver.MavenToPath(name).Replace('\\', '/'))
            return null;

        return uri.GetLeftPart(UriPartial.Path)[..^4] + "-universal.jar";
    }

    private static async Task<string> ReadMavenSha1Async(
        string artifactUrl,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            artifactUrl + ".sha1");

        request.Headers.AcceptEncoding.ParseAdd("identity");

        using HttpResponseMessage response =
            await MetadataClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

        ValidateMetadataResponse(response);

        if (response.Content.Headers.ContentLength
                is long declaredLength
            && declaredLength > MaximumChecksumBytes)
        {
            throw new InvalidDataException(
                $"SHA-1 文件超过允许大小：{artifactUrl}.sha1");
        }

        await using Stream source =
            await response.Content.ReadAsStreamAsync(
                cancellationToken).ConfigureAwait(false);

        // 多留一个字节，用于识别正文超过限制。
        byte[] buffer =
            new byte[MaximumChecksumBytes + 1];

        int length = 0;

        while (length < buffer.Length)
        {
            int read = await source.ReadAsync(
                buffer.AsMemory(length),
                cancellationToken).ConfigureAwait(false);

            if (read == 0)
                break;

            length += read;
        }

        if (length > MaximumChecksumBytes)
        {
            throw new InvalidDataException(
                $"SHA-1 文件超过允许大小：{artifactUrl}.sha1");
        }

        string sha1 =
            Encoding.ASCII.GetString(buffer, 0, length)
                .Trim();

        if (sha1.Length != 40
            || !sha1.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException(
                $"仓库返回的 SHA-1 格式无效：{artifactUrl}.sha1");
        }

        return sha1.ToLowerInvariant();
    }

    private static async Task<long> ReadMavenSizeAsync(
        string artifactUrl,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Head,
            artifactUrl);

        request.Headers.AcceptEncoding.ParseAdd("identity");

        using HttpResponseMessage response =
            await MetadataClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

        ValidateMetadataResponse(response);

        if (response.Content.Headers.ContentLength
                is not long size
            || size < 0)
        {
            throw new NotSupportedException(
                $"仓库未提供可用的文件大小：{artifactUrl}");
        }

        return size;
    }

    private static void ValidateMetadataResponse(
        HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();

        if (response.StatusCode != HttpStatusCode.OK
            || response.Content.Headers.ContentRange is not null)
        {
            throw new InvalidDataException(
                "元数据请求必须返回完整的 HTTP 200 响应。");
        }

        bool encoded =
            response.Content.Headers.ContentEncoding.Any(
                value => !value.Equals(
                    "identity",
                    StringComparison.OrdinalIgnoreCase));

        if (encoded)
        {
            throw new NotSupportedException(
                "元数据请求暂不接受压缩响应。");
        }
    }
}
