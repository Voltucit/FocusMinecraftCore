using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Downloads;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

namespace FMCore.Modsloader;

public sealed partial class OptiFineInstaller
{
    private readonly string _installerPath;
    private readonly string _root;
    private readonly string _javaPath;
    private readonly string? _versionId;
    private readonly int _maxConcurrency;
    private readonly int _processorMemoryMb;
    private int _running;

    public DownloadSource Source { get; init; } = DownloadSource.Official;

    public OptiFineInstaller(
        string installerJarPath,
        string minecraftRoot,
        string javaPath,
        string? versionId = null,
        int maxConcurrency = 4,
        int processorMemoryMb = 2048)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("当前安装入口只支持 Windows。");

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processorMemoryMb);

        _installerPath = PathSafety.Resolve(installerJarPath);
        _root = PathSafety.RequireDirectoryPath(minecraftRoot);
        _javaPath = PathSafety.Resolve(javaPath);
        _versionId = versionId;
        _maxConcurrency = maxConcurrency;
        _processorMemoryMb = processorMemoryMb;

        if (_root.Contains(';') || _installerPath.Contains(';'))
            throw new NotSupportedException("根目录和安装包路径不能包含分号。");

        if (versionId is not null)
            ValidateName(versionId);
    }

    public async Task<OptiFineInstallResult> InstallAsync(
        Action<OptiFineInstallProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(Source);
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
        void Report(
            OptiFineInstallStage stage,
            string message,
            DownloadBatchProgress? download = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(new OptiFineInstallProgress(stage, message, download));
        }

        Report(OptiFineInstallStage.ReadingInstaller, "读取 OptiFine 安装包……");

        using ZipArchive archive = ZipFile.OpenRead(_installerPath);
        var (gameVersion, edition) = await ReadInstallerVersionAsync(
            archive, cancellationToken).ConfigureAwait(false);
        string optiFineVersion = gameVersion + "_" + edition;
        string versionId = _versionId ?? gameVersion + "-OptiFine-" + edition;

        ValidateName(versionId);

        if (PathSafety.Comparer.Equals(versionId, gameVersion))
            throw new ArgumentException("实例 ID 不能与原版相同。");

        string wrapperVersion = Encoding.UTF8.GetString(
            await ReadEntryAsync(
                archive, "launchwrapper-of.txt", 256,
                cancellationToken).ConfigureAwait(false)).Trim();

        if (!Regex.IsMatch(wrapperVersion, @"\A[0-9]+(?:\.[0-9]+)+\z"))
            throw new InvalidDataException("launchwrapper-of 版本无效。");

        byte[] wrapper = await ReadEntryAsync(
            archive, "launchwrapper-of-" + wrapperVersion + ".jar",
            4 * 1024 * 1024, cancellationToken).ConfigureAwait(false);

        using (MemoryStream buffer = new(wrapper, writable: false))
        using (ZipArchive wrapperArchive = new(buffer, ZipArchiveMode.Read))
        {
            if (wrapperArchive.GetEntry("net/minecraft/launchwrapper/Launch.class") is null)
                throw new InvalidDataException("内嵌启动库缺少 Launch 类。");
        }

        string versionsRoot = PathSafety.RequireDirectoryPath(
            Path.Combine(_root, "versions"));
        string directory = PathSafety.RequireDirectoryPath(
            Path.Combine(versionsRoot, versionId));

        if (!PathSafety.Comparer.Equals(Path.GetDirectoryName(directory), versionsRoot))
            throw new InvalidDataException("实例目录必须直接位于 versions 下。");

        string jsonPath = PathSafety.Resolve(
            Path.Combine(directory, versionId + ".json"));

        if (!PathSafety.IsWithin(jsonPath, directory) || Directory.Exists(jsonPath))
            throw new InvalidDataException("实例配置路径无效或被目录占用。");

        MinecraftVersionReader reader = new();
        var installed = await reader.GetVersionsAsync(
            _root, cancellationToken).ConfigureAwait(false);

        var parents = installed.Where(item => item.Id == gameVersion).ToArray();

        if (parents.Length > 1)
            throw new InvalidDataException("原版版本 ID 存在重名冲突。");

        if (parents.Length == 1 && parents[0].InheritsFrom is not null)
            throw new NotSupportedException("OptiFine 需要原版作为父版本。");

        var targets = installed.Where(item => item.Id == versionId).ToArray();

        if (targets.Length > 1 || targets.Any(item =>
            !PathSafety.Comparer.Equals(
                PathSafety.Resolve(item.Directory), directory)))
            throw new InvalidDataException("此实例 ID 已被其他目录使用。");

        Report(OptiFineInstallStage.CheckingJava, "探测补丁工具使用的 Java……");
        JavaInfo java = await JavaReader.GetJavaInfoAsync(
            _javaPath, cancellationToken).ConfigureAwait(false);

        Architecture architecture = java.Architecture switch
        {
            "x86" => Architecture.X86,
            "x64" => Architecture.X64,
            "arm64" => Architecture.Arm64,
            _ => throw new NotSupportedException("无法确定 Java 架构。")
        };

        Report(OptiFineInstallStage.PreparingBaseGame, "准备原版客户端与资源……");

        if (parents.Length == 0)
        {
            MinecraftInstaller vanilla = new(
                gameVersion, _root, _maxConcurrency, architecture)
            {
                Source = this.Source
            };

            _ = await vanilla.InstallAsync(
                progress => Report(
                    OptiFineInstallStage.PreparingBaseGame,
                    progress.Message, progress.Download),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            GameFileCompleter vanilla = new(
                gameVersion, _root, _maxConcurrency,
                targetArchitecture: architecture)
            {
                Source = this.Source
            };

            _ = await vanilla.CompleteAsync(
                progress => Report(
                    OptiFineInstallStage.PreparingBaseGame,
                    progress.Message, progress.Download),
                cancellationToken).ConfigureAwait(false);
        }

        MinecraftVersion parent = await reader.GetVersionAsync(
            gameVersion, _root, cancellationToken).ConfigureAwait(false);

        JsonObject parentJson =
            MinecraftVersionReader.ReadVersionJson(parent.Directory);

        if (parentJson["arguments"] is not JsonObject)
            throw new NotSupportedException("本入口先支持新版 arguments 格式。");

        if (parentJson["javaVersion"] is JsonNode requiredNode)
        {
            if (requiredNode is not JsonObject requiredJava
                || requiredJava["majorVersion"] is not JsonValue major
                || !major.TryGetValue<int>(out int required) || required <= 0)
                throw new InvalidDataException("原版 Java 版本声明无效。");

            if (java.MajorVersion < required)
                throw new NotSupportedException($"当前游戏要求至少 Java {required}。");
        }

        string clientJar =
            LibraryResolver.GetClientJarSource(parent, _root).FilePath;
        string coordinate = "optifine:OptiFine:" + optiFineVersion;
        string wrapperCoordinate = "optifine:launchwrapper-of:" + wrapperVersion;
        string outputPath = LibraryPath(coordinate);
        string wrapperPath = LibraryPath(wrapperCoordinate);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        string temporary = Path.Combine(
            Path.GetDirectoryName(outputPath)!,
            ".optifine-patch-" + Guid.NewGuid().ToString("N") + ".jar");

        Exception? failure = null;

        try
        {
            Report(OptiFineInstallStage.ApplyingPatches, "生成 OptiFine 客户端库……");

            await RunPatcherAsync(
                java.Executable, clientJar, temporary,
                message => Report(OptiFineInstallStage.ApplyingPatches, message),
                cancellationToken).ConfigureAwait(false);

            using (ZipArchive patched = ZipFile.OpenRead(temporary))
            {
                ZipArchiveEntry tweaker =
                    patched.GetEntry("optifine/OptiFineTweaker.class")
                    ?? throw new InvalidDataException("补丁产物缺少 OptiFineTweaker。");

                await using Stream stream = tweaker.Open();
                byte[] magic = new byte[4];

                await stream.ReadExactlyAsync(
                    magic.AsMemory(), cancellationToken).ConfigureAwait(false);

                if (!magic.AsSpan().SequenceEqual(
                        new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }))
                    throw new InvalidDataException("补丁产物的 Tweaker 类无效。");
            }

            byte[] patchedBytes = await ReadPatchedJarAsync(
                temporary, 64 * 1024 * 1024,
                cancellationToken).ConfigureAwait(false);

            JsonObject profile = new()
            {
                ["id"] = versionId,
                ["inheritsFrom"] = gameVersion,
                ["type"] = "release",
                ["mainClass"] = "net.minecraft.launchwrapper.Launch",
                ["arguments"] = new JsonObject
                {
                    ["game"] = new JsonArray("--tweakClass", "optifine.OptiFineTweaker")
                },
                ["libraries"] = new JsonArray(
                    LocalLibrary(coordinate, patchedBytes),
                    LocalLibrary(wrapperCoordinate, wrapper))
            };

            CheckProfile(directory, jsonPath, profile);

            Report(OptiFineInstallStage.WritingFiles, "保存 OptiFine 依赖与实例配置……");

            await PublishAsync(
                outputPath, patchedBytes, cancellationToken).ConfigureAwait(false);
            await PublishAsync(
                wrapperPath, wrapper, cancellationToken).ConfigureAwait(false);

            CheckProfile(directory, jsonPath, profile);

            if (!File.Exists(jsonPath))
            {
                await PublishAsync(
                    jsonPath,
                    JsonSerializer.SerializeToUtf8Bytes(
                        profile, new JsonSerializerOptions { WriteIndented = true }),
                    cancellationToken).ConfigureAwait(false);
            }

            Report(OptiFineInstallStage.Completed, "OptiFine 客户端安装完成。");

            return new OptiFineInstallResult(
                versionId, gameVersion, optiFineVersion, _root, directory);
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
            catch (Exception cleanup) when (failure is not null)
            {
                throw new AggregateException(
                    "OptiFine 安装失败，补丁临时文件清理也失败。", failure, cleanup);
            }
        }
    }

    private async Task RunPatcherAsync(
        string javaPath,
        string clientJar,
        string target,
        Action<string> onLog,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo start = new()
        {
            FileName = javaPath,
            WorkingDirectory = _root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (string argument in new[]
        {
            $"-Xmx{_processorMemoryMb}M",
            "-Dfile.encoding=UTF-8",
            "-cp", _installerPath,
            "optifine.Patcher",
            clientJar, _installerPath, target
        })
            start.ArgumentList.Add(argument);

        foreach (string key in new[]
                 { "JAVA_TOOL_OPTIONS", "JDK_JAVA_OPTIONS", "_JAVA_OPTIONS" })
            start.Environment.Remove(key);

        using CancellationTokenSource deadline = new(TimeSpan.FromMinutes(10));
        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, deadline.Token);

        try
        {
            int exitCode = await Launcher.RunProcessAsync(
                start, onLog, onLog, linked.Token,
                onProgress: null).ConfigureAwait(false);

            if (exitCode != 0)
                throw new InvalidOperationException(
                    $"OptiFine 补丁工具失败，退出码 {exitCode}。");
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested
                && deadline.IsCancellationRequested)
        {
            throw new TimeoutException("OptiFine 补丁处理超过 10 分钟。", error);
        }
    }
}

