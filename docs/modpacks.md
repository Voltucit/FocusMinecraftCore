# 整合包管理（1.0.3 维护候选）

本手册对应当前 1.0.3 源码，尚未发布；已发布包请以对应 Tag 的文档为准。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Modpacks;

var archive = Environment.GetEnvironmentVariable("FMCORE_ARCHIVE") ?? throw new InvalidOperationException("请设置 FMCORE_ARCHIVE。");
var info = await ModpackArchive.ReadAsync(archive);
Console.WriteLine($"{info.Name} {info.Version}: {info.Files.Count}");
```


命名空间 `FMCore.Modpacks`，仅支持 **Windows / .NET 10**。基础整合包功能始于 1.0.0，本轮兼容增强和普通实例导出属于 1.0.3。

## 自动识别格式

`ModpackArchive.ReadAsync(path)` 就是自动识别入口，不需要先判断扩展名或指定平台。读取压缩包根目录的 `modrinth.index.json` 时识别为 `ModpackFormat.Modrinth`，读取 `manifest.json` 的标准 CurseForge 清单时识别为 `ModpackFormat.CurseForge`。因此内部采用 Modrinth 格式的 `.zip` 和 `.mrpack` 使用完全相同的解析、导入、安装和更新流程。返回的 `Format`、`MinecraftVersion`、`Loader`、`LoaderVersion`、`Files` 可直接使用。

扩展名不能代替清单：缺少根清单、同时包含两种清单、清单损坏或不支持的格式版本会明确失败。不会猜测私有启动器配置，也不依赖导出工具名称。清单之外的普通覆盖文件按安全路径和哈希处理；它们的存在不是识别或启动的前置条件，不执行其中的脚本或私有指令。只读检查不提取或创建游戏目录。

`ModpackArchive` 读取 ZIP 元数据；`ModpackInstaller` 管理一个 `root/versions/id`。游戏、Libraries、Assets 由已有 Installer/Loaders 在共享根安装；实例中的 Mod、覆盖文件、两平台来源记录和版本配置作为同一事务提交。不要求依赖注入或调用方管理备份目录。

## 最简导入与安装

独立、可编译的调用代码见[本模块示例](examples.md#modpacks)。

`ImportAsync` 是 `InstallAsync` 的简便入口，二者行为一致。它们读取本地归档、安装清单指定的客户端内容、准备准确的游戏/加载器，并创建或更新独立实例配置。同一已确认整合包重复安装会校验并复用；不同包或修改过的包请调用 `UpdateAsync`。

## 公开 API

所有下表异步方法均在最后接收可选 `CancellationToken cancellationToken = default`。耗时安装/更新还接收可选 `IProgress<ModpackProgress> progress = null`。

| 入口 | 参数与返回值 |
| --- | --- |
| `ModpackArchive.ReadAsync` | `archivePath`；根据内部根清单自动识别，返回 `ModpackInfo`，只读解析，不安装。`Files` 包含可选客户端文件，不包含服务端专用文件 |
| `new ModpackInstaller(rootDirectory, instanceId, options?)` | 实例 ID 沿用 Instances 安全规则；构造不下载 |
| `ImportAsync` / `InstallAsync` | 本地归档路径；返回 `ModpackInstallResult`：安装快照、`UpdateId`、`Reused` |
| `GetInstalledAsync` | 返回 `InstalledModpack?`；不存在为 null，自动恢复中断事务，不联网 |
| `GetUpdatePlanAsync` | 新归档路径；解析准确的平台文件引用，返回 `ModpackUpdatePlan`，不下载文件正文 |
| `UpdateAsync` | 新归档路径；重新在实例锁内计算计划并应用整包事务 |
| `CheckModUpdatesAsync` | 返回 `IReadOnlyList<ModpackModUpdate>`；只检查包内已识别来源的 Mod，不更新文件 |
| `UpdateModAsync` | `projectId, authorize = false, source = "modrinth", progress?`；复用 T11 更新单个 Mod 及必要依赖，返回整包事务结果 |
| `CreateValidationLauncherAsync` | `LaunchIdentity account`；返回标准 `MinecraftLauncher`，读取最新实例配置；由调用方保持其生命周期 |
| `ConfirmUpdateAsync` | `Guid updateId`；明确确认当前更新正常，返回 `ModpackCleanupResult`；根据配置自动清理 |
| `CleanupAsync` | `Guid updateId`；手动清理已确认更新，只删除它的旧备份和不再活动的对应包下载缓存 |
| `RollbackAsync` | `Guid updateId`；恢复最近且尚未清理的更新；跨越后续更新、外部修改或备份损坏时拒绝 |
| `ExportAsync` | `destination, ModpackExportOptions?`；已有整包保留原格式；普通实例生成标准 Modrinth 清单，可保存为 `.zip` 或 `.mrpack`。不覆盖目标或修改源游戏文件 |

`InstalledModpack` 保存格式、包名、版本、准确 Minecraft/加载器、文件、来源标识、安装时间、修订号、`PendingUpdateId`、`RuntimeInstalled` 和 `IsModified`。`ModpackFile` 保存安全相对路径、大小、SHA-512/SHA-1、下载引用、可选状态、覆盖标志及 CurseForge 项目/文件 ID。调用方可读取快照，但修改返回模型不会改变磁盘上的所有权。

## 高级配置与进度

独立、可编译的调用代码见[本模块示例](examples.md#modpacks)。

`ModpackOptions` 默认安装 Runtime、排除可选文件、保留用户修改配置、仅在明确确认后自动清理。`Modrinth` 可传调用方的 `ModrinthClient`，`CurseForge` 使用合法 Key；`DownloadClient` 必须是无认证凭据的独立连接，不会转发 API Key。`JavaPath` 只用于需要 Java 的加载器处理器；运行时 Java 仍由实例/启动配置选择。

包作者可能为新 Minecraft 固定一个平台只标注旧游戏版本的 Mod，或为旧 Mod 固定比其平台声明更新的依赖版本。默认以清单固定选择为依据，在 `ModpackInfo.CompatibilityWarnings` 返回这些文件的安全相对路径，不自行更换版本，也不据此宣称实际游戏兼容。包来源记录保留原始依赖声明；T11 来源清单将被整包覆盖的必需版本约束记录为项目依赖，以便后续复用其检查。缺少必需项目或声明不兼容时仍失败。设置 `RequireDeclaredModCompatibility = true` 可对这些差异明确拒绝。单独调用 T11 更新时仍按 T11 的目标环境和依赖筛选，不继承这种包作者选择权。

`InstallRuntime = false` 是高级的**仅包内容安装**模式。此时 `RuntimeInstalled = false`，不代表游戏或加载器已安装、可启动。默认流程确实调用已有原版/Fabric/Forge/NeoForge 安装器，检查其实际结果；不会仅生成 profile 就报告完整安装。处理器失败时活动实例不改变，共享下载缓存可以保留。Runtime 的旧版 resources/natives 布局在共享暂存目录准备，启动时复用现有 Launch 在实例内物化。

进度阶段：Reading、Resolving、Runtime、Downloading、BackingUp、Committing、Completed。下载进度沿用 Downloader；Runtime 细节沿用 `InstallationProgress`。回调应及时返回，不在回调中同步等待同一实例操作；UI 自行切换到界面线程。

## 整包更新、确认与回滚

独立、可编译的调用代码见[本模块示例](examples.md#modpacks)。

计划包含 `Add`、`Delete`、`Replace`、`Conflict`、`Preserve`。它是当时状态的预览，真正更新会重新计算，不能把预览当成文件预留。文件正文先下载/校验，原文件和控制文档先备份；持久化日志后，先移出全部旧文件，再引入新文件，最后原子提交整包修订号。两平台来源记录和实例版本配置也在该事务中。失败/取消恢复整个旧集合；被外部改动阻碍时保留证据和备份，明确返回 `RecoveryRequired`。

每个实例同一时间最多一个未确认更新（首次安装亦如此）。下一次修改前必须确认或回滚；避免依次覆盖尚未验收的恢复点。确认后关闭自动清理时仍可回滚，但仅限最近修订，且活动文件不能被外部改动。中断恢复以提交点区分“尚未完成”与“已完成但日志尾部未写入”，不会将完整的新包误回退。

旧 Mod 的备份位于实例私有控制目录，始终不在 `mods` 中。未确认、失败启动或中断均不触发自动清理。`CleanupAsync` 只接受已确认的更新 ID；不递归删除备份目录，不删除未知文件、其他实例、共享文件或其他更新的备份。删除失败通过 `FailedPaths` 返回，可在游戏停止后重试，不修改活动文件。T11 自身的通用下载缓存不属于整包旧备份清理范围。

用户修改过的配置默认保留并放弃其包所有权，不强行覆盖或删除。未改动的包配置可随清单更新，旧内容有备份。游戏运行后写回配置不会阻止确认；主动回滚仍拒绝覆盖这种新写入。存档、账户、共享运行文件和用户自加 Mod 不纳入整包删除集合；用户 Mod 与已识别项目重复/冲突时复用 T11 检测并报告。

## 启动验证和显式确认

独立、可编译的调用代码见[本模块示例](examples.md#modpacks)。

启动复用普通 Launch 的账号、版本规则、Java、日志、退出信息和锁。成功创建进程、退出码 0 或缺少崩溃日志都不是“新版正常”的可靠证明；SDK 从不据此自动确认。游戏准备/运行期间安装、确认、回滚、清理和冲突实例修改返回 Busy/Conflict，停止并等待退出后才能继续。中断事务也会在标准启动入口检查恢复，避免启动半套 Mod。

## 包内单个 Mod 更新与批量检查

独立、可编译的调用代码见[本模块示例](examples.md#modpacks)。

两个入口调用 T11 的 `CheckUpdatesAsync` / `UpdateAsync`，复用依赖解析、冲突检测和下载校验，不另造“最新版”规则。整包管理的 Mod 默认锁定；直接从 T11 调用修改它们也返回 `ResourceErrorCode.ModpackLocked`。授权单 Mod 更新后设置 `IsModified = true`，使用同样的确认、清理和显式回滚机制。

**批量检查不等于批量执行**。T12 不新增普通 Mod 的原子批量更新 API；逐个更新需要逐个确认或回滚。整包整体更新按新版清单的精确版本执行，不调用 Latest。清单未固定必需依赖时返回 `DependencyConflict`，不擅自补最新版。无法在平台识别来源的外部/覆盖 Mod 可随整包更新，但不能猜测其项目并单独自动更新。

## 导出

独立、可编译的调用代码见[本模块示例](examples.md#modpacks)。

默认仅输出准确下载引用，配置/覆盖文件不默认导出。已有整包保留原格式；CurseForge 整包继续只写项目/文件引用，不打包 CDN JAR，不因输出文件名是 `.zip` 就改变清单格式。`Name`、`Version` 可显式覆盖导出包的展示名称和版本，不修改原实例。

普通游戏实例使用相同的 `new ModpackInstaller(root, instanceId).ExportAsync("instance.zip")`。SDK 从实例配置或同名版本目录读取已有版本元数据，区分实际目录、启动版本和基础 Minecraft，生成根 `modrinth.index.json`。没有整包状态也可导出，不需要先伪造导入记录或安装一个整合包。默认包名来自实例名，包版本为 `1.0.0`，可通过选项指定；不会重新安装 Runtime 或修改来源记录。

普通实例会检查 `mods` 中的 JAR、`resourcepacks` / `shaderpacks` 中的 ZIP：通过 SHA-512 查询 Modrinth 准确文件，验证 SHA-1、大小及项目类别后写入下载引用；FMCore 已记录来源的 CurseForge Mod 需传合法 `CurseForgeClient`，验证分发许可、固定文件及本地哈希。未识别的资源、无法验证的下载权限、已修改的托管 Mod，以及不能用标准 Minecraft/Fabric/Forge/NeoForge 依赖准确表示的自定义启动配置会明确拒绝。不会静默遗漏未知资源，也不猜测 CDN 地址或绕过权限；不支持把解压后的资源包/光影目录直接冒充平台 ZIP。

普通实例的标准 Modrinth 导出保留准确下载引用，不保证导入后仍保留 CurseForge 项目/文件 ID 的单 Mod 更新语义；需要该语义时应保留原有 CurseForge 整包格式。

覆盖文件由 `ModpackExportOptions` 明确控制：

| 选项 | 行为 |
| --- | --- |
| `IncludeOverrides = false`（默认） | 不导出覆盖文件 |
| `IncludeOverrides = true`，`OverridePaths` 为空 | 保留旧行为：仅导出校验一致的整包托管覆盖文件；普通实例不自动扫描配置 |
| `IncludeOverrides = true`，`OverridePaths` 非空 | 只导出指定的相对文件，例如 `config/example.toml`、`options.txt`，可避免导出其他私有配置 |
| `AllowBinaryOverrides = true` | 调用方明确确认所选二进制覆盖文件拥有分发权；不豁免敏感数据、路径或哈希检查 |

配置文本须为有效 UTF-8，并经过敏感信息检查；含账号、密钥或私有身份字段时拒绝。受保护的存档、账户、共享运行文件、日志和截图等路径仍不能被 `OverridePaths` 选中。修改过的包托管配置不冒充原内容导出。目标已存在、输出位于源实例内、取消或源文件在导出时被外部修改均不会覆盖已有目标；临时归档在发布到目标前通过同一导入解析器复检。

独立示例：[普通实例最简导出](examples/pack-export-instance.cs)、[选择配置、取消与错误处理](examples/pack-export-instance-advanced.cs)。

## 格式、安全与限制

- Modrinth：支持 formatVersion 1，SHA-1 与 SHA-512、客户端 env、可选文件选择及 downloads → overrides → client-overrides 分层；排除 server-overrides。下载初始域名白名单为 cdn.modrinth.com、github.com、raw.githubusercontent.com、gitlab.com，并兼容 edge.forgecdn.net、mediafilez.forgecdn.net、media.forgecdn.net 的清单明确引用；必须 HTTPS，不接受凭据、查询参数或片段，不生成或猜测缺失下载地址。两种哈希和大小仍严格校验。`.mrpack` 是该格式的标准扩展名；`.zip` 是 FMCore 提供的容器识别兼容，上传平台请遵守其文件命名和域名规则。见 [Modrinth 官方格式](https://support.modrinth.com/en/articles/8802351-modrinth-modpack-format-mrpack)。
- CurseForge：支持 manifestVersion 1 / minecraftModpack、固定 projectID/fileID、required、覆盖目录及单一主加载器；平台文件通过合法 API Key 解析，禁止分发、缺少 URL、提前付费访问等仍明确限制，不绕过。当前引用文件仅支持 T11 已支持的 Minecraft Mod JAR；包含其他资源类别时明确 Unsupported。格式导入导出参见 [CurseForge 官方说明](https://support.curseforge.com/support/solutions/articles/9000198501-exporting-and-importing-modpacks)。
- 游戏依赖支持原版、Fabric、Forge、NeoForge；Quilt、混合加载器、未知依赖或旧格式明确拒绝。未声明加载器的包可以包含一般覆盖内容，不能宣称识别了未知 JAR 的运行需求。
- 归档上限：10,000 个 ZIP 条目、5,000 个选中文件、每个平台最多 512 个可识别 Mod（沿用 T11 来源清单上限）、单文件 512 MiB、总展开及归档 2 GiB、超过 1 MiB 的条目压缩比不超过 200；JSON 上限 2 MiB。实际解压流另按声明长度限流，拒绝伪造尺寸。事务恢复最多检查 1,000 个操作目录，超限明确报错；请避免无限累积失败预下载目录。
- 拒绝目录穿越、Windows 设备名/流、文件与父目录冲突、大小写重复、ZIP 符号链接及本地 reparse/junction 越界。客户端与服务端专用覆盖的同路径分层是格式规定的覆盖行为，与同层重复条目不同。
- 所有操作共用实例锁；外部非 SDK 进程写入通过哈希复检发现。发生冲突不强行恢复或清理外部文件，也不保证第三方绕过锁写入时仍可无条件自动回滚。
- 只在整包安装时自动创建缺失实例；不会导入第三方启动器的账号、个人目录、整套 Runtime 或实例专属启动参数。

## 异常与验收范围

`ModpackException.Code` 区分 InvalidRequest、InvalidArchive、ArchiveLimitExceeded、Unsupported、UnsafePath、Conflict、Busy、NotFound、DownloadFailed、DownloadRestricted、DependencyConflict、RecoveryRequired、StorageCorrupted、ConfirmationRequired、UpdateLocked、RuntimeFailed、IoError。`ResourceError` / `DownloadError` 保留来源类别，异常不包含原始 HTTP 响应、下载地址、Key 或内部异常。取消单独抛 `OperationCanceledException`；开始回滚后会忽略取消完成安全恢复。

本地自动化使用真实格式脱敏 JSON、模拟平台/下载、真实文件系统与模拟游戏子进程；这不等于真实 CurseForge 权限或真实整合包游戏启动。当前源码测试及独立包引用结果见 README/路线记录，真实接口验证与限制单独记录。发布版本与状态见项目 README。

T12 验收：新增 89 项自动化测试，全套 819 项通过；独立 NuGet 调用方覆盖两格式和完整更新生命周期，47 段文档 C# 示例编译通过。真实 Modrinth 包内容验证为 Fabulously Optimized 5.10.1 → 5.10.2、Minecraft 1.20.4、Fabric 0.15.11，两版各 78 文件独立 SHA-512，整包更新/回滚/确认清理/引用导出通过。该真实流程明确关闭 Runtime 安装，未进行对应游戏启动。其 5.10.3 归档含同一路径不同哈希的重复清单项，已按安全规则拒绝，没有采用最后一项覆盖；不宣称该包版本可安装。CurseForge 仍无合法 Key 的真实验收。

1.0.3 样本验证（2026-10-24）：`来自洞穴.zip` 由标准 Modrinth 清单识别为 Minecraft 1.21.1 / NeoForge 21.1.251，包含 5 个下载资源和 12 个覆盖文件。Windows x64 / Java 21 下，独立目录的真实联网包内容安装和默认完整 Minecraft / NeoForge 安装均成功，17 个文件独立哈希/大小校验通过；选择一份普通配置导出 ZIP 后，6 个清单文件再次导入成功。未依赖私有启动器配置，未启动该整合包游戏，不将安装通过表述为 Mod 运行、世界或联机已验证。样本与实际下载文件仅本地保留；自动化回归中的离线传输使用这些真实字节和模拟 HTTP，与真实联网验证分别记录。普通实例资源导出的平台许可分支使用模拟接口回归，未新增真实 CurseForge API Key 验收。

## 独立调用示例

- [只解析整合包](examples/pack-read.cs)
- [导入整包](examples/pack-import.cs)
- [安装整包](examples/pack-install.cs)
- [整包更新计划](examples/pack-plan.cs)
- [整包事务更新](examples/pack-update.cs)
- [读取整包状态](examples/pack-installed.cs)
- [导出整包引用](examples/pack-export.cs)
- [普通实例导出标准 ZIP](examples/pack-export-instance.cs)
- [普通实例导出配置与取消](examples/pack-export-instance-advanced.cs)
- [确认正常使用](examples/pack-confirm.cs)
- [清理已确认备份](examples/pack-cleanup.cs)
- [主动回滚整包](examples/pack-rollback.cs)
- [包内 Mod 批量检查](examples/pack-check-mods.cs)
- [授权更新包内单 Mod](examples/pack-update-mod.cs)
- [整包高级更新（高级）](examples/pack-advanced.cs)
- [导入 CurseForge 整包](examples/pack-curseforge-import.cs)
- [整包验证启动器](examples/pack-validation-launcher.cs)

首次整包导入/安装同样返回待确认事务；确认用户正常使用（或明确回滚）之后，才能继续更新/修改。不能只按“是否替换旧包”判断是否需要确认。

目录名称、启动版本 ID、共享资源和旧目录迁移规则见[目录管理](directory-layout.md)。
