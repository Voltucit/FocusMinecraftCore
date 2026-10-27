using FMCore.Downloads;
using System.Runtime.InteropServices;

namespace FMCore.Installer;

public sealed class GameFileCompleter
{
    private readonly string _versionId;
    private readonly string _minecraftRoot;
    private readonly string _architecture;
    private readonly int _maxConcurrency;
    private readonly bool _replaceInvalidFiles;
    public DownloadSource Source { get; init; } =
        DownloadSource.Official;
    private int _running;

    public GameFileCompleter(
        string versionId,
        string minecraftRoot,
        int maxConcurrency = 4,
        bool replaceInvalidFiles = true,
        Architecture? targetArchitecture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "当前文件补全只支持 Windows。");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            maxConcurrency);

        _versionId = versionId;

        _minecraftRoot =
            PathSafety.RequireDirectoryPath(minecraftRoot);

        _maxConcurrency = maxConcurrency;
        _replaceInvalidFiles = replaceInvalidFiles;

        _architecture =
            (targetArchitecture ?? RuntimeInformation.OSArchitecture)
                switch
                {
                    Architecture.X86 => "x86",
                    Architecture.X64 => "x64",
                    Architecture.Arm64 => "arm64",
                    _ => throw new NotSupportedException(
                        "当前只支持 x86、x64 或 arm64 目标架构。")
                };
    }

    public async Task<GameFileCompletionResult> CompleteAsync(
        Action<GameFileCompletionProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(Source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(Source),
                "不支持这个下载源。");
        }
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

    private async Task<GameFileCompletionResult> CompleteCoreAsync(
        Action<GameFileCompletionProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Report(
            GameFileCompletionStage stage,
            string message,
            DownloadBatchProgress? download = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            onProgress?.Invoke(
                new GameFileCompletionProgress(
                    stage,
                    message,
                    download));
        }

        Report(
            GameFileCompletionStage.SelectingArchitecture,
            $"目标架构：{_architecture}");

        Report(
            GameFileCompletionStage.ReadingMetadata,
            "读取客户端、依赖及 Maven 下载声明……");

        IReadOnlyList<GameDownloadFile> files =
            await GameFileDownloadPlanner.ReadResolvedAsync(
                _versionId,
                _minecraftRoot,
                _architecture,
                cancellationToken).ConfigureAwait(false);

        GameDownloadFile[] incomplete =
            files.Where(file => file.Request is null)
                .ToArray();

        // 所有下载声明先准备齐全，再开始写入文件。
        if (incomplete.Length != 0)
        {
            string details = string.Join(
                Environment.NewLine,
                incomplete.Take(10).Select(
                    file => $"{file.Name}：" +
                            string.Join(", ", file.MissingFields)));

            throw new NotSupportedException(
                $"仍有 {incomplete.Length} 个文件缺少下载信息。" +
                Environment.NewLine +
                details);
        }

        DownloadRequest[] requests =
            files.Select(
                    file => ApplyPolicy(file.Request!))
                .ToArray();

        Report(
            GameFileCompletionStage.CompletingClientAndLibraries,
            "补全并校验客户端与依赖库……");

        IReadOnlyList<DownloadResult> binaryResults =
            await FileDownloader.DownloadManyAsync(
                requests,
                _maxConcurrency,
                progress => Report(
                    GameFileCompletionStage
                        .CompletingClientAndLibraries,
                    "客户端与依赖库",
                    progress),
                cancellationToken).ConfigureAwait(false);

        Report(
            GameFileCompletionStage.CompletingResources,
            "补全并校验资源与日志配置……");

        GameResourceCompleter resourceCompleter = new(
            _versionId,
            _minecraftRoot,
            _maxConcurrency,
            _replaceInvalidFiles)
        {
            Source = this.Source
        };

        ResourceCompletionResult resources =
            await resourceCompleter.CompleteAsync(
                progress => Report(
                    GameFileCompletionStage.CompletingResources,
                    progress.Message,
                    progress.Download),
                cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        int downloadedFiles =
            binaryResults.Count(item => !item.AlreadyPresent)
            + resources.DownloadedFiles;

        int reusedFiles =
            binaryResults.Count(item => item.AlreadyPresent)
            + resources.ReusedFiles;

        GameFileBackup[] backups =
            binaryResults
                .Where(item => item.BackupPath is not null)
                .Select(item => new GameFileBackup(
                    item.FilePath,
                    item.BackupPath!))
                .Concat(
                    resources.Backups.Select(
                        item => new GameFileBackup(
                            item.FilePath,
                            item.BackupPath)))
                .ToArray();

        // 内部清单首项固定为客户端，其余为依赖文件。
        GameFileCompletionResult result = new(
            ClientFiles: 1,
            LibraryFiles: files.Count - 1,
            ResourceMetadataFiles: resources.MetadataFiles,
            AssetFiles: resources.AssetFiles,
            DownloadedFiles: downloadedFiles,
            ReusedFiles: reusedFiles)
        {
            Backups = Array.AsReadOnly(backups)
        };

        Report(
            GameFileCompletionStage.Completed,
            $"游戏文件校验完成，共 {result.TotalFiles} 个文件。");

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
