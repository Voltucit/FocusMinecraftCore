# 更新日志

版本号采用阶段性策略。此文件只记录已实现能力，不将模拟测试表述为真实游戏验证。

## 未发布：公开仓库整理

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
