# T04：原版游戏安装

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Installer;

var result = await new GameInstaller(".minecraft").InstallAsync("1.16.5");
Console.WriteLine(result);
```


入口：`FMCore.Installer.GameInstaller`、`FMCore.Assets.AssetManager`、`FMCore.Installer.NativeExtractor`。保留三个项目，无新增第三方依赖。

T06 补充 `new GameInstaller(root)`、默认只读本地缓存的 `VerifyAsync` 和复用安装流程的 `RepairAsync`；原构造和方法签名保留。核验范围、缺索引和未提供哈希时的语义见 [P0 SDK](sdk.md)，与自动启动的衔接见 [Launch](launch.md)。

T08 当前源码补充解压后原生文件检查，`VerifyAsync` / `RepairAsync` 保留原签名并复用相同安装清单；1.0.0 包含这些配置与返回值，详见[完整性检查与修复](integrity.md)。

## SDK 用法

| API | 主要参数 | 返回值 |
| --- | --- | --- |
| `new GameInstaller(root)` | 游戏根目录 | 自动组合现有下载/版本/资源模块 |
| `InstallAsync(id, options, progress, ct)` | 版本 ID，可选规则、游戏目录、缓存、进度/取消 | `InstallationResult`，安装计划、路径、文件/字节统计 |
| `VerifyAsync(id, options, progress, ct)` | 同一版本 ID，默认仅用缓存 | `InstallationCheckResult`：IsValid、Problems、Files、AssetsChecked |
| `RepairAsync(id, options, progress, ct)` | 同安装配置 | 同安装结果，复用有效完整文件 |
| `CreatePlan(resolved, options)` | 有效继承元数据与规则上下文 | `InstallationPlan`，不开始下载 |

最简调用：`await new GameInstaller(root).InstallAsync("1.16.5", cancellationToken: ct)`。不需要先检测 Java 才能下载文件；若指定其他架构，应传匹配的 `InstallationOptions.Context`。高级共享下载器示例如下。取消抛 `OperationCanceledException`；安装、版本、下载及资源错误分别保留其模块异常和错误码，详见文末及对应模块手册。

独立、可编译的调用代码见[本模块示例](examples.md#installer)。

安装器与版本管理器必须共用根目录。已选定 Java 时，`Context` 应使用 `selectedJava.GetRuleContext(features)`，使操作系统、架构和功能条件与未来启动保持一致；下载游戏文件本身不要求运行 Java。`GameDirectory` 是相对根目录的游戏目录，空字符串使用根目录，后续隔离实例可传入 `instances/name`。缓存策略沿用 T02，只影响版本元数据。

`InstallationResult` 返回计划、客户端路径、绝对 Classpath 文件列表、原生目录、资源布局和日志路径，以及下载/跳过的文件数、成功落盘的下载字节。字节统计不包含失败重试的网络流量或元数据请求。结果表示所列安装步骤已完成，不能代替实际游戏启动验证。

## 安装流程与文件布局

阶段依次为 Metadata、Client、Libraries、AssetIndex、AssetObjects、AssetLayout、Natives、Logging、Completed。每阶段报告完成/总文件数；下载事件还包含文件相对路径、已收字节、总字节、尝试次数和状态。并发下载的进度回调可能来自不同线程，UI 应调度到自己的线程。只有全部步骤成功才发送 Completed，取消沿用 `OperationCanceledException`。

```text
<下载根目录>/
├── .cache/install.lock
├── versions/<客户端来源 ID>/<客户端来源 ID>.jar
├── versions/<版本 ID>/natives/<os>-<arch>/
├── libraries/<Maven 路径>
├── assets/indexes/<索引 ID>.json
├── assets/objects/<哈希前两位>/<SHA-1>
├── assets/log_configs/<日志文件 ID>
├── assets/virtual/<索引 ID>/<逻辑路径>    # virtual
└── <GameDirectory>/resources/<逻辑路径> # map_to_resources
```

客户端使用 T02 已解析的 `ClientJarId` 与 `ClientDownload`，继承版本可以复用父客户端。Libraries 通过同一规则引擎筛选，再按相对路径去重；冲突的哈希/大小不会静默覆盖。普通 artifact 和 legacy `natives` classifier 同时存在时都下载；classifier 缺少显式 path 时由 Maven 坐标生成。日志文件按元数据哈希/大小下载。

新版 LWJGL 常把原生 JAR 写成独立的 `downloads.artifact`。这些 JAR 保留在 Classpath 中，按元数据规则选取；不根据文件名自行改写规则或拆除 Classpath 条目。部分官方规则会允许同一系统的多种架构 JAR，由对应库在运行时选择。

下载复用 T01：已有文件先按提供的 SHA-1/大小校验，通过则跳过 HTTP；损坏或缺失时重下。未完成的临时下载不会作为成品复用，后续调用会重新下载该文件；不提供 HTTP Range 续传。元数据既无哈希又无大小时无法证明已有文件完整，会重新下载；仅有大小时只能检查大小，不能宣称通过哈希认证。官方 1.16.5 验证中的全部下载文件都有 SHA-1 与大小。

同根目录使用独占的 `.cache/install.lock` 文件句柄，重叠安装返回 InstallationBusy，取消/异常会释放句柄；空锁文件可以保留。安装不是全目录事务，失败时已完成并校验的文件留待重试复用。

## 资源及旧版布局

`AssetIndex.Parse` 校验对象 SHA-1、非负大小、逻辑路径和重复声明；相同哈希的对象大小必须一致。索引限制 16 MiB、深度 16、最多 200,000 个逻辑对象。下载以哈希去重，使用官方对象地址 `https://resources.download.minecraft.net/<前两位>/<SHA-1>`。

