# Java Runtime 查询、下载与安装（T14）

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Java;

var versions = await JavaRuntimeUtil.GetAvailableVersionsAsync(21);
foreach (var v in versions) Console.WriteLine(v);
```


命名空间 `FMCore.Java`，仅支持 Windows / .NET 10 / C# 14。本功能包含于 FocusMinecraftCore 1.0.0。

入口为 `JavaRuntimeUtil`，查询、选择、安装三步可以分开。默认使用 Eclipse Temurin 官方 GA JRE、当前 Windows 架构和 `%LOCALAPPDATA%/FMCore/runtimes`，也支持 Azul Zulu 的公开社区包。只在调用 `InstallAsync` 时下载安装；查询、本机检测及游戏启动均不会替调用方自动下载 Java。

## 最简安装与直接接入启动配置

独立、可编译的调用代码见[本模块示例](examples.md#java-runtime)。

安装返回 T03 已有的 `JavaRuntime`，实际成员为 `ExecutablePath`、`HomeDirectory`、`Version`、`Architecture`、`Vendor`；项目没有另一个 `JavaInfo` 类型。上例只展示安装和配置映射，不会自动启动游戏。

Minecraft 对应哪个 Java 主版本由启动器开发者决定；示例明确传入 21。FMCore 的启动兼容检查仍严格消费版本元数据，不把更高主版本当作任意替代。安装不修改系统 PATH、JAVA_HOME、注册表或既有系统 Java。

## 只查询、由用户选择精确版本

以下完整示例列出可下载版本，选择第一项并仅在明确确认后安装。GUI 可将列表绑定到界面，然后将选中的同一个 `JavaRuntimeVersion` 交给安装方法。

独立、可编译的调用代码见[本模块示例](examples.md#java-runtime)。

不传主版本的 `GetAvailableVersionsAsync()` 查询该发行版当前能提供的 GA Windows ZIP 包；不是本机安装列表，也不是只列 LTS 主版本。查询按厂商分页读取并按 Java 数字版本从新到旧排序，结果是不可变包描述，不能手工修改下载地址或校验值。

`JavaRuntimeVersion` 提供 `Id`、`FullVersion`、`MajorVersion`、`Distribution`、`Architecture`、`PackageType`、`Size`、`SizeIsApproximate`、`Sha256`、`DownloadUri`。同一个 Java 版本可能有不同厂商构建，用包描述安装可保留精确的厂商、SHA-256 与包标识，不擅自换成更新版本。查询不创建安装目录、不下载 ZIP、不执行 Java。

## 高级配置与本机已安装列表

独立、可编译的调用代码见[本模块示例](examples.md#java-runtime)。

本机查询继续使用 T03 的 `JavaUtil.GetJavasAsync()` / `GetJavaInfoAsync(path)`。默认托管根已加入 Windows 的有界常见目录，仍只检查根及一层子目录，不扫描磁盘。自定义根用 `SearchDirectories` 补充，或直接使用安装返回的可执行路径。默认根可从 `JavaRuntimeUtil.DefaultInstallationDirectory` 获取。

## API 参数、默认值与返回值

| API | 参数与行为 | 返回值 |
| --- | --- | --- |
| `GetAvailableVersionsAsync(majorVersion, options, ct)` | `majorVersion` 默认 null；可指定 1–999 范围主版本，来源无此包则返回空列表 | `IReadOnlyList<JavaRuntimeVersion>` |
| `InstallAsync(majorVersion, options, progress, ct)` | 必填主版本；在指定发行版、架构、类型及可选完整版本中选择最新匹配项 | 实际执行验证后的 `JavaRuntime` |
| `InstallAsync(version, options, progress, ct)` | 必填查询结果；其发行版、架构、类型和精确版本优先，不受 options 中的筛选字段覆盖 | 实际执行验证后的 `JavaRuntime` |

`JavaRuntimeOptions` 的筛选字段用于查询及按主版本安装；传入精确包时只使用目录、超时、下载和 HTTP 配置。

| 配置 | 默认 | 说明 |
| --- | --- | --- |
| `Distribution` | `Temurin` | 另支持 `Zulu`，仅官方公开 GA 包 |
| `Architecture` | 当前 Windows 架构 | 支持 `X86` / `X64` / `Arm64`；实际必须能在当前机器执行，不能假定跨架构模拟可用 |
| `PackageType` | `Jre` | 可选 `Jdk`；只接受 ZIP，不执行 MSI 或系统安装器 |
| `FullVersion` | null | 包含构建号的精确匹配；使用查询结果原值，Java 8 与现代格式复用 T03 解析器 |
| `InstallationDirectory` | 默认托管根 | 是多版本共存的根，每个发行包各有独立子目录；不是任意现有 Java 目录的覆盖目标 |
| `QueryTimeout` | 30 秒 | 每次元数据请求，最大 5 分钟 |
| `ProbeTimeout` | 10 秒 | 每次 Java 执行探测，最大 1 分钟 |
| `Downloads` | `DownloadOptions` 默认值 | 复用 Downloader 的超时、重试、校验及完整文件缓存 |
| `HttpClient` | 核心共享客户端 | 可选宿主自有传输用于代理或测试；核心不释放它，不应设置账号凭据默认头 |

`JavaRuntimeProgress` 提供 `Stage`、`CompletedFiles`、`TotalFiles` 和可选 `DownloadProgress`。阶段包括查询、恢复、校验、下载、解压、运行验证、提交、完成及复用。`Progress<T>` 可在 GUI 线程创建，普通进度实现应快速返回，不要同步等待同一任务。

## 校验、修复、恢复与目录边界

- 复用 Downloader，按厂商公布的 SHA-256 下载 ZIP。Temurin 同时核对公布大小；Zulu 的 Size 可能近似（SizeIsApproximate=true），优先从已通过哈希验证的缓存取得精确长度；没有缓存时读取官方 ZIP 响应头，再把精确长度交给 Downloader 核对，完整 SHA-256 校验不放宽。已校验的归档保存在托管根私有缓存中；相同完整文件不重复下载。下载中断由原下载器清理临时文件，不承诺字节范围断点续传。
- ZIP 保留厂商许可证及运行文件，拒绝路径穿越、设备名、链接/重解析属性、重复或大小写冲突、文件目录冲突和异常压缩比。限定 30000 个条目、单文件 512 MiB、解压总量 4 GiB；未知布局明确失败。
- 在隔离暂存目录提取文件并建立 SHA-256 清单，使用现有 JavaManager 实际运行探测，核对主版本、补丁、架构及 java.home 边界。下载归档的精确哈希锁定所选构建；运行探测中的 java.version 可能不包含构建号，不伪造探测字段。
- 每个包持有跨进程独占锁。同包并发返回 `RuntimeBusy`；不同包独立目录可共存。安装成功返回最终目录的实际探测结果。
- 重复安装先检查所有清单文件并实际运行 Java，完整时直接复用；缺失、损坏或失效运行文件重新从同一个已校验包修复，不升级版本。损坏清单也可根据原包重新生成；所有权标记损坏/丢失或目录混入未知文件时拒绝覆盖，不删除这些文件。
- 替换前写入有界事务记录，原目录安全备份；最终路径再次执行通过后才提交。提交前失败/取消恢复原目录，进程意外中断时下次安装同一精确包自动恢复；恢复失败保留记录及备份并返回明确错误。不可识别的孤立备份不自动删除。
- 路径、解压、备份和清理只作用于该包的托管子目录，拒绝目录链接；不更改 PATH、JAVA_HOME、注册表或其他系统 Java。缓存及恢复文件由 SDK 管理，调用方无需移动备份。

## 错误与取消

失败为已有 `JavaException`，读取 `Code`；用户取消仍抛 `OperationCanceledException`。原有错误码顺序不变，新增值追加于末尾。

| 错误码 | 含义及建议 |
| --- | --- |
| `UnsupportedPlatform` / `RuntimeInvalidOptions` | 平台、架构、版本、超时或路径配置不支持 |
| `RuntimeQueryFailed` / `RuntimeQueryTimeout` | 官方服务状态、网络或查询超时；可由宿主提示重试 |
| `RuntimeMetadataInvalid` / `RuntimeNotAvailable` | 缺少大小、SHA-256、可信 URL 或无匹配包；不猜测镜像地址 |
| `RuntimeDownloadFailed` / `RuntimeIntegrityFailed` | 下载失败或校验不符；未发布不完整安装 |
| `RuntimeArchiveInvalid` / `RuntimeUnsafePath` | ZIP、路径、链接或规模检查失败 |
| `RuntimeValidationFailed` | 实际版本、架构或 java.home 与所选包不符 |
| `ExecutionFailed` / `ProbeTimeout` / `InvalidOutput` 等 T03 错误 | Java 实际执行失败、超时或输出无效 |
| `RuntimeBusy` / `RuntimeConflict` | 同包正在操作，或存在不属于 SDK 的文件/所有权冲突 |
| `RuntimeRecoveryFailed` / `IoError` | 恢复记录、备份或文件系统操作未完成；保留原目录并重试同一包，勿手工删备份 |

错误不保留原始 HTTP 正文、进程输出、URL 参数或原始系统异常，不把 HTTP 成功、ZIP 下载成功或 Mock 探测当成真实 Runtime 安装成功。

## 官方资料与验证边界

Temurin 使用 [Adoptium 官方 API](https://api.adoptium.net/q/swagger-ui/) 的可用主版本与分页 GA 资产；Zulu 使用 [Azul 官方 Metadata API](https://docs.azul.com/core/install/metadata-api)，明确限定 CA 公开 ZIP，不获取订阅包或绕过访问限制。发行包的许可证随原始内容保留在托管目录。

自动化与独立调用方验证覆盖查询/筛选、精确包、复用、损坏修复、取消、失败/中断恢复、锁及路径安全。T14 新增 **50 项自动化测试**，Windows 全套 **928 项通过**。Windows x64 真实验证 Temurin **21.0.12.1+1-LTS JRE** 与 Zulu **21.0.12.1+1 JRE**：官方查询/下载、ZIP SHA-256、分别 315 / 320 个文件独立核对、实际 Java 执行、T03 有界发现、重复复用及缓存修复；重复安装和修复均零额外网络。x86/ARM64、JDK 和其他 Java 版本未实际安装，未启动 Minecraft，也未修改系统 Java。

Zulu 本次公布大小为 49,264,400 字节，而官方 ZIP 实际为 49,264,410 字节，独立 SHA-256 与厂商完全一致；SizeIsApproximate 如实标记，安装前解析精确长度，不通过忽略哈希解决差异。

本阶段不验证 Minecraft 游戏或多人服务器，安装测试不等于所有来源、架构和包类型都已实机验收。

## 独立调用示例

- [可下载 Runtime 查询](examples/runtime-available.cs)
- [按主版本安装](examples/runtime-install.cs)
- [安装用户选中的精确包](examples/runtime-exact.cs)
- [Runtime 高级安装（高级）](examples/runtime-advanced.cs)

## 1.0.0 长路径兼容修复

Windows 运行时探测可能返回带 `\\?\` 的 home 目录。SDK 比较包目录时识别其与普通绝对路径的等价形式；仍拒绝外部/兄弟目录，不放宽文件访问、符号链接或所有权校验。
