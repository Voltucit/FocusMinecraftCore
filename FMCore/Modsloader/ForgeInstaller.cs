using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

namespace FMCore.Modsloader;

public sealed partial class ForgeInstaller
{
    private readonly string _gameVersion;
    private readonly string _forgeVersion;
    private readonly string _root;
    private readonly string _javaPath;
    private readonly int _maxConcurrency;
    private readonly int _processorMemoryMb;
    private int _running;

    public string VersionId { get; }
    public DownloadSource Source { get; init; } = DownloadSource.Official;

    public ForgeInstaller(
        string gameVersion,
        string forgeVersion,
        string minecraftRoot,
        string javaPath,
        string? versionId = null,
        int maxConcurrency = 4,
        int processorMemoryMb = 2048)
        : this(
            false, gameVersion, forgeVersion, minecraftRoot, javaPath,
            versionId, maxConcurrency, processorMemoryMb)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("当前 Forge 安装只支持 Windows。");

        ValidateName(gameVersion);
        ValidateName(forgeVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processorMemoryMb);

        _gameVersion = gameVersion;
        _forgeVersion = forgeVersion;
        _root = PathSafety.RequireDirectoryPath(minecraftRoot);
        _javaPath = PathSafety.Resolve(javaPath);
        _maxConcurrency = maxConcurrency;
        _processorMemoryMb = processorMemoryMb;

        if (_root.Contains(';'))
            throw new NotSupportedException("Minecraft 根目录不能包含分号。");

        VersionId = versionId ?? $"{gameVersion}-forge-{forgeVersion}";
        ValidateName(VersionId);

