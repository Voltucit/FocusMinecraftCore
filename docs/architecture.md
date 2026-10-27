# 源码结构与开发

核心保持单个类库，按功能分目录。启动器、CLI 和示例引用 FMCore；核心不引用界面框架或本地工具。

## 核心模块

| 目录 | 职责 | 主要公开入口 |
| --- | --- | --- |
| FMCore/Versions/ | 版本发现、JSON、同根继承与入口 | MinecraftVersionReader |
| FMCore/Java/ | Java 候选、探测与 Windows x64 官方运行时安装 | JavaReader、JavaInstaller |
| FMCore/Accounts/ | 统一启动账号、离线身份与 Microsoft 登录 | IMinecraftAccount、OfflineAccount.Create、MicrosoftAuthenticator |
| FMCore/Launch/ | 配置、启动准备、进度、进程与日志 | LaunchConfig、MinecraftLauncher |
| FMCore/Installer/ | 路径工具、资源与游戏文件清单、Maven 元数据与补全编排 | MinecraftInstaller、GameResourceCompleter、GameFileCompleter；清单和路径工具为内部类型 |
| FMCore/Downloads/ | 请求、单文件与批次、进度、校验复用 | FileDownloader.DownloadAsync / DownloadManyAsync |
| GameInstance/、Logger/、ModPacks/、Modsloader/、ModsResources/ | 保留的后续分类 | 尚未交付 |
| docs/ | 中文手册与公开接口 | public-api.md |
| examples/ | 独立查询与配置示例 | 见 examples/README.md |

## 公开入口设计

