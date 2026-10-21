# FocusMinecraftCore

使用 **.NET 10 / C# 14** 开发的 Minecraft Java 版启动核心。一个核心类库即可接入 WPF、Avalonia、WinUI 或控制台应用，核心不依赖 GUI 框架，不强制使用依赖注入。

当前已实现 **T01–T06 / P0**，已发布 [**FocusMinecraftCore 0.1.0-preview.2**](https://www.nuget.org/packages/FocusMinecraftCore/0.1.0-preview.2)。GitHub 仓库、程序集与代码命名空间仍为 `FMCore`。[GitHub Release](https://github.com/Voltucit/FMCore/releases/tag/v0.1.0-preview.2)提供核心包、符号包及中文更新日志。

## 安装

在目标为 .NET 10 的项目执行：

```shell
dotnet add package FocusMinecraftCore --version 0.1.0-preview.2
```

已通过独立项目从 NuGet.org 恢复并调用公开 API 验证。源码开发、本地产物源及后续发布步骤见[发布与更新手册](https://github.com/Voltucit/FMCore/blob/main/docs/releasing.md)。包附带 XML API 文档、SourceLink 与独立符号包；部分存量模型成员的 XML 说明尚未补全。

## 快速入门

以下为完整控制台示例。先安装游戏元数据要求的 Java；此例使用 Minecraft **1.16.5 / Java 8**。启动时自动安装或修复缺失文件，首次安装需要联网。

```csharp
using FMCore.Authentication;
using FMCore.Launch;

using var accounts = new AccountManager(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "FMCoreSample", "accounts"));
await using var launcher = new MinecraftLauncher(new()
{
    GameCore = new() { RootDirectory = ".minecraft", VersionId = "1.16.5" },
    Account = await accounts.LoginOfflineAsync("Player")
});
var result = await launcher.LaunchAsync();
if (result.Success)
    await launcher.WaitForExitAsync();
else
    Console.Error.WriteLine(result.Error?.Message);
```

设置、存档与游戏日志默认隔离到 `instances/<版本 ID>`。保持启动器对象存活，`DisposeAsync` 会停止其拥有的游戏；GUI 应将其保存为窗口/应用字段，并在关闭时异步释放。指定 Java、内存、窗口、进度、日志、退出监听和停止见[高级启动示例](https://github.com/Voltucit/FMCore/blob/main/docs/launch.md)。

离线登录不联网，但不授予游戏购买资格或正版服务器权限。正版登录使用调用方自己合法注册的 Microsoft Client ID，经设备授权、Xbox/XSTS 和 Minecraft Services 验证；支持刷新，不收集密码，不在失败后自动切换离线身份。Windows 默认使用当前用户 DPAPI 保存令牌，其他平台需宿主提供安全存储实现。完整示例见[账号手册](https://github.com/Voltucit/FMCore/blob/main/docs/authentication.md)。

## 功能与开发者 API

| 模块 | 当前能力与入口 | 使用手册 |
| --- | --- | --- |
| Downloader | `DownloadManager`：单文件/批量并发、进度、取消、哈希、重试与有效文件复用 | [下载](https://github.com/Voltucit/FMCore/blob/main/docs/downloader.md) |
| Versions | `GameCoreUtil` / `VersionManager`：官方列表、正式版/快照、本地查询、缓存、继承与条件规则 | [版本](https://github.com/Voltucit/FMCore/blob/main/docs/versions.md) |
| Java | `JavaUtil` / `JavaManager`：有界检测、自定义路径、实际探测及主版本/架构匹配 | [Java](https://github.com/Voltucit/FMCore/blob/main/docs/java.md) |
| Installer / Assets | `GameInstaller`：原版安装、校验/修复、资源布局与原生依赖安全解压 | [安装](https://github.com/Voltucit/FMCore/blob/main/docs/installer.md) |
| Authentication | `AccountManager`：正版/离线、稳定 UUID、多账号、切换、默认账号、刷新与安全存储 | [账号](https://github.com/Voltucit/FMCore/blob/main/docs/authentication.md) |
| Launch | `MinecraftLauncher`：统一配置、自动准备、实例目录、参数、日志、停止与退出结果 | [启动](https://github.com/Voltucit/FMCore/blob/main/docs/launch.md) |

异步 I/O 支持 `CancellationToken`，耗时任务提供进度或事件，业务失败具有结构化错误码。单次启动成功只表示进程建立，游戏状态仍需结合日志和退出事件判断。完整签名、返回值和异常见[P0 SDK 清单](https://github.com/Voltucit/FMCore/blob/main/docs/sdk.md)及各模块手册。

下载复用已完成且校验有效的文件，中断后的未完成文件重新尝试，暂不支持 HTTP Range 断点续传。尚未实现加载器安装、完整实例管理、Mod/整合包管理或 Java 自动下载。

## 兼容性与验证

| 项目 | 实际验证范围 |
| --- | --- |
| FMCore 宿主 | .NET 10 / C# 14；Windows x64 |
| 游戏启动 | Minecraft Java 正式版 **1.16.5**，Java **1.8.0_482 x64**；真实正版与离线身份 |
| 验证深度 | 观察窗口、音频和纹理初始化后通过 SDK 停止；未验收世界或多人服务器 |
| 其他版本 | 版本/规则 Fixtures 与元数据验证，不代表实际安装或启动通过 |
| Linux / macOS | 路径和规则模拟测试；尚未实机启动，正版令牌存储需宿主提供安全实现 |

首版发布前，Windows 自动化测试 **402 项通过**，并完成独立 NuGet 包调用与 **14 段 C# 示例**编译。自动化测试使用 Fixtures、模拟认证、本机 HTTP、测试子进程和 DPAPI；真实游戏验证范围以上表为准。内部测试与详细报告保留在维护者本地，不随公开源码分发。

## 源码开发

公开仓库包含 `FMCore/` 核心源码、必要工程配置和中文开发者文档，业务按功能文件夹分类。安装 `global.json` 指定的 .NET 10 SDK，以只引用核心项目的 `FMCore.slnx` 打开方案，全新克隆后即可执行：

```shell
dotnet restore FMCore.slnx
dotnet build FMCore.slnx -c Release --no-restore
dotnet pack FMCore/FMCore.csproj -c Release --no-build -o artifacts/packages
```

GitHub Actions 自动构建核心并生成 `.nupkg` / `.snupkg`；公开发布只由维护者手动确认触发。CLI、测试、内部报告与验收脚本不随公开仓库分发，工作流不依赖这些文件；维护者使用本地 `FMCore.Local.slnx` 保留完整开发与测试能力，详见[发布与更新手册](https://github.com/Voltucit/FMCore/blob/main/docs/releasing.md)。后续每次功能或公开 API 变更，同步维护 README、相关中文使用手册、示例与 CHANGELOG。

## 文档与许可证

- [开发者 API 与组合示例](https://github.com/Voltucit/FMCore/blob/main/docs/sdk.md) · [架构](https://github.com/Voltucit/FMCore/blob/main/docs/architecture.md) · [开发路线](https://github.com/Voltucit/FMCore/blob/main/docs/roadmap.md)
- [中文更新日志](https://github.com/Voltucit/FMCore/blob/main/CHANGELOG.md) · [发布及后续更新](https://github.com/Voltucit/FMCore/blob/main/docs/releasing.md)
- 源码采用 [MIT 许可证](https://github.com/Voltucit/FMCore/blob/main/LICENSE)，作者 **Voltucit**；依赖与游戏内容说明见 [NOTICE](https://github.com/Voltucit/FMCore/blob/main/NOTICE.md)。

FMCore 是独立的第三方工具，与 Mojang 或 Microsoft 无隶属或认可关系。发行包不包含 Minecraft 客户端、资源、Java 或账号凭据；游戏文件由用户通过官方服务获取，相关内容仍受其原始条款约束。
