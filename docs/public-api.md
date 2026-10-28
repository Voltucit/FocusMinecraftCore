# 完整接口参考

通常先看[文档目录](README.md)中的最短示例。本页保留实际类型、方法和完整参数，方便需要调整细节时查找；代码里的英文名称不能替换成中文。

适用于准备发布的 `2.0.0`，使用 .NET 10 / C# 14，代码名称以 `FMCore` 开头。包名是 `FocusMinecraftCore`。

| 要查什么 | 位置 |
| --- | --- |
| 本地版本 | [Versions](#fmcoreversions) |
| Java | [Java](#fmcorejava) |
| 账号 | [Accounts](#fmcoreaccounts) |
| 游戏启动 | [Launch](#fmcorelaunch) |
| 安装和补全 | [Installer](#fmcoreinstaller) |
| 普通下载与下载源 | [Downloads](#fmcoredownloads) |
| 加载器安装 | [Modsloader](#fmcoremodsloader) |
| 社区资源 | [ModsResources](#fmcoremodsresources) |
| 问题检查 | [Logger](#fmcorelogger) |

`SHA-1` 是检查文件是否符合预期的校验值；`CancellationToken` 是取消开关；`Action`、`Func` 表示你传入的方法。普通调用可以省略可选参数。下面出现的 `internal` 成员属于内部实现，不能当作可直接调用的方法。

## FMCore.Versions

读取已安装的游戏，不下载或修改文件。常用写法见[版本查询](versions.md)。

### MinecraftVersion

这是一个版本的信息。`Id` 来自版本 JSON，`Directory` 是实际文件夹，两者名称可以不同。读取器返回完整路径；自己创建这个对象不会检查游戏是否完整。

```text
sealed record MinecraftVersion(
    string Id, string? VersionType, string Directory,
    string? InheritsFrom = null, string? MainClass = null)
```

### MinecraftVersionReader

直接创建对象使用。不传根目录时，查看当前工作目录和 APPDATA 下的 `.minecraft`；传入目录时只查那个根目录。不会创建缺失目录。

列表保留不同目录中同名的版本；单个查询要求 ID 唯一。父版本只能在同一根目录中查找，查找顺序是当前版本到父版本，游戏入口优先使用子版本的设置。

找不到版本报告 `KeyNotFoundException`；重名、父子循环、无效字段或缺少入口报告 `InvalidDataException`。JSON、权限和文件读取错误会直接报告，取消时报告 `OperationCanceledException`。

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

## FMCore.Java

读取或安装 Java，使用示例见[查找 Java](java.md)和[安装 Java](java-installation.md)。

### JavaInfo

`Executable` 是完整路径，`Version` 是完整版本，`MajorVersion` 是主版本号。`Architecture` 为 `x86`、`x64`、`arm64`，无法识别时为空。自己创建这个对象不会运行 Java。

```text
sealed record JavaInfo(
    string Executable, string Version, int MajorVersion,
    string? Architecture = null)
```

### JavaReader

查询会实际运行所选 Java，每次最多等待 10 秒。搜索来自环境变量、用户 `.jdks`、Windows 登记信息和额外搜索目录；不扫描整个磁盘、不修改系统设置、不自动安装或选择 Java。

单个候选失败时继续查找，可用 `onError` 接收路径和原因。你传入的方法出错时也会报告。找不到文件、无法执行、超时和取消会返回对应错误。

```text
Task<JavaInfo> GetJavaInfoAsync(
    string javaPath, CancellationToken cancellationToken = default)
Task<IReadOnlyList<JavaInfo>> GetJavasAsync(
    IEnumerable<string>? searchPaths = null,
    Action<string, Exception>? onError = null,
    CancellationToken cancellationToken = default)
```

### JavaInstaller

`majorVersion` 可选 8、16、17、21、25，安装目录由启动器选择，同时处理数默认 4。只安装官方提供的 Windows x64 游戏运行时。

成功后返回实际读取到的 Java 和下载数量。已有正确文件继续使用，不符的文件保留并报错。安装失败保留已完成文件。同一对象同时只安装一次。

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

## FMCore.Accounts

离线身份通常用 `OfflineAccount.Create()`，名字接受 1 到 16 个英文字母、数字或下划线，保留大小写。相同名字得到相同 UUID。直接调用对象构造方法不会重新检查名字或计算 UUID。

它不联网、不保存账号，也不表示拥有正版授权。三类账号都实现 `IMinecraftAccount`，可直接交给启动配置，见[账号](accounts.md)。

```text
sealed record OfflineAccount(string Username, Guid Uuid)
static OfflineAccount OfflineAccount.Create(string username)
```

### IMinecraftAccount 与 MicrosoftAuthenticator

`clientId` 由启动器作者提供。`onLoginCode` 必填，用于显示登录网址和登录码；`onProgress` 可选。登录会完成 Microsoft、Xbox 和 Minecraft 的验证，成功后返回 `MicrosoftAccount`，可直接填入 `LaunchConfig.Account`。

账号还提供 `ExpiresAt` 表示有效期。不要把 `AccessToken` 写入日志。`TokenCache` 用来接入启动器自己的登录数据存储；只有一个可恢复账号时先尝试不显示登录码恢复，否则进入登录码流程。保存、加密和账号切换由启动器负责。

每个对象同时只能登录一次。登录数据错误、服务拒绝、网络、超时、取消和你提供的方法出错时，会报告实际原因。完整写法见[Microsoft 登录](microsoft-login.md)。

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

### YggdrasilAuthenticator 与外置账号

`apiRoot` 填服务的完整 HTTPS 地址。账号已绑定角色时直接使用；只有一个角色时自动选择，多个角色时调用 `selectProfile`，由你返回列表中所选角色的 UUID。

`RestoreAsync()` 使用保存的登录名、角色 UUID 和两个登录凭据换取新账号。成功后必须更新保存的数据。无法恢复时重新登录，不要把网络错误当作首次登录。

启动需要已有的 authlib-injector JAR，填入 `JavaConfig.AuthlibInjectorPath`。账号数据的保存和加密由启动器实现。HTTP、格式、角色选择、取消等失败会直接报告，见[外置验证](yggdrasil-login.md)。

```text
public YggdrasilAuthenticator(string apiRoot)
public Uri ApiRoot { get; }

public Task<YggdrasilAccount> LoginAsync(
    string username,
    string password,
    Func<IReadOnlyList<YggdrasilProfile>, CancellationToken, Task<Guid>>?
        selectProfile = null,
    CancellationToken cancellationToken = default)

public Task<YggdrasilAccount> RestoreAsync(
    string loginName,
    Guid profileUuid,
    string accessToken,
    string clientToken,
    CancellationToken cancellationToken = default)

public sealed record YggdrasilProfile(Guid Uuid, string Name)
```

## FMCore.Launch

普通启动使用 `LaunchConfig` 和 `MinecraftLauncher`，见[最短启动示例](launch.md)。

### LaunchConfig 与分组模型

`Account`、`GameCoreConfig` 和 `JavaConfig` 必填；窗口大小可选。内存单位是 MB，必须大于 0；窗口宽高同时填写或同时省略。

`Root` 是游戏根目录，`Version` 是 JSON 中的版本 ID；`GameDirectory` 不填时使用所选版本目录。`JavaPath` 填完整 Java 路径。额外 Java 选项每项放一个字符串，路径不用自己加引号。

外置账号填写已有的 `AuthlibInjectorPath`，不能含等号。想改设置时重新创建配置和启动器。

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
    public string? AuthlibInjectorPath { get; init; }
}

public sealed class GameWindowConfig
{
    public int? Width { get; init; }
    public int? Height { get; init; }
}
```

### MinecraftLauncher

`CheckAsync()` 检查已有启动设置，`LaunchAsync()` 启动游戏。普通调用不必先执行检查。启动会读取版本、确认 Java、准备参数，并在需要时解压原生库。

启动方法一直等到游戏退出和最后的日志读完，返回整数退出码。`JavaStarted` 只表示 Java 已运行。接收进度和日志的方法可能在后台执行，界面需自己安排控件更新；它们出错时本次操作也会失败。

同一对象同时只允许一次操作。取消正在运行的游戏会强制关闭它，不代替游戏内保存退出。设置、文件、账号、Java、网络或取消等问题会报告实际原因，见[启动说明](launch.md)和[错误处理](advanced.md)。

```text
public sealed partial class MinecraftLauncher

public MinecraftLauncher(LaunchConfig config)

public Task<LaunchCheckResult> CheckAsync(
    Action<LaunchProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

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
| Task&lt;int&gt; | Java 退出并完成日志最后清理后结束，结果为 Java 退出码 |

### LaunchProgress 与 LaunchStage

`Stage` 说明当前在哪一步，`Message` 是可显示的文字。收到退出阶段也要查看退出码；取消或出错时，不保证还会收到最后一个阶段。

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

### PreparedLaunchConfig

这是给自行读取版本和 Java 信息的高级用法。普通启动不用创建它，使用上面的 `LaunchConfig` 即可。它会检查已有对象、目录、内存、窗口大小和额外 Java 选项，但不会启动游戏。

```text
public PreparedLaunchConfig(
    MinecraftVersion version,
    JavaInfo java,
    IMinecraftAccount account,
    int maxMemoryMb,
    string? gameDirectory = null,
    int? windowWidth = null,
    int? windowHeight = null,
    IEnumerable<string>? jvmArguments = null,
    string? authlibInjectorPath = null)
```

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
-Dauthlibinjector.yggdrasil.prefetched
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
| AuthlibInjectorPath | string? |

### Launcher 高级静态入口

这些方法使用已经准备好的 `PreparedLaunchConfig`。普通启动直接调用 `MinecraftLauncher.LaunchAsync()`，无需拆开准备过程。

方法等待 Java 退出，接收日志并支持取消。取消通常报告 `OperationCanceledException`；原问题和最后清理同时失败时，用 `AggregateException` 保留多个原因。

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

## FMCore.Modsloader

安装加载器后，仍使用普通游戏启动方法。安装结果中的根目录和版本 ID 可以直接填入启动配置。

### FabricInstaller

填写原版版本、Loader 版本和根目录，不需要 Java。缺少原版时会准备原版，再生成独立版本并补齐文件。只安装 Loader，不安装 Fabric API 或其他 Mod。

`versionId` 可选，同时处理数默认 4，目标系统类型默认当前系统。`Source` 可选下载源。查询返回 `Version` 和 `Stable`，选择哪个版本由启动器决定。

同一个对象同时只安装一次。已有不同配置会保留并报错；失败后保留完成文件。完整限制、进度和错误见[Fabric 安装](fabric-installation.md)。

```text
public sealed partial class FabricInstaller

public FabricInstaller(
    string gameVersion,
    string loaderVersion,
    string minecraftRoot,
    string? versionId = null,
    int maxConcurrency = 4,
    Architecture? targetArchitecture = null)

public string VersionId { get; }
public DownloadSource Source { get; init; } = DownloadSource.Official;

public static Task<IReadOnlyList<FabricLoaderVersion>> GetLoaderVersionsAsync(
    string gameVersion,
    CancellationToken cancellationToken = default)

public static Task<IReadOnlyList<FabricLoaderVersion>> GetLoaderVersionsAsync(
    string gameVersion,
    DownloadSource source,
    CancellationToken cancellationToken = default)

public Task<FabricInstallResult> InstallAsync(
    Action<FabricInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public sealed record FabricLoaderVersion(string Version, bool Stable)

public enum FabricInstallStage
{
    ReadingMetadata,
    InstallingBaseGame,
    WritingProfile,
    CompletingFiles,
    Completed
}

public sealed record FabricInstallProgress(
    FabricInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null)

public sealed record FabricInstallResult(
    string VersionId,
    string GameVersion,
    string LoaderVersion,
    string MinecraftRoot,
    string VersionDirectory)
```

### ForgeInstaller

原版版本、Forge 版本、根目录和 Java 路径必填。Java 用来运行安装补丁工具；额外内存默认 2048 MB，同时处理数默认 4。可选自定义版本 ID 和下载源。

当前支持使用 `spec=1` 说明的客户端安装包。安装包按官方大小和 SHA-1 检查，生成文件按声明检查；失败或已有不同配置会报错，不回滚整个安装。详情见[Forge 安装](forge-installation.md)。

```text
public sealed partial class ForgeInstaller

public ForgeInstaller(
    string gameVersion,
    string forgeVersion,
    string minecraftRoot,
    string javaPath,
    string? versionId = null,
    int maxConcurrency = 4,
    int processorMemoryMb = 2048)

public string VersionId { get; }
public DownloadSource Source { get; init; } = DownloadSource.Official;

public static Task<IReadOnlyList<string>> GetVersionsAsync(
    string gameVersion,
    CancellationToken cancellationToken = default)

public static Task<IReadOnlyList<string>> GetVersionsAsync(
    string gameVersion,
    DownloadSource source,
    CancellationToken cancellationToken = default)

public Task<ForgeInstallResult> InstallAsync(
    Action<ForgeInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public enum ForgeInstallStage
{
    CheckingJava,
    DownloadingInstaller,
    PreparingBaseGame,
    DownloadingLibraries,
    RunningProcessors,
    WritingProfile,
    CompletingResources,
    Completed
}

public sealed record ForgeInstallProgress(
    ForgeInstallStage Stage,
    string Message,
    DownloadProgress? InstallerDownload = null,
    DownloadBatchProgress? Download = null)

public sealed record ForgeInstallResult(
    string VersionId,
    string GameVersion,
    string ForgeVersion,
    string MinecraftRoot,
    string VersionDirectory)
```

### NeoForgeInstaller

填写原版版本、NeoForge 版本、根目录和 Java 路径。可选参数与 Forge 类似，但公开结果和进度使用 NeoForge 自己的类型。

部分版本没有给补丁生成文件提供预期校验值，此时只能检查生成 JAR 的结构，不能说成已按官方校验值验证。下载、补丁运行、取消等失败会报告实际原因，见[NeoForge 安装](neoforge-installation.md)。

```text
public sealed class NeoForgeInstaller

public NeoForgeInstaller(
    string gameVersion,
    string neoForgeVersion,
    string minecraftRoot,
    string javaPath,
    string? versionId = null,
    int maxConcurrency = 4,
    int processorMemoryMb = 2048)

public string VersionId { get; }
public DownloadSource Source { get; init; } = DownloadSource.Official;

public static Task<IReadOnlyList<string>> GetVersionsAsync(
    string gameVersion,
    CancellationToken cancellationToken = default)

public static Task<IReadOnlyList<string>> GetVersionsAsync(
    string gameVersion,
    DownloadSource source,
    CancellationToken cancellationToken = default)

public Task<NeoForgeInstallResult> InstallAsync(
    Action<NeoForgeInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public enum NeoForgeInstallStage
{
    CheckingJava,
    DownloadingInstaller,
    PreparingBaseGame,
    DownloadingLibraries,
    RunningProcessors,
    WritingProfile,
    CompletingResources,
    Completed
}

public sealed record NeoForgeInstallProgress(
    NeoForgeInstallStage Stage,
    string Message,
    DownloadProgress? InstallerDownload = null,
    DownloadBatchProgress? Download = null);

public sealed record NeoForgeInstallResult(
    string VersionId,
    string GameVersion,
    string NeoForgeVersion,
    string MinecraftRoot,
    string VersionDirectory);
```

### OptiFineInstaller 与 OptiFineOnlineInstaller

本地入口使用已有可信 JAR；在线入口使用游戏版本和 `Edition`，默认从 BMCLAPI 获取安装包。两者都需要 Java 执行补丁，返回相同安装结果类型。

在线入口不能直接选 `Official`；自定义服务需要兼容当前 BMCLAPI 返回格式。本地入口的 `Source` 只影响原版和依赖。在线包没有预期 SHA-1 时，会检查版本和内容结构，但不能据此确认官方来源。

版本 ID、同时处理数和补丁工具内存可选。已有不同配置、坏文件、下载或补丁错误会报出，见[OptiFine 安装](optifine-installation.md)。

```text
public sealed partial class OptiFineInstaller

public OptiFineInstaller(
    string installerJarPath,
    string minecraftRoot,
    string javaPath,
    string? versionId = null,
    int maxConcurrency = 4,
    int processorMemoryMb = 2048)

public DownloadSource Source { get; init; } = DownloadSource.Official;

public sealed partial class OptiFineOnlineInstaller

public OptiFineOnlineInstaller(
    string gameVersion,
    string edition,
    string minecraftRoot,
    string javaPath,
    string? versionId = null,
    int maxConcurrency = 4,
    int processorMemoryMb = 2048)

public string VersionId { get; }
public DownloadSource Source { get; init; } = DownloadSource.BmclApi;

public static Task<IReadOnlyList<OptiFineInstallVersion>> GetVersionsAsync(
    string gameVersion,
    CancellationToken cancellationToken = default)

public static Task<IReadOnlyList<OptiFineInstallVersion>> GetVersionsAsync(
    string gameVersion,
    DownloadSource source,
    CancellationToken cancellationToken = default)

// 两种对象都提供：
public Task<OptiFineInstallResult> InstallAsync(
    Action<OptiFineInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public enum OptiFineInstallStage
{
    ReadingInstaller,
    CheckingJava,
    PreparingBaseGame,
    ApplyingPatches,
    WritingFiles,
    Completed,
    ReadingVersions,
    DownloadingInstaller
}

public sealed record OptiFineInstallProgress(
    OptiFineInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null,
    DownloadProgress? InstallerDownload = null);

public sealed record OptiFineInstallResult(
    string VersionId,
    string GameVersion,
    string OptiFineVersion,
    string MinecraftRoot,
    string VersionDirectory);

public sealed record OptiFineInstallVersion(
    string GameVersion,
    string Edition,
    string FileName,
    string DownloadUrl)
{
    public bool IsPreview => FileName.StartsWith(
        "preview_", StringComparison.OrdinalIgnoreCase);
}
```

## FMCore.ModsResources

`ModrinthClient` 提供五类资源搜索、中文分类、作者、介绍、图片、历史版本和文件下载，不需要 API Key。

搜索的 `Limit` 默认 20，范围 1 到 100；`Offset` 默认 0。版本、加载器和类别筛选可省略。用英文 ID 筛选，用 `DisplayName` 显示中文。未知标签保留原文，介绍和作者名不自动翻译。

`GetProjectAsync()` 会获取作者团队。版本列表按发布时间排序，不批量读取更新日志；选中版本后用 `GetVersionAsync()` 获取完整详情。`Followers` 是关注人数，不是单独的收藏量。数据包可能保留 `mod` 兼容标记，完整类型同时在 `AllProjectTypes` 中。

下载到调用方指定文件，要求大小与 SHA-1。安装资源、选择依赖、启停和整个整合包系统由启动器实现。当前没有可调用的 CurseForge 入口。网络、返回格式、文件检查和取消等错误会报告，详见[Modrinth](modrinth.md)。

```text
public sealed class ModrinthClient

public ModrinthClient(
    string userAgent = "FocusMinecraftCore/2.0.0-dev (+https://github.com/Voltucit/FocusMinecraftCore)")

public DownloadSource Source { get; init; } = DownloadSource.Official;

public static IReadOnlyList<ModrinthResourceTypeInfo> GetResourceTypes()

public Task<IReadOnlyList<ModrinthCategory>> GetCategoriesAsync(
    ModrinthResourceType resourceType,
    CancellationToken cancellationToken = default)

public Task<ModrinthSearchResult> SearchAsync(
    ModrinthSearchOptions options,
    CancellationToken cancellationToken = default)

public Task<ModrinthProject> GetProjectAsync(
    string projectIdOrSlug,
    CancellationToken cancellationToken = default)

public Task<IReadOnlyList<ModrinthTeamMember>> GetTeamMembersAsync(
    string teamId,
    CancellationToken cancellationToken = default)

public Task<IReadOnlyList<ModrinthVersion>> GetVersionsAsync(
    string projectIdOrSlug,
    string? gameVersion = null,
    string? loader = null,
    CancellationToken cancellationToken = default)

public Task<ModrinthVersion> GetVersionAsync(
    string versionId,
    CancellationToken cancellationToken = default)

public Task<DownloadResult> DownloadAsync(
    ModrinthFile file,
    string destinationFile,
    Action<DownloadProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

```csharp
public enum ModrinthResourceType { Mod, ResourcePack, DataPack, Shader, Modpack }
public enum ModrinthSearchSort { Relevance, Downloads, Follows, Newest, Updated }
```

| 数据模型 | 主要字段 |
| --- | --- |
| ModrinthResourceTypeInfo | Type、Id、DisplayName |
| ModrinthCategory | Name/Id、DisplayName、ProjectType、Header、GroupName、Icon |
| ModrinthSearchResult | Hits、Offset、Limit、TotalHits |
| ModrinthSearchHit | ProjectId、Slug、Title、Description、Author/AuthorId、Organization、PublishedAt、UpdatedAt、Downloads、Followers/Follows、IconUrl、PreviewImageUrl、Gallery、Tags、原始分类、Versions |
| ModrinthProject | Id、Slug、Title、Description、Body、PublishedAt、UpdatedAt、Downloads、Followers、TeamMembers、Authors、IconUrl、Gallery、Tags、GameVersions、Loaders、Versions、License 和项目链接 |
| ModrinthUser / TeamMember | Id、Username、Name、DisplayName、AvatarUrl；User、Role、Accepted |
| ModrinthGalleryImage | Url、RawUrl、OriginalImageUrl、Featured、Title、Description、Created、Ordering |
| ModrinthVersion | Id、ProjectId、Name、VersionNumber、VersionType/VersionTypeName、PublishedAt/DatePublished、Downloads、Changelog、GameVersions、Loaders、Files、PrimaryFile、Dependencies |
| ModrinthFile / Hashes | Url、Filename、Size、Primary、FileType、Hashes；Sha1、Sha512 |
| ModrinthDependency | ProjectId、VersionId、FileName、DependencyType |
| ModrinthLabel | Id、DisplayName |

## FMCore.Logger

`GameFileChecker` 只读取本地受管理的游戏文件，不下载、不修复、不运行 Java。结果还提供 `MissingFiles`、`InvalidFiles`、`UnverifiableFiles` 和 `FileContentVerified`。

资源列表缺失或损坏时，资源文件总数未知；缺大小或校验值时返回 `MetadataMissing`，表示无法确认，不等于损坏。`IsLocallyGenerated` 为真时，缺失或坏文件需要对应加载器安装器处理。

`LaunchCheckResult` 来自 `MinecraftLauncher.CheckAsync()`，还提供 `ConfigurationVerified`。通过表示 `Issue` 为空；`Java` 和 `UsesModernArguments` 在读取之前可以为空。只检查启动设置，不完整检查所有文件、不在线验证账号。

检查文件或设置通过，都不表示游戏已打开。读取、格式、权限和取消等问题会报告，见[检查说明](diagnostics.md)。

```text
public sealed class GameFileChecker

public GameFileChecker(
    string versionId,
    string minecraftRoot,
    System.Runtime.InteropServices.Architecture? targetArchitecture = null)

public Task<GameFileCheckResult> CheckAsync(
    Action<GameFileCheckProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public enum GameFileIssueKind
{
    Missing, SizeMismatch, Sha1Mismatch, MetadataMissing
}

public sealed record GameFileIssue(
    string Name, string FilePath, GameFileIssueKind Kind,
    string Message, bool IsLocallyGenerated = false)

public sealed record GameFileCheckProgress(
    int CheckedFiles, int TotalKnownFiles, string? FilePath)

public sealed record GameFileCheckResult(
    int CheckedFiles, int VerifiedFiles, bool AssetObjectsEnumerated,
    IReadOnlyList<GameFileIssue> Issues)
```

```text
public sealed record LaunchCheckIssue(
    LaunchStage Stage, string Message, string ExceptionType)

public sealed record LaunchCheckResult(
    string VersionId, JavaInfo? Java,
    bool? UsesModernArguments, LaunchCheckIssue? Issue)
```

## FMCore.Downloads

安装和文件补全已包含下载步骤。只有自己下载其他已知文件时，才需要直接创建 `DownloadRequest`。

### DownloadSource

可选官方、BMCLAPI 或自定义地址转换。自定义方法接收原地址，返回新地址；返回空值表示不替换。地址必须为完整 HTTP 或 HTTPS 地址，不接受账号信息和 `#` 片段。

它只改变地址，不改变文件目标、预期大小或校验值。不同入口的覆盖范围见[下载源](download-sources.md)。

```text
public sealed class DownloadSource
{
    public static DownloadSource Official { get; }
    public static DownloadSource BmclApi { get; }
    public string Name { get; }

    public DownloadSource(string name, Func<string, string?> resolveUrl);
    public string GetUrl(string originalUrl);
    public override string ToString();
}
```

### DownloadRequest

地址、目标文件、大小和 SHA-1 必填。超时默认 5 分钟，额外重试默认 2 次，基本等待默认 1 秒。默认保留已有坏文件并报错；`ReplaceInvalidFiles = true` 时，检查好新文件后保留备份再替换。

额外重试数可设 0 到 10，等待时间可设 0 到 30 秒；大小允许为 0。创建请求不会立即下载。

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

### FileDownloader

成功返回时，目标文件已经检查完成，或已有正确文件得到复用。100% 进度不能代替方法成功返回。

你传入的方法出错时也会报告，并清理本次临时下载。参数、网络、文件、大小、SHA-1、超时和取消会返回对应错误，多个问题可能通过 `AggregateException` 一起报告。

```text
public static partial class FileDownloader

public static Task<DownloadResult> DownloadAsync(
    DownloadRequest request,
    Action<DownloadProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

### DownloadProgress 与 DownloadResult

`Percentage` 按字节计算。`BytesPerSecond` 是最近约 5 秒的速度，`RemainingTime` 是可空的估计剩余时间。`TransferredBytes` 只统计本次实际传输，包含重试，不包含复用文件大小。

`AlreadyPresent` 表示复用了已有正确文件。`Attempt` 是当前第几次尝试，`Attempts` 是成功所用的尝试次数，`BackupPath` 是本次替换前保留的备份位置。

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

### DownloadManyAsync 与 DownloadBatchProgress

默认同时处理 4 个文件。`requests` 由调用方准备，结果与请求顺序对应。进度包含文件总数、完成数、当前活动数、字节百分比和近期速度；文件数量与字节百分比不一定一起增长。

一个文件失败会取消其余操作，等待清理结束后再报告问题；已经完成的正式文件保留。进度方法可能在后台执行。

只对适合重试的网络错误及 HTTP 408、429、500、502、503、504 重试。大小不符、校验失败、磁盘错误、取消或你传入的方法出错时不重试。所有尝试共用原有超时时间，见[下载说明](downloads.md)。

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

## FMCore.Installer

普通调用使用下面的安装器或补全器，不需要先读取内部下载列表。

### MinecraftInstaller 与在线版本

只需版本号和游戏根目录，不需要 Java。可选同时处理数（默认 4）、目标系统类型和下载源。

版本列表提供 ID、类型和发布时间，由启动器选择显示范围。安装返回根目录、版本 ID、保存位置及文件处理结果。只安装原版，不安装 Java、加载器或 Mod，也不启动游戏。

已有正确文件继续使用，坏文件修复前留备份。已有不同配置、网络或文件错误会报出，见[原版安装](installation.md)。

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

### GameFileCompleter

处理客户端、依赖、旧版原生库压缩包、资源和日志配置，不需要 Java 或账号。默认同时处理 4 个文件，并在修复坏文件前保留备份。

`Source` 选择下载源，目标系统类型默认当前系统。`TotalFiles` 是总数，`DownloadedFiles` 是新下载数，`ReusedFiles` 是继续使用的下载文件数；本地生成文件单独计入 `VerifiedGeneratedFiles`。`Backups` 提供备份路径，`RepairedFiles` 是修复数。

缺少下载校验信息时会尝试从原仓库读取；没有下载地址的生成文件缺失或损坏时，需要重新运行对应加载器安装器。失败保留已完成文件和备份，不表示游戏已启动。见[游戏文件](game-files.md)。

```text
public sealed partial class GameFileCompleter

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
    VerifyingGeneratedFiles,
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
    public int VerifiedGeneratedFiles { get; init; }
    public int ClientAndLibraryFiles { get; }
    public int TotalFiles { get; }
    public IReadOnlyList<GameFileBackup> Backups { get; init; }
    public int RepairedFiles { get; }
}
```

### GameResourceCompleter

只处理资源列表、资源文件和日志配置，不处理客户端、依赖库、Java 或 Mod。需要已有版本 JSON。可选同时处理数默认 4，默认备份替换坏文件。

`Download` 有值时是当前这批文件的进度，不能作为整个操作的统一百分比。成功后返回处理数量及备份路径。网络、文件和取消等失败会报告实际原因，见[游戏资源](resources.md)。

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

## 内部实现与后续范围

`PathSafety`、下载列表生成器、参数构造器、规则判断和原生库解压器属于内部实现，普通使用不直接调用。标记为 `internal` 的方法不属于公开入口。

普通启动调用 `MinecraftLauncher`，原版安装调用 `MinecraftInstaller`，文件修复调用 `GameFileCompleter`。尚未提供的 CurseForge 和整合包专用方法不能作为可用接口。当前范围见[后续计划](roadmap.md)及各功能页。
