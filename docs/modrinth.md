# Modrinth 资源与 Mod 管理（T11 第一部分）

本手册对应当前源码。目录布局调整尚未发布，已发布包请以对应 Tag 的文档为准。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Resources;

var client = new ModrinthClient();
var result = await client.SearchAsync("fabric api", new() { MinecraftVersion = "1.20.4", Loader = "fabric" });
Console.WriteLine(result);
```


`FMCore.Resources` 包含于 1.0.0，可通过 NuGet 包或项目引用使用。只增加核心内的 Resources 文件夹，不新增业务项目、第三方依赖或 GUI 绑定。

本页保留 Modrinth 第一部分的 API 与历史验收。当前源码另已完成 [CurseForge 第二部分](curseforge.md)，使用调用方 API Key 并尊重禁止自动下载设置；两平台共用实例锁和事务恢复。

## 最简安装

独立、可编译的调用代码见[本模块示例](examples.md#modrinth)。

目录为 `.minecraft/versions/fabric-1204/mods`。这个入口允许在安装游戏之前准备独立目录，但**不会安装游戏或加载器**；`ModEnvironment` 是调用方明确声明的目标，不代表环境已经存在。使用 `MinecraftLauncher` 时设置相同的 `RootDirectory`、`InstanceId` 及实际安装的加载器版本。已有 FMCore 托管实例请使用下面的快照入口，显式目录入口会拒绝绕过其配置检查。

自动选择同时支持目标 Minecraft 版本和加载器的最新发布日期版本，默认只采用平台标记为 `release` 的渠道；不比较版本号字符串。`IncludePrereleases=true` 允许自动选择 `beta/alpha`；显式 `versionId` 或平台必要依赖指定的版本可以采用其他渠道，但仍须通过 Minecraft/加载器检查。版本名包含 `pre` 不代表其平台渠道一定是 beta。

## 接入托管实例与启动

独立、可编译的调用代码见[本模块示例](examples.md#modrinth)。

前提：`fabric-play` 已创建且本地版本元数据可解析。快照入口从实例的 `VersionId` 自动读取 Minecraft 版本及唯一的 Fabric、Forge 或 NeoForge 加载器，检查实例是否被修改或删除。OptiFine 单独安装不是这里支持的 Mod 加载器。实例运行、启动准备、修改、删除和资源操作共用同一套实例锁；冲突返回 `Busy`。不保证外部启动器/文件管理器遵守 SDK 锁，操作期间应避免外部修改目录。

## 查询、取消、进度与更新

独立、可编译的调用代码见[本模块示例](examples.md#modrinth)。

调用方拥有 `HttpClient`，须保持其存活。SDK 为每个 API 请求设置可识别的 User-Agent；正式接入请使用自己启动器的名称和联系方式。响应正文上限 16 MiB，API 默认超时 45 秒（包含完整正文）；HTTP 429 返回 `RateLimited` 和可用的 `RetryAfter`，API 查询不会暗中重试。文件下载复用 Downloader 的并发、超时、重试、进度、取消、大小和 SHA-512 校验。取消不会自动切换平台或身份。进度可能来自工作线程，GUI 请使用 `Progress<T>` 或自己的 UI 调度器；回调不应抛出异常。

检查更新仅返回比记录日期更新、ID 不同且适配目标的候选，不改变活动 Mod 文件；不会把较早版本自动当作更新。检查到候选不表示整组依赖必定可升级，`UpdateAsync` 会再次解析必要依赖，并检查已安装其他 Mod 的固定版本要求。需要协调升级时可显式安装父项目的新版本；目前没有通用依赖回溯求解或批量原子更新所有项目。

## 公开 API

所有 I/O 方法返回 `Task<T>`，最后一个可选参数为 `CancellationToken cancellationToken = default`。`InstallAsync` / `UpdateAsync` 另有可选 `IProgress<ResourceProgress>`。

| 类型/方法 | 参数与默认值 | 返回/行为 |
| --- | --- | --- |
| `ModrinthClient(userAgent)` | 默认标识 FMCore | 公共 V2 API；无需登录令牌 |
| `ModrinthClient(httpClient, userAgent, timeout)` | 调用方客户端；timeout 默认 45 秒 | 自定义代理/HTTP Mock；不转移客户端所有权 |
| `SearchAsync(query, options, ct)` | query 默认空；type=mod、sort=relevance、limit=20、offset=0；可筛选 MinecraftVersion/Loader | `ModrinthSearchResult`：Hits、Offset、Limit、TotalHits |
| `GetProjectAsync(projectId, ct)` | 项目 ID 或 slug | `ModrinthProject`：Id、Slug、Title、Description、ProjectType |
| `GetVersionsAsync(projectId, environment, ct)` | environment 默认 null，返回所有渠道；提供环境时在服务端和本地筛选 | 按日期降序的 `IReadOnlyList<ModrinthVersion>` |
| `GetVersionAsync(versionId, ct)` | 精确版本 ID | 保留兼容版本、加载器、文件及依赖的 `ModrinthVersion` |
| `GetVersionByHashAsync(sha512, ct)` | 已有文件的 SHA-512 十六进制 | 对应版本；只有 404 返回 null，不会接管该文件 |
| `ModrinthInstaller(instance, client, downloadOptions)` | 不可变 `InstanceInfo`；后两项可省略 | 自动读取本地版本和加载器，检查快照 |
| `ModrinthInstaller(rootDirectory, instanceId, environment, client, downloadOptions)` | 实例目录固定为 root/versions/id | 明确目标，可在游戏安装前准备；拒绝已托管实例 |
| `GetInstalledAsync(ct)` | 无额外参数 | 来源记录列表；不是任意 JAR 扫描器，不保证记录文件当前仍有效 |
| `InstallAsync(projectId, versionId, progress, ct)` | versionId 默认 null，自动选择 | `ModInstallResult`：本次依赖闭包 Installed 和 Reused |
| `CheckUpdatesAsync(ct)` | 当前受管项目 | `IReadOnlyList<ModUpdate>`：Installed / Available |
| `UpdateAsync(projectId, progress, ct)` | 已受管项目的 ID 或 slug | 成功提交的 `ModInstallResult`；无更新时复用旧版本 |

`ModrinthVersion` 包含 Id、ProjectId、Name、VersionNumber、VersionType、PublishedAt、GameVersions、Loaders、Files、Dependencies。`ModrinthFile` 保留 FileName、Url、Sha512、Sha1、Size、Primary、FileType；诊断字符串不输出 URL。`ModDependency` 保留 ProjectId、VersionId、FileName、Type。`InstalledMod` 记录 Source=`modrinth`、项目/版本 ID、版本名、发布时间、文件名、SHA-512、大小、安装时间和依赖，不记录下载 URL。

搜索允许 `mod/modpack/resourcepack/shader`，但本次只安装 Mod。需要其他资源文件时，可查询版本后把其 URL、SHA-512、大小传给现有 `DownloadManager.DownloadAsync`；不代表已实现整合包导入或资源包安装管理。安装环境仅支持 `fabric/forge/neoforge`，不适配 Quilt。文件要求 HTTPS Modrinth CDN、SHA-512、正数大小以及无额外文件类型标记的主 JAR；没有 primary 标记时按官方规则选择第一个文件。不能把 sources/dev/javadoc 等附属包当作主 Mod。

## 依赖、冲突与文件安全

- `required` 自动递归解析；优先保留已安装且满足要求的依赖，精确 `version_id` 必须匹配其项目并通过环境检查。矛盾固定版本、循环、超过 32 层/128 个解析项目或仅有外部文件名的必要依赖或不兼容声明返回明确错误。单个实例最多记录 512 项。
- `optional` 不自动安装，`embedded` 已由发布者内嵌，不另下载；`incompatible` 对整个受管集合双向检查，也检查能够通过 Modrinth 哈希接口识别的手动 Mod。平台元数据不能证明任意 Mod 的运行时兼容性。
- 同一项目只有一条受管记录；同一文件名按不区分大小写检查冲突。相同版本/文件重复安装校验后复用，不重复下载。必要依赖下载失败时，尚未提交的整个闭包不会进入活动 mods。
- 只替换来源清单中名称和 SHA-512 均匹配的旧文件。用户自行添加、重命名或修改的文件不接管、不覆盖、不删除；可识别的手动同项目 JAR 会返回 Conflict，即使其文件名不同。未被平台识别的 JAR 保留原样，无法据此保证其身份或兼容性。用户文件检查最多 1,024 个目录条目、单 JAR 512 MiB、合计 2 GiB，超过限额明确拒绝。
- 使用固定实例路径和现有链接/路径检查；拒绝绝对路径、路径穿越、设备名、符号链接及重解析点。不修改共享 versions/libraries/assets 或兄弟实例。
- 来源清单位于实例 `.fmcore/resources/modrinth.json`。更新先完整下载、校验、暂存并备份旧文件，再写入事务记录并替换；最终原子写入来源清单为提交点。提交前失败/取消会尝试恢复，程序中断后下次调用会先恢复。若恢复遇到外部修改或备份损坏，返回 `RecoveryRequired`，保留事务、旧备份和用户文件，不冒险覆盖。
- 下载缓存、旧文件备份和未用暂存文件位于同目录下的专用子目录，暂不自动清理；不得在未完成事务存在时手工删除。SDK 锁保护 SDK 内的并发操作；不承诺与不遵守锁的外部写入程序并发修改安全。

## 错误与验收

`ResourceException.Code` 包含 InvalidRequest、NotFound、HttpError、RateLimited、Timeout、NetworkError、InvalidMetadata、Unsupported、Incompatible、DependencyConflict、Conflict、UnsafePath、Busy、StorageCorrupted、RecoveryRequired、DownloadFailed、IoError。HTTP 失败提供 StatusCode/RetryAfter；下载失败提供 DownloadError，不附加原始 HTTP 正文、地址或网络异常。取消使用 `OperationCanceledException`。错误恢复需要先保留 `.fmcore/resources` 下的备份，不能直接删除来源清单来绕过冲突。

本地新增 **74 项测试**，Windows 全套 **648 项通过**。Fixtures 使用官方 V2 字段格式和脱敏内容；HTTP、平台依赖和下载失败均为模拟，文件校验、锁、取消、替换、回滚和中断恢复使用真实本地文件。

真实 Modrinth 接口另验证 **Minecraft 1.20.4 / Fabric / Mod Menu 9.2.0（FawTQg29）**，必要依赖为 **Fabric API 0.97.3+1.20.4（BPX6fK06）**、**Placeholder API 2.4.0-pre.3+1.20.4（VYMQNs7A）**。在独立临时实例完成搜索、兼容版本查询、下载、来源记录、独立 SHA-512 复核、平台哈希识别、重复复用与更新检查。本轮没有实际安装/启动对应游戏或加载器，不代表这些 Mod 的游戏运行、世界、联机或跨平台实机验证通过。

协议依据：[Modrinth API 与 User-Agent](https://docs.modrinth.com/api/)、[搜索 facets](https://docs.modrinth.com/api/operations/searchprojects/)、[版本与依赖](https://docs.modrinth.com/api/operations/getprojectversions/)、[文件与主文件选择](https://docs.modrinth.com/api/operations/getversion/)、[文件哈希识别](https://docs.modrinth.com/api/operations/versionfromhash/)。

## 与整合包共存（T12 当前源码）

普通独立 Mod 的原有接口保留；整合包拥有的 Mod 默认锁定，直接修改返回 `ResourceErrorCode.ModpackLocked`。改用 `ModpackInstaller.UpdateModAsync(..., authorize: true)` 明确授权；更新检查不解锁、不修改文件。包内更新复用本模块的解析和下载，并获得整包确认/清理/主动回滚生命周期；普通 T11 事务恢复不等于成功后的历史回滚。完整调用见 [整合包手册](modpacks.md)。

## 独立调用示例

- [资源搜索](examples/modrinth-search.cs)
- [项目资料](examples/modrinth-project.cs)
- [筛选文件版本](examples/modrinth-versions.cs)
- [来源清单](examples/modrinth-installed.cs)
- [批量检查更新](examples/modrinth-check.cs)
- [安装一个 Mod 及必要依赖](examples/modrinth-install.cs)
- [更新单个 Mod](examples/modrinth-update.cs)
- [单 Mod 更新进度与取消（高级）](examples/modrinth-advanced.cs)
- [安装明确选中的文件（高级）](examples/modrinth-install-exact.cs)

目录名称、启动版本 ID、共享资源和旧目录迁移规则见[目录管理](directory-layout.md)。

## 1.0.2 维护候选：项目图片

ModrinthProject 增加可选 Uri IconUrl 和只读 Gallery，不修改原构造函数及解构签名。搜索直接使用 icon_url，不为每个命中追加详情请求。GetProjectAsync 的 gallery 映射 ResourceImage：Url、Title、Description、Featured、CreatedAt、Ordering；缩略图未提供时 ThumbnailUrl 为 null。图片缺失不会影响查询/安装。

示例：[搜索图标](examples/modrinth-search-icon.cs)、[详情图库](examples/modrinth-project-images.cs)。SDK 不下载/缓存图片、不依赖 UI，异常可选图片被忽略；完整边界见[资源图片](resource-images.md)。
