using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Installer;

namespace FMCore.Java;

public sealed partial class JavaInstaller
{
    private readonly int _majorVersion;
    private readonly string _component;
    private readonly string _installDirectory;
    private readonly int _maxConcurrency;

    private int _running;

    public JavaInstaller(
        int majorVersion,
        string installDirectory,
        int maxConcurrency = 4)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            majorVersion);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            maxConcurrency);

        if (!OperatingSystem.IsWindows()
            || RuntimeInformation.OSArchitecture
                != Architecture.X64)
        {
            throw new PlatformNotSupportedException(
                "当前 Java 安装只支持 Windows x64。");
        }

        _component = majorVersion switch
        {
            8 => "jre-legacy",
            16 => "java-runtime-alpha",
            17 => "java-runtime-gamma",
            21 => "java-runtime-delta",
            25 => "java-runtime-epsilon",

            _ => throw new NotSupportedException(
                "当前支持选择 Java 8、16、17、21 或 25。")
        };

        _majorVersion = majorVersion;
        _maxConcurrency = maxConcurrency;

        _installDirectory =
            PathSafety.RequireDirectoryPath(installDirectory);
    }

    public async Task<JavaInstallResult> InstallAsync(
        Action<JavaInstallProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(
                ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "这个 Java 安装器正在执行操作。");
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

    private async Task<JavaInstallResult> InstallCoreAsync(
        Action<JavaInstallProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Report(
            JavaInstallStage stage,
            string message,
            DownloadBatchProgress? download = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            onProgress?.Invoke(new JavaInstallProgress(
                stage,
                message,
                download));
        }

        Report(
            JavaInstallStage.ReadingMetadata,
            $"读取 Java {_majorVersion} 官方运行时清单……");

        JsonObject catalog = await ReadJsonAsync(
            CatalogUrl,
            cancellationToken).ConfigureAwait(false);

        if (catalog["windows-x64"]?[_component]
                is not JsonArray runtimes
            || runtimes.Count == 0
            || runtimes[0] is not JsonObject runtime
            || runtime["manifest"] is not JsonObject declaration)
        {
            throw new NotSupportedException(
                $"官方清单没有可用的 Java {_majorVersion} Windows x64 运行时。");
        }

        JsonObject manifest = await ReadJsonAsync(
            ReadText(declaration, "url"),
            cancellationToken,
            declaration).ConfigureAwait(false);

        var plan = ReadPlan(
            manifest,
            cancellationToken);

        Report(
            JavaInstallStage.DownloadingFiles,
            $"安装 Java {_majorVersion} 文件……");

        foreach (string directory in plan.Directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(directory);
        }

        IReadOnlyList<DownloadResult> files =
            await FileDownloader.DownloadManyAsync(
                plan.Requests,
                _maxConcurrency,
                progress => Report(
                    JavaInstallStage.DownloadingFiles,
                    "Java 运行时文件",
                    progress),
                cancellationToken).ConfigureAwait(false);

        Report(
            JavaInstallStage.VerifyingJava,
            "执行 Java 版本与架构检查……");

        string executable = Path.Combine(
            _installDirectory,
            "bin",
            "java.exe");

        JavaInfo java = await JavaReader.GetJavaInfoAsync(
            executable,
            cancellationToken).ConfigureAwait(false);

        if (java.MajorVersion != _majorVersion
            || java.Architecture != "x64")
        {
            throw new InvalidDataException(
                $"安装结果不符合要求：Java {java.Version}，" +
                $"架构 {java.Architecture ?? "未知"}。");
        }

        JavaInstallResult result = new(
            java,
            _installDirectory,
            files.Count,
            files.Count(file => !file.AlreadyPresent),
            files.Count(file => file.AlreadyPresent));

        Report(
            JavaInstallStage.Completed,
            $"Java {java.Version} 安装并验证完成。");

        return result;
    }
}