`AssetManager.GetIndexAsync` 下载/校验并解析索引，`CreateDownloads` 返回去重的对象请求，`MaterializeAsync` 生成布局。

| 索引标记 | 布局 | 返回给后续启动的 GameAssetsDirectory |
| --- | --- | --- |
| 均为 false | `assets/objects` | `assets` |
| virtual=true | 另复制到 `assets/virtual/<index>` | 对应虚拟目录 |
| map_to_resources=true | 另复制到 `<GameDirectory>/resources` | 对应 resources 目录 |
| 两者都为 true | 两种布局都生成 | 虚拟目录，另返回 ResourcesDirectory |

资源对象始终保存在内容寻址目录；映射副本以 SHA-1/大小校验，使用临时文件验证后替换，可重复执行及修复。resources 布局只更新索引列出的文件。索引及副本路径拒绝穿越、设备名和符号链接/目录联接。

官方 1.6.4 的 legacy 索引使用 virtual，1.5.2 的 pre-1.6 索引使用 map_to_resources；此处按实际索引标记处理，不按版本字符串猜测。`AssetsRootDirectory` 可用于 `${assets_root}`，`GameAssetsDirectory` 可用于旧参数 `${game_assets}`，日志路径供 `${path}` 替换；参数展开和进程启动留给后续 Launch 阶段。

本阶段支持有客户端描述和资源索引的旧元数据；缺少这些信息时返回 MissingClient/MissingAssetIndex，不调用已停用的历史资源列表服务。旧版完整安装/游戏运行不以索引 Fixtures 通过为依据。

## 原生依赖复用与解压

`installer.CreatePlan(resolvedVersion, options)` 只生成计划，不下载；`plan.NativeArchives`、`plan.ClasspathEntries` 和 `plan.Context` 可供启动模块复用。独立的 `new NativeExtractor(root).ExtractAsync(plan.NativeArchives, plan.NativeDirectory, progress, ct)` 使用相同解压逻辑。目标目录须专供解压器管理，成功替换时会移除旧目录中的过期文件。

