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

```text
sealed class LaunchConfig
LaunchConfig(MinecraftVersion version, JavaInfo java, OfflineAccount account,
    int maxMemoryMb, string? gameDirectory = null)
```

只读属性：Version、Java、Account、MaxMemoryMb、GameDirectory。省略工作目录时使用 version.Directory；指定自定义目录不会修改版本 ID。内存单位 MB，必须大于 0；bool 或字符串由 C# 类型系统拒绝。

null 模型为 ArgumentNullException，非正内存为 ArgumentOutOfRangeException，空白目录为 ArgumentException，文件占用目录为 IOException。其余路径与权限错误传播。构造时规范化路径但不创建目录，不探测 Java、不验证授权或文件完整性，也不启动游戏。

## 内部实现

Internal/PathSafety、JavaDiscovery 和 Launch/LibraryResolver 不属于公开 SDK。依赖读取、系统规则、Maven 坐标和普通依赖路径仍由本地测试验证，启动器不应自行调用内部流程来拼装启动。

record 的编译器生成成员不作为单独的业务入口。后续 API 或行为变化须同步更新此清单、手册、示例与测试。
