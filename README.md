# FocusMinecraftCore（FMCore）

FocusMinecraftCore（简称 FMCore）是面向 .NET 启动器开发者的 Minecraft Java 版启动核心 SDK。它负责下载、安装、身份认证和游戏进程生命周期，让桌面应用或控制台宿主通过公开 API 组合自己的启动流程。核心不依赖 GUI 框架，也不强制使用依赖注入。

## 功能

| 模块 | 实际作用 |
| --- | --- |
| 下载 | 单文件与并发批量下载、超时重试、进度、取消、哈希校验和原子替换 |
| 版本 | 官方版本目录、本地版本、元数据缓存、继承与规则解析 |
| Java | 本机探测、指定路径校验、兼容检查；Temurin / Zulu 官方 Windows Runtime 查询和显式安装 |
| 游戏安装 | 客户端、依赖库、资源索引/对象、原生依赖、日志配置及检查/修复 |
| 账号 | 离线身份、Microsoft 设备码授权、资格/资料、令牌刷新与本地多账号管理 |
| 启动 | 强类型配置、隔离实例、进程状态、脱敏日志、退出事件、停止和诊断 |
| 加载器 | Fabric、Forge、NeoForge、独立 OptiFine 查询与安装 |
| 资源 | Modrinth / CurseForge 查询、Mod 依赖安装、来源清单、更新检查和单项事务更新 |
| 整合包 | mrpack v1 / CurseForge manifest v1 解析、导入、安装、导出、整包计划/更新、回滚及确认清理 |
| 高级设置 | 窗口、全屏、服务器、独立最小/最大堆与自定义 JVM/游戏参数 |

本页对应 **1.0.0**。发布记录见 [GitHub Release](https://github.com/Voltucit/FocusMinecraftCore/releases/tag/v1.0.0)，包版本见 [NuGet](https://www.nuget.org/packages/FocusMinecraftCore/1.0.0)；当前公开状态以这两个页面为准。

## 设计

- 常见操作提供简短入口，高级调用沿用同一实现和配置模型。
- 耗时操作提供 Async API，可按需接入进度与 CancellationToken。
- 安装自动处理依赖、文件校验、目录边界和必要事务，返回强类型结果。
- 账号、Java 主版本、内存预算由宿主明确决定，启动不会自动下载安装 Java。
- 令牌在日志、进度和异常输出边界过滤；Windows 刷新凭据默认使用当前用户 DPAPI 保存。

## 平台与引用

开发与运行目标为 Windows 10/11、.NET 10、C# 14。需要可用的兼容 Java、对应官方服务网络；正版登录需要开发者自己的合法 Microsoft 应用与玩家授权，CurseForge 需要调用方合法 API Key。核心只有 `FMCore` 一个业务类库；可在 JetBrains Rider 中开发。

安装 1.0.0：

```powershell
dotnet add package FocusMinecraftCore --version 1.0.0
```

使用源码时，在宿主项目引用 `FMCore/FMCore.csproj`，或按[本地构建说明](docs/releasing.md)生成本地包。包名是 `FocusMinecraftCore`，程序集和命名空间为 `FMCore`。

## 开发者文档

| 阅读顺序 | 内容 |
| --- | --- |
| [开发者手册](docs/sdk.md) | 全模块入口、返回值与文档导航 |
| [独立功能示例](docs/examples.md) | 每项功能的完整 C# 源码、最简与高级调用 |
| [高级指南](docs/advanced.md) | 配置、进度、取消、事件、错误与资源释放 |
| [完整源码架构](docs/architecture.md) | 文件树、模块依赖和调用流程 |
| [完整启动器项目](docs/sample-launcher.md) | .NET 10 控制台示例、项目引用、运行与前置条件 |
| [公开 API 清单](docs/public-api.md) | 当前源码导出的类型、方法、属性、事件和枚举 |
| [API 调整与迁移](docs/api-changes.md) | T10 新增便捷入口及兼容性说明 |
| [CHANGELOG](CHANGELOG.md) | 阶段变化、修复及验证边界 |

## 状态与限制

1.0.0 的平台与验证边界如下。T10 的完整控制台示例已在 Windows x64 / Minecraft 1.16.5 / Java 8 / 离线身份下实际运行，观察到音频/纹理初始化并由 SDK 停止。历史实机验收包括 Windows x64 的 Minecraft 1.16.5 原版正版/离线启动，以及文档所列 Fabric、Forge、NeoForge、OptiFine 离线组合；这些结果不代表任意版本、Mod、世界或多人服务器均可运行。各模块手册保留准确组合和验证条件。

Quilt 未适配。资源安装当前针对 Mod；资源包/光影可查询，不提供同等安装能力。Mod 批量检查不修改文件，逐项更新不是跨项目原子批量事务；整包更新使用独立入口。CurseForge 真实 API 与下载权限仍需合法 Key 验证，不绕过分发限制。Java 下载实机范围和整包内容验证范围见对应手册。

CLI、测试与内部验证报告仅保留在维护者本地，不进入公开仓库或 NuGet。公开工作流构建核心与包，不宣称运行未公开的测试。

## 许可

源码仓库：[Voltucit/FocusMinecraftCore](https://github.com/Voltucit/FocusMinecraftCore)。项目展示名称、程序集和命名空间保持 FMCore。

FMCore 使用 [MIT License](LICENSE)。Minecraft、Java 发行版及其他第三方内容的权利归各自权利人，必要归属见 [NOTICE](NOTICE.md)。SDK 不分发游戏文件或代替外部服务授权。
