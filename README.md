
<div align="center">

<img Height="250" Width="250" src="assets/icon.png"/>
</div>
<hr>
# FocusMinecraftCore

[![Stars](https://img.shields.io/github/stars/Voltucit/FocusMinecraftCore?style=flat)](https://github.com/Voltucit/FocusMinecraftCore/stargazers)
[![Forks](https://img.shields.io/github/forks/Voltucit/FocusMinecraftCore?style=flat)](https://github.com/Voltucit/FocusMinecraftCore/forks)
[![Issues](https://img.shields.io/github/issues/Voltucit/FocusMinecraftCore?style=flat)](https://github.com/Voltucit/FocusMinecraftCore/issues)
[![License](https://img.shields.io/github/license/Voltucit/FocusMinecraftCore?style=flat)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=flat)

<hr>

用 C# 开发 Minecraft Java 版启动器时，把安装、下载、登录和启动交给 FMCore。你负责界面和用户选择，FMCore 负责把游戏准备好并运行。

包名是 `FocusMinecraftCore`，代码中使用 `FMCore`。当前 2.0 面向 Windows，需要 .NET 10；2.0 与 1.x 的调用方式不同。

## 能做什么

- 查询、安装原版游戏，补齐缺失文件，检查并修复下载不完整的文件。
- 启动已有游戏，接收进度、游戏日志和退出结果。
- 查找或安装 Java，使用 Microsoft、外置验证或离线账号。
- 安装 Fabric、Forge、NeoForge 和 OptiFine。
- 在 Modrinth 搜索模组、资源包、数据包、光影和整合包，获取详情、中文分类与下载文件。
- 选择官方、BMCLAPI 或开发者自己的下载源。

## 安装指南

先安装 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。目前 2.0.0 正在准备发布；使用仓库中的 2.0 代码，可以直接引用项目。

将本仓库放在你的启动器项目旁边，在启动器项目目录执行：

```powershell
git clone --branch v2.0 https://github.com/Voltucit/FocusMinecraftCore.git ../FocusMinecraftCore
dotnet reference add ../FocusMinecraftCore/FMCore/FMCore.csproj
```

你的项目需要使用 `net10.0` 或兼容的 Windows 目标。已有本地包的用法见[打包说明](docs/packaging.md)。正式包发布后，再使用下面的 NuGet 命令：

```powershell
dotnet package add FocusMinecraftCore --version 2.0.0
```

## 使用示例

### 安装原版

只需要版本号和保存目录，不需要 Java 路径：

```csharp
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft");
var result = await installer.InstallAsync();
```

游戏会保存到 `.minecraft/versions/1.21.10`，共用文件放在 `.minecraft/assets` 和 `.minecraft/libraries`。

### 启动游戏

填好账号、游戏目录和 Java，再调用 `LaunchAsync()`：

```csharp
using FMCore.Accounts;
using FMCore.Launch;

LaunchConfig config = new()
{
    Account = OfflineAccount.Create("Steve"),
    GameCoreConfig = new() { Root = ".minecraft", Version = "1.21.10" },
    JavaConfig = new()
    {
        JavaPath = "C:/Java/jdk-25/bin/java.exe",
        MaxMemoryMb = 2048
    }
};

var launcher = new MinecraftLauncher(config);
int exitCode = await launcher.LaunchAsync();
```

这里的 Java 路径是示例；实际由启动器读取用户选择的 Java。离线账号只用于离线身份，正版登录见[Microsoft 登录](docs/microsoft-login.md)。

`LaunchAsync()` 会等到游戏退出后返回退出码。要显示进度，把最后一行改为：

```csharp
int exitCode = await launcher.LaunchAsync(
    progress => Console.WriteLine(progress.Message));
```

### 补全文件后再启动

已有上面的 `config` 和 `launcher` 时：

```csharp
await new GameFileCompleter(
    config.GameCoreConfig.Version,
    config.GameCoreConfig.Root).CompleteAsync();

int exitCode = await launcher.LaunchAsync();
```

这段还需要 `using FMCore.Installer;`。正确文件会直接使用，缺失文件会下载，坏文件修复前会保留备份。补全失败或取消时，游戏不会接着启动。

## 文档

从[文档目录](docs/README.md)选择你要做的事：

| 想做什么 | 从这里开始 |
| --- | --- |
| 安装、启动游戏 | [原版安装](docs/installation.md)、[游戏启动](docs/launch.md) |
| 登录账号、准备 Java | [账号](docs/accounts.md)、[Java](docs/java.md) |
| 下载、修复文件 | [游戏文件](docs/game-files.md)、[下载源](docs/download-sources.md) |
| 安装加载器 | [Fabric](docs/fabric-installation.md)、[Forge](docs/forge-installation.md)、[NeoForge](docs/neoforge-installation.md)、[OptiFine](docs/optifine-installation.md) |
| 获取社区资源 | [Modrinth](docs/modrinth.md) |
| 接入自己的启动器 | [示例目录](examples/README.md)、[进度、取消与错误处理](docs/advanced.md) |
| 查具体参数 | [接口参考](docs/public-api.md) |

## 当前范围

已在 Windows x64 上运行过原版 1.8.9、1.21.10 和所选加载器版本；这不代表所有游戏版本都已测试。1.16.5 这类使用新版启动参数、旧版原生库说明的组合暂不支持。其他限制见[启动说明](docs/launch.md#使用范围)。

CurseForge 暂未接入。模组的安装管理、整个整合包系统、账号保存与加密由启动器作者实现。Microsoft 登录需要作者自己的 Client ID。FMCore 只在指定位置创建功能需要的文件。

## 贡献指南

欢迎通过 [Issue](https://github.com/Voltucit/FocusMinecraftCore/issues)反馈问题，或通过 [Pull Request](https://github.com/Voltucit/FocusMinecraftCore/pulls)提交改进。

1. 说明要解决的问题；报错时附上版本、系统和必要日志，去掉密码、令牌与密钥。
2. 从 `v2.0` 分支开始修改，保持调用简单，并同步相关文档与示例。
3. 说明你如何确认改动有效；涉及游戏运行时，写明实际使用的游戏和 Java 版本。

在仓库根目录可用 `dotnet build FMCore.slnx -c Release` 检查核心能否生成。不要提交游戏数据、账号文件或本地调试目录。

## 许可证

本项目使用 [MIT 许可证](LICENSE)。使用第三方下载服务和资源时，也请遵守对应服务的规则及资源作者的许可证。
