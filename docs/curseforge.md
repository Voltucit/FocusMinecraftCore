# CurseForge 资源与 Mod 管理（T11 第二部分）

本手册对应当前源码。目录布局调整尚未发布，已发布包请以对应 Tag 的文档为准。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Resources;

var key = Environment.GetEnvironmentVariable("FMCORE_CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("请设置 FMCORE_CURSEFORGE_API_KEY。");
using var client = new CurseForgeClient(key);
var result = await client.SearchAsync("fabric api", new() { MinecraftVersion = "1.20.4", Loader = "fabric" });
Console.WriteLine(result);
```


当前源码在 `FMCore.Resources` 提供 `CurseForgeClient` 和 `CurseForgeInstaller`。本功能包含于 FocusMinecraftCore 1.0.0。保持 .NET 10 / C# 14、单核心类库，不增加第三方依赖或 GUI 绑定。

## 最简安装

独立、可编译的调用代码见[本模块示例](examples.md#curseforge)。

示例项目为 [Fabric API](https://www.curseforge.com/minecraft/mc-mods/fabric-api)，不代表本轮已完成其真实 API 下载或游戏启动验证。安装目录固定为 `.minecraft/versions/fabric-1204/mods`，目标环境由调用方声明，此入口不会安装游戏或加载器。已有托管实例使用下一节的快照构造入口。

API Key 必须由调用方合法申请并保存在宿主的安全配置中，示例环境变量仅展示注入方式；不要放进源码、Git、日志、命令行或实例配置，也不要把自己的 Key 发到对话中。SDK 只在内存中使用 Key，按请求添加到官方 API 的 `x-api-key` 请求头，不存入来源清单、异常或诊断字符串。申请方式和使用条件以 [CurseForge 官方说明](https://support.curseforge.com/support/solutions/articles/9000208346-about-the-curseforge-api-and-how-to-apply-for-a-key)为准。

## 托管实例与来源列表

独立、可编译的调用代码见[本模块示例](examples.md#curseforge)。

要求 `fabric-play` 已有可解析的本地加载器版本元数据。此入口自动读取 Minecraft 和唯一的 Fabric、Forge 或 NeoForge 加载器，并校验实例快照。运行、启动准备、实例修改/删除及两平台资源操作共用同一个实例锁，冲突返回 `Busy`。过时快照返回 `Conflict`；被删除的托管实例不会被静默重建。Mod 安装完成后，继续使用原来的 `instance.CreateLaunchConfig(account)` 与 `MinecraftLauncher` 启动，见[启动手册](launch.md)。

## 搜索、分页、进度与更新

独立、可编译的调用代码见[本模块示例](examples.md#curseforge)。

客户端默认超时 45 秒，包含完整正文；正文限制 16 MiB。高级构造接受调用方持有的 `HttpMessageHandler`，可配置代理或模拟 HTTP，SDK 包装器不会接管该 handler 的释放；请保持它与客户端在操作期间存活。标准 `HttpClientHandler` / `SocketsHttpHandler` 及其委托链会禁用自动重定向，3xx 不会继续携带 Key 请求其他地址。自定义 handler 属于调用方的可信代码，不能自行转发凭据。

下载始终使用独立的无认证 `HttpClient`，默认由 SDK 复用；注入下载连接时禁止设置 x-api-key、Authorization、Proxy-Authorization 或 Cookie，运行期间也不要向该连接追加凭据。下载地址仅接受 API 原样返回的 HTTPS ForgeCDN 地址，不构造或猜测路径。响应、异常不包含原始请求头、响应正文、Key 或带参数的下载地址。429 提供可用的 `RetryAfter`；API 查询不暗中重试，文件下载复用 Downloader 的超时和重试策略。

## 公开 API 与模型

所有 I/O 方法为异步，末尾提供可选 `CancellationToken cancellationToken = default`。安装与更新另提供 `IProgress<ResourceProgress>`。进度回调可能来自工作线程，GUI 可使用 `Progress<T>`，回调不要抛出异常。

| API | 参数/默认值 | 返回与行为 |
| --- | --- | --- |
| `CurseForgeClient(apiKey, timeout)` | Key 必填；timeout 默认 45 秒 | 固定 CurseForge V1 API；可释放，无 Key 公开属性 |
| `CurseForgeClient(apiKey, handler, timeout)` | 调用方拥有 handler | 代理、解压、测试注入；标准 handler 禁止重定向 |
| `SearchAsync(query, options, ct)` | query 默认空；ClassId=6、SortField=2、SortOrder=desc、Index=0、PageSize=20 | `CurseForgeSearchResult`：Mods、Index、PageSize、TotalCount |
| `GetProjectAsync(modId, ct)` | 正整数项目 ID | `CurseForgeProject`：Id、GameId、Name、Slug、Summary、ClassId、AllowModDistribution、IsAvailable |
| `GetFileAsync(modId, fileId, ct)` | 正整数项目/文件 ID | `CurseForgeFile`；下载地址缺失仍可查询，不会补全或猜测 |
| `GetFilesAsync(modId, environment, index, pageSize, ct)` | environment 默认 null；index=0、pageSize=50 | `CurseForgeFilePage`：Files、Index、PageSize、TotalCount、ResultCount；后四项来自平台原始页 |
| `CurseForgeInstaller(instance, client, downloadOptions, downloadClient)` | 后两项可省略 | 从托管实例自动识别目标环境 |
| `CurseForgeInstaller(root, instanceId, environment, client, downloadOptions, downloadClient)` | 后两项可省略 | 固定独立目录；拒绝绕过已托管实例的快照检查 |
| `GetInstalledAsync(ct)` | 无额外参数 | 当前平台的 `IReadOnlyList<InstalledMod>` 来源记录，不接管任意用户 JAR |
| `InstallAsync(modId, fileId, progress, ct)` | fileId 默认 null，自动选择 | `ModInstallResult`：本次依赖闭包 Installed / Reused |
| `CheckUpdatesAsync(ct)` | 当前平台的受管项目 | `IReadOnlyList<CurseForgeUpdate>`，含 Installed 与 Available；不改变活动文件 |
| `UpdateAsync(modId, progress, ct)` | 已受管项目 ID | 重新检查权限/依赖后安全替换；没有更新时复用 |

`CurseForgeSearchOptions` 另支持 `MinecraftVersion`、`Loader`、`CategoryId`，ClassId 可查询其他 Minecraft 资源类别；安装入口仅接受 Mods（class 6）。SortField 为官方 1–12 数值。单页大小 1–50，Index + PageSize 不得超过 10,000。

`CurseForgeFile` 保留 Id、ModId、GameId、DisplayName、FileName、ReleaseType（1 正式、2 Beta、3 Alpha）、PublishedAt、Size、Sha1、Md5、DownloadUrl、IsAvailable、IsServerPack、IsEarlyAccessContent、ExposeAsAlternative、GameVersions 和 Dependencies。`CurseForgeDependency` 保留 ModId 与原始 RelationType（1 EmbeddedLibrary、2 OptionalDependency、3 RequiredDependency、4 Tool、5 Incompatible、6 Include）。原始下载 URL 的 `ToString`/调试显示被隐藏。

两平台继续共用 `InstalledMod`、`ModInstallResult`、`ResourceProgress`、`ModEnvironment` 和 `ResourceException`。CurseForge 的 `InstalledMod.Source` 为 `curseforge`，ProjectId 是项目 ID 的十进制字符串，VersionId 是文件 ID 字符串，VersionNumber 使用平台 DisplayName。`InstalledMod.Sha512` 是文件经平台 SHA-1 校验后计算的**本地所有权哈希**，不声称平台提供了 SHA-512；修改已返回的快照不会修改磁盘来源。

## 自动选择、依赖与平台限制

- 同时按明确的 Minecraft 标签和加载器标签检查，不按文件名猜测。支持 fabric=4、forge=1、neoforge=6；不适配 Quilt。自动选择按发布时间，默认只采用正式渠道，不把更大的文件 ID 或显示名称当作较新版本。`IncludePrereleases=true` 允许 Beta/Alpha；显式 fileId 仍要检查环境和权限。
- 自动选择遍历完整平台分页，不能只看第一页。分页缺失、重复或中途总数变化会明确失败，避免不完整选择。列表超过平台 10,000 条上限返回 `Unsupported`，可由调用方指定确切 fileId；不会静默截断。
- 必要依赖（3）递归安装，优先保留已安装的兼容文件；循环、超过 32 层或 128 个解析项目明确拒绝。CurseForge 的关系未提供固定 fileId，不伪造版本范围。可选依赖和工具不自动安装，EmbeddedLibrary/Include 按已嵌入或包含的内容记录，不再独立下载。`Incompatible` 对受管项目集合双向检查。
- `allowModDistribution=false`、文件没有 downloadUrl、不可用或属于受保护的抢先体验内容，返回 `DownloadRestricted`。nullable 分发标记没有明确禁止时，仍要求授权 API 返回实际可用地址；null 不会用于绕过 false。必要依赖受限时父 Mod 也不提交，不跳过依赖。
- 不调用其他平台、镜像或补地址接口来绕过限制，不直接访问网页或拼接 CDN 文件编号。服务端整合包、替代附件、非 JAR 文件及缺少 SHA-1 的文件明确拒绝安装；仅 MD5 不作为本阶段下载校验依据。

## 来源、安全更新与兼容性

来源清单位于实例 `.fmcore/resources/curseforge.json`；API Key、下载地址均不写入来源或事务。下载先核对大小及平台 SHA-1，随后计算 SHA-512，复用现有来源清单/备份/原子提交/恢复实现。提交前下载失败、取消或文件错误保留原版本；提交途中失败恢复旧文件。程序中断后，两平台任一入口都会先尝试恢复双方未完成事务。

如果备份损坏或发现外部修改，返回 `RecoveryRequired` 并保留备份和用户文件。正在运行或被其他 SDK 操作使用的实例拒绝变更。不承诺与不遵守锁的外部程序并发改写安全。缓存、旧备份与暂存文件暂不自动清理，存在未完成事务时不要手工删除。

只修改清单中名称和 SHA-512 均一致的本平台文件。用户文件、另一平台文件与其他实例的数据不接管、不覆盖或误删；同名冲突明确报错。查询当前目标下已选项目和已声明不兼容项目的完整文件列表，可用 SHA-1 发现改名的手动同项目版本；两平台完全相同内容也视为重复。**不做完整 CurseForge 指纹库识别，也不能判断所有未知用户 Mod、不同平台同项目或运行时兼容性**。没有识别出的文件原样保留，不强行声称无冲突。用户目录检查最多 1,024 个条目、单 JAR 512 MiB、合计 2 GiB。

既有 Modrinth 方法签名和清单路径保留。`InstalledMod.Source` 新增 init 访问器，默认仍为 `modrinth`，返回值可以表示 CurseForge；旧 Modrinth 清单继续可读。结构化错误码在原枚举末尾追加 `DownloadRestricted`、`AuthenticationFailed`，已有数值不变。实例锁、自动环境识别和事务文件处理抽取为内部共用代码，避免第二套更新/恢复逻辑。

## 验收与未验证范围

新增 **82 项测试**，Windows 全套 **730 项通过**。测试采用官方 V1 字段格式的脱敏 Fixtures 和模拟 HTTP，覆盖认证失败、密钥与下载隔离、权限/下载地址限制、分页、渠道/加载器、必要依赖、冲突、重复、安全路径、用户文件保护、事务恢复和两平台共存；本地文件/锁/回滚是真实文件操作。

本轮**没有提供调用方合法 API Key，因此没有真实 CurseForge API 搜索、下载或权限验证**，也没有携带 CurseForge Mod 启动游戏；不将模拟成功视作真实验证。第一部分 Modrinth 的历史真实接口记录仍见 [Modrinth 手册](modrinth.md)，不能代表 CurseForge 或两平台混装已完成游戏验证。

协议来源：[CurseForge REST API（认证、搜索、文件、哈希、分页、依赖与分发字段）](https://docs.curseforge.com/rest-api/)。

## 与整合包共存（T12 当前源码）

普通独立 Mod 的原有接口保留；整合包拥有的 Mod 默认锁定，直接修改返回 `ResourceErrorCode.ModpackLocked`。改用 `ModpackInstaller.UpdateModAsync(..., authorize: true)` 明确授权；更新检查不解锁、不修改文件。包内更新复用本模块的解析和下载，并获得整包确认/清理/主动回滚生命周期；普通 T11 事务恢复不等于成功后的历史回滚。完整调用见 [整合包手册](modpacks.md)。

## 独立调用示例

- [资源搜索](examples/curseforge-search.cs)
- [项目资料](examples/curseforge-project.cs)
- [筛选文件版本](examples/curseforge-versions.cs)
- [来源清单](examples/curseforge-installed.cs)
- [批量检查更新](examples/curseforge-check.cs)
- [安装一个 Mod 及必要依赖](examples/curseforge-install.cs)
- [更新单个 Mod](examples/curseforge-update.cs)
- [单 Mod 更新进度与取消（高级）](examples/curseforge-advanced.cs)
- [指定文件资料](examples/curseforge-file.cs)
- [安装明确选中的文件（高级）](examples/curseforge-install-exact.cs)

目录名称、启动版本 ID、共享资源和旧目录迁移规则见[目录管理](directory-layout.md)。

## 1.0.2 维护候选：项目图片

CurseForgeProject 增加可选 Uri IconUrl 与只读 Gallery，保留原构造/解构签名。IconUrl 优先取 logo.thumbnailUrl，回退 logo.url；screenshots 映射 ResourceImage，保留图片 ID、标题、说明、Url 和 ThumbnailUrl。搜索数据已包含图片时直接复用，无额外图片或详情请求。

示例：[搜索图标](examples/curseforge-search-icon.cs)、[详情截图](examples/curseforge-project-images.cs)。合法 Key 只发送到 API 请求头，不附加到图片 URL，不向图床转发认证头；不安全的图片地址忽略为 null/空列表。未提供真实 Key，本轮图片字段通过 Mock 验证，不表述为真实 CurseForge 接口验收。详见[资源图片](resource-images.md)。
