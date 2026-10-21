# P0 首次发布检查报告

日期：2026-10-21。项目与仓库：**FMCore / Voltucit/FMCore**。当前 NuGet PackageId：**FocusMinecraftCore**，首个 NuGet 目标版本：**0.1.0-preview.2**。作者 Voltucit，MIT 许可证。

## 发布状态与包名调整

用户已审阅本地检查报告并明确确认公开发布。仓库已公开创建，源码与 `v0.1.0-preview.1` Tag 已上传。GitHub Windows CI [39573114122](https://github.com/Voltucit/FMCore/actions/runs/39573114122) 的构建、402 项测试、打包、独立包调用和 14 个示例编译全部通过。

原包名 `FMCore` 的[首次发布工作流](https://github.com/Voltucit/FMCore/actions/runs/39573656696)完成构建验收和 OIDC 认证后，NuGet 上传被服务端拒绝：HTTP 400，包名与已存在的 `fm.Core` 过于相似。未产生 FMCore NuGet 版本，后续 GitHub Release 作业跳过。此前 Flat Container 的 404 只能证明未查询到公开版本，不能证明名称可注册。

用户随后指定新包名 **FocusMinecraftCore**。使用 `0.1.0-preview.2` 重新准备，保留已有 Tag，不覆盖远程历史；程序集 `FMCore.dll`、命名空间 `FMCore.*`、GitHub 仓库及公开业务 API 均保持原样。用户已更新 NuGet Trusted Publishing 包模式。

**当前阶段：新包名版本的本地/远程复验与发布进行中。** 未将前一轮成功或配置完成表述为新包已发布。最终 NuGet 上传、索引、官方源引用与 GitHub Release 以本报告后续验收结果及官方页面为准。

## 本地与 SDK 验收范围

| 检查 | 内容与证据 |
| --- | --- |
| Release 构建 | .NET SDK 10.0.402，C# 14 / net10.0；单核心与原三项目结构 |
| 测试基线 | 本地与远程均 402 通过、0 失败、0 跳过；本次更名未改业务 C# 源码 |
| API 兼容性 | 与 T06 程序集的 1,643 条导出类型/成员/参数记录对照，检查更名后仍一致 |
| 独立包调用 | 仅 PackageReference，隔离缓存；六模块公开 API、哈希、本地版本、严格 Java 选择、模拟安装/零下载修复、离线账号及启动失败/取消 |
| 文档示例 | README、SDK 和六模块手册共 14 个 C# 块分别以包引用编译，不执行授权或游戏示例 |
| 包与文档 | FocusMinecraftCore 的 nupkg/snupkg；程序集仍为 FMCore.dll，XML 文档为 FMCore.xml，196 个成员记录，存量模型说明未全覆盖 |
| SourceLink | Portable PDB，51 个规范化文档路径；47 个已提交源文件与校验和、4 个内嵌生成源文件。原候选的 47 个 GitHub 源文件均已实际下载核验；新提交发布时复验 |
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

## 公开地址及后续验收

- [GitHub 仓库](https://github.com/Voltucit/FMCore)已创建。
- 原 `v0.1.0-preview.1` 为失败的 NuGet 候选 Tag，未覆盖或重复上传。
- 新版本预期 Release：`https://github.com/Voltucit/FMCore/releases/tag/v0.1.0-preview.2`。
- 新版本预期 NuGet：`https://www.nuget.org/packages/FocusMinecraftCore/0.1.0-preview.2`。
- 新版上传成功后，验证官方源指定版本恢复与公开 API 调用、预览版搜索、符号上传、Release 附件和 SourceLink 远程读取，并更新实际结果。

不试传验证权限、不重复上传已存在版本、不强推或覆盖 Tag。若 NuGet 成功而 GitHub Release 失败，只恢复失败部分。后续步骤见[发布手册](../releasing.md)。
