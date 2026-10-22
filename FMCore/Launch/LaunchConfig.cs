using System.Diagnostics;
using System.Text.Json.Serialization;
using FMCore.Authentication;
using FMCore.Downloader;
using FMCore.Java;
using FMCore.Instances;
using FMCore.Logging;
using FMCore.Versions;

namespace FMCore.Launch;

/// <summary>Shared game files and the working directory for one instance. Instance directories stay inside RootDirectory.</summary>
public sealed record GameCoreConfig
{
    [JsonIgnore] internal InstanceInfo? ManagedInstance { get; init; }
    /// <summary>Shared versions, libraries and assets root. Defaults to .minecraft under the current directory.</summary>
    public string RootDirectory { get; init; } = ".minecraft";
    /// <summary>The installed or official version ID, for example 1.16.5.</summary>
    public string VersionId { get; init; } = "";
    /// <summary>Use instances/{InstanceId or VersionId} for settings, saves and logs. Enabled by default.</summary>
    public bool Isolate { get; init; } = true;
    /// <summary>Optional safe instance name; defaults to VersionId.</summary>
    public string? InstanceId { get; init; }
    /// <summary>Explicit working directory relative to RootDirectory; overrides automatic isolation.</summary>
    public string? GameDirectory { get; init; }
    /// <summary>Automatically install/repair files before starting. False uses only cached metadata and verified local downloads.</summary>
    public bool AutoInstall { get; init; } = true;
    /// <summary>Metadata cache policy when automatic installation is enabled.</summary>
    public VersionCachePolicy CachePolicy { get; init; } = VersionCachePolicy.PreferCache;
}

/// <summary>Java 选择、独立的最小/最大内存及 JVM 参数；每项为独立参数，不拼接 Shell 命令。</summary>
[DebuggerDisplay("Java launch configuration")]
public sealed record JavaConfig
{
    /// <summary>Explicit Java executable/home. Takes priority over discovery; an incompatible path fails without fallback.</summary>
    public string? JavaPath { get; init; }
    /// <summary>最小堆内存（MiB），默认 512；设为 0 时不传 -Xms，由 JVM 自行决定。保留整数属性以兼容已有调用方。</summary>
    public int MinMemoryMb { get; init; } = 512;
    /// <summary>最大堆内存（MiB），默认 2048；范围 1–1048576，不得小于非零的 MinMemoryMb；不等于可用物理内存保证。</summary>
    public int MaxMemoryMb { get; init; } = 2048;
    /// <summary>自定义 JVM 参数，每项是一个完整参数，例如 -Dkey=value；内存使用统一属性，冲突选项明确拒绝，相同项去重。</summary>
    [JsonIgnore] public IReadOnlyList<string> Arguments { get; init; } = [];
    /// <summary>Advanced compatibility policy; metadata requirements are authoritative.</summary>
    public JavaSelectionOptions Selection { get; init; } = new();
    /// <summary>Bounded runtime discovery/probe settings.</summary>
    public JavaDetectionOptions Detection { get; init; } = new();
    /// <summary>Does not expose custom JVM arguments.</summary>
    public override string ToString() => "Java launch configuration";
}

/// <summary>实例游戏窗口配置；统一处理元数据中的默认参数和重复项。</summary>
public sealed record GameWindowConfig
{
    /// <summary>窗口宽度（像素），范围 1–16384，默认 854；全屏时实际显示模式由游戏和显示器决定。</summary>
    public int Width { get; init; } = 854;
    /// <summary>窗口高度（像素），范围 1–16384，默认 480。</summary>
    public int Height { get; init; } = 480;
    /// <summary>是否以全屏启动，默认 false；与自定义 --fullscreen 不同值时返回参数冲突。</summary>
    public bool Fullscreen { get; init; }
}

/// <summary>Unified, UI-independent launch configuration. Account is a T05 LaunchIdentity; Accounts optionally refreshes it just before launch.</summary>
[DebuggerDisplay("Minecraft launch configuration")]
public sealed record LaunchConfig
{
    /// <summary>用版本、目录、账号、已选 Java 和明确的内存上限创建配置；后续可用 with 渐进设置窗口、服务器和自定义参数。</summary>
    /// <param name="versionId">官方或已安装版本 ID。</param>
    /// <param name="rootDirectory">共享游戏根目录。</param>
    /// <param name="account">AccountManager 返回的启动身份。</param>
    /// <param name="java">探测或显式安装返回的 JavaRuntime；不重新决定 Java 主版本。</param>
    /// <param name="maxMemoryMb">调用方选择的最大堆内存（MiB）。</param>
    /// <param name="minMemoryMb">最小堆内存（MiB），默认 0 省略 Xms。</param>
    /// <returns>原有 LaunchConfig；由 MinecraftLauncher 执行相同的配置及兼容性校验。</returns>
    public static LaunchConfig Create(string versionId, string rootDirectory, LaunchIdentity account,
        JavaRuntime java, int maxMemoryMb, int minMemoryMb = 0)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(java);
        return new()
        {
            GameCore = new() { VersionId = versionId, RootDirectory = rootDirectory },
            Java = new() { JavaPath = java.ExecutablePath, MaxMemoryMb = maxMemoryMb, MinMemoryMb = minMemoryMb },
            Account = account
        };
    }

    /// <summary>Version, shared directory and instance settings.</summary>
    public GameCoreConfig GameCore { get; init; } = new();
    /// <summary>Java 自动/手动选择、最小/最大堆内存及自定义 JVM 参数。</summary>
    public JavaConfig Java { get; init; } = new();
    /// <summary>窗口宽高与全屏设置；与自定义窗口选项冲突时拒绝启动，同值自动去重。</summary>
    public GameWindowConfig Window { get; init; } = new();
    /// <summary>可选的自动加入服务器设置；null 不生成服务器参数。不绕过正版服务器认证或连接确认。</summary>
    public GameServerConfig? Server { get; init; }
    /// <summary>Microsoft or offline identity returned by AccountManager. Never persists launch credentials.</summary>
    [JsonIgnore] public LaunchIdentity? Account { get; init; }
    /// <summary>Optional caller-owned account manager. Refreshes Account or uses its currently selected account when Account is null.</summary>
    [JsonIgnore] public AccountManager? Accounts { get; init; }
    /// <summary>Additional game argument tokens; identity, version and directory overrides are rejected.</summary>
    [JsonIgnore] public IReadOnlyList<string> GameArguments { get; init; } = [];
    /// <summary>可选版本功能条件；窗口与 Quick Play 条件由 SDK 控制，不通过此字典覆盖窗口或服务器设置。</summary>
    public IReadOnlyDictionary<string, bool> Features { get; init; } = new Dictionary<string, bool>();
    /// <summary>Download concurrency/retry settings for automatic installation.</summary>
    public DownloadOptions Downloads { get; init; } = new();
    /// <summary>Bounded logs and current-session diagnostic collection; no automatic log-file export.</summary>
    public LoggingOptions Logging { get; init; } = new();
    /// <summary>Launcher brand substituted in official metadata.</summary>
    public string LauncherName { get; init; } = "FMCore";
    /// <summary>Launcher version substituted in official metadata.</summary>
    public string LauncherVersion { get; init; } = "1.0.0";
    /// <summary>Does not expose accounts or custom launch arguments.</summary>
    public override string ToString() => "Minecraft launch configuration";
}
