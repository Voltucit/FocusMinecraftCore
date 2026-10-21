# 发布与后续更新

仓库：`https://github.com/Voltucit/FMCore`；PackageId：`FocusMinecraftCore`；首个已发布 NuGet 版本：`0.1.0-preview.2`。程序集与命名空间保持 `FMCore`，代码仍使用 `FMCore.*`。该版本已完成官方源安装及搜索验收，见[发布报告](validation/t065-release.md)。后续版本仍需逐项检查，不以本次成功替代新的发布验收。

## 本地构建和验收

在项目根目录使用 `global.json` 指定的 .NET 10 SDK：

```powershell
dotnet restore FMCore.slnx
dotnet build FMCore.slnx -c Release --no-restore
dotnet test FMCore.slnx -c Release --no-build
dotnet pack FMCore/FMCore.csproj -c Release --no-build -o artifacts/packages
./scripts/Verify-Package.ps1
./scripts/Verify-Examples.ps1
```

`Verify-Package.ps1` 在忽略的 `artifacts/package-validation` 新建独立 .NET 10 调用方，只使用 `PackageReference`，隔离包缓存，从本地产物源及 NuGet.org 还原。验证六模块的公开 API、哈希、版本、本地离线账号、严格 Java 选择、模拟安装/零下载修复，以及启动失败和取消。它不使用测试友元，不会启动真实游戏或完成真实 Microsoft 登录。独立项目为临时验收产物，不加入三项目方案。

`Verify-Examples.ps1` 从 README、SDK 和六模块手册提取 C# 代码块，分别建立临时包引用调用方编译。仅补充片段注明由宿主提供的变量，不执行下载、授权或游戏；局部函数示例不需要为了编译而调用。

在其他开发者项目引用本地产物：

```powershell
dotnet add package FocusMinecraftCore --version 0.1.0-preview.2 --source E:/rider/project/FMCore/artifacts/packages
dotnet restore --source E:/rider/project/FMCore/artifacts/packages --source https://api.nuget.org/v3/index.json
```

本地源用于首次验收；正式公开后使用 README 中的 NuGet.org 安装命令。Windows 终端如执行策略限制脚本，可使用受信任的 PowerShell 会话运行，避免全局修改执行策略。

正式发行以对应 Tag 工作流的产物为准。主分支的普通 CI 包是构建快照；文档更新后的主分支可以生成不同提交信息的包，不能因此覆盖或重新上传已经发布的同一版本。`0.1.0-preview.2` 绑定 `v0.1.0-preview.2` 对应的 `475721a` 提交。

## 包内容与 SourceLink

- 核心包只包含 `FMCore.dll`、`FMCore.xml`、README、MIT 许可证、NOTICE、CHANGELOG 和 NuGet 必需元数据。
- `.snupkg` 提供 Portable PDB，SourceLink 由 .NET 10 SDK 内置工具生成，不额外添加运行时依赖。
- SourceLink 需要真实 Git 提交和 GitHub origin，发布包必须从最终提交或 Tag 构建。尚未推送的提交只能核验映射结构，不能宣称 GitHub 源码下载已验证。
- 不上传整个 `artifacts`、本机账号、Minecraft JAR、Libraries、Assets、Java、存档或验证日志。发行附件只选定 `.nupkg`、`.snupkg` 和中文 CHANGELOG。
- NuGet 包和源码采用 MIT；游戏及依赖不因此改为 FMCore 的许可，见 [NOTICE](../NOTICE.md)。

## 首次 GitHub 与 NuGet 配置

首次公开创建 `Voltucit/FMCore`、上传源码、Tag、Release、NuGet 之前，必须向用户展示检查报告并获得明确确认。不得更改已有仓库可见性、覆盖远程历史、强推或删除资源。

仓库已在用户确认后创建；`origin` 指向该仓库。原 `v0.1.0-preview.1` 候选因 NuGet 包名近似冲突未能上传，Tag 保留。用户随后决定将包名改为 `FocusMinecraftCore`，使用新版本 `0.1.0-preview.2`，不移动原 Tag。NuGet 用户名为 `Voltucit`，账号及邮箱验证由用户完成；公开用户名存在不等于已获得包发布权限。

在 NuGet.org 的 **Trusted Publishing** 添加策略：

| 字段 | 值 |
| --- | --- |
| Repository Owner | `Voltucit` |
| Repository | `FMCore` |
| Workflow File | `release.yml`，不带目录 |
| Environment | `nuget` |
| 包模式 | `FocusMinecraftCore`，限制到该包；输入后确认已加入规则列表 |
| 允许操作 | 首次创建包、发布新版本；按页面实际可选权限设置 |

将策略绑定到实际拥有包的用户/组织。首次命名仍可能受名称保留和服务端权限约束，404 只能说明未查询到公开版本。策略生效与首次认证应单独核实，不能通过试传相同版本测试权限。

在 GitHub 创建 `nuget` Environment，按账号/仓库套餐可用能力设置指定维护者审批。工作流仅支持从 `main` 手动调度，不因 push、Tag 或 CI 成功自动发布；首次调度也必须在本次对话的发布确认后。

## 手动触发公开发布

1. 更新 `FMCore.csproj` 版本和中文 CHANGELOG、README、相关手册/示例；重新完成全部检查。
2. 确认公开发布，再推送该提交及对应 `v<版本>` Tag。不要移动或覆盖已发布 Tag。
3. GitHub Actions → **手动公开发布** → Run workflow，选择 `main`，版本填写 `0.1.0-preview.2`，确认文本填写 `publish FocusMinecraftCore 0.1.0-preview.2`。
4. `prepare` 从指定 Tag 构建、测试、打包、独立包验收并保存产物；`nuget` 经环境审批后，通过 `NuGet/login` 交换 OIDC 短期凭据。只推 `.nupkg`，相邻 `.snupkg` 由 NuGet 客户端一起上传。
5. NuGet 步骤成功后创建 GitHub Release，附加同一批包和中文更新日志。任一步失败都以 Actions 实际状态为准，不声称整体成功。
6. 公开后检查包页面、预览版搜索、指定版本安装/引用和 SourceLink 源文件读取；NuGet 收录/搜索索引可能延迟，应分别记录上传与索引状态。

若 NuGet 已上传成功而 GitHub Release 失败，只重试失败的 Release 作业，或手动补建缺失 Release；不得重新上传相同 NuGet 版本。版本已存在或可用性查询失败时，工作流停止，不使用 `--skip-duplicate` 隐藏失败。

账号密码、长期 API Key 和授权令牌不得进入项目、脚本参数示例、日志或对话。此工作流不需要保存长期 NuGet API Key；如未来变更认证方式，须重新审查权限范围及秘密存储。

## 版本策略和文档验收

T06 首个 NuGet 版本按本次包名调整改为 `0.1.0-preview.2`；P1：`0.5.0-preview.1`；P2：`0.9.0-beta.1`；全部功能、文档和兼容性验收后：`1.0.0`。每次功能或公开 API 变更同步更新 README、六模块相关手册、真实示例和 CHANGELOG。文档未同步不得标记为发布就绪。

来源：[NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)、[NuGet 符号包](https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg)、[SDK 内置 SourceLink](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/8.0/source-link)。
