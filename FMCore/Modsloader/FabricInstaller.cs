using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Modsloader;

public sealed partial class FabricInstaller
{
    private readonly string _gameVersion;
    private readonly string _loaderVersion;
    private readonly string _minecraftRoot;
    private readonly int _maxConcurrency;
    private readonly Architecture _architecture;
    private int _running;

    public string VersionId { get; }

    public DownloadSource Source { get; init; } = DownloadSource.Official;

    public FabricInstaller(
        string gameVersion,
        string loaderVersion,
        string minecraftRoot,
        string? versionId = null,
        int maxConcurrency = 4,
        Architecture? targetArchitecture = null)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("当前安装流程只支持 Windows。");

        ValidateName(gameVersion, nameof(gameVersion));
        ValidateName(loaderVersion, nameof(loaderVersion));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);

        _gameVersion = gameVersion;
        _loaderVersion = loaderVersion;
        _maxConcurrency = maxConcurrency;
        _minecraftRoot = PathSafety.RequireDirectoryPath(minecraftRoot);

        if (_minecraftRoot.Contains(';'))
            throw new NotSupportedException("Minecraft 根目录不能包含分号。");

        _architecture = targetArchitecture ?? RuntimeInformation.OSArchitecture;

        if (_architecture is not (
                Architecture.X86 or Architecture.X64 or Architecture.Arm64))
        {
            throw new NotSupportedException("目标架构只支持 x86、x64 或 arm64。");
        }

        VersionId = versionId ?? $"{gameVersion}-Fabric-{loaderVersion}";
        ValidateName(VersionId, nameof(versionId));

        if (PathSafety.Comparer.Equals(VersionId, gameVersion))
            throw new ArgumentException("Fabric 实例 ID 不能与原版版本相同。");
    }

    public async Task<FabricInstallResult> InstallAsync(
        Action<FabricInstallProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Source);

        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("这个 Fabric 安装器正在执行安装。");

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

    private async Task<FabricInstallResult> InstallCoreAsync(
        Action<FabricInstallProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Report(
            FabricInstallStage stage,
            string message,
            DownloadBatchProgress? download = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(new FabricInstallProgress(stage, message, download));
        }

        var (directory, jsonPath) = GetTargets();

        MinecraftVersionReader reader = new();
        IReadOnlyList<MinecraftVersion> installed =
            await reader.GetVersionsAsync(
                _minecraftRoot, cancellationToken).ConfigureAwait(false);

        MinecraftVersion[] parents = installed
            .Where(item => item.Id == _gameVersion).ToArray();

        if (parents.Length > 1)
            throw new InvalidDataException("原版版本 ID 存在重名冲突。");

        if (parents.Length == 1 && parents[0].InheritsFrom is not null)
            throw new NotSupportedException("Fabric 安装需要原版游戏作为父版本。");

        MinecraftVersion[] targets = installed
            .Where(item => item.Id == VersionId).ToArray();

        if (targets.Length > 1
            || targets.Any(item => !PathSafety.Comparer.Equals(
                PathSafety.Resolve(item.Directory), directory)))
        {
            throw new InvalidDataException(
                "此实例 ID 已被其他目录使用，请指定不同的 versionId。");
        }

        Report(FabricInstallStage.ReadingMetadata, "读取 Fabric 客户端配置……");

        JsonObject profile = await ReadProfileAsync(
            cancellationToken).ConfigureAwait(false);

        // 在安装原版之前发现已有实例配置冲突。
        CheckExistingProfile(directory, jsonPath, profile);

        if (parents.Length == 0)
        {
            Report(FabricInstallStage.InstallingBaseGame, "安装缺少的原版游戏……");

            MinecraftInstaller vanilla = new(
                _gameVersion,
                _minecraftRoot,
                _maxConcurrency,
                _architecture)
            {
                Source = this.Source
            };

            _ = await vanilla.InstallAsync(
                progress => Report(
                    FabricInstallStage.InstallingBaseGame,
                    progress.Message,
                    progress.Download),
                cancellationToken).ConfigureAwait(false);
        }

        Report(FabricInstallStage.WritingProfile, "保存 Fabric 实例配置……");

        await WriteProfileAsync(
            directory, jsonPath, profile, cancellationToken).ConfigureAwait(false);

        Report(FabricInstallStage.CompletingFiles, "补全 Fabric 与游戏文件……");

        GameFileCompleter completer = new(
            VersionId,
            _minecraftRoot,
            _maxConcurrency,
            targetArchitecture: _architecture)
        {
            Source = this.Source
        };

        _ = await completer.CompleteAsync(
            progress => Report(
                FabricInstallStage.CompletingFiles,
                progress.Message,
                progress.Download),
            cancellationToken).ConfigureAwait(false);

        FabricInstallResult result = new(
            VersionId,
            _gameVersion,
            _loaderVersion,
            _minecraftRoot,
            directory);

        Report(FabricInstallStage.Completed, "Fabric 客户端安装完成。");
        cancellationToken.ThrowIfCancellationRequested();

        return result;
    }

    private (string Directory, string JsonPath) GetTargets()
    {
        string versions = PathSafety.RequireDirectoryPath(
            Path.Combine(_minecraftRoot, "versions"));

        string directory = PathSafety.RequireDirectoryPath(
            Path.Combine(versions, VersionId));

        if (!PathSafety.Comparer.Equals(
                Path.GetDirectoryName(directory), versions)
            || !PathSafety.Comparer.Equals(
                Path.GetFileName(directory), VersionId))
        {
            throw new InvalidDataException(
                "Fabric 实例必须位于 versions 下对应名称的直接子目录。");
        }

        string jsonPath = PathSafety.Resolve(
            Path.Combine(directory, VersionId + ".json"));

        if (!PathSafety.IsWithin(jsonPath, directory))
            throw new InvalidDataException("Fabric 配置路径越出了实例目录。");

        if (Directory.Exists(jsonPath))
            throw new IOException("Fabric 配置文件路径已被目录占用。");

        return (directory, jsonPath);
    }

    private static void ValidateName(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        PathSafety.ValidateWindowsRelativePath(value);

        if (value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("版本或实例 ID 必须是单个名称。", parameterName);
    }
}

