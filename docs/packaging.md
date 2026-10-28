# 生成和使用本地包

包名是 `FocusMinecraftCore`，代码中使用 `FMCore`，目标为 .NET 10。当前准备版本为 `2.0.1`，分支为 `v2.0`。

## 生成包

在仓库根目录运行：

```powershell
dotnet pack ./FMCore/FMCore.csproj -c Release -o ./artifacts/packages
```

会生成两个文件：

| 文件 | 用途 |
| --- | --- |
| `FocusMinecraftCore.2.0.1.nupkg` | 给启动器引用的核心包 |
| `FocusMinecraftCore.2.0.1.snupkg` | 调试时定位问题用的信息包 |

这条命令只生成本地文件，不上传。此前检查通过的是 `2.0.0-dev` 包，新的 `2.0.1` 包仍需要实际生成并确认。

## 在自己的项目中使用

最直接的开发方式是[引用源码项目](../README.md#安装指南)。使用本地包时，在 Rider 或 Visual Studio 的 NuGet 设置中，把本仓库的 `artifacts/packages` 文件夹添加为一个包来源，再选择 `FocusMinecraftCore` 的 `2.0.1`。

保留 `nuget.org` 来源：核心依赖的 `Microsoft.Identity.Client` 仍需从那里获取，不用手动复制它的 DLL。

## 包里应该有什么

普通包包含 `lib/net10.0/FMCore.dll`、包内说明、MIT 许可证和包信息。调试信息包包含对应的 `FMCore.pdb`。两者的包名和版本应一致。

本地 CLI、测试、Java、游戏数据、账号、IDE 设置及教学记录都不应进入包。公开手册和示例在源码仓库中查看。

## 准备正式发布

本地打包成功不等于已经上传。公开发布的流程和需要设置的账号信息见[发布准备](release-preparation.md)。2.0 与 1.x 的调用不兼容，请按[当前示例](README.md)接入。

[dotnet pack 官方说明](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack)
