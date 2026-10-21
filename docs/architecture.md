# 项目结构与模块职责

## 公开仓库与本地开发

公开仓库的 `FMCore.slnx` 仅引用 `FMCore/FMCore.csproj`，保留核心源码、必要工程配置、GitHub Actions、中文手册与许可证。全新克隆不需要 CLI、测试、内部报告或验收脚本即可还原、构建和打包。

维护者本地使用不纳入 Git 的 `FMCore.Local.slnx`，保留完整三项目布局：

| 本地项目 | 职责 | 项目引用 |
| --- | --- | --- |
| FMCore | 核心 SDK，唯一业务类库，也是公开方案的唯一项目 | 无其他内部项目依赖 |
| FMCore.Cli | 本地命令行交互与开发调试入口 | FMCore |
| FMCore.Tests | 本地单元、集成和安全测试 | FMCore |

本地测试依赖版本由 `FMCore.Tests/Directory.Packages.props` 管理，并导入根级核心依赖配置；公开的根级文件仅列出核心需要的依赖。核心的测试友元声明只用于本地测试访问，不构成项目引用。

核心不依赖 CLI、测试或任何 GUI 框架。`FmCore` 作为未来统一入口，业务实现仍放在各模块中，避免入口类承担全部逻辑。

当前 Downloader 已实现，直接公开 `DownloadManager`；其请求、结果、配置、进度和错误类型只服务于下载，保留在 `FMCore.Downloader` 命名空间。尚未出现实际跨模块需求，因此没有提前创建通用模型或异常基类。

Versions 已实现 `VersionManager`、`VersionParser` 和 `RuleEvaluator`。版本模型归属 `FMCore.Versions`，作为后续 Installer、Java、Loaders、Launch 的统一元数据来源；保留未建模的原始 JSON 字段。版本缓存复用 `DownloadManager` 的下载、哈希和路径边界，不另写 HTTP 客户端或另建业务项目。

本地版本枚举通过 `GetLocalVersionsAsync` 读取现有游戏目录，复用相同解析器与继承逻辑。它不访问网络、不修改缓存，也不依赖官方列表缓存；单个损坏配置通过 `LocalVersionInfo.ErrorCode` 报告，不中断其他版本的扫描。客户端文件存在检查与未来 Installer 的完整性检查分开表述。

Java 已实现 `JavaManager`、`JavaCompatibility` 和专属模型。检测按 JAVA_HOME、PATH、常见安装目录依次收集有限候选，实际运行 Java 获取属性；子进程使用独立参数、并发输出读取、超时/取消与输出限制。选择复用 `VersionMetadata.Java`，继承版本应传入已解析的 `ResolvedVersion.Metadata`；严格匹配 Java 主版本和目标架构。同一主版本内优先较新补丁，缺失要求需调用方明确指定。选定 Java 可生成供 Libraries/参数规则使用的 `RuleContext`。不新增第三方依赖或通用进程框架，运行时下载留待后续阶段。

Installer 已实现 `GameInstaller` 与 `NativeExtractor`。安装计划直接消费 `ResolvedVersion`、Libraries 和条件规则，返回可复用的 Classpath 文件列表及原生压缩包选择结果；不另建版本模型或 HTTP 客户端。客户端来源遵循 T02 的 `ClientJarId`/`ClientDownload`，重复依赖在下载前去重及检查冲突。安装阶段复用 Downloader 的并发、进度、校验、重试和临时文件替换；同根目录使用独占安装锁。

Assets 已实现 `AssetIndex` 和 `AssetManager`，管理索引解析、按内容哈希去重的对象下载计划及旧版布局映射。`virtual` 映射到 `assets/virtual/<index>`，`map_to_resources` 映射到指定游戏目录下的 `resources`，并返回后续启动参数所需路径。NativeExtractor 校验原生 JAR、限制解压规模、拒绝路径穿越和链接，在临时目录完成后替换专用原生目录；Launch 可以复用相同计划与解压 API。Utilities 的 `GameFiles` 仅提供 Assets/Installer 实际共用的安全路径、流式校验、限量读取及原子文件写入。

Authentication 公开 `AccountManager`，统一管理 Microsoft 和离线账号。内部认证客户端负责设备授权、OAuth 刷新、Xbox/XSTS、Minecraft 资格与资料查询；不依赖 GUI、Downloader 或外部认证 SDK。设备码提示通过异步回调交给宿主，阶段通过 `IProgress<AuthenticationProgress>` 报告，账号变化通过 `AccountsChanged` 通知。离线身份独立创建，任何在线认证失败均不会改变账号类型或返回离线凭据。

账号元数据在 `accounts.json` 中原子保存；当前选择属于管理器实例，持久化默认账号供下次打开使用。同实例串行操作，存储目录用独占锁防止多个管理器同时修改。仅 Microsoft 刷新凭据写入安全存储，其他令牌只保留在内存；默认 Windows DPAPI CurrentUser 依赖 Microsoft 官方 ProtectedData 包。`IAccountTokenStore` 只用于宿主系统密钥环集成和测试，不引入通用仓储或插件框架。非 Windows 未提供安全存储时仍能使用离线账号，在线登录会明确报错。

