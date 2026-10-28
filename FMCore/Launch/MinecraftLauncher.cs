using FMCore.Installer;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

public sealed partial class MinecraftLauncher
{
    private readonly LaunchConfig _config;
    private readonly string _root;
    private readonly string _javaPath;
    private readonly string? _gameDirectory;
    private readonly string[] _jvmArguments;
    private readonly string? _authlibInjectorPath;
    private int _running;

    public MinecraftLauncher(LaunchConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(config.Account);
        ArgumentNullException.ThrowIfNull(config.GameCoreConfig);
        ArgumentNullException.ThrowIfNull(config.JavaConfig);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            config.GameCoreConfig.Version);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            config.JavaConfig.MaxMemoryMb);

        _config = config;

        // 在创建启动器时确定路径，不受之后工作目录变化影响。
        _root = PathSafety.RequireDirectoryPath(
            config.GameCoreConfig.Root);

        _javaPath = PathSafety.Resolve(
            config.JavaConfig.JavaPath);

        _gameDirectory =
            config.GameCoreConfig.GameDirectory is null
                ? null
                : PathSafety.RequireDirectoryPath(
                    config.GameCoreConfig.GameDirectory);

        // 复制集合，避免外部修改原列表影响本次启动设置。
        _jvmArguments =
            config.JavaConfig.JvmArguments?.ToArray() ?? [];

        _authlibInjectorPath = config.JavaConfig.AuthlibInjectorPath is null
            ? null
            : PathSafety.Resolve(config.JavaConfig.AuthlibInjectorPath);
    }

    public async Task<int> LaunchAsync(
        Action<LaunchProgress>? onProgress = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(
                ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "这个启动器实例正在检查、启动或运行游戏，请等待结束。");
        }

        try
        {
            // 同步文件解析也在后台执行，便于界面按钮直接 await。
            return await Task.Run(
                () => LaunchCoreAsync(
                    onProgress,
                    onOutput,
                    onError,
                    cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task<int> LaunchCoreAsync(
        Action<LaunchProgress>? onProgress,
        Action<string>? onOutput,
        Action<string>? onError,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        onProgress?.Invoke(new LaunchProgress(
            LaunchStage.ReadingVersion,
            "正在读取所选版本。"));

        MinecraftVersionReader reader = new();

        MinecraftVersion version =
            await reader.GetVersionAsync(
                _config.GameCoreConfig.Version,
                _root,
                cancellationToken).ConfigureAwait(false);

        onProgress?.Invoke(new LaunchProgress(
            LaunchStage.DetectingJava,
            "正在探测所选 Java。"));

        JavaInfo java =
            await JavaReader.GetJavaInfoAsync(
                _javaPath,
                cancellationToken).ConfigureAwait(false);

        PreparedLaunchConfig prepared = new(
            version,
            java,
            _config.Account,
            _config.JavaConfig.MaxMemoryMb,
            gameDirectory: _gameDirectory,
            windowWidth: _config.GameWindowConfig?.Width,
            windowHeight: _config.GameWindowConfig?.Height,
            jvmArguments: _jvmArguments,
            authlibInjectorPath: _authlibInjectorPath);

        return await Launcher.RunWithProgressAsync(
            prepared,
            _root,
            onProgress,
            onOutput,
            onError,
            cancellationToken).ConfigureAwait(false);
    }
}
