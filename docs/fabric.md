# Fabric 版本查询、安装与启动

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Loaders;

var versions = await new FabricInstaller(".minecraft").GetLoaderVersionsAsync("1.16.5");
foreach (var v in versions) Console.WriteLine(v);
```


本模块包含于 FocusMinecraftCore 1.0.0，使用 NuGet 包或项目引用。唯一入口为 `FMCore.Loaders.FabricInstaller`；安装完成后继续使用现有 `MinecraftLauncher`、`InstanceManager` 和 `GameInstaller`，不需要另一套启动模型。

## 最简调用

以下完整示例查询可用版本、安装指定组合并启动。需事先安装元数据要求的 Java；示例组合要求 Java 8。离线身份不提供游戏购买或正版服务器权限。

独立、可编译的调用代码见[本模块示例](examples.md#fabric)。

省略 `loaderVersion` 时，选择官方列表中第一个 `Stable=true` 的兼容 Loader。没有稳定版本则报错，不会自动安装不稳定版本；需要时可明确传入查询所得的不稳定版本号。游戏的 `Stable` 表示正式版/快照，Loader 的 `Stable` 表示其自身稳定性，两者独立。

## 公开 API

| API / 模型 | 参数、返回值和行为 |
| --- | --- |
| `new FabricInstaller(rootDirectory = ".minecraft")` | 默认网络客户端与共享游戏目录 |
| `new FabricInstaller(downloads, versions = null, metaUri = null)` | 自定义 Downloader、同根 VersionManager、可选 HTTPS Fabric Meta **V2 根地址**；不接受 URL 凭据、查询串或片段 |
| `GetGameVersionsAsync(cachePolicy = Refresh, cancellationToken = default)` | `IReadOnlyList<FabricGameVersion>`；含 Version、Stable，保留官方顺序 |
| `GetLoaderVersionsAsync(minecraftVersion, cachePolicy = Refresh, cancellationToken = default)` | `IReadOnlyList<FabricLoaderVersion>`；含 Version、Stable、IntermediaryVersion，保留官方顺序，不隐去旧格式或不稳定版本 |
| `InstallAsync(minecraftVersion, loaderVersion = null, options = null, progress = null, cancellationToken = default)` | `FabricInstallationResult`；包含 MinecraftVersion、LoaderVersion、可启动的 VersionId 和既有 InstallationResult |
| `FabricInstallOptions.VersionId` | 默认 `fabric-loader-<Loader>-<Minecraft>`；可指定合法自定义 ID，不能覆盖原版或不同配置 |
| `FabricInstallOptions.Installation` | 既有 InstallationOptions，配置平台/架构/功能规则、实例相对目录、原生提取和缓存；默认 PreferCache |
| `FabricInstallationProgress` | Stage、CompletedFiles、TotalFiles；Installation 为既有安装阶段/下载进度，Download 为 SHA-1 元数据下载进度 |
| `FabricException.Code` | 见下文错误表；不会附带远程正文、URL、凭据或原始内部异常 |

查询默认刷新列表，安装默认优先使用已验证可解析的本地元数据。`Refresh` 重取 Fabric 和 Minecraft 元数据；`CacheOnly` 不获取元数据或 SHA-1，但**安装缺失的游戏文件仍可能联网**。如需完全离线启动，预先安装完成，再设置 `AutoInstall=false`；如需只读检查，用 `GameInstaller.VerifyAsync`。缓存按服务地址与版本组合隔离，写入前验证格式。损坏缓存在 PreferCache 下重取，CacheOnly 下明确失败，不掩盖错误。

安装进度顺序为 Metadata → Checksums → Installing → Completed；Installing 中复用原版阶段。字节进度从 `Installation.Download` 或 `Download` 读取。计数为当前阶段范围，不是整个安装的单调百分比。回调可能来自后台线程，GUI 自行切回 UI 线程；回调应快速返回。

## 高级配置、实例与修复

以下完整示例从实际 Java 生成目标规则、配置自定义版本 ID 和独立实例，验证/修复安装文件后生成统一启动配置。`FMCORE_JAVA` 可设置为 Java 8 的可执行文件或主目录；不设置时走有界自动检测。

独立、可编译的调用代码见[本模块示例](examples.md#fabric)。

重复执行安装会复用有效下载文件；示例的实例创建只需执行一次，已创建时改为 `GetAsync("fabric-demo")`。正版账号直接将 `LoginMicrosoftAsync`/`GetLaunchIdentityAsync` 的结果传给同一 `Account` 或 `CreateLaunchConfig(account)`，不需要 Fabric 专用认证。正版失败仍不降级为离线身份，完整流程见[账号管理](authentication.md)。

## 安装和支持边界

使用官方 Fabric Meta V2 的 `launcherMeta` 格式 1/2 和 Knot 客户端 profile，支持 `net.fabricmc.loader.launch.knot.KnotClient` 与 `net.fabricmc.loader.impl.launch.knot.KnotClient`。客户端下载链接、Libraries、参数、规则和原始扩展字段继续使用 Versions 的模型；继承原版客户端、资源、日志配置及 Java 需求，不生成占位客户端 JAR，不解析服务端配置，不运行任意处理器。

从 Maven `.jar.sha1` 补齐 profile 中缺失的依赖 SHA-1，写入最终版本 JSON，后续安装、检查和启动复用相同哈希。不以文件存在或 ZIP 非空替代完整性校验。侧边哈希缺失/无效、下载损坏均失败；哈希表示完整性检查，不是发布者数字签名。未自动安装 Fabric API 或任何 Mod。

原版缺少明确 Java 元数据，或 Loader 最低 Java 高于原版要求时，返回 UnsupportedJavaRequirement；不假设提高 Java 主版本后原版仍兼容。未来需要不同安装步骤的格式、LaunchWrapper、旧 minecraftArguments 型 Fabric profile、额外客户端 JAR/资源替换及处理器型 profile 均明确拒绝，不能仅因查询到版本就宣称安装受支持。

完整安装在内存解析合并后复用原版流水线，完成客户端、适用 Libraries、资源、原生提取和日志文件后才原子保存 profile。取消或失败时保留已验证的共享文件以供重试，可能保留该 ID 的原生目录，但不会新增可启动的 JSON。已有不同 profile 保持原样；语义相同且只有 time/releaseTime 变化时复用已有文件，不覆盖。此机制不能阻止其他应用直接修改共享游戏文件。

| FabricErrorCode | 含义 |
| --- | --- |
| InvalidMetadata | JSON、坐标、必需依赖或 SHA-1 无效 |
| UnsupportedFormat | 安装步骤/启动格式未实现，不静默跳过 |
| UnsupportedCombination | 返回的游戏/Loader 不匹配，或没有可自动选择的稳定版 |
| UnsupportedJavaRequirement | 无法在原版严格 Java 要求下使用此 Loader |
| ProfileConflict | ID 与原版相同，已有配置不同或发布时发生并发变化 |
| NotCached | CacheOnly 缺少必要元数据/哈希缓存 |
| InstallationBusy | 其他 FabricInstaller 正在安装同根目录 |
| UnsafePath / IoError | 路径越界、链接或文件访问失败 |

Downloader、Versions、Installer 失败保留原 `DownloadException`、`VersionException`、`InstallationException` 及其错误码；JSON/版本路径错误在 Fabric 解析边界归类为 FabricException。取消抛出 OperationCanceledException。安装器对象串行操作，不同对象通过同根 Fabric 文件锁协调，实际文件安装仍使用既有全局安装锁；暂存文件不作为成功标志。

## 验证记录

| 组合 / 环境 | 实际结果 |
| --- | --- |
| Minecraft **1.16.5** / Fabric Loader **0.19.5** / Java **1.8.0_482 x64** / Windows x64 / **离线身份** | 2026-10-21 使用公开 API 真实查询、下载 8 个 Fabric 依赖、复用 2,640 个原版文件；检查 2,686 个下载与原生文件通过；再次安装零下载、零 HTTP 请求 |
| 同一组合启动 | Fabric 加载日志、OpenAL 与纹理图集初始化，进程 MainWindowHandle 非零；通过 SDK 主动停止，退出码 -1，StoppedByCaller=true；不视为游戏正常主动退出 |
| 其他版本 / 格式 1 / 旧 Knot / 旧原版参数 | 官方格式 Fixtures 与合成依赖、测试子进程验证；不代表这些组合已实机启动 |
| 正版身份、世界/多人游戏、真实 Mod、Linux/macOS | 本阶段未实际验证 |

官方协议依据：[Fabric Meta V2](https://github.com/FabricMC/fabric-meta#v2)、[Fabric 官方客户端安装流程](https://github.com/FabricMC/fabric-installer/blob/master/src/main/java/net/fabricmc/installer/client/ClientInstaller.java)。Fabric 的客户端流程获取 profile 并下载 Libraries；FMCore 额外通过已有流水线完成继承的原版文件与资源安装。本文记录 T09-A 的独立验收。后续已实现 [Forge](forge.md)、[NeoForge](neoforge.md) 和 [OptiFine](optifine.md)；Quilt 已取消适配。

## 独立调用示例

- [查询加载器版本](examples/fabric-versions.cs)
- [安装加载器](examples/fabric-install.cs)
- [安装高级配置（高级）](examples/fabric-advanced.cs)
- [支持的游戏版本](examples/fabric-games.cs)
