# 0.1.0-preview.1 首次发布检查报告

检查日期：2026-10-21。目标仓库 `Voltucit/FMCore`；NuGet 用户 `Voltucit`；MIT，署名 Voltucit。

**状态：本地发布检查通过，等待用户确认公开发布；远程配置与 NuGet 权限验收尚未完成。** 尚未创建远程仓库、推送 Tag、创建 Release 或上传 NuGet，不将本地验收表述为发布成功。

## 本地验收

| 项目 | 实际结果 |
| --- | --- |
| Release 构建 | .NET SDK 10.0.402；0 警告、0 错误 |
| 自动化测试 | **402 通过，0 失败，0 跳过** |
| NuGet 打包 | `FMCore.0.1.0-preview.1.nupkg` 与 `.snupkg`，SDK 包验证通过 |
| 独立包引用 | 临时 .NET 10 调用方，仅 PackageReference，隔离包缓存；六模块公开 API 验证通过 |
| 文档示例 | README、SDK 与六模块手册共 **14 个 C# 代码块**，提取后分别以包引用编译通过；未执行授权/游戏示例 |
| API 兼容性 | 对照 T06 程序集的 **1,643 条导出类型/成员/参数记录**一致；未修改业务 C# 源码 |
| XML | 包含 `FMCore.xml`，196 个成员记录；存量模型文档尚未全覆盖 |
| SourceLink | Portable PDB 内含真实本地候选提交映射，51 个文档路径已规范化；远程源码读取待推送后验证 |
| 依赖 | 一个运行时依赖 `System.Security.Cryptography.ProtectedData 10.0.10`，MIT；测试包不作为核心依赖 |
| 漏洞查询 | NuGet.org 数据源扫描全部三个项目及传递依赖，未报告已知漏洞；结果仅反映检查时的数据库 |
| 工作流 | YAML 与发布条件本地检查；自动 CI 构建/测试/打包/包引用/示例，公开发布只允许 main 手动确认触发；尚未远程运行 |

独立包调用使用模拟 HTTP 下载/安装素材，验证单文件/批量哈希、本地版本、严格 Java 主版本选择、有界检测、安装检查、零请求修复、离线账号管理及启动错误/取消。它未运行真实 Java 或游戏，也未完成真实 Microsoft 登录。T06 真实环境记录另见[原始脱敏报告](t06-p0.json)：Windows x64、Minecraft 1.16.5、Java 1.8.0_482，正版与离线身份启动至窗口/音频/纹理初始化后停止。此次发布准备没有将历史真实验证表述为新的 NuGet 实机验证。

## 文件、安全与文档

- 核心包采用明确内容清单：DLL、XML、README、LICENSE、NOTICE、CHANGELOG 及 NuGet 必需元数据，共 10 个 ZIP 项。符号包仅含 PDB 及符号元数据。
- 源码及包未发现 Minecraft JAR、Libraries、音效、纹理、Java、存档或真实账号文件。官方 JSON 测试元数据与模拟素材保留来源说明，不作为核心包内容发布。相关权利与下载使用说明见 [NOTICE](../../NOTICE.md)。
- 高置信度凭据格式扫描未发现 GitHub/NuGet Token、真实 JWT 或私钥；账号模拟凭据已按 Fixtures 说明核对。该检查是本次源码审查结果，不能保证不存在所有形式的秘密。
- 本机真实授权数据位于项目外，不读取或分发；游戏安装验证目录 `artifacts/` 受忽略规则保护。发行附件只允许两个包和中文更新日志。
- README 仅围绕 FMCore，未发现其他启动核心品牌、遗留安装命令或仓库信息。README、六模块手册、SDK、发布手册和 CHANGELOG 使用简体中文；代码标识符为英文。
- 本地候选提交使用 GitHub 隐藏邮箱；公开上传前再次核对候选提交、文件清单、包 SHA-256 与 README/手册一致性。本机 `artifacts/package-audit.json` 记录实际提交、包哈希和元数据检查，不把尚未公开的地址标为发布成功。

## 账号、名称与待完成事项

| 项目 | 已知状态 | 仍须完成 |
| --- | --- | --- |
| GitHub | 连接器与本机 Git 身份均为 `Voltucit`；本机 OAuth 包含 repo/workflow；目标仓库尚未创建 | 用户确认后新建公开 `Voltucit/FMCore`、推送候选提交及 `v0.1.0-preview.1`；验证远程 CI |
| NuGet | 用户已提供注册用户名 `Voltucit`，公开个人页面 HTTP 200 | 账号邮箱确认由用户完成；配置 OIDC Trusted Publishing 及首次创建包的权限，确认策略生效 |
| PackageId | `FMCore` 公共页面与 Flat Container 索引均为 HTTP 404（检查时） | 404 不证明名称未保留或用户有发布权；首次服务端权限仍待认证验证，公开上传前重新查询 |
| 公开发布 | 尚未执行 | 用户明确确认；配置 GitHub `nuget` Environment 与 NuGet `release.yml` 策略；上传、Tag、Release、符号收录 |
| 发布后验收 | 尚未执行 | 官方源指定版本恢复/引用、预览版搜索、包/Release 附件和 SourceLink 远程源码读取 |

预期地址（尚未创建或上传）：`https://github.com/Voltucit/FMCore`、`https://github.com/Voltucit/FMCore/releases/tag/v0.1.0-preview.1`、`https://www.nuget.org/packages/FMCore/0.1.0-preview.1`。

不使用试传来验证权限，不重复上传同一版本，不强推、不覆盖 Tag、不改变已有仓库可见性。若 NuGet 已成功而 Release 失败，只恢复失败部分；每一步如实记录，不把部分成功表述为整体成功。具体配置与后续版本步骤见[发布手册](../releasing.md)。
