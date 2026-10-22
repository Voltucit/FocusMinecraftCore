# OptiFine 高清修复查询、安装与启动

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Loaders;

var versions = await new OptiFineInstaller(".minecraft").GetVersionsAsync("1.16.5");
foreach (var v in versions) Console.WriteLine(v);
```


`FMCore.Loaders.OptiFineInstaller` 包含于 1.0.0。本阶段只处理独立 OptiFine profile，未实现 Forge/NeoForge 与 OptiFine 的混合安装或自动 Mod 管理。

## 最简调用

独立、可编译的调用代码见[本模块示例](examples.md#optifine)。

官网是 HTML 页面，SDK 读取官方下载目录并去重 Mirror 链接，保留官网顺序，区分正式/预览版本。省略版本仅自动选择非预览版。页面结构改变或没有可用官方链接时明确失败，不伪造列表、不切换第三方下载源。

## API 和高级配置

| API / 模型 | 含义 |
| --- | --- |
| `OptiFineInstaller(rootDirectory = ".minecraft")` | 默认官方网络客户端和共享游戏目录 |
| `OptiFineInstaller(downloads, versions = null)` | 使用调用方下载器与可选同根版本管理器 |
| `GetVersionsAsync(minecraftVersion, cachePolicy = Refresh, cancellationToken = default)` | `IReadOnlyList<OptiFineVersion>`：MinecraftVersion、Version（例如 HD_U_G8）、FileName、IsPreview、官方公共 DownloadPage |
| `InstallAsync(minecraftVersion, optiFineVersion = null, options = null, progress = null, cancellationToken = default)` | 从官网取得原始 JAR 后安装，返回统一 LoaderInstallationResult |
| `InstallLocalAsync(installerPath, options = null, progress = null, cancellationToken = default)` | 使用调用方自己从官网取得的未修改原始 JAR，不访问 OptiFine 官网；缺少原版文件仍会按元数据安装 |

下面的完整示例使用本地原始安装器，并显示进度、手动 Java 和结构化错误。`OPTIFINE_INSTALLER` 设置为官方 JAR 的完整路径，保留原文件名；`FMCORE_JAVA` 可指向兼容 Java 8，也可省略走有界自动选择。

独立、可编译的调用代码见[本模块示例](examples.md#optifine)。

下载页面可能包含短期有效链接，SDK 每次需要下载时重新取得，公共模型和异常不暴露这个链接；重复安装复用已完成且本地 SHA-256 匹配的原始 JAR。官网当前未提供可直接使用的安装器发布哈希，因此首次下载只依据 HTTPS、安装器结构及内部版本核对，不声称验证了作者签名或官方发布哈希。本地 SHA-256 用于后续检测缓存损坏和确认同一安装器。

## 必要安装步骤与安全边界

SDK 不修改或重新打包官方安装器。在专用工作目录先准备原版 profile/JAR，再通过自有 Java 8 桥接调用安装器公开的 `getOptiFineVersion()` 和 `doInstall(File)`。这会实际运行 OptiFine 的补丁器并生成依赖；不会仅拼接 JSON 宣称安装完成。小型桥接程序只有 FMCore 自有反射调用代码，源文件随核心源码保留，预编译字节码包含于 FMCore，运行时只需要兼容 JRE，不要求 javac。

桥接明确传入工作目录，不调用忽略目录参数的安装器 main，避免修改用户默认 .minecraft。读取安装器前检查 ZIP 条目路径、链接、重复项和规模；文件名与安装器内部版本必须一致。调用本地安装器会执行该 JAR 的 Java 代码，因此调用方应只提供自己信任的官方文件。缺少受支持 API 时返回 UnsupportedFormat，不绕过补丁步骤。

安装后检查退出码、预期 profile ID、继承、LaunchWrapper 主类、OptiFineTweaker 及相应坐标；所有必需依赖必须实际存在、非空且为有效 JAR。将这些产物的哈希写入统一 Versions/Installer 模型；生成文件只校验复用，缺失时重新执行 OptiFineInstaller.InstallAsync 或 InstallLocalAsync。普通原版文件继续由既有安装器修复。来源、依赖、实例配置或哈希不匹配时不覆盖不同 profile。

成功复用安装时 InstallerExitCode=null，表示本次没有再执行安装器；实际运行时记录真正退出码，0 也要通过产物检查。超时或取消终止进程树；脱敏日志保存在 `.cache/loaders/work/optifine/<安装器 SHA-256>/installer.filtered.log`。原始官网 JAR、处理后游戏文件及工作缓存均不进入 Git 或 NuGet。

已有账号、Java、隔离实例、分辨率、日志及 StopAsync 均复用 [启动 API](launch.md)。错误采用 [统一 LoaderException](forge.md)，另有官网门控 DownloadUnavailable、无效归档 InvalidArchive、内部版本不符 UnsupportedCombination 等明确结果。界面线程调度由宿主负责。

## 支持与验证记录

支持具有上述公开安装 API、生成继承 profile 和 LaunchWrapper/OptiFineTweaker 的独立安装格式；旧版或未来改变安装入口/产物结构时明确拒绝。能查询到官网文件不表示每个历史版本已适配或实机通过。

2026-10-21：**Windows x64 / Minecraft 1.16.5 / OptiFine HD U G8 / Java 1.8.0_482 / 离线身份**。经公开 API 从官网下载并执行原始安装器/补丁器，退出码 0；校验 2,680 个文件通过，音频/纹理及窗口初始化后由 SDK 停止，退出码 -1，StoppedByCaller=true。重复安装零下载、零 SDK HTTP 请求。

自动化测试另覆盖真实官网 HTML 格式、预览标记、重复下载链接、下载门控、无效归档、路径穿越、本地安装、缺产物、失败退出、超时/取消、生成文件修复和现有启动集成；进程为模拟安装器，不代表更多版本实机通过。未验收光影/材质包、世界、联机、真实 Mod、正版身份或 Linux/macOS。

使用和分发须遵守 [OptiFine 官方版权说明](https://optifine.net/copyright)；FMCore 不附带或公开分发 OptiFine JAR。版本来自[官方下载目录](https://optifine.net/downloads)。

## 独立调用示例

- [查询加载器版本](examples/optifine-versions.cs)
- [安装加载器](examples/optifine-install.cs)
- [安装高级配置（高级）](examples/optifine-advanced.cs)
- [使用合法本地安装器](examples/optifine-local.cs)
