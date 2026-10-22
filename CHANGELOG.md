# 更新日志

版本号采用阶段性策略。此文件只记录已实现能力，不将模拟测试表述为真实游戏验证。

## 1.0.0 — 2026-10-22

### 核心功能

- 单文件/并发下载、哈希/大小校验、超时重试、取消、进度与原子文件替换。
- 官方与本地 Minecraft 版本、元数据缓存/继承/规则；本机 Java 探测与兼容检查，Temurin / Zulu Windows Runtime 显式安装。
- 原版安装/检查/修复、Microsoft 设备码认证与令牌刷新、离线账号及安全多账号存储；强类型启动配置、实例、进程管理、脱敏日志和诊断。
- Fabric、Forge、NeoForge、独立 OptiFine；Modrinth / CurseForge Mod 查询、必要依赖安装、来源和单项更新；整合包解析/导入/安装/导出、计划/更新/恢复/回滚/确认清理。
- 窗口/全屏/服务器、独立最小/最大内存、自定义 JVM/游戏参数；完整中文文档、独立功能示例及可运行控制台启动器。

### 修复与兼容性

- 修复 Windows 长路径探测返回 `\\?\` 前缀时，同一 Java 托管目录被误判为包外目录的问题；只归一化比较形式，包外目录/链接/所有权检查保持有效。

- 复用统一安装计划和规则，正确保留普通 artifact / natives、处理继承及加载器生成依赖；缺失生成文件明确要求重跑对应安装器。
- 统一参数范围、重复/冲突检查和元数据驱动的服务器参数选择；MinMemoryMb=0 省略 Xms，保留原属性类型与其余默认值。
- 路径/链接/归档边界、哈希清单、实例互斥和事务恢复保护用户文件；日志在事件/缓存/导出前过滤凭据。
- PackageId 保持 FocusMinecraftCore；程序集简单名称/命名空间保持 FMCore，公开类型、方法与枚举值保留。AssemblyVersion / FileVersion 为 1.0.0.0；Version / PackageVersion / InformationalVersion 为 1.0.0。
- 默认 User-Agent 联系链接统一为规范仓库地址，默认 LauncherVersion 字符串为 1.0.0；宿主显式设置仍生效，不更改认证或平台接口。

### 验证范围与限制

发布前重新执行 Release 构建、全量测试、包引用、CLI 与示例编译。历史游戏/Java 实机组合与验证边界见 README 和各模块手册；Microsoft/CurseForge 的 Mock、本地真实文件/模拟进程及实际在线验证分别记录，不把 1.0.0 版本号当作所有组合均已实测的保证。

Quilt、跨 Mod 原子批量执行、单 Mod 历史回滚、OptiFine 混合加载器安装尚未实现；真实 CurseForge 权限/下载、世界/服务器及其他组合仍需条件具备时验证。CLI、测试和内部报告不公开、不打包。完整说明见 [v1.0.0](docs/releases/v1.0.0.md)。

以下是并入 1.0.0 的历史开发阶段记录；其中“当时未发布”等描述只反映该阶段状态。

## 开发阶段：T10 全功能 SDK 开发体验

- 新增 DownloadManager.DownloadFileAsync(url, destinationPath) 和实例 DownloadAsync(url, relativePath)，复用原下载、目录边界、URL、取消和原子替换逻辑。
- 新增 GameCoreUtil.GetAvailableVersionsAsync 返回原 VersionManifest；新增 LaunchConfig.Create 直接接收 LaunchIdentity / JavaRuntime，并要求明确最大内存。原构造与默认值保留，工厂 minMemoryMb 默认 0。
- 检查全部已实现模块，继续使用原有 Java Runtime、安装、认证、加载器、资源、整包和高级设置实现，没有新增通用接口层、依赖或业务项目。
- README 改为项目介绍与功能/限制导航；中文手册、共享高级指南、完整源码树、模块调用关系、公开 API 清单与迁移说明同步。101 份独立功能源码及完整控制台项目分别维护，不再为正式代码示例注入缺失变量。
- 本地 CLI 补齐下载、版本/元数据、Java、安装/检查/修复、账号、实例、启动/日志、四类加载器、两平台资源和整包入口，统一帮助、进度、取消与退出码。CLI/测试/脚本/内部报告继续只保留在本地。
- 本轮 939 项自动化测试全部通过；原 3276 条公开 API 记录保留，当前 3280 条（195 个类型）。101 份独立源码、完整项目和手册代码共 120 份通过仅引用本地 NuGet 包的原样编译；16 类模块的独立包调用方与 34 项 CLI 子进程检查通过。
- 真实在线查询 Mojang、Modrinth、Fabric、Temurin；完整控制台示例在 Windows x64 / Minecraft 1.16.5 / Corretto 1.8.0_482 / 显式 2048 MiB / 离线身份运行，观察音频/纹理初始化后由 SDK 停止。Mock 协议、本地文件/模拟进程与实际游戏验收分别记录；本轮未重新验证正版授权、CurseForge 权限、世界或服务器。
- GitHub 当前仓库名为 FocusMinecraftCore，文档和包项目链接改为其规范地址；PackageId 保持 FocusMinecraftCore，程序集/命名空间保持 FMCore。未发布新 NuGet，未提交/推送或创建正式 Release。

## 开发阶段：T14 Java Runtime

- 新增 JavaRuntimeUtil.GetAvailableVersionsAsync / InstallAsync，官方 Temurin 默认和 Zulu CA GA 包、主版本/完整版本/架构/JRE/JDK 筛选、精确包安装。
- 复用 Downloader 的 SHA-256/长度/取消/重试和 T03 实际运行探测，返回原有 JavaRuntime；默认用户级托管根加入有界检测。
- 新增按包隔离、文件清单验证、完整复用、损坏修复、跨进程锁及提交前失败/中断恢复；不改 PATH、JAVA_HOME、注册表或系统 Java。
- Zulu 列表大小可能近似，SizeIsApproximate 明确标记；下载仍核对实际响应长度和厂商 SHA-256，不改变精确包或忽略哈希。
- 原有公开签名与错误码数值保留，新错误值末尾追加；无新增依赖或业务项目。中文 XML、README、SDK 和 Runtime 手册及独立示例同步，尚未发布新包。

- T14 新增 **50 项自动化测试**，Windows 全套 **928 项通过**。Windows x64 真实验证 Temurin **21.0.12.1+1-LTS JRE** 与 Zulu **21.0.12.1+1 JRE**：官方查询/下载、ZIP SHA-256、分别 315 / 320 个文件独立核对、实际 Java 执行、T03 有界发现、重复复用及缓存修复；重复安装和修复均零额外网络。x86/ARM64、JDK 和其他 Java 版本未实际安装，未启动 Minecraft，也未修改系统 Java。

## 开发阶段：T13 高级启动设置

- 保留内存整数属性和 512 / 2048 MiB 默认值，MinMemoryMb 新增 0 表示省略 Xms；范围检查沿用，最小/最大可独立指定。
- 新增 LaunchConfig.Server / InstanceConfig.Server 及 GameServerConfig，支持域名、IPv4、IPv6、端口和实例配置持久化。
- 复用继承元数据及 OS/架构/功能规则选择 Quick Play 或可识别旧版服务器参数，未知旧格式返回新增 UnsupportedGameSettings；不生成可选 Quick Play 历史日志。
- 统一 JVM/窗口/服务器重复项处理，元数据默认值由显式配置替换，自定义列表内不同值或冲突 GC 明确拒绝；UTF-8 输出约定保留。
- API 签名、默认值及原有错误码数值保留，旧实例缺少 Server 可读取。行为收紧：依靠追加冲突参数覆盖的用法需迁移至统一属性；新 JSON 不承诺旧 SDK 能反向读取。
- 新增 59 项测试，全套 878 项通过，中文 XML/README/模块手册及独立示例同步。50 段示例独立编译，NuGet 调用方验证持久化与模拟启动，真实 Java 8/17 验证堆参数及生效值。仅支持 Windows，未新增真实 Minecraft 或服务器连接验收，未发布新包。

## 开发阶段：T12 整合包

- 新增 Windows 整合包 API：Modrinth mrpack / CurseForge manifest v1，准确版本安装和安全导出。
- 新增整包新增/删除/替换/冲突计划、原子提交点、中断恢复、显式回滚及安装来源记录。
- 复用 T11 更新检查和单 Mod 更新，整合包托管 Mod 默认锁定，独立更新须明确授权。
- 旧 Mod 保留在活动目录以外；明确确认后按配置统一清理，删除错误独立返回，不影响活动文件。
- 复用 Runtime 安装与启动；未确认事务阻止冲突实例配置修改。公开签名保留，ResourceErrorCode 末尾追加 ModpackLocked。
- 更新中文 README、SDK/模块手册与调用示例；新增 89 项测试，全套 819 项通过，未将模拟认证或游戏视为真实成功。

## 开发阶段：T11 第二部分 CurseForge

- 新增 CurseForgeClient、CurseForgeInstaller、平台查询/文件/依赖/分页模型。API Key 由调用方提供，只用于固定 API 请求；标准连接禁止重定向，文件下载独立，不携带 Key。
- 遵守分发设置和下载地址限制，禁止自动下载、URL 缺失或受保护文件返回 DownloadRestricted，不猜测地址或使用其他来源绕过规则；支持完整分页、Minecraft/加载器/渠道检查、必要依赖、冲突及重复安装。
- 复用并抽取内部 ResourceInstance 与 ResourceFiles，两平台共用锁、快照、环境识别及事务更新/恢复；下载按平台 SHA-1 校验，随后计算本地 SHA-512 作为所有权依据。失败保留/恢复旧文件，用户和另一平台文件不接管、不覆盖。
- 新增 82 项测试，全套 730 项通过。CurseForge HTTP 和认证均为模拟；没有提供合法 Key，未验收真实 CurseForge 接口或携带其 Mod 启动游戏。同步中文 README、SDK 与平台手册。
- 兼容性：保留既有方法签名和来源清单路径；InstalledMod.Source 新增 init 访问器，默认仍为 modrinth，支持 curseforge。ResourceErrorCode 在末尾追加 DownloadRestricted / AuthenticationFailed，已有数值不变。无新增依赖或业务项目，尚未发布新 NuGet。

## 开发阶段：T11 第一部分 Modrinth

- 新增 Resources 的 ModrinthClient、ModrinthInstaller 及强类型查询/版本/来源/进度/错误模型；支持 Minecraft/加载器筛选、必要依赖、平台不兼容关系、重复安装、更新检查与替换。
- 复用 Downloader 的 SHA-512/大小校验、取消、进度和重试，复用 Instances 的稳定锁与快照检查；来源清单不包含下载地址或凭据。更新先下载与备份，提交失败回滚，中断后恢复；未知/用户修改的文件不会覆盖、接管或删除。
- 新增 74 项模拟接口与真实文件测试，全套 648 项通过。真实 Modrinth 接口验证 1.20.4/Fabric/Mod Menu 9.2.0、Fabric API 0.97.3+1.20.4、Placeholder API 2.4.0-pre.3+1.20.4 的查询、下载、独立 SHA-512、哈希识别及重复复用；未启动对应游戏或验证 Mod 运行。
- 同步中文 README、Modrinth/SDK 手册与支持边界；保留所有既有 API 签名，无新增依赖。CurseForge 留到第二部分，尚未发布新 NuGet。

## 开发阶段：T09-C–E，Quilt 取消

- 按用户要求取消 Quilt，移除本轮新增的 Quilt 专用源码和测试，保持 Fabric 已有能力。
- 新增 ForgeInstaller、NeoForgeInstaller、OptiFineInstaller 及共用配置/进度/结果/错误；复用下载、继承、安装与启动。Forge/NeoForge 执行未修改官方客户端安装器，OptiFine 实际调用原始安装器及补丁器，检查退出码和产物，支持缓存、取消、超时和明确失败。
- 生成库通过 IsLocalOnly/LocalOnly 校验，额外支持文件 GeneratedFiles 进入唯一安装清单，不伪造下载源；通用修复无法生成时明确要求重跑对应加载器安装器。
- 修复 BootstrapLauncher 对继承客户端 JAR 的排除规则；保留现有公开签名，无新增第三方运行时依赖。
- 新增 28 项测试，全套 574 项通过；30 段文档示例及独立包调用验收通过。Windows x64 真实离线验证 Forge 1.16.5/36.2.42/Java8、NeoForge 1.20.4/20.4.253/Java17、OptiFine 1.16.5/HD U G8/Java8，均完成窗口/音频/纹理初始化与 SDK 停止，重复安装零下载/SDK HTTP。不代表正版、Mod、世界/联机、其他组合或平台通过。
- 同步中文 README、SDK、模块手册、支持边界和已知限制；尚未发布新 NuGet。

## 开发阶段：T09-A Fabric

- 新增 `FabricInstaller`：Fabric Meta V2 游戏/兼容 Loader 查询、稳定版自动选择或手动版本、完整客户端安装与阶段/字节进度、缓存和取消。
- 复用 Versions 继承和 Installer 全流水线；为缺 SHA-1 的 Maven 依赖获取侧边哈希，安装完成后才发布版本配置，已有不同配置不覆盖。检查、修复、实例和启动仍共用现有 API，无新增依赖或业务项目。
- 支持 launcherMeta 1/2 的 Knot 客户端；未支持格式、处理器或不兼容 Java 要求明确报错，未自动开始 T09-B–E。
- 新增 41 项测试，Windows 全套 546 项通过；23 段文档示例及独立包调用验收通过。实际离线验证 1.16.5 / Fabric Loader 0.19.5 / Java 1.8.0_482 x64：音频、纹理和窗口初始化后经 SDK 停止；再次安装零下载/HTTP。其他组合、正版 Fabric、Mod、世界/联机及 Linux/macOS 未实机验收。
- 增加中文 Fabric 手册，同步 README、SDK、架构和路线。现有公开签名保留；本功能尚未发布新 NuGet。

## 开发阶段：T08 完整性与日志诊断

- 复用安装计划、资源索引/布局选择和原生归档读取规则；检查范围新增解压后的原生文件，不维护第二份下载规则。
- `VerifyAsync` 默认增加原生文件长度/SHA-256 检查；缺失 DLL 过去可能返回有效，现在会报告问题。可用 `VerifyNativeFiles=false` 选择旧范围，结果 `NativeFilesChecked` 标明是否检查。原方法签名及原阶段数值保留。
- 修复复用安装流水线，仅下载缺失/损坏的清单文件，解压文件可从有效原生压缩包零下载重建；测试保护存档、Mod、其他版本与范围外文件。
- Logging 支持有界实时日志、JSONL 显式导出、会话基线排除历史报告、退出信息与基于证据的常见错误信号；不能判断时保留原文证据和 Unknown，不靠退出码编造原因。
- 在事件、缓存、诊断和导出之前过滤凭据；报告读取/日志长度均有限额，原始游戏文件保持原样。
- 新增 54 项测试，Windows 全套 505 项通过；20 段示例编译通过，独立包调用验收覆盖新增入口。使用测试子进程与模拟游戏，不声称重新通过真实游戏/正版登录或 Linux/macOS 验证。
- 更新中文完整性、日志、SDK 和启动手册；T07–T08 均为当前源码能力，尚未发布新 NuGet。

## 开发阶段：T07 实例管理

- 新增 `FMCore.Instances`：实例创建、列表、读取、修订更新与安全删除，配置独立保存，版本、Libraries 和 Assets 保持共享。
- `InstanceInfo.CreateLaunchConfig` 直接接入既有启动 API；启动、修改和删除复用稳定文件锁，旧快照不能重建已删除实例或启动过时配置。
- 删除前检查全部目录并拒绝链接和越界路径，仅逐项删除目标实例；支持进度、取消和中断后重试。
- 新增 49 项本地测试，Windows 全套 451 项通过，覆盖真实文件锁/子进程和 Fixtures 集成；未重新验证真实游戏或其他实机平台。
- 增加中文实例手册和完整示例；旧公开 API 签名保留，无新增依赖。此功能仅在当前源码中，尚未发布新的 NuGet 版本。

## 开发阶段：公开仓库整理

- 公开方案仅保留 FMCore 核心项目；维护者本地通过 `FMCore.Local.slnx` 保留 CLI 和完整测试能力。
- 开发约定、CLI、测试、内部验收报告与脚本取消 Git 跟踪并加入忽略规则，本地文件保留。
- GitHub Actions 改为独立构建、打包核心，不再引用本地内部项目或脚本；中文文档同步调整。
- 不修改公开 API 或业务逻辑，不改变现有 NuGet 包版本、Tag 或 Release。

## 0.1.0-preview.2 — 2026-10-21，首个 NuGet 预览版

### 发布与兼容性

- NuGet 包名按用户确认调整为 **FocusMinecraftCore**；仓库、程序集 `FMCore.dll`、命名空间 `FMCore.*` 和公开业务 API 保持原样。
- 安装命令调整为 `dotnet add package FocusMinecraftCore --version 0.1.0-preview.2`；包引用验收与手册示例同步更新。
- 保留 `v0.1.0-preview.1` Tag，不覆盖已公开历史；新版本使用独立 Tag 和发行产物。
- 已发布到 [NuGet.org](https://www.nuget.org/packages/FocusMinecraftCore/0.1.0-preview.2) 与 [GitHub Release](https://github.com/Voltucit/FMCore/releases/tag/v0.1.0-preview.2)，包含核心包、符号包及中文更新日志。
- 本版本沿用已验收的 P0 能力；402 项测试、14 段示例、官方源独立包调用、搜索、仓库签名及 SourceLink 验收通过。

## 0.1.0-preview.1 — 候选版本，未发布 NuGet

GitHub 已推送该 Tag；NuGet 认证成功，但上传被服务端以包名近似冲突拒绝（HTTP 400），因此不存在该版本的 FMCore NuGet 发布。后续按用户决定使用新包名与 `0.1.0-preview.2`，不重复上传失败候选或覆盖 Tag。

### 新增

- 下载：单文件与批量并发、取消、进度、哈希/大小校验、超时和有限重试。
- 版本：官方列表、正式版/快照、JSON 解析、缓存、本地版本、版本继承及平台/功能规则。
- Java：JAVA_HOME、PATH 与常见目录检测，真实进程探测，按元数据严格选择主版本和架构。
- 原版安装：客户端、依赖、资源、原生文件、日志配置、完整性检查与复用有效文件修复。
- 账号：Microsoft 设备授权、Xbox/XSTS/Minecraft 资格与资料、刷新、离线 UUID、多账号及安全存储。
- 启动：统一配置、自动准备、目录隔离、现代/旧版参数、日志、退出与停止。
- 开发者交付：中文六模块手册、SDK 示例、NuGet 元数据、XML、SourceLink、符号包和手动发布工作流。

### API 兼容性

- 首个公开预览版，无此前已发布 NuGet 版本作为兼容基线。
- 发布准备不修改 T06 公开业务 API；保留已有模块入口和方法签名。
- 预览版后续如需破坏性变更，会在此记录影响及迁移方法。

### 验证与限制

- T06 历史验收：Windows x64、Minecraft 1.16.5、Java 1.8.0_482，真实正版与离线身份启动至窗口/音频/纹理初始化后通过 SDK 停止。
- 未验证世界、多人服务器、其他版本实际启动、Linux/macOS 实机或加载器。未实现 Mod/整合包和 Java 自动下载。
- 首版发布前通过 402 项自动化测试、14 段示例编译和独立包引用验收；内部详细报告保留在维护者本地。
- 公开上传、Tag、Release 和 NuGet 收录结果以对应官方页面为准。
