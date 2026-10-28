# FocusMinecraftCore

用 C# 开发 Minecraft Java 版启动器时，把游戏安装、下载、登录和启动交给 FMCore。你负责界面和用户选择。

适用于 2.0.0，使用 .NET 10，当前安装和启动面向 Windows。包名是 `FocusMinecraftCore`，代码中使用 `FMCore`。2.0 与 1.x 的调用方式不同。

## 安装原版

只需要版本和保存位置，安装时不需要 Java：

```csharp
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft");
var result = await installer.InstallAsync();
```

## 启动已有游戏

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

Java 路径、账号、目录和内存由启动器选择。离线身份不等于正版授权。`LaunchAsync()` 等游戏退出后返回退出码；它不自动下载缺失文件。

## 补全已有游戏

```csharp
using FMCore.Installer;

var completer = new GameFileCompleter("1.21.10", ".minecraft");
var result = await completer.CompleteAsync();
```

正确文件直接使用，缺失文件下载，坏文件修复前保留备份。成功后即可调用启动方法。

## 其他功能

支持选择官方、BMCLAPI 或自定义下载源；查找和安装 Java；Microsoft 和外置账号登录；安装 Fabric、Forge、NeoForge、OptiFine；获取 Modrinth 五类资源。

Microsoft Client ID、账号保存与加密、资源安装管理和整个整合包系统由启动器作者实现。CurseForge 暂未接入，1.16.5 所用的新版参数与旧式原生库组合尚未支持。已有 Windows x64 样本不代表所有版本都已测试。

查看[完整使用文档](https://github.com/Voltucit/FocusMinecraftCore/blob/v2.0/docs/README.md)和[示例](https://github.com/Voltucit/FocusMinecraftCore/tree/v2.0/examples)。项目使用 [MIT 许可证](https://github.com/Voltucit/FocusMinecraftCore/blob/v2.0/LICENSE)。
