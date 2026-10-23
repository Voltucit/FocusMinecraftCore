# T02 版本管理

本手册对应当前源码。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Versions;

var catalog = await GameCoreUtil.GetAvailableVersionsAsync();
foreach (var v in catalog.Versions) Console.WriteLine($"{v.Id} {v.Type}");
```


命名空间：`FMCore.Versions`。无新增第三方依赖。

T06 补充 `GameCoreUtil.GetGameCoresAsync(root)` / `GetGameCoreAsync(id, root)` 及同步便捷方法，返回加载器识别、有效 Java 要求、继承元数据和本地 JAR 状态。原 VersionManager API 保留，示例见 [P0 SDK](sdk.md)。

## 入口

| API | 主要参数 | 返回值 |
| --- | --- | --- |
| `GameCoreUtil.GetGameCoresAsync(root, ct)` | 默认 `.minecraft`，可选取消 | `IReadOnlyList<GameCoreInfo>`，保留损坏项错误 |
| `GameCoreUtil.GetGameCoreAsync(id, root, ct)` | 本地版本 ID、根目录 | 单个 `GameCoreInfo`，含 Java、加载器识别、有效元数据 |
| `VersionManager.GetManifestAsync(policy, ct)` | 可选缓存策略 | `VersionManifest`：LatestRelease、LatestSnapshot、Versions |
| `GetVersionAsync(id, policy, ct)` / `ResolveAsync(id, policy, ct)` | 版本 ID、缓存策略 | 原始 `VersionMetadata` / 合并继承后的 `ResolvedVersion` |
| `GetLocalVersionsAsync(ct)` / `SaveLocalAsync(metadata, ct)` | 本地扫描 / 解析后的元数据 | 本地记录列表 / 保存完成任务 |
| `VersionParser.Parse(json)` / `ParseManifest(json)` | JSON 字符串 | 同一套元数据模型，不访问网络 |

最简本地查询：`var cores = await GameCoreUtil.GetGameCoresAsync(root, ct)`。需要联网目录、缓存或完整继承时使用下方 VersionManager 示例。异步方法支持取消；无效 ID、循环、缓存和 JSON 失败使用 `VersionException.Code`，错误列表见文末。同步别名会阻塞 UI 线程。

独立、可编译的调用代码见[本模块示例](examples.md#versions)。

一个根目录复用一个下载器和版本管理器。调用方负责 HttpClient 生命周期；Versions 不会释放它。

| API | 用途 |
| --- | --- |
| GetManifestAsync | 读取官方列表，包含最新正式版、快照标识及各版本下载地址、类型、SHA-1 和发布日期 |
| GetVersionAsync | 获取单个版本的原始元数据，不合并父版本 |
| GetLocalVersionsAsync | 离线枚举现有版本，返回解析错误及客户端 JAR 是否存在 |
| ResolveAsync | 递归合并继承，返回最终元数据、实际客户端 JAR 来源、下载描述和从父到子的继承链 |
| SaveLocalAsync | 写入已解析的本地/加载器配置；不安装加载器 |
| GetMetadataPath | 获取经过验证的版本 JSON 缓存路径 |
| VersionParser.Parse / ParseManifest | 直接解析 JSON，不访问网络或缓存 |

未来模块应使用已解析的模型和规则结果。DownloadArtifact、Libraries、Assets、Java、Logging 和启动参数已有统一表示；未建模字段通过 `ToJson()` 的独立副本取得，不影响已解析对象。原始版本与合并结果各自保留未知字段；合并结果的 JSON 是最终配置，不等于任一原始文件。

`ClientJarId` 与 `ClientDownload` 是客户端来源的最终结果，不能简单用子版本 ID 或合并 JSON 中的 downloads.client 推断 JAR。没有客户端信息的本地配置允许被解析，后续安装阶段应检查其是否完整。

## 获取本地版本

将 DownloadManager 的根目录设为现有 Minecraft 游戏目录（通常是 `.minecraft`），然后使用同一个版本管理器：

独立、可编译的调用代码见[本模块示例](examples.md#versions)。

只扫描一层 `versions/<id>/<id>.json`，按 ID 稳定排序，不扫描整块磁盘，也不递归搜索任意游戏文件夹。没有同名 JSON 的普通目录会被忽略。操作只读，不下载、不修改游戏文件，不读取或刷新官方列表缓存；已被其他启动器调整的本地配置也可独立解析。

结果为 LocalVersionInfo：Id、Type、MetadataPath、ClientJarId、ClientJarPath、HasClientJar、ErrorCode。继承版本使用最终解析的客户端来源，不要求每个子版本都拥有自己的 JAR。

HasClientJar 为 true 只表示已解析出安全路径，且对应客户端文件存在、大小非零；不检查 JAR 格式、大小是否符合官方清单、哈希、Libraries、Assets 或原生依赖。它不能作为游戏已完整安装或可启动的判断。

JSON 无效、父版本缺失、继承循环或单个目录不安全时，保留该条记录并设置 ErrorCode；HasClientJar 为 false 表示未确认存在有效来源的客户端文件，具体原因应结合 ErrorCode 判断。被拒绝的链接路径不会返回为可用 JAR 路径。整个 versions 根目录不安全或无法读取时，抛出 VersionException。支持 CancellationToken。

## 缓存

| 策略 | 行为 |
| --- | --- |
| PreferCache（默认） | 优先使用有效缓存，缺失或已知损坏时请求网络；不自动按时间刷新列表 |
| Refresh | 刷新官方列表及所请求版本链中的官方元数据；不覆盖列表外的有效本地配置 |
| CacheOnly | 只读本地文件；缺失或校验失败时直接报错，不访问网络 |

若缓存列表尚不包含新版本，应显式使用 Refresh。在线刷新失败不会静默把旧数据作为新结果返回，但此前的有效文件会保留，可再次用 CacheOnly 读取。

缓存布局：

```text
<下载根目录>/
├── .cache/metadata/version_manifest_v2.json
├── .cache/metadata/incoming/        # 校验前的临时元数据
└── versions/<id>/<id>.json
```

远程 JSON 经下载、SHA-1（版本清单提供时）、结构及 ID 验证后，才替换缓存。SDK 已记录的官方缓存会重新检查 SHA-1；本地自定义配置遵循文末 1.0.2 的保留策略。本地自定义配置或没有清单的离线 JSON 只能做结构与路径检查，不会被表述为通过官方哈希认证。

缓存读取和本地保存限制为 16 MiB，JSON 深度限制 64。路径复用 Downloader 检查，拒绝目录穿越、设备名、符号链接和 Windows 目录联接。管理器内部串行化缓存修改，不提供跨进程目录锁，也不承诺抵御恶意本地进程替换路径的竞态。

## 继承语义

- 子版本的标量字段覆盖父版本；未知对象字段递归保留，未知数组由子版本替换。
- Libraries 按 Maven 的 group、artifact、classifier、extension 判断冲突，忽略版本号。子版本条目优先，冲突的父条目被移除；不同 classifier 的依赖继续保留。
- 新版 JVM/Game 参数按父版本在前、子版本在后追加，保留每项的条件规则和参数数组。
- 子版本提供旧版 minecraftArguments 时，替换继承的游戏参数；新版参数继承旧版时，将旧版参数分词后作为前缀。不会展开变量、拼接 shell 命令或启动进程。
- 客户端下载、资源索引、Java 要求和日志文件的描述按各自语义覆盖，新的文件地址不会沿用父文件的哈希或大小。
- 没有自带客户端的子版本使用父版本客户端；显式 jar 可指定客户端来源。JAR 引用也参与循环检测。
- 继承深度最多 32 层。继承链从基础版本到当前版本排列，额外 JAR 引用由 ClientJarId 表达。

## OS、架构和功能规则

`RuleContext` 支持显式传入 Mojang OS 名称（windows、linux、osx）、架构、OS 版本和功能字典。`RuleContext.Current` 使用运行 .NET 的当前平台，未来选择不同 Java 架构时应传入对应上下文，不能直接把 .NET 进程架构当作 Java 架构。

无 rules 字段表示允许，空规则列表表示不允许；按顺序执行匹配规则，最后一个匹配的 allow/disallow 生效。OS、架构、版本及功能条件需要同时满足，未提供的功能视为 false。

常见的 x64/amd64/x86_64、arm64/aarch64、x86/i386/i686 别名按同一架构比较；其他架构表达式做完整正则匹配。OS 版本使用正则搜索，支持官方格式的前缀表达式。正则匹配有 100 ms 超时，错误规则不会静默放行。

原生依赖保留 classifiers、natives 和 extract.exclude；`GetNativeClassifier` 根据 OS 与 32/64 位替换 `${arch}`。T04 的 GameInstaller 复用这些模型选择和下载依赖，NativeExtractor 负责解压；实际 Classpath 字符串和游戏进程启动属于后续阶段。

## 错误与验证

版本错误使用 VersionException：InvalidMetadata、UnsafePath、NotFound、NotCached、InheritanceCycle、InheritanceTooDeep、RuleEvaluation、IoError。下载失败沿用 DownloadException，取消沿用 OperationCanceledException。异常不回显原始 JSON、来源 URL 或底层 HTTP 异常。

Windows / .NET 10 下 139 项测试通过，无失败或跳过，其中 T02 原有 58 项、本地枚举补充 17 项。包含真实官方 JSON、真实 Fabric 配置、构造的继承边界样本、规则条件、缓存、哈希、取消、本地文件存在检查及路径边界检查；默认自动测试不访问公网。本地枚举使用临时测试目录，未扫描用户的实际游戏安装目录。

2026-10-21 另外通过实际 SDK 访问官方服务，取得当时 920 条版本记录，解析 `1.21.1` 与 `26.4-snapshot-5` 的元数据，并在禁止联网的客户端下解析缓存及 Fabric 继承。此结果只证明元数据流程，不代表加载器安装、游戏安装或游戏启动通过验证；其他操作系统尚未实际验证。

脱敏测试样本及其来源、校验和保留在维护者本地，不随公开仓库分发。官方接口：[Mojang 版本列表](https://piston-meta.mojang.com/mc/game/version_manifest_v2.json)、[Fabric 元数据说明](https://github.com/FabricMC/fabric-meta/blob/master/README.md)。

T09-C–E 的历史规则要求空 URL 带 SHA-1；1.0.2 另允许已有第三方本地文件缺少发布者哈希，返回 Unverified，不作完整性认证。DownloadArtifact.IsLocalOnly=true、Url=about:blank。额外支持文件存于 VersionMetadata.GeneratedFiles（RelativePath/Sha1/Size；JSON 扩展 fmcoreGeneratedFiles），仅允许 libraries/ 内路径，由原 Installer 清单校验，不自动加入 Classpath。已有公开签名保留，详见加载器手册。

## 独立调用示例

- [官方可用版本](examples/versions-available.cs)
- [本地版本列表](examples/versions-installed.cs)
- [指定本地版本](examples/versions-info.cs)
- [继承解析](examples/versions-resolve.cs)
- [原始版本元数据](examples/versions-profile.cs)
- [保存本地元数据](examples/versions-save.cs)
- [缓存刷新与取消（高级）](examples/versions-advanced.cs)

## T10 官方目录入口

`GameCoreUtil.GetAvailableVersionsAsync(root, cachePolicy, cancellationToken)` 返回 VersionManifest，内部复用 VersionManager.GetManifestAsync。root 默认 .minecraft，缓存策略默认 PreferCache；本地查询仍使用 GetGameCoresAsync / GetGameCoreAsync，不混淆可下载版本与本地 profile。

## 1.0.2 维护候选：第三方目录与版本身份

使用原 GetGameCoresAsync / GetGameCoreAsync / GetLocalVersionsAsync / ResolveAsync 即可查询已有安装，不增加导入管理器。这里的“导入”是只读读取并将信息展示在宿主界面，不复制、保存或迁移已有目录。

- 本地 Id / ResolvedVersion.DirectoryName 是实际目录名，也是查询和 LaunchConfig 的 VersionId；MetadataId / Metadata.Id 是原 JSON id。两者可以不同。
- 同名 JSON 优先；没有同名 JSON 时读取该目录唯一的顶层 JSON；多个候选会报错，不猜测。继承引用可匹配唯一 JSON ID 别名；冲突返回新增 ConflictingProfiles。
- ResolvedVersion 和 GameCoreInfo 提供真实 MetadataPath、目录、客户端相对/绝对路径、基础版本提示、主类、继承参数、IsCustom 及 Warnings。基础版本是元数据推导提示，不保证是官方目录中的版本。
- 未识别的主类/加载器可返回 Custom，版本不会仅因未知分类被过滤。分类不承诺安装器或运行成功。
- 外部版本不会因与官方 ID 冲突而被 Refresh 替换。SDK 下载的官方缓存保留来源/哈希记录及原刷新修复行为，外部自定义配置则保留原文件并提供信任提示。

独立示例：[扫描导入](examples/external-directory-scan.cs)、[单版本查询](examples/external-version-query.cs)。完整身份及安全规则见[第三方兼容](compatibility.md)。
