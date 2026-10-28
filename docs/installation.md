# 安装原版游戏

用 `FMCore.Installer` 下载一个完整原版游戏。只需版本号和保存位置，安装时不需要账号或 Java。

## 最短调用

```csharp
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft");
var result = await installer.InstallAsync();
```

这会下载客户端、依赖库、声音、贴图和日志配置，并检查文件是否完整。它只安装游戏，不启动游戏。安装 Java 见 [Java 安装](java-installation.md)。

## 让用户选择版本

```csharp
var versions = await MinecraftInstaller.GetVersionsAsync();
foreach (var version in versions)
    Console.WriteLine($"{version.Id}：{version.Type}，{version.ReleaseTime}");
```

`Type` 可以区分正式版和快照。拿到列表后，由启动器决定显示哪些版本。

## 显示安装进度

```csharp
var result = await installer.InstallAsync(
    progress => Console.WriteLine(progress.Message));
```

`progress.Download` 有值时，可以读取当前这批文件的数量、百分比和速度。某一批达到 100% 后，可能还要处理下一批；等 `InstallAsync()` 成功返回才算安装完成。

## 常用设置

| 设置 | 用途 |
| --- | --- |
| `versionId` | 要安装的版本，如 `1.21.10` |
| `minecraftRoot` | 游戏根目录，如 `.minecraft` |
| `maxConcurrency` | 同时处理多少个文件，默认 4 |
| `targetArchitecture` | 为哪种系统位数准备文件，默认跟随当前系统 |
| `Source` | 下载源，默认官方；可选 BMCLAPI 或自定义源 |

不用调整的设置直接省略。完整声明见[接口参考](public-api.md#fmcoreinstaller)。选择下载源见[下载源](download-sources.md)，取消操作见[取消说明](advanced.md)。

## 文件保存在哪里

根目录由你决定，不会额外套一层随机文件夹：

```text
.minecraft/
  versions/1.21.10/1.21.10.json
  versions/1.21.10/1.21.10.jar
  assets/
  libraries/
```

返回的 `result.VersionId`、`result.MinecraftRoot`、`result.VersionDirectory` 可以直接用于下一步启动或展示安装位置。`result.Files` 是这次文件处理结果。

## 重复安装和失败

已有正确文件会继续使用。文件损坏时，会先保留备份再替换。下载失败后已经完成的文件会留下，再次安装可以继续使用。

已有版本配置与要安装的内容冲突时，会保留原文件并报错。网络、权限、文件说明不完整等问题也会报错，启动器可以显示错误并让用户重试。

当前安装流程面向 Windows。只安装原版；加载器另用 [Fabric](fabric-installation.md)、[Forge](forge-installation.md)、[NeoForge](neoforge-installation.md) 或 [OptiFine](optifine-installation.md)。支持安装某版本的文件，不代表已经确认该版本能被当前启动入口运行；具体见[启动范围](launch.md#使用范围)。
