# 安装 OptiFine

使用 `FMCore.Modsloader`。可以让核心下载所选版本，也可以使用已有的 OptiFine 安装包。安装需要 Java 来运行补丁工具，不会启动游戏。

## 在线安装

`javaPath` 是启动器选好的 Java 路径：

```csharp
using FMCore.Modsloader;

var installer = new OptiFineOnlineInstaller(
    "1.21.10", "HD_U_J7_pre11", ".minecraft", javaPath);
var result = await installer.InstallAsync();
```

在线入口默认使用 BMCLAPI。查询可选版本：

```csharp
var versions = await OptiFineOnlineInstaller.GetVersionsAsync("1.21.10");
foreach (var version in versions)
    Console.WriteLine(version.Edition);
```

把选中的 `Edition` 作为上面第二个参数。列表也提供 `IsPreview`，可区分预览版。

## 使用本地安装包

`installerJarPath` 是用户选好的 OptiFine JAR 文件路径：

```csharp
var installer = new OptiFineInstaller(installerJarPath, ".minecraft", javaPath);
var result = await installer.InstallAsync();
```

本地入口从包内读取版本。已有文件不代表来源可靠，启动器应让用户选择可信安装包。

## 进度和安装结果

```csharp
var result = await installer.InstallAsync(
    progress => Console.WriteLine(progress.Message));
```

结果中的 `MinecraftRoot`、`VersionId` 交给[普通启动方法](launch.md)。`VersionDirectory` 是保存位置。缺少原版时会先准备原版，再执行补丁。

可选 `versionId` 自定义版本名称，`maxConcurrency` 默认 4，`processorMemoryMb` 默认 2048 MB。完整参数见[接口参考](public-api.md#fmcoremodsloader)。取消和界面更新见[通用说明](advanced.md)。

## 下载源和限制

在线入口可通过 `Source` 选择 BMCLAPI 或兼容其返回格式的自定义服务；自定义规则转换的是原始 BMCLAPI 地址。不能直接选择 `DownloadSource.Official` 在线下载，因为这里没有接入官方自动下载服务。已有官方 JAR 时使用本地入口。

本地入口的 `Source` 只影响原版与依赖下载。选择方法和 BMCLAPI 使用要求见[下载源](download-sources.md)。

在线服务不提供预期 SHA-1 和完整大小说明时，只能限制安装包大小、检查包内容与版本；不能说成已经按官方校验值验证来源。安装后的生成文件检查也不能代替来源检查。

当前面向 Windows，已实际运行过 `1.21.10 / HD_U_J7_pre11` 的本地和在线安装结果。这里只安装独立 OptiFine 版本，不负责将其与 Forge 等混合安装，也不安装光影。已有不同配置会保留并报错；生成文件损坏时先备份移走再安装。

[调用示例](../examples/optifine-installation.md) · [游戏启动](launch.md)
