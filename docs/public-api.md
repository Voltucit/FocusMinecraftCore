# 公开 API 清单

适用于 `v2.0` 的 C# 开发版。目标框架 `net10.0`，语言 C# 14；程序集与命名空间前缀均为 `FMCore`。只有项目引用，无需额外第三方运行依赖。

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

## FMCore.Accounts

```text
sealed record OfflineAccount(string Username, Guid Uuid)
static OfflineAccount OfflineAccount.Create(string username)
```

Create 接受 1–16 个英文字母、数字或下划线，保留大小写；采用 Minecraft 的 OfflinePlayer: 前缀与 MD5/v3 UUID 规则。null 为 ArgumentNullException，格式无效为 ArgumentException。直接构造模型不验证或重算 UUID。

纯内存同步操作，不执行正版认证或保存账号。

## FMCore.Launch

普通启动使用 `LaunchConfig → MinecraftLauncher → LaunchAsync`。命名空间：`FMCore.Launch`，完整用法见 [游戏启动](launch.md)。

### LaunchConfig 与分组模型

以下为公开配置声明；均使用对象初始化器，required 成员必须填写：

```text
public sealed class LaunchConfig
{
    public required OfflineAccount Account { get; init; }
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
    OfflineAccount account,
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
| Account | OfflineAccount |
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

## 内部实现与后续范围

PathSafety、JavaDiscovery、依赖和资源解析器、参数构造器、LaunchRules 与 NativeExtractor 不是公开 SDK。`Launcher.RunWithProgressAsync` 也是内部连接步骤，外部调用方通过 MinecraftLauncher 使用进度。

下载、安装、资源补全、正版登录和运行进程对象尚未提供。规划和本轮待写代码不当作已可调用的 API。后续源码变化同步更新此清单、功能手册和示例。
