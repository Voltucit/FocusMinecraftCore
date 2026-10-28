using FMCore.Downloads;
using FMCore.Installer;
using FMCore.Launch;

namespace FMCore.Modsloader;

public sealed partial class OptiFineOnlineInstaller
{
    private readonly string _gameVersion;
    private readonly string _edition;
    private readonly string _root;
    private readonly string _javaPath;
    private readonly int _maxConcurrency;
    private readonly int _processorMemoryMb;
    private int _running;

    public string VersionId { get; }
    public DownloadSource Source { get; init; } = DownloadSource.BmclApi;

    public OptiFineOnlineInstaller(
        string gameVersion,
        string edition,
        string minecraftRoot,
        string javaPath,
        string? versionId = null,
        int maxConcurrency = 4,
        int processorMemoryMb = 2048)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("当前安装入口只支持 Windows。");

        ValidateName(gameVersion);
        ValidateName(edition);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processorMemoryMb);

        _gameVersion = gameVersion;
        _edition = edition;
        _root = PathSafety.RequireDirectoryPath(minecraftRoot);
        _javaPath = PathSafety.Resolve(javaPath);
        _maxConcurrency = maxConcurrency;
        _processorMemoryMb = processorMemoryMb;
        VersionId = versionId ?? gameVersion + "-OptiFine-" + edition;
        ValidateName(VersionId);

        if (_root.Contains(';'))
            throw new NotSupportedException("Minecraft 根目录不能包含分号。");

        if (PathSafety.Comparer.Equals(VersionId, gameVersion))
            throw new ArgumentException("实例 ID 不能与原版相同。");
    }

    public async Task<OptiFineInstallResult> InstallAsync(
        Action<OptiFineInstallProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSource(Source);
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("这个安装器正在执行安装。");

        try
        {
            return await Task.Run(
                () => InstallCoreAsync(onProgress, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task<OptiFineInstallResult> InstallCoreAsync(
        Action<OptiFineInstallProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        onProgress?.Invoke(new OptiFineInstallProgress(
            OptiFineInstallStage.ReadingVersions, "读取 OptiFine 在线版本列表……"));

        var versions = await GetVersionsAsync(
            _gameVersion, Source, cancellationToken).ConfigureAwait(false);

        OptiFineInstallVersion selected =
            versions.SingleOrDefault(item => item.Edition == _edition)
            ?? throw new InvalidOperationException(
                $"所选服务没有 {_gameVersion} / {_edition}。");

        string libraries = PathSafety.RequireDirectoryPath(Path.Combine(_root, "libraries"));
        string packagePath = PathSafety.Resolve(Path.Combine(
            libraries,
            LibraryResolver.MavenToPath(
                $"optifine:installer:{_gameVersion}_{_edition}")));

        if (!PathSafety.IsWithin(packagePath, libraries))
            throw new InvalidDataException("安装包路径越出了 libraries。");

        await EnsurePackageAsync(
            selected, packagePath, onProgress, cancellationToken).ConfigureAwait(false);

        var installer = new OptiFineInstaller(
            packagePath, _root, _javaPath, VersionId,
            _maxConcurrency, _processorMemoryMb)
        {
            Source = this.Source
        };

        return await installer.InstallAsync(
            onProgress, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateName(string value)
    {
        PathSafety.ValidateWindowsRelativePath(value);

        if (value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("版本与实例 ID 必须是安全的单个名称。");
    }

    private static void ValidateSource(DownloadSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (ReferenceEquals(source, DownloadSource.Official))
            throw new NotSupportedException(
                "OptiFine 在线安装使用 BMCLAPI 或兼容服务；"
                + "官网下载的 JAR 请使用 OptiFineInstaller 本地安装入口。");
    }
}

