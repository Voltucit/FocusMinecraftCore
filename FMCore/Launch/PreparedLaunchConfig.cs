using FMCore.Accounts;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

/// <summary>启动配置。构造配置时不会启动游戏。</summary>
public sealed class PreparedLaunchConfig
{
    public MinecraftVersion Version { get; }
    public JavaInfo Java { get; }
    public IMinecraftAccount Account { get; }
    public int MaxMemoryMb { get; }
    public string GameDirectory { get; }

    public int? WindowWidth { get; }
    public int? WindowHeight { get; }

    public IReadOnlyList<string> JvmArguments { get; }

    public string? AuthlibInjectorPath { get; }

    /// <summary>仅用于版本 JSON 中非标准占位符；标准占位符始终由启动配置生成。</summary>
    public IReadOnlyDictionary<string, string> PlaceholderOverrides { get; }

    public Action<string>? UnknownPlaceholderReporter { get; }

    public PreparedLaunchConfig(
        MinecraftVersion version,
        JavaInfo java,
        IMinecraftAccount account,
        int maxMemoryMb,
        string? gameDirectory = null,
        int? windowWidth = null,
        int? windowHeight = null,
        IEnumerable<string>? jvmArguments = null,
        string? authlibInjectorPath = null,
        IReadOnlyDictionary<string, string>? placeholderOverrides = null,
        Action<string>? unknownPlaceholderReporter = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(java);
        ArgumentNullException.ThrowIfNull(account);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            maxMemoryMb);

        if (windowWidth.HasValue != windowHeight.HasValue)
            throw new ArgumentException(
                "窗口宽度和高度必须同时设置，或同时省略。");

        if (windowWidth is <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(windowWidth),
                "窗口宽度必须大于 0。");

        if (windowHeight is <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(windowHeight),
                "窗口高度必须大于 0。");

        Version = version;
        Java = java;
        Account = account;
        MaxMemoryMb = maxMemoryMb;

        GameDirectory = PathSafety.RequireDirectoryPath(
            gameDirectory ?? version.Directory);

        WindowWidth = windowWidth;
        WindowHeight = windowHeight;

        JvmArguments = CopyJvmArguments(jvmArguments);
        PlaceholderOverrides = CopyPlaceholderOverrides(placeholderOverrides);
        UnknownPlaceholderReporter = unknownPlaceholderReporter;

        if (authlibInjectorPath is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(authlibInjectorPath);

            string agentPath = PathSafety.Resolve(authlibInjectorPath);

            if (agentPath.Contains('=') || agentPath.Contains('\0'))
            {
                throw new ArgumentException(
                    "authlib-injector 路径不能包含等号或空字符。",
                    nameof(authlibInjectorPath));
            }

            if (!File.Exists(agentPath))
                throw new FileNotFoundException("找不到 authlib-injector JAR。", agentPath);

            AuthlibInjectorPath = agentPath;
        }

        if (account is YggdrasilAccount && AuthlibInjectorPath is null)
        {
            throw new ArgumentException(
                "外置验证账号启动时必须设置 AuthlibInjectorPath。",
                nameof(authlibInjectorPath));
        }

    }


    private static IReadOnlyDictionary<string, string> CopyPlaceholderOverrides(
        IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> copy = new(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key)
                || pair.Key.Contains("${", StringComparison.Ordinal)
                || pair.Key.Contains('}', StringComparison.Ordinal)
                || pair.Key.Any(char.IsWhiteSpace)
                || pair.Key.Any(char.IsControl))
                throw new ArgumentException("占位符名称无效。", nameof(values));
            if (pair.Value is null || pair.Value.Contains('\0'))
                throw new ArgumentException("占位符覆盖值不能为 null 或包含空字符。", nameof(values));
            copy[pair.Key] = pair.Value;
        }
        return copy.AsReadOnly();
    }

    private static IReadOnlyList<string> CopyJvmArguments(
        IEnumerable<string>? jvmArguments)
    {
        List<string> result = [];

        if (jvmArguments is null)
            return result.AsReadOnly();

        string[] reservedOptions =
        [
            "-cp",
            "-classpath",
            "--class-path",
            "-jar",
            "-m",
            "--module",
            "-Djava.class.path",
            "-Djava.library.path",
            "-Dfile.encoding",
            "-Dlog4j.configurationFile",
            "-Dauthlibinjector.yggdrasil.prefetched",
        ];

        foreach (string argument in jvmArguments)
        {
            if (string.IsNullOrWhiteSpace(argument)
                || argument.Contains('\0'))
            {
                throw new ArgumentException(
                    "JVM 参数不能是空白，也不能包含空字符。",
                    nameof(jvmArguments));
            }

            if (!argument.StartsWith(
                    "-", StringComparison.Ordinal)
                || argument == "--")
            {
                throw new ArgumentException(
                    "每项必须是一条完整的 JVM 选项。" +
                    "需要取值的长参数请使用 --名称=值。",
                    nameof(jvmArguments));
            }

            bool reserved =
                argument.StartsWith(
                    "-Xmx", StringComparison.Ordinal)
                || reservedOptions.Any(option =>
                    argument == option
                    || argument.StartsWith(
                        option + "=",
                        StringComparison.Ordinal));

            if (reserved)
            {
                throw new ArgumentException(
                    $"参数 {argument} 由启动配置或基础启动流程管理，" +
                    "请勿通过额外 JVM 参数重复设置。",
                    nameof(jvmArguments));
            }

            result.Add(argument);
        }

        return result.AsReadOnly();
    }

    public bool Fullscreen { get; init; }

    private IReadOnlyList<string> _gameArguments = Array.Empty<string>();

    public IReadOnlyList<string> GameArguments
    {
        get => _gameArguments;
        init
        {
            ArgumentNullException.ThrowIfNull(value);

            string[] arguments = value.ToArray();

            if (arguments.Any(argument =>
                    argument is null || argument.Contains('\0')))
            {
                throw new ArgumentException(
                    "游戏参数不能为 null，也不能包含空字符。",
                    nameof(GameArguments));
            }

            _gameArguments = Array.AsReadOnly(arguments);
        }
    }
}
