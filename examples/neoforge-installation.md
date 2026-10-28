# NeoForge 安装示例

在已引用 FMCore 的项目中调用：

```csharp
using FMCore.Modsloader;

var installer = new NeoForgeInstaller("1.21.10", "21.10.64", ".minecraft", javaPath);
var result = await installer.InstallAsync();
Console.WriteLine(result.VersionId);
```

`javaPath` 是启动器选好的 Java，用于运行安装补丁工具。

需要显示状态时，把安装这一行改为：

```csharp
var result = await installer.InstallAsync(
    progress => Console.WriteLine(progress.Message));
```

安装成功后，将 `result.MinecraftRoot`、`result.VersionId` 填入普通[启动配置](../docs/launch.md)。不要把示例中的版本写死为启动器唯一选项，实际使用用户选择的版本。

[可选设置、失败处理和限制](../docs/neoforge-installation.md) · [下载源](../docs/download-sources.md)
