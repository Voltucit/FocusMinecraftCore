# P0 开发者 API 清单

目标 .NET 10 / C# 14。首个版本为 `FMCore 0.1.0-preview.1`，公开发布状态见[发布报告](validation/t065-release.md)。发布后执行 `dotnet add package FMCore --version 0.1.0-preview.1`；发布前使用[本地 NuGet 源](releasing.md)验证。源码开发也可引用 `FMCore/FMCore.csproj`。项目只包含一个业务核心类库，不依赖 WPF/Avalonia/WinUI，不强制 DI。包包含 `FMCore.xml`，新增入口及主要操作带 XML API 提示，存量模型成员的 XML 说明逐步补充。

## 入口与兼容性

| 模块 / 命名空间 | 简单入口 | 高级配置与结果 |
| --- | --- | --- |
| Downloader / `FMCore.Downloader` | `new DownloadManager(root)`，`DownloadAsync`、`DownloadManyAsync`、`VerifyAsync` | 原 `DownloadManager(HttpClient, root, options)` 保留；`DownloadRequest`、哈希算法、并发/重试选项、进度与结构化异常 |
| Versions / `FMCore.Versions` | `GameCoreUtil.GetGameCoresAsync(root)`、`GetGameCoreAsync(id, root)`；同步 `GetGameCores`、`GetGameCore` | `GameCoreInfo` 含有效元数据、基础版本/JAR 来源、加载器识别、Java 要求和错误；`VersionManager` 获取官方列表、缓存和继承 |
| Java / `FMCore.Java` | `JavaUtil.GetJavasAsync()`、`GetJavaInfoAsync(path)`；同步 `GetJavas`、`GetJavaInfo` | `JavaManager.DetectAsync/SelectAsync` 保留；真实探测、失败候选、扫描边界和兼容性策略 |
| Installer / `FMCore.Installer` | `new GameInstaller(root)`，`InstallAsync`、`VerifyAsync`、`RepairAsync` | 原下载器构造保留；`InstallationOptions`、计划、阶段/文件进度、完整下载复用及原生/资源布局 |
| Authentication / `FMCore.Authentication` | `AccountManager` 的 `LoginMicrosoftAsync`、`LoginOfflineAsync`、`GetAccountsAsync`、`SwitchAsync`、`SetDefaultAsync`、`RefreshAsync`、`DeleteAsync` | 原 `CreateOfflineAccountAsync`、`GetLaunchIdentityAsync` 保留；公开资料、安全存储、账号事件、`LaunchIdentity` |
| Launch / `FMCore.Launch` | `new MinecraftLauncher(LaunchConfig)`，`LaunchAsync`、`WaitForExitAsync`、`StopAsync` | `GameCoreConfig`、`JavaConfig`、`GameWindowConfig`、`LaunchResult`、状态、stdout/stderr 与退出事件、异步释放 |

旧方法签名保留。新增方法组合现有实现，不建立另一套版本、账号或 Java 模型；`LaunchConfig.Account` 直接接收 T05 返回的 `LaunchIdentity`，离线便捷登录也返回同一类型。`InstallationStage.Verification` 追加在已有值之后，已有阶段数值未调整。

同步便捷查询会阻塞调用线程，UI 使用 Async 版本。异步 I/O 方法支持取消；进度、状态和错误随具体操作提供。核心默认网络客户端由 SDK 复用，高级调用方仍可提供自己的 HttpClient 配置代理等，不交出该客户端的所有权。

## 下载与校验

下面的操作复用官方目录中的已验证 URL/哈希，不需要开发者自己建立另一套版本来源。`ct` 为调用方取消令牌。

```csharp
using FMCore.Downloader;
using FMCore.Versions;

var downloads = new DownloadManager(".minecraft");
var versions = new VersionManager(downloads);
var catalog = await versions.GetManifestAsync(cancellationToken: ct);
var entry = catalog.Versions.Single(v => v.Id == "1.16.5");
var request = new DownloadRequest(entry.Url, "samples/profile.json") { ExpectedHash = entry.Sha1 };
var progress = new Progress<DownloadProgress>(p => Console.WriteLine($"{p.State}: {p.BytesReceived}"));
await downloads.DownloadAsync(request, progress, ct);
await downloads.DownloadManyAsync([
    request with { RelativePath = "samples/first.json" },
    request with { RelativePath = "samples/second.json" }
], progress, ct);
var check = await downloads.VerifyAsync(request, ct);
```

`VerifyAsync` 无网络/写入；报告缺失、大小错误、哈希错误、有效或 `Unverified`。`Unverified` 表示未给大小或哈希，只能确认文件存在，不能作为完整性保证。正常原版文件使用官方大小/哈希。下载失败后已完成的有效文件可复用，未完成文件保留原子替换边界；不把批次当作事务。

## 版本与 Java

