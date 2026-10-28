# OptiFine 安装示例

`javaPath` 使用启动器选好的 Java。在线安装默认使用 BMCLAPI：

```csharp
using FMCore.Modsloader;

var installer = new OptiFineOnlineInstaller(
    "1.21.10", "HD_U_J7_pre11", ".minecraft", javaPath);
var result = await installer.InstallAsync();
```

已有可信安装包时，改用本地入口，`installerJarPath` 是用户选好的 JAR 路径：

```csharp
var installer = new OptiFineInstaller(installerJarPath, ".minecraft", javaPath);
var result = await installer.InstallAsync();
```

两段是二选一的写法。结果中的 `MinecraftRoot` 和 `VersionId` 交给[普通启动方法](../docs/launch.md)。显示进度时可传入 `progress => Console.WriteLine(progress.Message)`。

在线入口不能直接选官方源；自定义源需要兼容它使用的 BMCLAPI 格式。[完整说明和限制](../docs/optifine-installation.md) · [下载源](../docs/download-sources.md)
