# 原版启动

本手册对应当前源码。目录布局调整尚未发布，已发布包请以对应 Tag 的文档为准。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Authentication;
using FMCore.Java;
using FMCore.Launch;

using var accounts = new AccountManager(".fmcore/accounts");
var path = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请设置 FMCORE_JAVA。");
var java = await JavaUtil.GetJavaInfoAsync(path);
var identity = await accounts.LoginOfflineAsync("Player");
var config = LaunchConfig.Create("1.16.5", ".minecraft", identity, java, maxMemoryMb: 2048);
await using var launcher = new MinecraftLauncher(config);
var result = await launcher.LaunchAsync();
if (!result.Success) { Console.WriteLine(result.Error); return; }
var exit = await launcher.WaitForExitAsync();
Console.WriteLine(exit.ExitCode);
```


`FMCore.Launch.MinecraftLauncher` 自动协调版本继承、Java、账号、安装检查、Classpath、条件参数、资源布局和原生依赖。仅引用 FMCore，无需 GUI 框架、DI 容器或手动构建命令行。

T07 当前源码新增[实例管理](instances.md)：`instance.CreateLaunchConfig(account)` 自动绑定配置和隔离目录，并在启动前检查修订。准备/运行期间与更新和删除互斥，已有手工构造方式保留。该功能包含于 1.0.0。

T08 当前源码新增 `LaunchConfig.Logging`、`launcher.Logger` / `LastDiagnostic`，准备失败和退出结果提供 `Diagnostics`；原事件与结果构造签名保留。诊断说明与完整示例见[日志与诊断](logging.md)。

T13 当前源码新增 `LaunchConfig.Server`，内存允许 `MinMemoryMb = 0` 省略 Xms，窗口/JVM/服务器参数统一归一化与冲突检查。公开签名与默认内存保留，完整参数表及持久化示例见[高级启动设置](advanced-launch.md)。

## 最简完整示例

| API / 事件 | 输入或内容 | 返回值 / 行为 |
| --- | --- | --- |
| `new MinecraftLauncher(config)` | `LaunchConfig`：GameCore、Java、Window、Account/Accounts | 只持有配置，调用 `LaunchAsync` 才准备游戏 |
| `LaunchAsync(progress, ct)` | 可选阶段进度与准备取消 | `LaunchResult`：Success、ProcessId、Error |
| `WaitForExitAsync(ct)` | 可选等待取消，不终止游戏 | `GameExitResult`：ExitCode、StoppedByCaller、ExitedAt |
| `StopAsync(ct)` / `DisposeAsync()` | 停止当前拥有的游戏 / 异步释放 | 完成准备取消或进程停止及资源清理 |
| `StateChanged` / `OutputReceived` / `ErrorReceived` / `Exited` | 状态、脱敏标准输出/错误、退出结果 | 后台回调，GUI 自行调度；不要阻塞等待启动器 |
| `State` / `ProcessId` / `ExitCode` | 当前状态查询 | 进程未建立或尚未退出时相应值可为 null |

业务启动失败通过 `LaunchResult.Error` 返回；配置或进程控制误用可能抛 `LaunchException`，取消抛 `OperationCanceledException`。下面为最简完整调用，后续高级示例和配置表说明默认值、账号刷新、Java、目录、窗口与参数。

可作为启用隐式 using 的 .NET 10 控制台 `Program.cs`。先安装对应 Java；默认会下载/修复原版游戏。离线登录不需要网络，游戏文件尚未安装时仍需联网安装。

独立、可编译的调用代码见[本模块示例](examples.md#launch)。

`LaunchAsync` 在进程创建后返回，**不代表游戏已到主菜单**。`WaitForExitAsync` 保持示例存活并获取真实退出码；异步释放会停止该启动器拥有的游戏。GUI 应把启动器保存为应用/窗口字段，而不是在按钮事件内立即释放。

## 高级完整示例

支持 Ctrl+C、正版/离线显式选择、自动刷新、独立实例、内存、窗口、自定义 JVM 参数、阶段/文件进度和日志。设置 `FMCORE_GAME_ROOT`、`FMCORE_VERSION`、`FMCORE_JAVA` 可覆盖目录、版本和 Java；未指定 Java 时自动选择。账号默认放在用户数据目录，可用 `FMCORE_ACCOUNT_DIRECTORY` 覆盖。

运行时加入 `--microsoft` 才发起正版设备授权，必须配置自己的 `FMCORE_CLIENT_ID`，短码只作为前台提示。两个示例都只调用公开 API，可独立编译。

独立、可编译的调用代码见[本模块示例](examples.md#launch)。

WPF、Avalonia、WinUI 使用相同配置与调用。`Progress<T>` 可在 UI 线程创建以捕获上下文；普通 .NET 事件和设备码回调可能运行在后台，宿主通过自己的 Dispatcher 更新界面。事件处理器应快速返回，不要同步等待启动器操作；处理器异常被隔离，避免阻断日志读取或误报进程状态。

## 配置与默认行为

| 配置 | 默认与含义 |
| --- | --- |
| `GameCore.RootDirectory` / `VersionId` | 当前目录下 `.minecraft` / 必须指定有效版本 ID |
| `Isolate` / `InstanceId` | 默认隔离，工作目录为 `versions/<InstanceId 或 VersionId>` |
| `GameDirectory` | 显式指定相对根目录的工作目录，优先于自动隔离；拒绝越界和目录链接 |
| `AutoInstall` | 默认 `true`，自动下载/修复；`false` 仅使用缓存元数据与本地下载文件，仍生成实例目录、原生依赖和旧版资源映射 |
| `CachePolicy` | 自动安装时默认 `PreferCache`；本地启动使用 `CacheOnly` |
| `JavaPath` | 显式路径优先，实际运行探测后仍须匹配版本/架构，失败不换用其他 Java |
| `MinMemoryMb` / `MaxMemoryMb` | 512 / 2048 MiB；最小设为 0 时省略 Xms，其他情况不得超过最大；32 位 Java 最大 1536 MiB |
| `Java.Selection` / `Detection` | 高级兼容/扫描策略；元数据缺 Java 要求时须显式配置 `FallbackMajorVersion` |
| `Window` | 854×480，非全屏；自动启用自定义分辨率条件 |
| `Server` | 可选 `GameServerConfig`，Host + Port（默认 25565）；按元数据规则使用旧版或 Quick Play 语法 |
| `Account` / `Accounts` | 直接接收 T05 `LaunchIdentity`；可传管理器重取/刷新，未给身份时使用其所选账号 |
| `Java.Arguments` / `GameArguments` | 每个元素一个参数，保留空格，不手动加 shell 引号；不允许覆盖受控的内存、Classpath、主类、账号或资源目录 |
| `Features` | 自定义版本功能条件，窗口及 Quick Play 的受控条件由 SDK 确定，其他条件保留 |
| `Downloads` | 默认下载并发/重试配置，可直接覆盖 |

BootstrapLauncher 的元数据 ignoreList 会补充实际继承客户端文件名，避免原版与处理器生成的 Minecraft 模块重复；不改写 version_name。

版本继承、规则和客户端选择直接消费 Versions 模型；同时具有普通 artifact 与 native classifier 的库会保留两者。Java 主版本严格匹配元数据，不把更高主版本当作兼容替代。已安装加载器 profile 通过版本 ID 复用相同启动逻辑。T06 当时未安装或实测加载器；后续 T09-A 当前源码已真实离线验证 1.16.5 / Fabric Loader 0.19.5 / Java 8，完整组合、安装入口和验收边界见 [Fabric 手册](fabric.md)。T09-C–E 也已分别通过 [Forge](forge.md)、[NeoForge](neoforge.md)、[OptiFine](optifine.md) 的真实离线验收；Quilt 自动安装阶段曾取消；1.0.2 通用读取已有 Quilt 元数据的能力与自动安装器分开，详见文末。

原生解压目录为共享根下 `.fmcore/launch/<随机 ID>/natives`，每次启动独立，进程退出后清理；设置、存档、游戏日志留在实例工作目录。相同工作目录有进程生命周期锁，第二个启动返回 `InstanceBusy`，不同实例可并行。实例隔离不等于本阶段实现完整实例导入、删除、备份管理。

旧版 `minecraftArguments`、现代 `arguments.jvm/game`、OS/架构/feature 条件、`${auth_session}`、`${game_assets}`、`virtual` 和 `map_to_resources` 均有自动化覆盖。已停用且缺少可用资源索引的古老版本仍返回明确安装错误；旧版真正运行和现代最新版本运行尚未实测。

## 状态、失败和停止

`LaunchAsync` 的业务失败返回 `LaunchResult { Success = false, Error }`，`Error.Code` 为启动分类，`DetailCode` 保留版本/Java/认证/下载等模块错误码名称，不包含服务原文。无效构造配置和进程控制误用抛 `LaunchException`；调用取消抛 `OperationCanceledException`。同启动器已有进程时返回 `AlreadyRunning` 并保持现有状态。

`State` 包含 `Idle → Preparing → Starting → Running → Exited`，以及 `Stopping`、`Failed`、`Canceled`。创建后立即崩溃的游戏仍可能先获得启动成功结果，应继续监听退出码和日志；游戏非零退出是进程结果，不重新解释为成功加载。

`LaunchAsync` 的取消令牌控制准备及创建进程前的取消，成功创建后由 `StopAsync` 或异步释放管理生命周期。`WaitForExitAsync(ct)` 取消只终止等待，不杀游戏。`StopAsync` 可取消准备，或请求结束拥有的进程树并等待主进程退出与输出处理；强制停止可产生非零退出码，`StoppedByCaller` 用于区分。退出后可重用同一启动器。释放前应协调完其他并发调用，调用方传入的 `AccountManager` 由调用方释放。

## 凭据保护与验证

SDK 不公开原始 `Process`、`ProcessStartInfo` 或完整参数字符串。使用独立参数而非 shell；清除继承的 Java 选项注入变量，默认 UTF-8 输出。并发读取 stdout/stderr，隐藏已知访问令牌、旧版 session 及 URL 编码形式；每行超过 16 Ki 字符时整行省略，避免部分令牌穿过截断边界。配置的账号与自定义参数不参与默认 JSON 序列化，诊断字符串也不输出它们。

此过滤覆盖 **FMCore 转发的日志事件**，不能控制游戏、Mod、JVM、系统诊断工具自行写入的文件或系统进程参数可见性。宿主应保护日志收集与诊断导出，不自行记录凭据、原始进程参数或内存转储；旧版游戏协议本身要求访问令牌传入进程。正版刷新/资格检查失败只会报错，不自动切换离线。

验收：Windows / .NET 10 自动化测试 402 项通过；真实游戏版本仅 **1.16.5**，Java **1.8.0_482 x64**。正版与离线均由外部调用方仅通过公开 SDK 启动，观察到窗口、OpenAL 和纹理图集初始化后自动停止，未进入世界或测试多人服务器。Linux/macOS 路径与规则通过模拟/参数测试，未做对应实机启动。公开入口与用法见 [P0 API 清单](sdk.md)，详细实机记录保留在维护者本地。

实现依据：[.NET ArgumentList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0)、[并行读取进程输出](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-10.0)、[等待退出](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexitasync?view=net-10.0)、[停止进程树](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0)、[Java 启动命令](https://docs.oracle.com/en/java/javase/21/docs/specs/man/java.html)；参数结构来自已归档的官方版本 JSON Fixtures。

T12 当前源码会在标准启动入口、持有实例锁后检查并恢复中断的整包事务；恢复受外部修改阻碍时返回带结构化 DetailCode 的安装错误并保留备份。游戏启动/退出从不自动确认或清理整合包更新。需要验证后确认时，使用 [ModpackInstaller 的验证与确认示例](modpacks.md)。

T13 的同值重复参数会去重，自定义列表内的冲突值明确失败；同类 JVM 自定义参数覆盖元数据默认值。窗口与服务器统一从强类型配置生成；原来靠追加不同值覆盖的用法需迁移。详细行为变化及新增 UnsupportedGameSettings 见[高级启动设置](advanced-launch.md)。

T14 当前源码可在调用方明确请求后，先通过 JavaRuntimeUtil.InstallAsync 安装指定主版本或精确包，再将返回的 ExecutablePath 设置到 Java.JavaPath。LaunchAsync 不自动下载安装 Java，也不改变严格兼容检查；见 [Java Runtime](java-runtime.md)。

## 独立调用示例

- [启动与等待退出](examples/launch-minimal.cs)
- [状态、日志、退出与取消（高级）](examples/launch-advanced.cs)
- [启动已有实例](examples/launch-instance.cs)

## T10 简单启动配置

`LaunchConfig.Create(versionId, rootDirectory, account, java, maxMemoryMb, minMemoryMb = 0)` 直接接收 LaunchIdentity 与 JavaRuntime，返回原有 LaunchConfig；账号、Java 与最大内存均需明确传入。最小内存默认 0 省略 Xms，原构造/属性默认值不变。高级设置用 with 继续配置；启动器执行相同校验，不创建第二套启动引擎。

目录名称、启动版本 ID、共享资源和旧目录迁移规则见[目录管理](directory-layout.md)。

## 1.0.2 维护候选：已有版本通用启动

LaunchConfig.GameCore.VersionId 使用查询结果的 Id（实际版本文件夹名），不要求与 JSON id 同名。未知加载器不会触发“加载器不支持”拒绝；仍需有效主类、参数、客户端、依赖及资源。

外部目录优先使用 AutoInstall=false 检查并复用本地文件。Isolate=false 将工作目录设为根目录；GameDirectory 可显式给出根目录内的相对路径，空字符串也是根目录。已有 instances 或其他自定义布局通过此显式工作目录继续使用，不强制迁移。自动新建隔离目录继续是 versions/<InstanceId 或 VersionId>。

gameDir 和身份等受控参数按宿主配置生成，即使旧 JSON 使用硬编码路径也不修改 JSON。Classpath/模块路径/Java agent 仍必须位于选定根目录内；不接受通过 @参数文件、-jar 或另一模块入口绕过所选主类。原版 legacy 资源布局和多条 tweakClass 保持有效。

缺少本地下载地址时可以复用已有客户端/依赖，缺少发布者哈希时 VerifyAsync 会如实返回 Unverified；安全路径、已声明的大小/哈希、资源对象和 ZIP 边界仍校验。自动启动不会覆盖校验不符的外部文件；明确的 InstallAsync/RepairAsync 属于写入/修复操作。缺失依赖的相对路径和状态写入脱敏日志/诊断。Java 要求缺失时由宿主明确配置 FallbackMajorVersion，不自动推荐或下载 Java。

完整独立代码：[已有版本启动](examples/external-version-launch.cs)，要求宿主/用户先信任版本来源。范围和限制见[兼容说明](compatibility.md)。
