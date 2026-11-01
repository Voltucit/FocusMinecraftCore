using FMCore.Versions;
using FMCore.Downloads;

namespace FMCore.Installer;

public sealed class GameResourceCompleter
{
    private readonly string _versionId;
    private readonly string _minecraftRoot;
    private readonly int _maxConcurrency;
    private readonly bool _replaceInvalidFiles;
    private int _running;

    public string? GameDirectory { get; init; }

    public DownloadSource Source { get; init; } =
        DownloadSource.Official;
    public GameResourceCompleter(
        string versionId,
        string minecraftRoot,
        int maxConcurrency = 4,
        bool replaceInvalidFiles = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrency),
                "最大并发数必须大于 0。");
        }

        _versionId = versionId;

        _minecraftRoot =
            PathSafety.RequireDirectoryPath(minecraftRoot);

        _maxConcurrency = maxConcurrency;

        _replaceInvalidFiles = replaceInvalidFiles;
    }

    public async Task<ResourceCompletionResult> CompleteAsync(
        Action<ResourceCompletionProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Source);
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(
                ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "这个补全对象正在执行操作，请等待完成后再调用。");
        }

        try
        {
            return await Task.Run(
                () => CompleteCoreAsync(
                    onProgress,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task<ResourceCompletionResult> CompleteCoreAsync(
        Action<ResourceCompletionProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Report(
            ResourceCompletionStage stage,
            string message,
            DownloadBatchProgress? download = null)
        {
            onProgress?.Invoke(
                new ResourceCompletionProgress(
                    stage,
                    message,
                    download));
        }

        Report(
            ResourceCompletionStage.ReadingMetadata,
            "读取资源下载声明……");

        IReadOnlyList<DownloadRequest> metadata =
            await ResourceDownloadPlanner.ReadMetadataAsync(
                _versionId,
                _minecraftRoot,
                cancellationToken).ConfigureAwait(false);

        metadata = metadata.Select(ApplyPolicy).ToArray();
        // 元数据读取器保证资源索引请求位于首项。
        DownloadRequest indexRequest = metadata[0];

        ResourceDownloadPlanner.ValidateIndexSize(indexRequest);

        Report(
            ResourceCompletionStage.CompletingMetadata,
            "补全并校验资源索引与日志配置……");

        IReadOnlyList<DownloadResult> metadataResults =
            await FileDownloader.DownloadManyAsync(
                metadata,
                _maxConcurrency,
                progress => Report(
                    ResourceCompletionStage.CompletingMetadata,
                    "资源索引与日志配置",
                    progress),
                cancellationToken).ConfigureAwait(false);

        Report(
            ResourceCompletionStage.ReadingAssetObjects,
            "读取并检查资源对象清单……");

        AssetDownloadPlan plan =
            await ResourceDownloadPlanner.ReadAssetObjectsAsync(
                indexRequest,
                _minecraftRoot,
                cancellationToken,
                gameDirectory: GameDirectory ?? new MinecraftVersionReader()
                    .GetVersion(_versionId, _minecraftRoot).Directory).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(plan.ObjectsDirectory);

        Report(
            ResourceCompletionStage.CompletingAssetObjects,
            "补全并校验资源对象……");

        IReadOnlyList<DownloadResult> assetResults =
            await FileDownloader.DownloadManyAsync(
                plan.Requests.Select(ApplyPolicy),
                _maxConcurrency,
                progress => Report(
                    ResourceCompletionStage.CompletingAssetObjects,
                    "资源对象",
                    progress),
                cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        List<DownloadResult> mappingResults = [];
        if (plan.MappingDirectories.Count != 0)
        {
            Report(ResourceCompletionStage.CompletingAssetMappings,
                "生成并校验旧版资源映射……");
            foreach (string directory in plan.MappingDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(directory);
            }
            long totalBytes = plan.Mappings.Sum(item => item.Request.Size);
            long processedBytes = 0;
            foreach (AssetMapping mapping in plan.Mappings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                mappingResults.Add(await FileDownloader.CopyVerifiedAsync(
                    mapping.SourcePath, ApplyPolicy(mapping.Request), cancellationToken)
                    .ConfigureAwait(false));
                processedBytes = checked(processedBytes + mapping.Request.Size);
                Report(ResourceCompletionStage.CompletingAssetMappings,
                    "旧版资源映射", new DownloadBatchProgress(
                        plan.Mappings.Count, mappingResults.Count, 0, 1,
                        processedBytes, totalBytes));
            }
        }
        cancellationToken.ThrowIfCancellationRequested();

        int downloadedFiles =
            metadataResults.Count(item => !item.AlreadyPresent)
            + assetResults.Count(item => !item.AlreadyPresent);

        int reusedFiles =
            metadataResults.Count(item => item.AlreadyPresent)
            + assetResults.Count(item => item.AlreadyPresent)
            + mappingResults.Count(item => item.AlreadyPresent);

        ResourceFileBackup[] backups =
            metadataResults
                .Concat(assetResults)
                .Concat(mappingResults)
                .Where(item => item.BackupPath is not null)
                .Select(item => new ResourceFileBackup(
                    item.FilePath,
                    item.BackupPath!))
                .ToArray();

        ResourceCompletionResult result = new(
            metadataResults.Count,
            assetResults.Count,
            downloadedFiles,
            reusedFiles)
        {
            Backups = Array.AsReadOnly(backups),
            MappedFiles = mappingResults.Count,
            CopiedFiles = mappingResults.Count(item => !item.AlreadyPresent)
        };

        Report(
            ResourceCompletionStage.Completed,
            $"资源校验完成，共 {result.TotalFiles} 个文件。");

        cancellationToken.ThrowIfCancellationRequested();

        return result;
    }
    private DownloadRequest ApplyPolicy(
        DownloadRequest request)
    {
        return DownloadSourcePolicy.Apply(
            request,
            Source,
            _replaceInvalidFiles);
    }
}

