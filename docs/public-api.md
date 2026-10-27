# 公开 API 清单

适用于 `v2.0` 的 C# 开发版。目标框架 `net10.0`，语言 C# 14；程序集与命名空间前缀均为 `FMCore`。通过项目引用使用；Microsoft 登录依赖 Microsoft.Identity.Client，由核心项目的 NuGet 引用提供。

## FMCore.Versions

### MinecraftVersion

```text
sealed record MinecraftVersion(
    string Id, string? VersionType, string Directory,
    string? InheritsFrom = null, string? MainClass = null)
```

各参数对应同名属性。Directory 保存实际版本文件夹，Id 来自 JSON，二者可以不同。通过读取器返回的路径为绝对路径；直接构造模型不读取文件、不验证安装完整性。record 属性采用 init 语义，支持 with 复制。

### MinecraftVersionReader

无参数构造，实例不保存根目录或账号状态。

```text
string[] GetVersionDirectories(string? minecraftRoot = null)
IReadOnlyList<MinecraftVersion> GetVersions(string? minecraftRoot = null)
Task<IReadOnlyList<MinecraftVersion>> GetVersionsAsync(
    string? minecraftRoot = null, CancellationToken cancellationToken = default)
MinecraftVersion GetVersion(string versionId, string? minecraftRoot = null)
Task<MinecraftVersion> GetVersionAsync(
    string versionId, string? minecraftRoot = null,
    CancellationToken cancellationToken = default)
IReadOnlyList<MinecraftVersion> GetInheritanceChain(
    MinecraftVersion version, IReadOnlyList<MinecraftVersion> versions)
string ResolveMainClass(
    MinecraftVersion version, IReadOnlyList<MinecraftVersion> versions)
```

- 根目录为空值时汇总当前工作目录与 APPDATA 的 `.minecraft`，同一实际根目录去重。显式传入空白字符串是参数错误。
- 列表保留不同目录的重复 ID；GetVersion 要求 ID 唯一，可指定根目录消除跨根歧义。
- 目录必须包含与文件夹同名的 JSON；查询不会创建缺失目录。JSON ID 不必等于文件夹名。
- 继承链按当前版本到父版本排序；只在同一 Minecraft 根目录查找父版本。
- 解析入口前验证完整继承链，子版本 mainClass 优先。
- 参数错误为 ArgumentException / ArgumentNullException；找不到版本或父版本为 KeyNotFoundException；重名、循环、无效字段、缺少入口为 InvalidDataException。
- JSON 语法错误为 System.Text.Json.JsonException 或其子类；文件、权限错误继续传播。异步取消为 OperationCanceledException。

## FMCore.Java

### JavaInfo

```text
sealed record JavaInfo(
    string Executable, string Version, int MajorVersion,
    string? Architecture = null)
```

属性对应参数。探测结果的 Executable 为选中程序的绝对路径；Architecture 归一化为 x86、x64、arm64，无法识别为 null。直接构造不执行探测。

### JavaReader

静态类：

```text
Task<JavaInfo> GetJavaInfoAsync(
    string javaPath, CancellationToken cancellationToken = default)
Task<IReadOnlyList<JavaInfo>> GetJavasAsync(
    IEnumerable<string>? searchPaths = null,
    Action<string, Exception>? onError = null,
    CancellationToken cancellationToken = default)
```

- javaPath 必须指向可执行文件。固定查询超时为每个子进程 10 秒。
- 先查询属性和版本；只有属性查询进程返回非零退出码时，才对同一个可执行文件退回普通版本查询，最多两次。超时、取消和文件错误不退回。
- 搜索来源为 JAVA_HOME、PATH、用户 .jdks、Windows JavaSoft 注册信息和 searchPaths。额外目录检查本身、bin 和直属安装子目录，不全盘递归。
- 同一实际程序路径去重，不自动选择 Java，不修改环境变量，不下载安装。
- 单个候选探测失败调用 onError；省略时写入 Trace 警告，然后继续。发现阶段错误和回调自身异常传播；取消立即传播。
- 参数错误、FileNotFoundException、IOException、UnauthorizedAccessException、Win32Exception、TimeoutException、InvalidDataException、InvalidOperationException 和 OperationCanceledException 按实际原因返回。非零退出异常不拼接完整属性输出。

