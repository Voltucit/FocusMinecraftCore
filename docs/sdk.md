# FMCore 开发者使用手册

本手册对应 FocusMinecraftCore 1.0.0，目标为 Windows 10/11、.NET 10、C# 14。安装命令为 `dotnet add package FocusMinecraftCore --version 1.0.0`，也可引用核心项目。程序集/命名空间为 `FMCore`，不依赖 CLI、测试或 GUI。

## 模块入口

| 功能 | 简单入口与返回值 | 使用手册 |
| --- | --- | --- |
| 单文件/批量下载 | DownloadFileAsync / DownloadAsync / DownloadManyAsync → DownloadResult；VerifyAsync → FileVerificationResult | [下载](downloader.md) |
| 可用/本地/指定版本 | GameCoreUtil.GetAvailableVersionsAsync → VersionManifest；GetGameCoresAsync / GetGameCoreAsync → GameCoreInfo；VersionManager → 元数据与继承 | [版本](versions.md) |
| 本机 Java | JavaUtil.GetJavasAsync / GetJavaInfoAsync → JavaRuntime；JavaManager → 有界检测/兼容性 | [Java](java.md) |
| 官方 Java 安装 | JavaRuntimeUtil.GetAvailableVersionsAsync → JavaRuntimeVersion；InstallAsync(主版本或精确对象) → JavaRuntime | [Runtime](java-runtime.md) |
| 原版安装 | new GameInstaller(root).InstallAsync(versionId) → InstallationResult | [安装](installer.md) |
| 检查/修复 | VerifyAsync → InstallationCheckResult；RepairAsync 复用安装逻辑 | [完整性](integrity.md) |
| 账号 | AccountManager.LoginOfflineAsync / LoginMicrosoftAsync / GetLaunchIdentityAsync → LaunchIdentity；账号资料 → AccountSnapshot | [认证](authentication.md) |
| 启动 | LaunchConfig.Create + MinecraftLauncher.LaunchAsync → LaunchResult；WaitForExitAsync → GameExitResult | [启动](launch.md) |
| 实例 | InstanceManager.Create/Get/Update/GetInstancesAsync → 不可变 InstanceInfo；DeleteAsync 安全删除 | [实例](instances.md) |
| 加载器 | FabricInstaller / ForgeInstaller / NeoForgeInstaller / OptiFineInstaller → 安装结果中的 VersionId | [Fabric](fabric.md)、[Forge](forge.md)、[NeoForge](neoforge.md)、[OptiFine](optifine.md) |
| 平台资源 | ModrinthClient/Installer、CurseForgeClient/Installer → 查询模型、ModInstallResult 与更新候选 | [Modrinth](modrinth.md)、[CurseForge](curseforge.md) |
| 整合包 | ModpackArchive.ReadAsync → ModpackInfo；ModpackInstaller → 计划、整包事务、确认/回滚/清理 | [整合包](modpacks.md) |
| 高级设置 | JavaConfig / GameWindowConfig / GameServerConfig；实例可持久化对应设置 | [高级设置](advanced-launch.md) |
| 日志与诊断 | LauncherLogger.GetEntries/SaveAsync；GameDiagnostics.Analyze → GameDiagnosticReport | [日志](logging.md) |

每个已实现主要功能分别提供[独立源码示例](examples.md)，包含必要 using、参数、返回值说明和适用条件。基础调用只给必要参数，高级示例补充配置、进度、取消和错误处理。完整导出类型及成员见[公开 API 清单](public-api.md)。

## 调用约定

下载根目录/版本 ID 在同一流程保持一致。返回的 LaunchIdentity、JavaRuntime 和加载器 VersionId 可以直接接入启动；无需解析字符串重建身份、Classpath 或文件安装计划。账号、Java 主版本、内存预算和系统优化策略由宿主决定。

异步、事件、异常、资源所有权与凭据规则见[高级指南](advanced.md)。T10 只增加便捷入口，原公开签名保留；差异和迁移见[API 调整](api-changes.md)。新增 API 时必须同步更新中文手册与独立示例，并重新执行示例编译。

## 更新边界

`CheckUpdatesAsync` 检查一组托管 Mod，不修改文件；`UpdateAsync(projectId)` 更新单个项目及必要依赖。逐项调用是独立事务，后项失败不会撤销前项。没有通用跨 Mod 原子批量执行或单 Mod 历史回滚 API。

整合包使用 `GetUpdatePlanAsync / UpdateAsync` 独立入口，内部负责包级变更、冲突、备份、恢复和主动回滚。包内单 Mod 使用 `CheckModUpdatesAsync / UpdateModAsync` 并明确授权；确认正常使用后才能清理备份。

## 完整源码架构

独立章节：[完整源码目录、模块依赖及调用流程](architecture.md)。

## 完整项目示例

独立章节：[可运行的 .NET 10 C# 控制台 Minecraft 启动器](sample-launcher.md)，包含项目结构、完整源代码、项目引用、运行命令及正版/联网条件。此综合项目与各功能独立示例分开维护。
