# 发布与后续更新

仓库：[Voltucit/FocusMinecraftCore](https://github.com/Voltucit/FocusMinecraftCore)。PackageId 为 FocusMinecraftCore，程序集和命名空间保持 FMCore。本文对应 1.0.0 发布流程；公开状态以 GitHub Release / NuGet 实际记录为准。历史 0.1.0-preview.2 保留，不覆盖旧包或移动旧标签。

## 公开源码构建

需要 global.json 指定的 .NET 10 SDK。FMCore.slnx 仅引用核心，不依赖维护者的测试、CLI 或内部脚本。

```powershell
dotnet restore FMCore.slnx
dotnet build FMCore.slnx -c Release --no-restore
dotnet pack FMCore/FMCore.csproj -c Release --no-build --no-restore -o artifacts/packages
```

包包含 net10.0/FMCore.dll、FMCore.xml、README、LICENSE、NOTICE、CHANGELOG 和 NuGet 必需元数据。唯一运行时包依赖为 Microsoft System.Security.Cryptography.ProtectedData，用于 Windows DPAPI，不把其 DLL 重复打包。

独立引用：

```powershell
dotnet add package FocusMinecraftCore --version 1.0.0
```

正式公开前，可从自行构建的本地目录还原 1.0.0；不要将本地候选包的验证当作 NuGet.org 已公开验证。

## 本地完整验收

维护者使用 FMCore.Local.slnx。FMCore.Tests/、FMCore.Cli/、AGENTS.md、scripts/、docs/validation/ 与 artifacts/ 由 .gitignore 排除；取消跟踪只能使用 git rm --cached，保留实际文件。

```powershell
dotnet build FMCore.Local.slnx -c Release
dotnet test FMCore.Local.slnx -c Release --no-build
./scripts/Verify-Package.ps1
./scripts/Verify-Examples.ps1
python scripts/Verify-Cli.py
```

独立调用方仅引用本地产出的 FocusMinecraftCore 包，使用隔离缓存验证公开 API；示例原样编译，不补 using 或缺失变量。协议 Mock、本地真实文件/进程、真实在线接口及真实游戏分别记录；公开 CI 不声称运行未上传的测试。

## 版本与来源

Version / PackageVersion / InformationalVersion 为 1.0.0；AssemblyVersion / FileVersion 为 1.0.0.0。默认启动器版本字符串为 1.0.0，宿主可覆盖。核心程序集简单名称仍为 FMCore，兼容性变化见 [API 说明](api-changes.md)。

候选包可在未提交工作区构建，但最终正式包必须从通过验收的最终提交及 v1.0.0 Tag 重建。SourceLink 只能指向实际已推送提交；未提交候选包不能声称远程精确源码可用。发布后检查 Release 中 SOURCE_COMMIT.txt、Tag 与 NuGet nuspec repository commit 相同，并验证 SourceLink。

## 需要明确确认的公开步骤

先完成版本/文档/构建/测试/包验收并展示结果。只有用户明确确认后，才依次执行：

1. 提交最终修改并推送 main。
2. 在同一已验收提交创建并推送 v1.0.0，不覆盖已有 Tag。
3. 从 main 手动调度 release.yml，version=1.0.0，confirmation=`publish FocusMinecraftCore 1.0.0`。
4. prepare 检查 Tag、包版本、发布冲突，构建一次并保存同一批产物。
5. github-release 校验 SHA-256，创建 FocusMinecraftCore v1.0.0 正式 Release，不标记 Pre-release。
6. nuget 在 nuget Environment 通过 NuGet/login 交换 OIDC 短期凭据，上传相同的 nupkg 和相邻 snupkg。
7. 检查两端公开状态，在全新 .NET 10 项目从 NuGet.org 安装 1.0.0 并运行基础 API。

工作流只有 workflow_dispatch，不因 push、Tag 或普通 CI 成功自动发布。1.0.0 为正式版本；若未来输入带预览后缀，则工作流按版本后缀设置 Pre-release。

Release 附件为 FocusMinecraftCore.1.0.0.nupkg、FocusMinecraftCore.1.0.0.snupkg、CHANGELOG.md、RELEASE_NOTES.md、SOURCE_COMMIT.txt 和 SHA256SUMS.txt。发布说明来自已审查的 [v1.0.0 中文说明](releases/v1.0.0.md)。两端不得分别重建不同文件。

## 安全凭据与失败处理

复用现有 NuGet Trusted Publishing，不把长期 API Key 写进源码、环境文件或日志。策略需匹配以下实际身份：

| 字段 | 值 |
| --- | --- |
| Repository Owner | Voltucit |
| Repository | FocusMinecraftCore |
| Workflow File | release.yml |
| Environment | nuget |
| NuGet 用户 | Voltucit |
| 包范围 | FocusMinecraftCore |

NuGet 同时校验仓库名和数值 ID，重命名后即使 ID 不变，也需要一条 Repository 为 FocusMinecraftCore 的策略；若旧策略编辑页不能修改 Repository，应保留旧策略并新增正确策略，包范围限定 FocusMinecraftCore，权限仅需发布已有包的新版本；参见[官方验证实现](https://github.com/NuGet/NuGetGallery/blob/dev/src/NuGetGallery.Services/Authentication/Federated/GitHubTokenPolicyValidator.cs#L222)。本地检查和历史成功均不能证明当前 OIDC 交换一定成功。不通过试传同一版本测试凭据。参见 [NuGet 官方 Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)。

冲突检查必须得到明确 404 才继续；网络失败或其他 HTTP 状态均停止，不使用 --skip-duplicate。Release 创建失败时 NuGet 不运行。Release 已公开而 NuGet 失败时，立即报告两端实际状态，检查认证/包状态后只恢复必要步骤，不盲目重跑 prepare 或重复上传。上传状态不明时先查询官方版本，不自动换版本或回滚删除公开资源。

后续每次 API 修改同步中文手册、独立示例、API 清单和 CHANGELOG。发布前重新执行完整验收，发布后验证下载到的实际官方包。