### JavaInstaller

安装 Mojang 提供的 Windows x64 游戏运行时，完整说明见 [Java 安装](java-installation.md)。

```text
public sealed partial class JavaInstaller

public JavaInstaller(
    int majorVersion,
    string installDirectory,
    int maxConcurrency = 4)

public Task<JavaInstallResult> InstallAsync(
    Action<JavaInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public enum JavaInstallStage
{
    ReadingMetadata,
    DownloadingFiles,
    VerifyingJava,
    Completed
}

public sealed record JavaInstallProgress(
    JavaInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null)

public sealed record JavaInstallResult(
    JavaInfo Java,
    string InstallDirectory,
    int TotalFiles,
    int DownloadedFiles,
    int ReusedFiles)
```

majorVersion 支持选择 8、16、17、21、25，对应 Mojang 官方组件；实际补丁版本由官方清单决定。安装目录由调用方提供，maxConcurrency 须为正，默认 4。构造规范化路径，不下载或执行 Java；InstallAsync 校验文件清单、下载并验证 raw 文件，再执行 JavaReader 的版本与架构探测。结果中的 Java.Executable 可直接用于 JavaConfig.JavaPath。

每个对象同时只允许一次安装，进度不保证 UI 线程。元数据请求单次 30 秒、响应上限 8 MiB；文件下载使用现有有限重试、取消和校验，已有合格文件复用，不匹配文件保留并报错。失败保留已完成文件与目录。用户已按 Java 25 安装及使用新 Java 启动 1.21.10 的步骤反馈成功，其他主版本和故障分支未据此宣称全部实测。

## FMCore.Accounts

```text
sealed record OfflineAccount(string Username, Guid Uuid)
static OfflineAccount OfflineAccount.Create(string username)
```

Create 接受 1–16 个英文字母、数字或下划线，保留大小写；采用 Minecraft 的 OfflinePlayer: 前缀与 MD5/v3 UUID 规则。null 为 ArgumentNullException，格式无效为 ArgumentException。直接构造模型不验证或重算 UUID。

纯内存同步操作，不执行正版认证或保存账号。OfflineAccount 实现 IMinecraftAccount；AccessToken 为 "0"、UserType 为 "legacy"、ClientId 为全零 GUID、Xuid 为 null。

### IMinecraftAccount 与 MicrosoftAuthenticator

```text
public interface IMinecraftAccount
{
    string Username { get; }
    Guid Uuid { get; }
    string AccessToken { get; }
    string UserType { get; }
    string ClientId { get; }
    string? Xuid { get; }
}

public MicrosoftAuthenticator(string clientId)

public Microsoft.Identity.Client.ITokenCache TokenCache { get; }

public Task<MicrosoftAccount> LoginAsync(
    Action<MicrosoftLoginCode> onLoginCode,
    Action<string>? onProgress = null,
    CancellationToken cancellationToken = default)

public sealed record MicrosoftLoginCode(
    string UserCode,
    Uri VerificationUri,
    DateTimeOffset ExpiresAt,
    string Message)
```

Client ID 必须由启动器作者提供，当前入口使用个人 Microsoft 账号的设备代码流程。onLoginCode 必填，由调用方展示网址和登录码；回调不保证 UI 线程。SDK 内部依次完成 Microsoft、Xbox、XSTS、Minecraft 令牌与玩家档案读取，每个验证器同时只允许一次登录。授权轮询与到期处理由 MSAL 管理；后续 HTTP 单次超时 30 秒，响应上限 1 MiB。

MicrosoftAccount 是只读属性类，由登录入口返回，包含接口所列属性及 DateTimeOffset ExpiresAt；UserType 为 msa。它的构造方法为 internal，ToString 只输出玩家名和 UUID。AccessToken 是实际 Minecraft 登录令牌，不是 Microsoft OAuth 令牌。完整示例、错误和当前范围见 [Microsoft 登录](microsoft-login.md)。

