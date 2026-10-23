# FocusMinecraftCore（FMCore）

## 最新版本更新

当前版本 **1.0.2**。发布记录见 [GitHub Releases](https://github.com/Voltucit/FocusMinecraftCore/releases)，包信息见 [NuGet](https://www.nuget.org/packages/FocusMinecraftCore)。

- 增强已有 Minecraft 目录的通用解析与启动，按版本元数据处理继承、依赖和参数，不因加载器名称未知而直接拒绝。
- 区分启动版本、JSON ID、实际目录名与游戏工作目录；只读扫描第三方目录，保留原有文件与布局。
- Modrinth / CurseForge 搜索结果和详情提供图标地址、图库与截图信息，便于启动器直接展示。
- 保留路径边界、文件校验和凭据过滤；现有公开 API 签名、FMCore 程序集与命名空间保持兼容。

当前源码正在准备 **1.0.3**（尚未发布）：增强标准清单 ZIP 整合包兼容，支持普通实例导出可验证来源的标准整合包。使用方式与限制见[整合包手册](docs/modpacks.md)。

## 项目介绍

FocusMinecraftCore（简称 FMCore）是面向 .NET 启动器开发者的 Minecraft Java 版启动核心 SDK。

## 功能

| 模块 | 实际作用 |
| --- | --- |
| 下载 | 单文件与并发批量下载、超时重试、进度、取消、哈希校验和原子替换 |
| 版本 | 官方版本目录、本地及第三方版本、目录别名、元数据缓存、继承与规则解析 |
| Java | 本机探测、指定路径校验、兼容检查；Temurin / Zulu 官方 Windows Runtime 查询和显式安装 |
| 游戏安装 | 客户端、依赖库、资源索引/对象、原生依赖、日志配置及检查/修复 |
| 账号 | 离线身份、Microsoft 设备码授权、资格/资料、令牌刷新与本地多账号管理 |
| 启动 | 强类型配置、隔离实例、进程状态、脱敏日志、退出事件、停止和诊断 |
| 加载器 | Fabric、Forge、NeoForge、独立 OptiFine 查询与安装 |
| 资源 | Modrinth / CurseForge 查询、图标与图库信息、Mod 依赖安装、来源清单、更新检查和单项事务更新 |
| 整合包 | 按标准清单识别 Modrinth ZIP / mrpack v1 与 CurseForge manifest v1；导入、安装、实例/整包导出、整包计划/更新、回滚及确认清理 |
| 高级设置 | 窗口、全屏、服务器、独立最小/最大堆与自定义 JVM/游戏参数 |

发布记录集中展示于顶部；完整历史保留在 [CHANGELOG](CHANGELOG.md)。

## 设计

- 常见操作提供简短入口，高级调用沿用同一实现和配置模型。
- 耗时操作提供 Async API，可按需接入进度与 CancellationToken。
- 安装自动处理依赖、文件校验、目录边界和必要事务，返回强类型结果。
- 账号、Java 主版本、内存预算由宿主明确决定，启动不会自动下载安装 Java。
- 令牌在日志、进度和异常输出边界过滤；Windows 刷新凭据默认使用当前用户 DPAPI 保存。

## 平台与引用

开发与运行目标为 Windows 10/11、.NET 10、C# 14。需要可用的兼容 Java、对应官方服务网络；正版登录需要开发者自己的合法 Microsoft 应用与玩家授权，CurseForge 需要调用方合法 API Key。核心只有 `FMCore` 一个业务类库；可在 JetBrains Rider 中开发。

1.0.2 可通过 NuGet 安装：

```powershell
dotnet add package FocusMinecraftCore --version 1.0.2
```

使用源码时，在宿主项目引用 `FMCore/FMCore.csproj`，或按[本地构建说明](docs/releasing.md)生成本地包。包名是 `FocusMinecraftCore`，程序集和命名空间为 `FMCore`。

## 开发者文档

| 阅读顺序 | 内容 |
| --- | --- |
| [开发者手册](docs/sdk.md) | 全模块入口、返回值与文档导航 |
| [独立功能示例](docs/examples.md) | 每项功能的完整 C# 源码、最简与高级调用 |
| [第三方版本兼容](docs/compatibility.md) | 只读导入、目录别名、通用启动与安全边界 |
| [资源图片](docs/resource-images.md) | Mod 搜索图标、详情图库及独立示例 |
| [目录管理](docs/directory-layout.md) | 当前源码的版本目录隔离、共享资源与旧实例迁移 |
| [高级指南](docs/advanced.md) | 配置、进度、取消、事件、错误与资源释放 |
| [完整源码架构](docs/architecture.md) | 文件树、模块依赖和调用流程 |
| [完整启动器项目](docs/sample-launcher.md) | .NET 10 控制台示例、项目引用、运行与前置条件 |
| [公开 API 清单](docs/public-api.md) | 当前源码导出的类型、方法、属性、事件和枚举 |
| [API 调整与迁移](docs/api-changes.md) | T10 新增便捷入口及兼容性说明 |
| [CHANGELOG](CHANGELOG.md) | 阶段变化、修复及验证边界 |


## 许可

源码仓库：[Voltucit/FocusMinecraftCore](https://github.com/Voltucit/FocusMinecraftCore)。项目展示名称、程序集和命名空间保持 FMCore。

FMCore 使用 [MIT License](LICENSE)。Minecraft、Java 发行版及其他第三方内容的权利归各自权利人，必要归属见 [NOTICE](NOTICE.md)。SDK 不分发游戏文件或代替外部服务授权。