解压先校验压缩包已有哈希/大小，默认跳过 JAR 的 META-INF，并应用 `extract.exclude` 前缀。官方 1.16.5 的多个原生 JAR 含有不同的索引和模块元信息，不能把这些元信息当作冲突的原生文件。共享压缩包的重复声明保留任一声明需要的文件；跨压缩包同名内容相同可复用，不同内容报 ConflictingFiles。

ZIP 条目拒绝绝对路径、路径穿越、反斜杠歧义、Windows 设备/流名称、重复及大小写别名、符号链接和特殊文件。默认最多 10,000 个条目、单条目 256 MiB、总解压 1 GiB，可通过 `NativeExtractionOptions` 调整；声明大小和实际读出大小都受检查。

在相邻临时目录完成解压后，通过目录重命名替换目标；发生受控错误/取消时清理自身临时目录并保留旧目录。目录替换包含两次重命名，进程被强制终止可能留下临时/备份目录；再次安装会重新生成最终原生目录。路径检查不承诺抵御恶意本地进程同时替换目录的竞态，也不绕过运行中游戏对 DLL 的文件锁。

## 错误与验证

安装专属错误使用 `InstallationException.Code`，例如 MissingClient、MissingAssetIndex、MissingNativeClassifier、UnsupportedPlatform、ConflictingFiles、InvalidArchive、ArchiveLimitExceeded、UnsafePath、IoError、InstallationBusy。资源错误使用 `AssetException.Code`；版本、下载和路径检查沿用 VersionException / DownloadException。异常不包含源 URL、原始 JSON 或原始系统异常。

Windows / .NET 10 下新增 64 项离线测试，全套 268 项通过、无跳过。测试覆盖真实官方 JSON/索引解析、模拟完整安装、重复执行、同长度损坏文件、取消与失败后重试、旧布局、依赖规则/继承、原生校验/覆盖/回滚、ZIP 路径和链接、解压限额。默认测试不访问公网，也不把模拟客户端字节作为实际游戏验证。

2026-10-21 实际验证正式版 **1.16.5 / Windows x64**，使用 Java 8u482 的平台上下文，通过 SDK 下载并独立检查 2,640 个文件的官方 SHA-1/大小，合计 **401,773,932 字节**；含 41 个依赖文件、1 个客户端、1 个索引、2,596 个去重资源对象（2,615 个逻辑资源名）及 1 个日志配置。8 个原生压缩包解压得到 38 个文件，并逐一与包内内容比较 SHA-256。安装失败后的完整下载文件在续装中复用；完成后再运行一次，下载数 0、跳过数 2,640、HTTP 请求数 0。

验证目录与脱敏统计保留在维护者本地，不随公开仓库分发。没有下载 Java，没有启动 Minecraft，也没有验证认证、加载器、Linux/macOS 或其他架构的实际安装与运行。

资料：[Mojang 版本清单](https://piston-meta.mojang.com/mc/game/version_manifest_v2.json)、[Mojang LegacyLauncher 的资源目录使用](https://github.com/Mojang/LegacyLauncher/blob/master/src/main/java/net/minecraft/launchwrapper/injector/VanillaTweakInjector.java)、[.NET ZIP/TAR 安全说明](https://learn.microsoft.com/en-us/dotnet/standard/io/zip-tar-best-practices)。

T09-A 当前源码新增 [FabricInstaller](fabric.md)，直接复用本文安装、校验和修复规则；上述“未启动或验证加载器”仅指 T04 当时的验证范围。Fabric 的实际版本组合和启动验收另见 Fabric 手册。

T09-C–E 进一步通过同一清单校验处理器生成库和附加支持文件。普通远程文件可按原规则修复；生成文件缺失/损坏时返回 LocalFileUnavailable，重新调用对应加载器安装器恢复，不为生成产物伪造下载地址。入口见 ForgeInstaller、NeoForgeInstaller、OptiFineInstaller 及各自中文手册。

## 独立调用示例

- [原版安装](examples/game-install.cs)
- [安装计划](examples/game-plan.cs)
- [安装进度与取消（高级）](examples/game-install-advanced.cs)
