# P0 首次发布检查报告

日期：2026-10-21。项目与仓库：**FMCore / Voltucit/FMCore**。NuGet PackageId：**FocusMinecraftCore**，首个已发布 NuGet 版本：**0.1.0-preview.2**。作者 Voltucit，MIT 许可证。

## 发布状态与包名调整

用户已审阅本地检查报告并明确确认公开发布。仓库已公开创建，源码与 `v0.1.0-preview.1` Tag 已上传。GitHub Windows CI [39573114122](https://github.com/Voltucit/FMCore/actions/runs/39573114122) 的构建、402 项测试、打包、独立包调用和 14 个示例编译全部通过。

原包名 `FMCore` 的[首次发布工作流](https://github.com/Voltucit/FMCore/actions/runs/39573656696)完成构建验收和 OIDC 认证后，NuGet 上传被服务端拒绝：HTTP 400，包名与已存在的 `fm.Core` 过于相似。未产生 FMCore NuGet 版本，后续 GitHub Release 作业跳过。此前 Flat Container 的 404 只能证明未查询到公开版本，不能证明名称可注册。

用户随后指定新包名 **FocusMinecraftCore**。使用 `0.1.0-preview.2` 重新准备，保留已有 Tag，不覆盖远程历史；程序集 `FMCore.dll`、命名空间 `FMCore.*`、GitHub 仓库及公开业务 API 均保持原样。用户已更新 NuGet Trusted Publishing 包模式。

**最终状态：发布及公开源验收完成。** 新版[发布工作流 39575984963](https://github.com/Voltucit/FMCore/actions/runs/39575984963)的 prepare、nuget、github-release 全部成功；核心包和符号包均由 NuGet 接收，GitHub Release 已公开。NuGet 官方版本索引、包页面和两个搜索服务均已收录，独立调用方已仅从 NuGet.org 恢复并运行公开 API。

发布源码绑定 **`v0.1.0-preview.2` / `475721a77a11ba1906dd4937c97db411061310ff`**，保留用户在 GitHub 修改的 README 标题。后续最终报告提交只更新文档，不移动发行 Tag，不重传相同 NuGet 版本。

## 本地与 SDK 验收范围

| 检查 | 内容与证据 |
| --- | --- |
| Release 构建 | .NET SDK 10.0.402，C# 14 / net10.0；0 警告、0 错误；单核心与原三项目结构 |
| 自动化测试 | 新版在本地与远程均 402 通过、0 失败、0 跳过；本次更名未改业务 C# 源码 |
| API 兼容性 | 与 T06 程序集的 1,643 条导出类型/成员/参数记录一致，无公开业务 API 变更 |
| 独立包调用 | 仅 PackageReference，隔离缓存；六模块公开 API、哈希、本地版本、严格 Java 选择、模拟安装/零下载修复、离线账号及启动失败/取消 |
| 文档示例 | README、SDK 和六模块手册共 14 个 C# 块分别以包引用编译，不执行授权或游戏示例 |
| 包与文档 | FocusMinecraftCore 的 nupkg/snupkg；程序集仍为 FMCore.dll，XML 文档为 FMCore.xml，196 个成员记录，存量模型说明未全覆盖 |
| SourceLink | Portable PDB，51 个规范化文档路径；新版 47 个 GitHub 源文件均已实际下载并核对 PDB 校验和，4 个生成源文件内嵌 |
| 依赖 | 唯一运行时依赖 System.Security.Cryptography.ProtectedData 10.0.10，MIT；三个项目及传递依赖的 NuGet 漏洞查询未报告已知漏洞 |
| 工作流 | 自动 CI 构建/测试/打包/包引用/示例；仅 main 手动触发公开发布，经 nuget Environment 维护者审批和 OIDC 认证 |

独立包验收中的网络和安装数据是模拟素材，不是真实 Microsoft 登录或游戏启动。历史真实验收见 [T06 脱敏记录](t06-p0.json)：Windows x64、Minecraft 1.16.5、Java 1.8.0_482，正版与离线身份均启动至窗口/音频/纹理初始化后停止。没有验收世界、多人服务器、其他游戏版本或 Linux/macOS 实机。

## 安全、内容与许可证

- 核心包仅 DLL、XML、README、LICENSE、NOTICE、CHANGELOG 与 NuGet 必需元数据；符号包为 Portable PDB 和符号元数据。测试、CLI 和游戏安装文件不作为核心包发布。
- 源码与包未发现 Minecraft 客户端、Libraries、音效、纹理、Java、存档或真实账号文件。官方 JSON 测试元数据保留来源，不进入核心包；游戏内容的原始权利和使用要求见 [NOTICE](../../NOTICE.md)。
- 高置信度凭据格式检查未发现 GitHub/NuGet Token、真实 JWT 或私钥。Fixtures 的认证凭据均有虚构说明；不将格式扫描当作不存在所有形式秘密的保证。
- 本机真实授权数据位于项目外，不读取或分发；artifacts、游戏目录与账号文件受忽略规则保护，发行附件仅选择两个包和中文更新日志。
- README 围绕 FMCore，安装命令使用 FocusMinecraftCore，代码使用 FMCore.*。中文 README、手册、示例、CHANGELOG 与包名/版本同步；未添加业务 API 或运行时依赖。
- 使用 GitHub 隐藏邮箱提交；本机 artifacts 下保存包/源码/依赖检查、CI 日志与发布记录，不把临时日志、凭据或安装验证文件上传到源码仓库。

## 公开源及发行验收

- [GitHub 仓库](https://github.com/Voltucit/FMCore)已创建。
- 原 `v0.1.0-preview.1` 为失败的 NuGet 候选 Tag，未覆盖或重复上传。
- [GitHub Release](https://github.com/Voltucit/FMCore/releases/tag/v0.1.0-preview.2)：公开预览版，非草稿；已附加 nupkg、snupkg、中文 CHANGELOG。三个附件已实际下载，SHA-256 与已审核的工作流产物一致。
- [NuGet 包](https://www.nuget.org/packages/FocusMinecraftCore/0.1.0-preview.2)：页面 HTTP 200；Flat Container 已包含该版本，两个官方搜索端点均返回精确 ID 与 `0.1.0-preview.2`（启用预览版搜索）。
- 独立项目固定引用 `FocusMinecraftCore [0.1.0-preview.2]`，仅配置 NuGet.org，使用隔离包缓存还原并实际运行六模块公开 API 验收通过，没有 ProjectReference 或内部类型。
- 已验证官方源包的 NuGet.org Repository 签名有效；实际安装的 FMCore.dll 与审核的发布 DLL SHA-256 一致。NuGet.org 的仓库签名会改变外层 nupkg，不将其外层哈希与未签名的 GitHub 附件混为一谈。
- OIDC 认证与核心包/符号上传均成功。符号包的内容及 SourceLink 已验证；未单独验收 IDE 从 NuGet 符号服务器自动加载 PDB 的交互流程。

GitHub 发行附件摘要：

| 文件 | SHA-256 |
| --- | --- |
| FocusMinecraftCore.0.1.0-preview.2.nupkg | f2cfcc0a12022c05a65d148614ab14e828e265cb2005302357e07c5f0a775d66 |
| FocusMinecraftCore.0.1.0-preview.2.snupkg | 60e4f7881f53a96273a2f6342a2e1849c362104fd2d07d1030223e7434a3a133 |

本机 `artifacts/release-validation` 保存发布日志、公开源验收与附件摘要记录。此次包引用和安装素材验证使用 Mock；没有将其视为新的真实正版登录或游戏启动。

不试传验证权限、不重复上传已存在版本、不强推或覆盖 Tag。若 NuGet 成功而 GitHub Release 失败，只恢复失败部分。后续步骤见[发布手册](../releasing.md)。