TokenCache 提供 MSAL 缓存回调；序列化通过回调参数 notification.TokenCache 执行。LoginAsync 发现一个缓存账号时先尝试静默授权；没有账号、多个账号或 MsalUiRequiredException 时进入设备代码流程，其他错误传播。用户已验收同一进程中向第二个验证器导入缓存并恢复相同玩家；跨进程保存、加密及账号管理由启动器作者实现，核心不提供账号文件存储。

## FMCore.Launch

普通启动使用 `LaunchConfig → MinecraftLauncher → LaunchAsync`。命名空间：`FMCore.Launch`，完整用法见 [游戏启动](launch.md)。

### LaunchConfig 与分组模型

以下为公开配置声明；均使用对象初始化器，required 成员必须填写：

```text
public sealed class LaunchConfig
{
    public required IMinecraftAccount Account { get; init; }
    public required GameCoreConfig GameCoreConfig { get; init; }
    public required JavaConfig JavaConfig { get; init; }
    public GameWindowConfig? GameWindowConfig { get; init; }
}

public sealed class GameCoreConfig
{
    public required string Root { get; init; }
    public required string Version { get; init; }
    public string? GameDirectory { get; init; }
}

public sealed class JavaConfig
{
    public required string JavaPath { get; init; }
    public required int MaxMemoryMb { get; init; }
    public IEnumerable<string>? JvmArguments { get; init; }
}

public sealed class GameWindowConfig
{
    public int? Width { get; init; }
    public int? Height { get; init; }
}
```

创建配置本身不校验游戏完整性或执行 Java。Root、Version、JavaPath、账号和内存由调用方指定；工作目录默认所选版本实际目录，窗口和额外 JVM 参数可省略。

### MinecraftLauncher

```text
public sealed class MinecraftLauncher

public MinecraftLauncher(LaunchConfig config)

public Task<int> LaunchAsync(
    Action<LaunchProgress>? onProgress = null,
    Action<string>? onOutput = null,
    Action<string>? onError = null,
    CancellationToken cancellationToken = default)
```

| 参数 / 返回 | 含义 |
| --- | --- |
| config | 构造时传入的分组配置 |
| onProgress | 实际启动阶段和消息 |
| onOutput / onError | Java 标准输出 / 标准错误逐行回调 |
| cancellationToken | 取消准备或尝试强制停止本次进程树 |
| Task&lt;int&gt; | Java 退出并完成日志收尾后结束，结果为 Java 退出码 |

构造时检查必填引用、版本 ID、内存与路径，确定路径并复制 JVM 集合；不读取版本 JSON、不探测 Java、不创建目录。宽高和额外参数等校验在启动准备中执行。相对路径按构造启动器时的工作目录确定。

内部在后台执行准备流程。进度和日志不保证界面线程，回调错误传播。每个实例同一时间只能执行一次启动或运行，重复调用抛 InvalidOperationException；结束后可再次调用。

### LaunchProgress 与 LaunchStage

```text
public sealed record LaunchProgress(LaunchStage Stage, string Message)

public enum LaunchStage
{
    ReadingVersion,
    DetectingJava,
    PreparingGame,
    StartingJava,
    JavaStarted,
    GameExited
}
```

没有百分比或独立 ProcessId 属性。JavaStarted 的 Message 包含 PID，但不证明主菜单已打开。GameExited 表示完成 Java 退出与日志收尾，可能是非零退出码。异常和取消通过异常传播。

### PreparedLaunchConfig

高级配置，保存调用方已读取的模型。它是原构造式 LaunchConfig 的新名称：

```text
public PreparedLaunchConfig(
    MinecraftVersion version,
    JavaInfo java,
    IMinecraftAccount account,
    int maxMemoryMb,
    string? gameDirectory = null,
    int? windowWidth = null,
    int? windowHeight = null,
    IEnumerable<string>? jvmArguments = null)
```

