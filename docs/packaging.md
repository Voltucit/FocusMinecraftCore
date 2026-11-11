# 生成和使用本地包

包名是 `FocusMinecraftCore`，代码中使用 `FMCore`，目标为 .NET 10。当前准备版本为 `2.0.7`，分支为 `v2.0`。

## 生成包

在仓库根目录运行：

```powershell
dotnet pack ./FMCore/FMCore.csproj -c Release -o ./artifacts/packages
```

会生成两个文件：

| 文件 | 用途 |
| --- | --- |
| `FocusMinecraftCore.2.0.7.nupkg` | 给启动器引用的核心包 |
| `FocusMinecraftCore.2.0.7.snupkg` | 调试时定位问题用的信息包 |

这条命令只生成本地文件，不上传。发布前核对普通包、调试信息包的版本和图标。

## 在自己的项目中使用

最直接的开发方式是[引用源码项目](../README.md#安装指南)。使用本地包时，在 Rider 或 Visual Studio 的 NuGet 设置中，把本仓库的 `artifacts/packages` 文件夹添加为一个包来源，再选择 `FocusMinecraftCore` 的 `2.0.7`。

保留 `nuget.org` 来源：核心依赖的 `Microsoft.Identity.Client` 仍需从那里获取，不用手动复制它的 DLL。

## 包里应该有什么

普通包包含 `lib/net10.0/FMCore.dll`、`icon.png`、包内说明、MIT 许可证和包信息。调试信息包包含对应的 `FMCore.pdb`。两者的包名和版本应一致。

本地 CLI、测试、Java、游戏数据、账号、IDE 设置及教学记录都不应进入包。公开手册和示例在源码仓库中查看。

## 准备正式发布

本地打包成功不等于已经上传。公开发布使用仓库的“手动公开发布”工作流：

1. 确认核心包版本已更新，并在 `CHANGELOG.md` 中写好同版记录。
2. 将代码和工作流改动推送到 `v2.0` 分支。
3. 在 GitHub Actions 中点击 **Run workflow**，选择 `v2.0`，填写版本号和对应的发布确认文字。

检查和打包通过后，工作流会自动创建 `v<版本号>` Tag、GitHub Release，并继续发布 NuGet，不需要提前创建 Tag。Tag 指向本次运行实际打包的提交；已有同名 Tag 时，仅允许它指向同一提交，不会覆盖其他提交的 Tag。发布正文只读取 `CHANGELOG.md` 的同版条目。

修改工作流后请新建一次运行；重跑旧记录仍会使用旧的工作流和提交。2.0 与 1.x 的调用不兼容，请按[当前示例](README.md)接入。

[dotnet pack 官方说明](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack)
