# 把安装和启动接到你的启动器

下面使用实际公开方法，不需要自己拼下载清单或启动参数。

## 新安装：安装后启动

```csharp
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft");
var result = await installer.InstallAsync();
```

安装成功后，把 `result.MinecraftRoot` 和 `result.VersionId` 用作下面配置的根目录和版本。

## 已有游戏：补全后启动

先填好用户选择的账号、游戏目录、版本和 Java：

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
```

再执行：

```csharp
using FMCore.Installer;
using FMCore.Launch;

await new GameFileCompleter(
    config.GameCoreConfig.Version,
    config.GameCoreConfig.Root).CompleteAsync();

var launcher = new MinecraftLauncher(config);
int exitCode = await launcher.LaunchAsync();
```

已有正确文件不会重复下载。补全报错或取消时，后面不会接着启动。不必在补全成功后再完整检查一遍文件。

上面的默认文件选择适用于 Java 与当前系统位数一致的情况。使用其他位数的 Java 时，通过补全器的 `targetArchitecture` 选择对应文件，见[文件补全](../docs/game-files.md)。

## 接到按钮

WPF 的按钮处理写法见[游戏启动](../docs/launch.md#接到启动按钮)。显示进度和取消按钮见[通用说明](../docs/advanced.md)。

`LaunchAsync()` 一直等到游戏退出，不会在游戏刚打开时结束。进度达到 100% 也不代表已经打开主菜单。

## 只想检查时

“检查文件”按钮调用 `GameFileChecker.CheckAsync()`；“检查设置”按钮调用 `MinecraftLauncher.CheckAsync()`。两者按需提供，不要求普通启动前都执行，见[检查说明](../docs/diagnostics.md)。

## 新版参数配旧式原生库的游戏

1.16.5 这类游戏仍使用相同的 `MinecraftLauncher(config).LaunchAsync()` 调用，不需要启动器作者手动解压。所选 Java 仍需适用于该游戏和 Mod。已经用 Java 8 启动新装的 1.16.5＋Forge 36.2.34＋OptiFine G8，确认进入主菜单。OptiFine 在这个组合中作为模组放在实例的 `mods` 目录，文件放入由启动器安排。