| 只读属性 | 类型 |
| --- | --- |
| Version | MinecraftVersion |
| Java | JavaInfo |
| Account | IMinecraftAccount |
| MaxMemoryMb | int |
| GameDirectory | string |
| WindowWidth / WindowHeight | int? |
| JvmArguments | IReadOnlyList&lt;string&gt; |

构造会规范化目录并复制 JVM 列表，不创建目录或运行游戏。内存和已指定宽高须为正整数，宽高必须配对。额外参数每项是一条完整选项，保留空格与顺序。拒绝空白、空字符、非选项条目和单独的 `--`；拒绝以 `-Xmx` 开头的选项，以及下列名称或其 `名称=值` 形式：

```text
-cp
-classpath
--class-path
-jar
-m
--module
-Djava.class.path
-Djava.library.path
-Dfile.encoding
-Dlog4j.configurationFile
```

其他 JVM 选项是否有效由所选 Java 检查。null 模型、无效参数、非正数字、路径和权限错误按实际原因传播；详细条件见 [启动手册](launch.md#日志取消与异常)。

### Launcher 高级静态入口

```text
public static Task<int> RunAsync(
    PreparedLaunchConfig config,
    string minecraftRoot,
    Action<string>? onOutput = null,
    Action<string>? onError = null,
    CancellationToken cancellationToken = default)

public static Task<int> RunLegacyAsync(
    PreparedLaunchConfig config,
    string minecraftRoot,
    Action<string>? onOutput = null,
    Action<string>? onError = null,
    CancellationToken cancellationToken = default)
```

RunAsync 按清单选择新旧参数流程；RunLegacyAsync 仅使用旧式流程。两个任务均在 Java 退出并完成日志收尾后返回退出码，平台限 Windows。普通启动器使用上面的实例入口即可。

取消通常为 OperationCanceledException；收尾也失败时为包含原始错误的 AggregateException。其他常见异常包括文件或目录缺失、无效元数据、不支持的格式、操作系统错误、回调错误与超时。JavaStarted、退出码和文件检查不能替代真实游戏验收。

## FMCore.Downloads

命名空间：`FMCore.Downloads`。当前提供单文件、多文件并发、字节进度、平均速度与剩余时间、有限重试与校验复用，完整规则见 [下载手册](downloads.md)。

### DownloadSource

```csharp
public enum DownloadSource
{
    Official,
    BmclApi
}
```

用于安装与补全入口的可选 Source 属性，命名空间 FMCore.Downloads。

### DownloadRequest

```text
public sealed class DownloadRequest
{
    public required string Url { get; init; }
    public required string FilePath { get; init; }
    public required long Size { get; init; }
    public required string Sha1 { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    public int MaxRetries { get; init; } = 2;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public bool ReplaceInvalidFiles { get; init; }
}
```

Url 必须为完整 HTTP(S) 地址；Size 非负；Sha1 为 40 位十六进制。FilePath 为调用方明确指定的目标路径。Timeout 必须为正且处于取消计时器支持的范围。MaxRetries 为额外重试次数，默认 2、范围 0 到 10；RetryDelay 默认 1 秒、范围 0 到 30 秒。Timeout 覆盖全部尝试和等待。填写模型不执行下载，调用时才检查。

### FileDownloader

```text
public static partial class FileDownloader

public static Task<DownloadResult> DownloadAsync(
    DownloadRequest request,
    Action<DownloadProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

任务成功返回时，文件已经通过大小和 SHA-1 校验，并已正式落盘或复用。ReplaceInvalidFiles 默认为 false，已有不匹配目标保留并报错。设为 true 后，下载并验证新文件，再保留同目录唯一备份并替换。取消使用调用方令牌，超时默认 5 分钟；临时文件清理也失败时聚合错误。仅回收本次创建的单个临时文件，不递归删除目录。

### DownloadProgress 与 DownloadResult

```text
public sealed record DownloadProgress(
    string FilePath,
    long DownloadedBytes,
    long TotalBytes)
{
    public double Percentage { get; }
    public int Attempt { get; init; } = 1;
    public bool AlreadyPresent { get; init; }
    public long TransferredBytes { get; init; }
    public double BytesPerSecond { get; init; }
    public TimeSpan? RemainingTime { get; init; }
}

public sealed record DownloadResult(
    string FilePath,
    long Size,
    string Sha1,
    bool AlreadyPresent)
{
    public int Attempts { get; init; } = 1;
    public string? BackupPath { get; init; }
}
```

Percentage 表示字节进度，0 字节文件为 100；100% 本身不证明校验完成。结果中的路径为规范化路径，Sha1 为小写，AlreadyPresent 表示已有文件校验通过后的复用。BackupPath 为本次成功替换生成的旧文件备份路径，否则为 null；SDK 不自动删除备份。文件系统替换失败时错误中包含备份候选路径，不保证旧文件仍位于原路径。

进度回调不保证 UI 线程，回调异常传播。常见异常为 ArgumentException、ArgumentOutOfRangeException、InvalidDataException、System.Net.Http.HttpRequestException、IOException、OperationCanceledException、TimeoutException 和 AggregateException。详见 [下载手册](downloads.md#取消超时和异常)。

### DownloadManyAsync 与 DownloadBatchProgress

```text
public static Task<IReadOnlyList<DownloadResult>> DownloadManyAsync(
    IEnumerable<DownloadRequest> requests,
    int maxConcurrency = 4,
    Action<DownloadBatchProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public sealed record DownloadBatchProgress(
    int TotalFiles,
    int CompletedFiles,
    int ActiveFiles,
    int PeakActiveFiles,
    long ProcessedBytes,
    long TotalBytes)
{
    public double Percentage { get; }
    public long TransferredBytes { get; init; }
    public double BytesPerSecond { get; init; }
    public TimeSpan? RemainingTime { get; init; }
}
```

maxConcurrency 必须为正，默认 4，限制当前批次的文件操作数。requests 枚举一次，先检查参数、重复目标和父目录冲突。空集合返回空结果；成功结果按请求顺序排列；大小求和溢出为 OverflowException。

ProcessedBytes 包含复用大小，不等于网络流量。CompletedFiles 在单文件操作成功后增加，字节 100% 不代替校验完成。总大小为 0 时按是否全部完成计算百分比。

同一批次回调串行执行，不保证 UI 线程。失败取消其余操作并等待收尾，已完成文件保留。单项非取消错误通过带路径上下文的 AggregateException 汇总；无其他已记录错误的取消通常为 OperationCanceledException。初始和最终回调错误可直接传播。完整参数说明和示例见 [下载手册](downloads.md#多文件下载)。

有限重试由 DownloadAsync 统一处理，批次继续复用。仅重试特定网络错误及 HTTP 408/429/500/502/503/504。支持 Retry-After 秒数或日期，总超时覆盖重试等待。回调、校验、磁盘、清理错误和取消不重试；最终失败传播最后错误。Attempt 为当前 SDK 尝试，Attempts 为成功次数。详细规则和验收边界见 [有限重试](downloads.md#有限重试)。TransferredBytes 是一次调用累计写入临时文件的正文大小，包含重试数据、不包含复用大小。BytesPerSecond 为最近约 5 秒的正文写入速度，开始时使用已有样本。RemainingTime 为剩余传输估计，暂不可估算时为 null。DownloadProgress.AlreadyPresent 标记本地校验复用，整批全部复用时传输量与速度均为 0。速度在回调时更新，0 秒不代表校验或落盘完成。详见 [速度与剩余时间](downloads.md#速度与剩余时间)。单文件分段 Range 尚未实现；原版安装与文件补全已提供下述公开入口。

## FMCore.Installer

### MinecraftInstaller 与在线版本

命名空间 FMCore.Installer，完整参数、进度与示例见 [原版安装](installation.md)。

```text
public sealed partial class MinecraftInstaller

public MinecraftInstaller(
    string versionId,
    string minecraftRoot,
    int maxConcurrency = 4,
    System.Runtime.InteropServices.Architecture? targetArchitecture = null)

public DownloadSource Source { get; init; } = DownloadSource.Official;

public Task<MinecraftInstallResult> InstallAsync(
    Action<MinecraftInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public static Task<IReadOnlyList<MinecraftInstallVersion>> GetVersionsAsync(
    CancellationToken cancellationToken = default)

public sealed record MinecraftInstallVersion(
    string Id, string Type, DateTimeOffset ReleaseTime)

public enum MinecraftInstallStage
{
    ReadingManifest,
    DownloadingVersionJson,
    CompletingFiles,
    Completed
}

public sealed record MinecraftInstallProgress(
    MinecraftInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null)

public sealed record MinecraftInstallResult(
    string VersionId,
    string MinecraftRoot,
    string VersionDirectory,
    GameFileCompletionResult Files)
```

安装只要求版本 ID 和根目录，不要求 Java。GetVersionsAsync 只查询官方清单，按发布时间降序返回只读列表；不创建目录、不安装游戏，列表中的历史版本不等于均已通过 FMCore 安装验收。支持取消；元数据 HTTP 单次超时 30 秒、响应上限 8 MiB。安装保留不匹配的已有版本 JSON 并报错，游戏文件按补全规则校验及备份修复。用户已通过在线正式版与快照列表验收、原版 1.21.10 安装与真实启动验收。

GameFileCompleter 和 GameResourceCompleter 也提供同类型的 Source 初始化属性。所有 Source 默认 Official，镜像规则见 [下载源](installation.md#下载源)。

### GameFileCompleter

已有实例的客户端、普通 artifact 依赖、资源与日志统一补全。命名空间 FMCore.Installer，完整参数与示例见 [游戏文件补全](game-files.md)。

```text
public sealed class GameFileCompleter

public GameFileCompleter(
    string versionId,
    string minecraftRoot,
    int maxConcurrency = 4,
    bool replaceInvalidFiles = true,
    System.Runtime.InteropServices.Architecture? targetArchitecture = null)

public Task<GameFileCompletionResult> CompleteAsync(
    Action<GameFileCompletionProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public enum GameFileCompletionStage
{
    SelectingArchitecture,
    ReadingMetadata,
    CompletingClientAndLibraries,
    CompletingResources,
    Completed
}

public sealed record GameFileCompletionProgress(
    GameFileCompletionStage Stage,
    string Message,
    DownloadBatchProgress? Download = null)

public sealed record GameFileBackup(
    string FilePath,
    string BackupPath)

public sealed record GameFileCompletionResult(
    int ClientFiles,
    int LibraryFiles,
    int ResourceMetadataFiles,
    int AssetFiles,
    int DownloadedFiles,
    int ReusedFiles)
{
    public int ClientAndLibraryFiles { get; }
    public int TotalFiles { get; }
    public IReadOnlyList<GameFileBackup> Backups { get; init; }
    public int RepairedFiles { get; }
}
```

版本与根目录两项必填，最大并发默认 4，备份修复默认开启。targetArchitecture 默认当前操作系统架构，支持 x86、x64 和 arm64。构造固定路径，CompleteAsync 内部按目标架构准备声明、校验或下载，再复用资源补全；同一对象防止重复操作，回调在后台执行。Download 为当前批次进度，百分比按字节计算；完全复用时传输速度为 0。Backups 为本次成功替换的备份，RepairedFiles 是 DownloadedFiles 的一部分。

用户已在 1.21.10 Fabric 配合 Java 25 上连续两次校验并复用 4476 个文件。旧式 Windows natives 归档已接入并完成 1.8.9 元数据验收；不安装版本 JSON、Java、加载器或 Mod，也不启动游戏。Maven 元数据仍可能联网查询。失败传播实际异常，保留已完成文件及备份，不回滚整个实例。

### GameResourceCompleter

资源补全使用简单入口，命名空间 FMCore.Installer。完整构造参数、控制台示例与限制见 [游戏资源补全](resources.md)。

```text
public sealed class GameResourceCompleter

public GameResourceCompleter(
    string versionId,
    string minecraftRoot,
    int maxConcurrency = 4,
    bool replaceInvalidFiles = true)

public Task<ResourceCompletionResult> CompleteAsync(
    Action<ResourceCompletionProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public enum ResourceCompletionStage
{
    ReadingMetadata,
    CompletingMetadata,
    ReadingAssetObjects,
    CompletingAssetObjects,
    Completed
}

public sealed record ResourceCompletionProgress(
    ResourceCompletionStage Stage,
    string Message,
    DownloadBatchProgress? Download = null)

public sealed record ResourceCompletionResult(
    int MetadataFiles,
    int AssetFiles,
    int DownloadedFiles,
    int ReusedFiles)

public int TotalFiles { get; }
public IReadOnlyList<ResourceFileBackup> Backups { get; init; }
public int RepairedFiles { get; }

public sealed record ResourceFileBackup(
    string FilePath,
    string BackupPath)
```

versionId 和 minecraftRoot 必填；maxConcurrency 为正，默认 4；replaceInvalidFiles 默认 true，false 时保留不匹配文件并报错。构造规范化路径，不读取版本 JSON 或下载。CompleteAsync 在后台编排元数据与资源对象，分别调用下载批次；进度 Download 可为空，其百分比仅代表当前批次。每个实例同时只允许一次操作，结束后可重用。

成功返回表示资源索引、去重后的对象和日志配置已校验；范围不包含客户端 JAR、依赖库、加载器或 Java，也不启动游戏。默认对不匹配文件下载并校验后保留备份再替换，Backups 汇总本次成功替换的文件与备份路径；RepairedFiles 是 DownloadedFiles 的一部分。参数、元数据、网络、文件系统、回调、取消、超时和聚合错误按实际原因传播。详见 [游戏资源补全](resources.md)。

## 内部实现与后续范围

PathSafety、ResourceDownloadPlanner、GameFileDownloadPlanner、GameDownloadFile、JavaDiscovery、依赖和资源解析器、参数构造器、LaunchRules 与 NativeExtractor 不是公开 SDK。ResourceDownloadPlanner.ReadMetadataAsync 已实现并通过 CLI 验收，仅用于内部读取资源索引与日志下载声明；NormalizeRequest 和 ReadInheritedObject 的 internal 可见性不代表公开入口。GameFileDownloadPlanner.ReadAsync 已由用户通过 game-file-metadata 验收，只读取客户端与普通 artifact 依赖声明；缺少信息通过 MissingFields 返回，不下载、不校验本地文件内容，旧式 Windows natives 归档已接入清单，1.8.9 元数据验收通过。内部 ReadResolvedAsync 进一步从 Maven 的 .sha1 与 HEAD 响应补齐缺失信息，已通过 --resolve-maven 的真实实例验收；它仍只返回清单，不提供文件完整性结论。`Launcher.RunWithProgressAsync` 也是内部连接步骤，外部调用方通过 MinecraftLauncher 使用进度。

MinecraftInstaller 已提供在线版本列表与原版安装，1.21.10 安装及真实启动已由用户验收。GameFileCompleter 已提供客户端、普通依赖、旧式 Windows 原生库归档及资源的统一补全；已验收新版样本校验复用及 1.8.9 元数据。JavaInstaller 已提供 Windows x64 官方运行时安装；Java 25 安装并用于启动游戏已验收。Microsoft 设备代码登录及使用正版身份启动游戏已验收；运行进程对象尚未提供。资源索引、对象与日志补全已由 GameResourceCompleter 提供并通过基础验收，内部清单读取不作为普通调用流程。保留备份后修复及再次复用已由用户通过本地样本与 1.21.10 Fabric 资源补全验收。规划和本轮待写代码不当作已可调用的 API。后续源码变化同步更新此清单、功能手册和示例。
