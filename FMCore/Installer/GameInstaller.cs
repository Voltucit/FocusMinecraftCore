using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Installer;

/// <summary>Installs, checks and repairs vanilla files, reusing version metadata, downloads and asset handling.</summary>
public sealed partial class GameInstaller
{
    private readonly DownloadManager downloads;
    private readonly VersionManager versions;
    private readonly AssetManager assets;
    public string RootDirectory => downloads.RootDirectory;

    /// <summary>Creates a ready-to-use installer with the SDK's shared download client.</summary>
    public GameInstaller(string rootDirectory, DownloadOptions? options = null) : this(new DownloadManager(rootDirectory, options)) { }

    /// <summary>Uses an existing downloader and optional version manager for advanced networking/cache configuration.</summary>
    public GameInstaller(DownloadManager downloads, VersionManager? versions = null)
    {
        ArgumentNullException.ThrowIfNull(downloads);
        this.downloads = downloads;
        this.versions = versions ?? new(downloads);
        if (!GameFiles.PathComparer.Equals(this.versions.RootDirectory, downloads.RootDirectory))
            throw new ArgumentException("安装器与版本管理器必须使用同一根目录。", nameof(versions));
        assets = new(downloads);
    }

    /// <summary>Builds the platform-aware file/native/classpath plan from already resolved metadata.</summary>
    public InstallationPlan CreatePlan(ResolvedVersion version, InstallationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        return InstallationPlanner.Create(version, options ?? new(), RootDirectory);
    }

    /// <summary>Installs missing or damaged files, restores legacy asset layouts and extracts natives. Valid files are reused.</summary>
    public async Task<InstallationResult> InstallAsync(string versionId, InstallationOptions? options = null,
        IProgress<InstallationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        VersionParser.Id(versionId);
        options ??= new();
        var policy = options.CachePolicy;
        return await InstallCoreAsync(ct => versions.ResolveAsync(versionId, policy, ct), options, progress, cancellationToken).ConfigureAwait(false);
    }

    // Loader metadata can be installed in memory; publish its profile only after the same pipeline succeeds.
    internal Task<InstallationResult> InstallResolvedAsync(ResolvedVersion version, InstallationOptions options,
        IProgress<InstallationProgress>? progress, CancellationToken cancellationToken) =>
        InstallCoreAsync(_ => Task.FromResult(version), options, progress, cancellationToken);

    private async Task<InstallationResult> InstallCoreAsync(Func<CancellationToken, Task<ResolvedVersion>> resolve,
        InstallationOptions options, IProgress<InstallationProgress>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var lockPath = GameFiles.Resolve(RootDirectory, ".cache/install.lock");
            GameFiles.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            using var lease = AcquireLease(lockPath);
            progress?.Report(new(InstallationStage.Metadata, 0, 1));
            var version = await resolve(cancellationToken).ConfigureAwait(false);
            var plan = CreatePlan(version, options);
            var extractor = new NativeExtractor(RootDirectory, options.NativeExtraction);
            progress?.Report(new(InstallationStage.Metadata, 1, 1));
            var all = new List<DownloadResult>();
            await Download(InstallationStage.Client, [plan.Client]).ConfigureAwait(false);
            await Download(InstallationStage.Libraries, plan.Libraries).ConfigureAwait(false);
            progress?.Report(new(InstallationStage.AssetIndex, 0, 1));
            var index = await assets.GetIndexAsync(plan.AssetIndex, new InlineProgress<DownloadProgress>(p =>
                progress?.Report(new(InstallationStage.AssetIndex, p.State is DownloadState.Completed or DownloadState.Skipped ? 1 : 0, 1, p))), cancellationToken).ConfigureAwait(false);
            all.Add(index.Download);
            await Download(InstallationStage.AssetObjects, assets.CreateDownloads(index.Index)).ConfigureAwait(false);
            var layout = await assets.MaterializeAsync(index.Index, plan.AssetIndex.Id, plan.GameDirectory,
                new InlineProgress<AssetLayoutProgress>(p => progress?.Report(new(InstallationStage.AssetLayout, p.CompletedFiles, p.TotalFiles))), cancellationToken).ConfigureAwait(false);
            var nativeDirectory = await extractor.ExtractAsync(plan.NativeArchives, plan.NativeDirectory,
                new InlineProgress<NativeExtractionProgress>(p => progress?.Report(new(InstallationStage.Natives, p.CompletedArchives, p.TotalArchives))), cancellationToken).ConfigureAwait(false);
            await Download(InstallationStage.Logging, plan.Logging == null ? [] : [plan.Logging]).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new(InstallationStage.Completed, all.Count, all.Count));
            return new(plan, GameFiles.Resolve(RootDirectory, plan.Client.RelativePath), plan.ClasspathEntries.Select(p => GameFiles.Resolve(RootDirectory, p)).ToArray(),
                nativeDirectory, layout, plan.Logging == null ? null : GameFiles.Resolve(RootDirectory, plan.Logging.RelativePath),
                all.Count(r => !r.WasSkipped), all.Count(r => r.WasSkipped), all.Where(r => !r.WasSkipped).Sum(r => r.Size));

            async Task Download(InstallationStage stage, IReadOnlyList<DownloadRequest> requests)
            {
                var completed = 0;
                progress?.Report(new(stage, 0, requests.Count));
                var callback = new InlineProgress<DownloadProgress>(p =>
                {
                    if (p.State is DownloadState.Completed or DownloadState.Skipped) Interlocked.Increment(ref completed);
                    progress?.Report(new(stage, Volatile.Read(ref completed), requests.Count, p));
                });
                all.AddRange(await downloads.DownloadManyAsync(requests, callback, cancellationToken).ConfigureAwait(false));
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new InstallationException(InstallationErrorCode.IoError, "安装文件操作失败。"); }
    }

    private static FileStream AcquireLease(string path)
    {
        try { return new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException e) when ((e.HResult & 0xffff) is 32 or 33 or 11)
        { throw new InstallationException(InstallationErrorCode.InstallationBusy, "同一游戏目录已有安装任务正在执行。"); }
    }

    private sealed class InlineProgress<T>(Action<T> action) : IProgress<T>
    {
        public void Report(T value) => action(value);
    }
}
