# 安装 Forge

使用 `FMCore.Modsloader`。填写游戏版本、Forge 版本和根目录，再调用一次安装方法。

## 最短调用

```csharp
using FMCore.Modsloader;

var installer = new ForgeInstaller("1.21.10", "60.1.18", ".minecraft", javaPath);
var result = await installer.InstallAsync();
```

`javaPath` 是启动器选好的 Java 路径。安装需要运行 Java 补丁工具来生成游戏文件，可以使用启动器已有的 Java 设置，不必让用户再选一次。

原版缺失时会准备原版，再安装 Forge 并补齐所需文件。安装不启动游戏。

## 查询版本

```csharp
var versions = await ForgeInstaller.GetVersionsAsync("1.21.10");
```

返回可选版本号，由启动器或用户选择，不自动替你选版本。

## 显示进度和选择下载源

```csharp
var result = await installer.InstallAsync(
    progress => Console.WriteLine(progress.Message));
```

安装器的 `Source` 可选官方、BMCLAPI 或自定义源，写法见[下载源](download-sources.md)。版本查询也可以在游戏版本后传入所选源。

`Message` 会说明当前步骤；`Download` 有值时，是当前这批文件的下载进度，不是整个安装的统一百分比。等方法返回再显示安装完成。取消和界面更新见[通用说明](advanced.md)。

## 安装后启动

把 `result.MinecraftRoot` 和 `result.VersionId` 填入 [LaunchConfig](launch.md) 的 `Root` 和 `Version`，沿用普通启动方法。

结果还提供 `GameVersion`、`ForgeVersion` 和 `VersionDirectory`，可用于显示版本与保存位置。

## 可选参数

| 参数 | 用途 |
| --- | --- |
| `versionId` | 自己指定安装后的版本名称，默认由游戏和加载器版本组成 |
| `maxConcurrency` | 同时处理的文件数，默认 4 |
| `processorMemoryMb` | 补丁工具最大内存，默认 2048 MB |

安装目录由启动器决定，新版本放在 `versions/版本名称`。新名称不能和原版相同，不能用带目录分隔符等不合法名称。根目录不能含分号。

## 失败和使用范围

当前支持使用 `spec=1` 安装说明的客户端安装包，不表示所有旧 Forge 安装包格式都可用。安装器会运行补丁工具并检查生成文件，退出码为 0 不能单独代替文件检查。

已有相同配置可以继续使用；已有不同配置会保留并报错。下载、权限或补丁执行失败后，已完成的文件会留下，重新运行可继续使用。加载器生成文件损坏时，应先备份移走，再重新安装。

当前面向 Windows。已实际安装并运行过 `1.21.10 / Forge 60.1.18`，不表示所有旧版、其他系统位数或 Mod 组合都已测试。启动本身的限制见[启动范围](launch.md#使用范围)。

[调用示例](../examples/forge-installation.md) · [完整参数和错误类型](public-api.md#fmcoremodsloader)