`LaunchIdentity` 为 Launch 提供统一的昵称、UUID、身份类型、访问令牌、旧版 session、Client ID 与可用的 XUID。正版与离线的 `IsAuthenticated`、`UserType` 和凭据明确区分；访问令牌与旧版 session 不参与 JSON 序列化。`LoginOfflineAsync` 为原有离线创建/身份读取提供便捷组合；其余原有账号 API 保留。接口、缓存与安全边界见 [账号管理](authentication.md)。

Launch 公开 `MinecraftLauncher(LaunchConfig)`，配置分为 `GameCoreConfig`、`JavaConfig`、`GameWindowConfig`，直接接收 T05 的 `LaunchIdentity`，也可复用调用方的 `AccountManager` 自动获取/刷新身份。核心内部协调版本解析、Java 选择、安装/校验、规则和参数，不要求 GUI 手动管理 Classpath、资源和原生目录。默认实例隔离到共享根下的 `instances/<版本>`，每次启动使用独立的 `.fmcore/launch/<随机 ID>/natives`，同一实例持有生命周期文件锁。共享已校验的客户端、Libraries 和资源对象，不复制完整游戏文件。

参数始终以 `ProcessStartInfo.ArgumentList` 传递，不拼接 shell 命令；复用版本模块的新旧参数解析和条件规则。手动 Java 优先且仍须兼容，不因失败自动换 Java 或降级账号。stdout/stderr 并发读取，逐行隐藏访问令牌和 session；过长行整体省略。事件不暴露 Process、原始参数或访问令牌，调用方负责 UI 线程调度。`LaunchAsync` 的成功表示进程已创建；实际退出码通过 `WaitForExitAsync`/`Exited` 获取，`StopAsync` 请求终止进程树，异步释放会等待停止。配置与生命周期见 [启动文档](launch.md)。

P0 SDK 继续保留原入口；`DownloadManager(root)`、`GameInstaller(root)` 提供合理默认网络配置，原 HttpClient/下载器构造入口继续供高级配置使用。`GameCoreUtil` 和 `JavaUtil` 只组合已有查询/探测逻辑；UI 推荐 Async 版本，同名同步便捷方法会阻塞调用线程。安装校验复用 Downloader 的相同哈希逻辑，修复直接复用安装流程。没有新增业务项目、DI 容器、通用 Factory 或第三方依赖。入口清单见 [P0 SDK](sdk.md)。

## 功能目录

| 目录 | 职责 |
| --- | --- |
| Versions | 获取官方版本列表，解析版本 JSON，处理正式版、快照版及版本继承 |
| Assets | 解析和管理 Minecraft 官方资源索引与资源对象 |
| Authentication | Microsoft 与离线身份、玩家资料、UUID、安全令牌存储、刷新与多账号 |
| Downloader | 并发下载、进度回调、哈希校验、超时和重试 |
| Installer | 协调客户端、Libraries、Assets、原生依赖和日志配置安装，检查与修复文件 |
| Launch | 构建 Classpath、解析 JVM/Game 参数、启动与管理游戏进程 |
| Java | Java 检测、自定义路径、版本兼容性检查、运行时下载与安装 |
| Instances | 实例创建、删除、游戏目录隔离及独立配置 |
| Loaders | Fabric、Forge、NeoForge、Quilt 的安装与版本集成 |
| Resources | Modrinth、CurseForge 资源查询、下载、Mod 安装与更新 |
| Modpacks | 整合包解析、导入、导出、安装与更新 |
| Logging | 实时输出、崩溃报告、退出信息和错误诊断 |
| Models | 跨模块公共数据模型、配置、事件和结构化错误码 |
| Utilities | 内部通用工具，例如安全路径、文件和哈希辅助逻辑 |

`Assets` 面向 Minecraft 官方游戏资源；`Resources` 面向第三方平台内容。两者均复用 Downloader。

Installer 负责安装流程编排，复用 Versions、Assets 和 Downloader；Launch 复用版本、Java、实例和认证信息，并向 Logging 输出进程状态。Loaders 与 Modpacks 复用已有安装流程，避免各自重复下载和版本解析。

内存、分辨率、服务器和 JVM 参数归入实例配置，由 Launch 应用；不另设通用“高级设置”项目。SDK 是 FMCore 本身，异步任务、取消、进度、事件和错误码随业务 API 实现，不增加额外框架层。

## 本地测试目录（不公开）

以下目录保留在维护者本地的 `FMCore.Tests/` 中，公开仓库和 GitHub Actions 不依赖它们。

- Unit：无需外部服务的解析、规则和参数构建测试。
- Integration：模块协作、文件系统和外部服务集成测试；联网测试明确标记。
- Security：路径穿越、压缩包解压、敏感信息泄漏等边界测试。
- Fixtures：脱敏后的版本 JSON、模拟响应及其他测试素材。

## 精简约定

一个功能目录不等于一个项目。暂不引入通用仓储、依赖注入容器、插件系统或复杂继承体系。只在实际功能需要时新增类、接口和子目录。