后续常用入口参考 [StarLight.Core 游戏安装器](https://starlight-core.github.io/Installer/Game.html) 的使用方式：少量明确参数构造功能对象，再调用异步主方法，进度与取消可选。复杂设置才引入配置对象。主流程由 SDK 内部编排，简单步骤无需强制实例化。

资源补全的清单读取作为内部准备。正式主入口的名称、结果和实际完成范围保持一致，完整功能尚未交付时不展示为已可用 SDK。

## 简单入口与底层流程

1. 启动器填写 LaunchConfig：Account、GameCoreConfig、JavaConfig 和可选 GameWindowConfig。
2. 创建 MinecraftLauncher，确定路径并复制 JVM 选项。
3. LaunchAsync 在后台读取版本、探测指定 Java，并报告阶段。
4. 内部组合 PreparedLaunchConfig，复用已有 Launcher 流程。
5. 解析同根依赖、客户端、资源和日志配置，准备支持格式的原生库。
6. 创建 Java，读取两个日志流，等待退出或处理取消。
7. 完成收尾，报告 GameExited 并返回退出码。

`LaunchConfig` 是填写型配置；`PreparedLaunchConfig` 是已解析模型，原构造式配置已改用这个名称。`Launcher.RunAsync` 和 `RunLegacyAsync` 仍公开供高级调用，普通启动器无需手动组合这一层。

## 实现组件

| 组件 | 作用 |
| --- | --- |
| MinecraftLauncher | 普通公开入口、读取与探测、实例重复调用防护 |
| LaunchProgress / LaunchStage | 阶段与消息，不带百分比 |
| Launcher | 现有准备流程、Java 进程、日志和收尾编排 |
| LibraryResolver / ModernLibraryResolver | 新旧依赖、classpath、客户端及架构选择 |
| LaunchRules | 新版参数与依赖共用规则 |
| LaunchResourceResolver | 索引和标准日志配置 |
| NativeExtractor | 旧式 Windows 原生库安全解压 |
| LegacyArgumentBuilder / ModernArgumentBuilder | 新旧参数构造 |
| ModernArgumentReader | 新版参数数组、条件与继承 |

除明确列为公开入口的类型外，解析器、构造器和 RunWithProgressAsync 不属于公开 SDK。

## 下载实现

DownloadRequest 填写 URL、目标路径、大小、SHA-1 和可选超时。FileDownloader 复用 HttpClient，流式写入同目录的唯一临时文件，校验后正式落盘；已有目标通过校验则复用，不匹配时默认保留并报错；ReplaceInvalidFiles 为 true 时，下载验证后由 FileDownloader.Commit.cs 保留备份再替换。失败只清理本次临时文件。

FileDownloader.Batch.cs 用 partial 拆分同一个静态类。DownloadManyAsync 预检目标冲突后通过 Parallel.ForEachAsync 限制本批次并发，复用 DownloadAsync 和参数规范化。

DownloadProgress 报告单文件字节进度，DownloadBatchProgress 汇总完成数、活动数、峰值与大小，DownloadResult 表示校验和落盘成功。批次失败取消其余操作并等待收尾，成功文件保留。FileDownloader.Retry.cs 提供公开 DownloadAsync，负责整轮超时、退避和错误分类；FileDownloader.cs 中 DownloadOnceAsync 为私有单次传输。仅在单次资源释放与清理完成后决定重试，回调错误不会被当作网络错误。用户已验收正常并发、复用及本地 HTTP 503 的恢复和上限；FileDownloader.Progress.cs 计算最近约 5 秒的正文写入速度与剩余传输时间；单文件用 AlreadyPresent 排除复用大小，重试传输累计保留，批次汇总各单项的传输增量。用户已反馈本地慢速样本的进度与结果验收成功。资源索引与日志配置下载清单已由用户通过两个真实实例验收；资源对象读取、统一补全与备份修复也已验收。详见 [下载手册](downloads.md)。

## 资源补全内部准备

ResourceDownloadPlanner.ReadMetadataAsync 已实现并由用户通过 CLI 验收。它按调用方指定根目录读取版本及同根继承，取得 assetIndex 和 logging.client.file 下载声明，并生成经过路径、URL、大小和 SHA-1 格式检查的 DownloadRequest。

该步骤在后台读取，不创建目录或下载文件。资源索引请求位于清单首项，日志配置存在声明时追加；缺少目标文件不会阻止生成清单。FileDownloader.NormalizeRequest 与 LaunchResourceResolver.ReadInheritedObject 作为内部共用方法复用，不因此变成公开 SDK。

用户输出确认：1.16.5 样本共 2 项、1 项存在、1 项缺失、声明总计 296115 字节；1.21.10 Fabric 样本共 2 项且均存在、声明总计 508435 字节。存在性统计不代表内容校验。

ResourceDownloadPlanner.Assets.cs 读取并验证同一份索引字节，检查布局标志，按哈希去重并生成对象请求。GameResourceCompleter 提供公开 CompleteAsync，在后台依次完成元数据和对象批次，通过 ResourceCompletionProgress 报告阶段，通过 ResourceCompletionResult 返回文件统计。

基础补全和再次复用已由用户在 1.16.5 样本上验收。对象与日志、索引是 GameResourceCompleter 的范围；客户端与普通 artifact 依赖已由 GameFileCompleter 编排，原版完整安装由 MinecraftInstaller 提供。GameResourceCompleter 默认开启备份修复，结果通过 Backups 与 RepairedFiles 汇总本次成功替换；底层 DownloadRequest 默认严格保留。下载校验前不移动旧文件，系统替换失败时报告备份候选位置，不承诺原路径仍在。该修复与再次复用已由用户通过本地样本及 1.21.10 Fabric 资源补全验收。客户端与依赖库清单、在线 Maven 元数据以及统一补全入口均已验收正常路径，旧式 Windows 原生库声明已接入并完成 1.8.9 元数据验收。用法见 [游戏资源补全](resources.md)。

## 客户端与依赖下载声明

GameFileDownloadPlanner.ReadAsync 在后台读取已有实例，复用 LibraryResolver.GetClientJarSource 定位客户端来源，复用 ModernLibraryResolver 选择依赖。GetClientJarSource 不要求 JAR 已存在，启动使用的 GetClientJarPath 继续执行存在性检查。

普通 artifact 声明直接读取 URL、大小及 SHA-1；无 downloads 的 Maven 声明从仓库地址与已校验坐标生成 URL。缺失字段通过 GameDownloadFile.MissingFields 返回，Request 为空；已填写但无效的值报错。读取不联网、不创建目录、不下载，也不验证本地内容；CLI 的 Java 探测是单独步骤。旧式 Windows natives 分类归档已接入下载清单。

用户在 1.21.10 Fabric 样本上验收：客户端 1、依赖 76、清单总数 77，可生成请求 75，缺少元数据 2，本地缺失文件 0。intermediary 1.21.10 与 fabric-loader 0.17.3 均缺 SHA-1 和大小；这不等于文件丢失。GameFileDownloadPlanner.Maven.cs 已提供内部 ReadResolvedAsync：只查询不完整的 Maven 声明，从 .sha1 获取校验值、从 HEAD 获取大小，保留已有值，结果仅存于本次清单。单个依赖共用 30 秒超时，SHA-1 正文限制为 1 KiB，要求 HTTP 200 与未压缩响应，缺少有效大小则报错。

用户随后通过 --resolve-maven 验收：总文件数仍为 77，可生成请求增至 77，缺少元数据变为 0，MetadataReady 为 true；FileContentVerified 仍为 false，没有下载或启动游戏。GameFileCompleter 已在内部读取这份清单并检查信息齐全，然后复用下载器与资源补全。

## 统一游戏文件补全

GameFileCompleter 构造时固定版本、根目录、可选目标架构和策略，CompleteAsync 在后台按架构准备下载请求，执行客户端与依赖批次，再调用 GameResourceCompleter。GameFileCompletionProgress 报告步骤和当前批次；GameFileCompletionResult 汇总数量及本次成功替换的备份。每实例有重复操作防护，失败保留已完成文件。

用户使用 1.21.10 Fabric 与 Java 25 连续两次执行 complete-game-files：客户端 1、依赖 76、资源元数据 2、对象 4397，共 4476，下载 0、复用 4476、修复 0。该结果覆盖校验与复用编排，没有据此宣称本入口的下载、损坏、取消和故障路径都已实测。调用方法见 [游戏文件补全](game-files.md)。

## 项目与本地工具

公开解决方案为 FMCore.slnx。核心引用路径为 FMCore/FMCore.csproj，目标框架与语言设置来自 Directory.Build.props，SDK 选择来自 global.json。

本机额外保留 FMCore.Cli、FMCore.Tests、FMCore.Local.slnx、游戏测试数据、AGENTS.md、HANDOFF.md 和 docs/validation，不作为公开 SDK 分发。本地 CLI 可通过 InternalsVisibleTo 查看内部步骤；它的调试命令不等于公开接口。

维护者可用现有 CLI 完成功能验收。launch 分支已使用分组配置与实例入口，支持窗口宽高和 --jvm 选项。查询、准备和真实启动要分别理解，配置打印不证明游戏已运行。

## 示例同步

ShowLaunchConfig 当前使用分组 LaunchConfig 打印配置，不运行游戏。源码已改用对象初始化器；此示例不在 CLI 的项目引用内，不能用 CLI 验收代替示例构建。见 [示例说明](../examples/README.md)。

构建只证明编译结果。真实游戏、窗口和 JVM 参数通过维护者提供的实际运行结果确认；本轮未由助手执行构建、自动化测试或游戏。

## 文档与规划

实际接口见 [公开 API](public-api.md)，最简调用和完整程序见 [启动手册](launch.md)。[开发路线](roadmap.md)和[阶段记录](stages/stage-02.md)说明当前交付项及历史；后续规划不当作已可用接口。

## 原版安装与在线版本

MinecraftInstaller.GetVersionsAsync 只读官方清单，返回按发布时间降序的 MinecraftInstallVersion 列表。实例 InstallAsync 读取同一处原始清单、确定版本声明、校验版本 JSON，再调用 GameFileCompleter；不要求 JavaPath。当前实现按 partial 拆分，命名以实际源文件为准。

DownloadSourcePolicy 是内部地址转换组件。Source 由每个安装或补全对象初始化；镜像请求保留原始 URL，在剩余重试与超时预算内回退，不改变大小与 SHA-1 标准。元数据与日志保持原来源。1.21.10 原版安装与启动、镜像安装与启动以及在线列表均已由用户验收，见 [原版安装](installation.md)。

## Java 运行时安装

JavaInstaller 按调用方指定的大版本选择 Mojang 运行时组件。JavaInstaller.Metadata.cs 在内存中校验官方文件清单，检查相对路径、目标冲突及 raw 下载声明；不写入额外清单缓存。JavaInstaller.cs 编排目录创建和 FileDownloader.DownloadManyAsync，最后复用 JavaReader 执行版本与架构检查。已有不匹配文件保留并报错，成功文件可复用，失败不回滚目录。

JavaInstallProgress 报告阶段及当前批次，JavaInstallResult 返回 JavaInfo、安装目录和文件统计。用户已反馈 Windows x64 Java 25 安装并用于启动 1.21.10 成功；核心不修改 JAVA_HOME 或 PATH。完整调用见 [Java 安装](java-installation.md)。

## Microsoft 登录与启动账号

IMinecraftAccount 描述启动参数所需的账号值，离线与 Microsoft 账号均可用于 LaunchConfig 和 PreparedLaunchConfig。MicrosoftAuthenticator 通过 MSAL 获取个人账号授权，再调用 Xbox、XSTS 与 Minecraft 服务读取令牌及玩家档案；Client ID 为调用方参数，不内置维护者或其他启动器的 ID。

MicrosoftAuthenticator.Http.cs 将令牌放在当前请求，保留协议字段原大小写，不跟随认证接口重定向；错误消息保留阶段、HTTP 状态和可选的数字 Xbox 错误码，不拼接完整响应。新旧参数构造器使用账号实际令牌和类型，Launcher 在创建 Java 前检查 Microsoft 会话有效期。用户已反馈完整登录并启动游戏成功。MicrosoftAuthenticator.Cache.cs 尝试恢复唯一缓存账号的授权，TokenCache 向调用方开放 MSAL 缓存回调；同一进程中导入缓存并恢复的流程已验收。存储、加密和账号管理由启动器作者实现，见 [Microsoft 登录](microsoft-login.md)。