        if (PathSafety.Comparer.Equals(VersionId, gameVersion))
            throw new ArgumentException("Forge 实例 ID 不能与原版相同。");
    }

    public Task<ForgeInstallResult> InstallAsync(
        Action<ForgeInstallProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
        => InstallWithSourceAsync(Source, onProgress, cancellationToken);

    internal async Task<ForgeInstallResult> InstallWithSourceAsync(
        DownloadSource source,
        Action<ForgeInstallProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("这个安装器正在执行安装。");

        try
        {
            return await Task.Run(
                () => InstallCoreAsync(source, onProgress, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }


    private async Task<ForgeInstallResult> InstallCoreAsync(
        DownloadSource source,
        Action<ForgeInstallProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Report(
            ForgeInstallStage stage,
            string message,
            DownloadProgress? single = null,
            DownloadBatchProgress? batch = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(new ForgeInstallProgress(stage, message, single, batch));
        }

        var (directory, jsonPath) = GetTargets();
        MinecraftVersionReader reader = new();

        IReadOnlyList<MinecraftVersion> installed = await reader.GetVersionsAsync(
            _root, cancellationToken).ConfigureAwait(false);

        MinecraftVersion[] parents = installed
            .Where(item => item.Id == _gameVersion).ToArray();

        if (parents.Length > 1)
            throw new InvalidDataException("原版版本 ID 存在重名冲突。");

        if (parents.Length == 1 && parents[0].InheritsFrom is not null)
            throw new NotSupportedException("Forge 安装需要原版游戏作为父版本。");

        MinecraftVersion[] targets = installed
            .Where(item => item.Id == VersionId).ToArray();

        if (targets.Length > 1 || targets.Any(item =>
            !PathSafety.Comparer.Equals(
                PathSafety.Resolve(item.Directory), directory)))
        {
            throw new InvalidDataException("此实例 ID 已被其他目录使用。");
        }

        Report(ForgeInstallStage.CheckingJava, "探测安装处理器使用的 Java……");
        JavaInfo java = await JavaReader.GetJavaInfoAsync(
            _javaPath, cancellationToken).ConfigureAwait(false);

        Architecture architecture = java.Architecture switch
        {
            "x86" => Architecture.X86,
            "x64" => Architecture.X64,
            "arm64" => Architecture.Arm64,
            _ => throw new NotSupportedException("无法确定所选 Java 的架构。")
        };

        Report(ForgeInstallStage.DownloadingInstaller, "读取 Forge 安装包下载声明……");
        DownloadRequest installerRequest = DownloadSourcePolicy.Apply(
            await ReadInstallerRequestAsync(
                cancellationToken).ConfigureAwait(false),
            source, replaceInvalidFiles: false);

        _ = await FileDownloader.DownloadAsync(
            installerRequest,
            progress => Report(
                ForgeInstallStage.DownloadingInstaller, "Forge 安装包", single: progress),
            cancellationToken).ConfigureAwait(false);

        using ZipArchive archive = ZipFile.OpenRead(installerRequest.FilePath);

        JsonObject install = await ReadJsonEntryAsync(
            archive, "install_profile.json", cancellationToken).ConfigureAwait(false);

        int? spec = install["spec"]?.GetValue<int>();
        bool supported = spec == 1
            || (!_isNeoForge && spec == 0
                && install["processors"] is JsonArray
                && install["data"] is JsonObject);
        if (!supported || ReadText(install, "minecraft") != _gameVersion)
        {
            throw new NotSupportedException(
                "当前入口支持匹配游戏版本、带补丁处理步骤的 Forge spec=0 或 spec=1 安装包。");
        }

        ValidatePackageIdentity(install);

        JsonObject profile = await ReadJsonEntryAsync(
            archive, ReadText(install, "json"), cancellationToken).ConfigureAwait(false);

        if (ReadText(profile, "id") != ReadText(install, "version")
            || ReadText(profile, "inheritsFrom") != _gameVersion)
        {
            throw new InvalidDataException("Forge 客户端配置与安装包不匹配。");
        }

        _ = ReadText(profile, "mainClass");
        profile["id"] = VersionId;
        CheckExistingProfile(directory, jsonPath, profile);

        Report(ForgeInstallStage.PreparingBaseGame, "准备原版客户端与资源……");

        if (parents.Length == 0)
        {
            MinecraftInstaller vanilla = new(
                _gameVersion, _root, _maxConcurrency, architecture)
            {
                Source = source
            };

            _ = await vanilla.InstallAsync(
                progress => Report(
                    ForgeInstallStage.PreparingBaseGame,
                    progress.Message, batch: progress.Download),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            GameFileCompleter vanilla = new(
                _gameVersion, _root, _maxConcurrency,
                targetArchitecture: architecture)
            {
                Source = source
            };

            _ = await vanilla.CompleteAsync(
                progress => Report(
                    ForgeInstallStage.PreparingBaseGame,
                    progress.Message, batch: progress.Download),
                cancellationToken).ConfigureAwait(false);
        }

        MinecraftVersion parent = await reader.GetVersionAsync(
            _gameVersion, _root, cancellationToken).ConfigureAwait(false);

        JsonObject vanillaJson = MinecraftVersionReader.ReadVersionJson(parent.Directory);

        if (vanillaJson["javaVersion"] is JsonNode requiredNode)
        {
            if (requiredNode is not JsonObject requiredJava
                || requiredJava["majorVersion"] is not JsonValue major
                || !major.TryGetValue<int>(out int required) || required <= 0)
                throw new InvalidDataException("原版 Java 版本声明无效。");

            if (java.MajorVersion < required)
                throw new NotSupportedException($"当前游戏要求至少 Java {required}。");
        }

        string clientJar = LibraryResolver.GetClientJarSource(parent, _root).FilePath;
        var plan = ReadLibraryPlan(install, profile, source);
        await RestoreEmbeddedLibrariesAsync(
            archive, plan.Generated, cancellationToken).ConfigureAwait(false);

        Report(ForgeInstallStage.DownloadingLibraries, "下载并校验 Forge 和处理器依赖……");

        _ = await FileDownloader.DownloadManyAsync(
            plan.Downloads,
            _maxConcurrency,
            progress => Report(
                ForgeInstallStage.DownloadingLibraries, "Forge 依赖库", batch: progress),
            cancellationToken).ConfigureAwait(false);

        Dictionary<string, string> data = await ReadDataAsync(
            archive, install, installerRequest.FilePath, clientJar,
            cancellationToken).ConfigureAwait(false);

        await RunProcessorsAsync(
            install, data, java.Executable,
            message => Report(ForgeInstallStage.RunningProcessors, message),
            cancellationToken).ConfigureAwait(false);

        if (_isNeoForge)
        {
            Report(
                ForgeInstallStage.RunningProcessors,
                "检查 NeoForge 客户端产物……");

            await CheckNeoForgeOutputAsync(
                data, cancellationToken).ConfigureAwait(false);
        }

        foreach (ExpectedFile file in plan.Generated)
            await RequireFileAsync(file, cancellationToken).ConfigureAwait(false);

        Report(ForgeInstallStage.WritingProfile, "保存 Forge 实例配置……");
        CheckExistingProfile(directory, jsonPath, profile);

        if (!File.Exists(jsonPath))
        {
            await WriteNewFileAsync(
                jsonPath,
                JsonSerializer.SerializeToUtf8Bytes(
                    profile, new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken).ConfigureAwait(false);
        }

        Report(ForgeInstallStage.CompletingResources, "校验实例资源与日志配置……");

        GameResourceCompleter resources = new(
            VersionId, _root, _maxConcurrency)
        {
            Source = source
        };

        _ = await resources.CompleteAsync(
            progress => Report(
                ForgeInstallStage.CompletingResources,
                progress.Message, batch: progress.Download),
            cancellationToken).ConfigureAwait(false);

        Report(ForgeInstallStage.Completed, "Forge 客户端安装完成。");
        cancellationToken.ThrowIfCancellationRequested();

        return new ForgeInstallResult(
            VersionId, _gameVersion, _forgeVersion, _root, directory);
    }

    private (string Directory, string JsonPath) GetTargets()
    {
        string versions = PathSafety.RequireDirectoryPath(Path.Combine(_root, "versions"));
        string directory = PathSafety.RequireDirectoryPath(Path.Combine(versions, VersionId));

        if (!PathSafety.Comparer.Equals(Path.GetDirectoryName(directory), versions)
            || !PathSafety.Comparer.Equals(Path.GetFileName(directory), VersionId))
            throw new InvalidDataException("实例必须位于 versions 下对应名称的直接子目录。");

        string jsonPath = PathSafety.Resolve(Path.Combine(directory, VersionId + ".json"));

        if (!PathSafety.IsWithin(jsonPath, directory) || Directory.Exists(jsonPath))
            throw new InvalidDataException("Forge 配置路径无效或被目录占用。");

        return (directory, jsonPath);
    }

    private string ArtifactPath(string coordinate)
    {
        string libraries = PathSafety.RequireDirectoryPath(Path.Combine(_root, "libraries"));
        string path = PathSafety.Resolve(
            Path.Combine(libraries, LibraryResolver.MavenToPath(coordinate)));

        if (!PathSafety.IsWithin(path, libraries))
            throw new InvalidDataException("Forge 依赖路径越出了 libraries。");

        return path;
    }

    private static void ValidateName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        PathSafety.ValidateWindowsRelativePath(value);

        if (value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("版本和实例 ID 必须是单个名称。");
    }

    private static string ReadText(JsonObject value, string key)
        => MinecraftVersionReader.ReadRequiredString(
            value, key, $"Forge 配置缺少有效的 {key}。");
}

