using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Versions;
using System.Runtime.InteropServices;

namespace FMCore.Installer;

public sealed partial class MinecraftInstaller
{
    private const string ManifestUrl =
        "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";

    private const int MaximumMetadataBytes =
        8 * 1024 * 1024;

    private static readonly HttpClient MetadataClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = MaximumMetadataBytes
    };

    private readonly string _versionId;
    private readonly string _minecraftRoot;
    private readonly Architecture? _targetArchitecture;
    private readonly int _maxConcurrency;
    public DownloadSource Source { get; init; } =
        DownloadSource.Official;
    private int _running;

    public MinecraftInstaller(
        string versionId,
        string minecraftRoot,
        int maxConcurrency = 4,
        Architecture? targetArchitecture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);


        PathSafety.ValidateWindowsRelativePath(versionId);

        if (versionId.Contains('/') || versionId.Contains('\\'))
        {
            throw new ArgumentException(
                "版本号必须是单个名称。",
                nameof(versionId));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            maxConcurrency);

        _versionId = versionId;

        _minecraftRoot =
            PathSafety.RequireDirectoryPath(minecraftRoot);

        _targetArchitecture = targetArchitecture;

        _maxConcurrency = maxConcurrency;

        if (_minecraftRoot.Contains(';'))
        {
            throw new NotSupportedException(
                "Minecraft 根目录不能包含分号。");
        }
    }

    public async Task<MinecraftInstallResult> InstallAsync(
        Action<MinecraftInstallProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Source);
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(
                ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "这个安装器正在执行安装，请等待完成后再调用。");
        }

        try
        {
            return await Task.Run(
                () => InstallCoreAsync(
                    onProgress,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task<MinecraftInstallResult> InstallCoreAsync(
        Action<MinecraftInstallProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Report(
            MinecraftInstallStage stage,
            string message,
            DownloadBatchProgress? download = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            onProgress?.Invoke(
                new MinecraftInstallProgress(
                    stage,
                    message,
                    download));
        }

        var (versionDirectory, jsonPath) = GetTargets();

        Report(
            MinecraftInstallStage.ReadingManifest,
            $"查询原版 {_versionId}……");

        JsonObject entry =
            await ReadManifestEntryAsync(
                cancellationToken).ConfigureAwait(false);

        string url = ReadText(entry, "url");
        string sha1 = ReadText(entry, "sha1");

        if (!Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri? downloadUri)
            || downloadUri is null
            || downloadUri.Scheme != "https"
            || sha1.Length != 40
            || !sha1.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException(
                "官方版本清单中的下载地址或 SHA-1 无效。");
        }

        long size;

        using (HttpResponseMessage head =
               await ReadMetadataResponseAsync(
                   HttpMethod.Head,
                   downloadUri.AbsoluteUri,
                   cancellationToken).ConfigureAwait(false))
        {
            if (head.Content.Headers.ContentLength
                    is not long length
                || length <= 0
                || length > MaximumMetadataBytes)
            {
                throw new InvalidDataException(
                    "版本 JSON 缺少有效大小，或超过允许大小。");
            }

            size = length;
        }

        Report(
            MinecraftInstallStage.DownloadingVersionJson,
            "下载并校验版本配置……");

        await FileDownloader.DownloadAsync(
            new DownloadRequest
            {
                Url = downloadUri.AbsoluteUri,
                FilePath = jsonPath,
                Size = size,
                Sha1 = sha1,

                // 内容不同的已有版本配置保留并报错。
                ReplaceInvalidFiles = false
            },
            progress => Report(
                MinecraftInstallStage.DownloadingVersionJson,
                $"版本配置：{progress.Percentage:F1}%"),
            cancellationToken).ConfigureAwait(false);

        JsonObject definition =
            MinecraftVersionReader.ReadVersionJson(
                versionDirectory);

        if (ReadText(definition, "id") != _versionId
            || definition["inheritsFrom"] is not null)
        {
            throw new InvalidDataException(
                "下载的配置不是所选原版版本的完整配置。");
        }

        Report(
            MinecraftInstallStage.CompletingFiles,
            "安装客户端、依赖库与资源……");

        GameFileCompleter completer = new(
            _versionId,
            _minecraftRoot,
            _maxConcurrency,
            targetArchitecture: _targetArchitecture)
        {
            Source = this.Source
        };

        GameFileCompletionResult files =
            await completer.CompleteAsync(
                progress => Report(
                    MinecraftInstallStage.CompletingFiles,
                    progress.Message,
                    progress.Download),
                cancellationToken).ConfigureAwait(false);

        MinecraftInstallResult result = new(
            _versionId,
            _minecraftRoot,
            versionDirectory,
            files);

        Report(
            MinecraftInstallStage.Completed,
            $"原版 {_versionId} 安装完成。");

        cancellationToken.ThrowIfCancellationRequested();

        return result;
    }

    private async Task<JsonObject> ReadManifestEntryAsync(
        CancellationToken cancellationToken)
    {
        JsonArray versions =
            await ReadManifestVersionsAsync(
                cancellationToken).ConfigureAwait(false);

        JsonObject[] matches =
            versions.OfType<JsonObject>()
                .Where(item =>
                    item["id"]?.GetValue<string>() == _versionId)
                .ToArray();

        if (matches.Length == 0)
        {
            throw new KeyNotFoundException(
                $"官方版本清单中没有版本：{_versionId}");
        }

        if (matches.Length != 1)
        {
            throw new InvalidDataException(
                $"官方版本清单中存在重复版本：{_versionId}");
        }

        return matches[0].DeepClone().AsObject();
    }

    private (string Directory, string JsonPath) GetTargets()
    {
        string versionsDirectory =
            PathSafety.RequireDirectoryPath(
                Path.Combine(_minecraftRoot, "versions"));

        string directory =
            PathSafety.RequireDirectoryPath(
                Path.Combine(versionsDirectory, _versionId));

        if (!PathSafety.Comparer.Equals(
                Path.GetDirectoryName(directory),
                versionsDirectory)
            || !PathSafety.Comparer.Equals(
                Path.GetFileName(directory),
                _versionId))
        {
            throw new InvalidDataException(
                "安装目录必须是 versions 下对应版本的直接子目录。");
        }

        string jsonPath = PathSafety.Resolve(
            Path.Combine(directory, _versionId + ".json"));

        if (!PathSafety.IsWithin(jsonPath, directory))
        {
            throw new InvalidDataException(
                "版本 JSON 路径越出了安装目录。");
        }

        return (directory, jsonPath);
    }

    private static async Task<HttpResponseMessage>
        ReadMetadataResponseAsync(
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
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"读取官方安装元数据超时：{url}",
                error);
        }

        try
        {
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
                    "官方元数据必须返回完整、未压缩的 HTTP 200 响应。");
            }

            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static string ReadText(
        JsonObject data,
        string key)
    {
        return MinecraftVersionReader.ReadRequiredString(
            data,
            key,
            $"官方配置缺少有效的 {key}。");
    }
}