```csharp
using FMCore.Java;
using FMCore.Versions;

var cores = await GameCoreUtil.GetGameCoresAsync(".minecraft", ct);
var core = await GameCoreUtil.GetGameCoreAsync("1.16.5", ".minecraft", ct);
var javas = await JavaUtil.GetJavasAsync(cancellationToken: ct);
var java = await new JavaManager().SelectAsync(core.ResolvedVersion!.Metadata,
    customPath: Environment.GetEnvironmentVariable("FMCORE_JAVA"), cancellationToken: ct);
var actual = await JavaUtil.GetJavaInfoAsync(java.ExecutablePath, ct);
```

GameCoreUtil 只查本地，不安装游戏。列表保留损坏 profile 的错误信息；单个查询对缺失/损坏抛出结构化异常。加载器由现有 Libraries 坐标识别为 Fabric/Forge/NeoForge/Quilt，不代表已安装完整或已实机验证。`MinecraftVersion` 表示可确定的基础 profile/JAR 来源 ID；完整继承链与原始扩展元数据仍保留在 `ResolvedVersion`。

自动 Java 严格匹配元数据要求和目标架构；手动路径优先，但也必须兼容。高级调用可传 `JavaSelectionOptions`、`JavaDetectionOptions`，仅当元数据缺要求时使用显式 `FallbackMajorVersion`。示例中的同步别名可用于控制台简短查询，GUI 避免在界面线程阻塞探测。

## 安装、检查与修复

```csharp
using FMCore.Installer;

var installer = new GameInstaller(".minecraft");
var progress = new Progress<InstallationProgress>(p =>
    Console.WriteLine($"{p.Stage}: {p.CompletedFiles}/{p.TotalFiles}"));
var installed = await installer.InstallAsync("1.16.5", progress: progress, cancellationToken: ct);
var check = await installer.VerifyAsync("1.16.5", progress: progress, cancellationToken: ct);
if (!check.IsValid)
    await installer.RepairAsync("1.16.5", progress: progress, cancellationToken: ct);
```

高级调用传 `InstallationOptions` 指定目标规则上下文、相对游戏目录和缓存策略；也可复用自定义下载器构造安装器。Verify 默认仅使用缓存，检查客户端、Libraries/原生压缩包、资源索引/对象、日志配置和声明的旧版布局副本。缺索引时 `AssetsChecked=false`，不报告检查通过。它不检查存档、Mod、全部 Java 类是否可执行或解压后的 native 文件内容；修复会重新安全解压，Launch 每次也生成私有 native 目录。检查仍遵循元数据可提供的校验强度。

## 账号与启动

离线最短调用为 `await accounts.LoginOfflineAsync("Player", ct)`；正版调用使用自己的 Client ID 和前台设备码提示，完整代码见 [账号文档](authentication.md)。两种身份可在同一管理器中共存，刷新失败不自动降级。`SwitchAsync` 为本管理器当前选择，`SetDefaultAsync` 持久化下次默认选择。

```csharp
using FMCore.Authentication;

using var accounts = new AccountManager(accountDirectory);
var offline = await accounts.LoginOfflineAsync("Player", ct);
var list = await accounts.GetAccountsAsync(ct);
await accounts.SwitchAsync(offline.Account.Id, ct);
await accounts.SetDefaultAsync(offline.Account.Id, ct);
var identity = await accounts.GetLaunchIdentityAsync(cancellationToken: ct);
// UI 明确要求删除时：await accounts.DeleteAsync(accountId, ct);
```

将 `identity` 直接传给 `LaunchConfig.Account`，不读取令牌并拼参数。可同时传 `Accounts = accounts` 在启动前刷新；也可只传 Accounts，使用当前所选账号。完整可编译的最简和高级启动流程见 **[启动示例](launch.md)**；高级示例包含正版授权、离线选择、Java、内存、实例、窗口、状态、日志、退出和停止。

## 验收记录

T06 Windows 自动化 **402 项通过**（新增 57 项）。真实子进程测试覆盖双路大量输出、退出码、停止及子进程树、取消准备、等待取消、实例并行/互斥、原生清理和日志过滤；协议/文件测试明确使用 Mock。

另外在仓库外建立仅引用 FMCore 的调用方程序，未使用测试友元或内部 API：真实本机 HTTP 完成单/批量 3 次下载及 SHA-256 校验；真实枚举本地版本、Java 探测与选择、校验 2,640 个原版文件、修复复用 2,640 个文件且零下载；验证离线账号创建/切换/默认/刷新/删除。通过真实 Microsoft 授权与刷新，分别用正版和离线账号启动 1.16.5 并观察窗口/音频/纹理初始化。记录见 [T06 JSON](validation/t06-p0.json)。

最简、高级完整示例从文档提取并独立编译；局部代码片段中的 `ct`、`accountDirectory` 由调用方提供。T06.5 新增独立 NuGet 包引用验收，具体结果见[发布报告](validation/t065-release.md)。未实际验证 Linux/macOS、其他游戏版本、加载器、世界或多人服务器。CLI 当前仍只有帮助/版本命令，SDK 功能完整性不表示 CLI 命令已实现。
