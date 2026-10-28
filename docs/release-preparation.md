# 2.0.1 发布准备

版本号为 `2.0.1`，分支为 `v2.0`，包名为 `FocusMinecraftCore`。这里只说明已准备的配置，不表示已经上传。

## 当前准备了什么

- 核心项目使用 `2.0.1`，同时生成普通包和 `.snupkg` 调试信息包。
- [发布说明草稿](releases/v2.0.1.md)已写好。
- 发布流程需要手动运行，只允许从 `v2.0` 分支启动。
- 发布时读取已有的 `v2.0.1` 版本标记，确认它来自 `v2.0`，再检查包名和版本是否一致。
- 同一批生成文件用于 GitHub Release 和 NuGet 上传，避免两边内容不同。

## 先生成本地包

```powershell
dotnet pack ./FMCore/FMCore.csproj -c Release -o ./artifacts/packages
```

应得到 `FocusMinecraftCore.2.0.1.nupkg` 和 `FocusMinecraftCore.2.0.1.snupkg`，内容见[打包说明](packaging.md)。此前确认的是 `2.0.0-dev`，不代替这两个新文件的核对。

## 真正发布前

1. 将准备发布的代码、文档和配置提交到远端 `v2.0`。
2. 为那次提交创建 `v2.0.1` 版本标记，也就是 Git 的 Tag。
3. 确认 NuGet 允许这个 GitHub 仓库上传。
4. 手动运行 `release.yml`，版本填写 `2.0.1`，确认文字填写 `publish FocusMinecraftCore 2.0.1`。

这些在线操作仍需维护者明确决定，本次文档整理不执行它们。流程不会自行创建版本标记。

## NuGet 上需要匹配的设置

当前流程使用 NuGet Trusted Publishing：GitHub 在发布时获取短期上传凭据，不在仓库里放长期密钥。

| 设置 | 当前值 |
| --- | --- |
| NuGet 账号 | `Voltucit` |
| GitHub 所有者 | `Voltucit` |
| 仓库 | `FocusMinecraftCore` |
| 工作流文件名 | `release.yml`，不带目录 |
| GitHub environment | `nuget` |

这部分远端设置及在线发布流程尚未实际确认。流程会在上传前再次检查是否已经存在同一版本，发现冲突或查询失败就停止。

[NuGet 官方发布设置说明](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
