# Forge 查询、安装与启动

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Loaders;

var versions = await new ForgeInstaller(".minecraft").GetVersionsAsync("1.16.5");
foreach (var v in versions) Console.WriteLine(v);
```


`FMCore.Loaders.ForgeInstaller` 包含于 1.0.0。调用方只需引用 FMCore；不依赖界面、DI 或额外业务项目。Quilt 已取消适配。

## 最简示例

独立、可编译的调用代码见[本模块示例](examples.md#forge)。

## API 和高级配置

| API | 参数与返回值 |
| --- | --- |
| `ForgeInstaller(rootDirectory = ".minecraft")` | 默认网络和共享游戏目录 |
| `ForgeInstaller(downloads, versions = null, mavenUri = null)` | 复用网络、同根 Versions 和可选 HTTPS Maven 镜像，不接收带凭据/查询串的服务根 URL |
| `GetVersionsAsync(minecraftVersion, cachePolicy = Refresh, cancellationToken = default)` | `IReadOnlyList<MavenLoaderVersion>`；包含 MinecraftVersion、Version、MavenVersion、IsPrerelease，数字版本从新到旧 |
| `InstallAsync(minecraftVersion, forgeVersion = null, options = null, progress = null, cancellationToken = default)` | `LoaderInstallationResult`；Loader、MinecraftVersion、LoaderVersion、VersionId、原 InstallationResult、InstallerExitCode；省略版本选最新稳定 Maven 发布，不表示 Forge 官方推荐版 |
| `LoaderInstallOptions` | VersionId、Installation、JavaPath、JavaDetection、InstallerTimeout（默认 10 分钟，上限 1 小时） |
| `LoaderInstallationProgress` | Stage、CompletedFiles、TotalFiles、Installation、Download、Message；提供原安装/字节进度，处理器阶段提供状态，不泄漏原始输出 |

独立、可编译的调用代码见[本模块示例](examples.md#forge)。

GameCoreConfig.VersionId 或 InstanceConfig.VersionId 直接使用安装结果。JavaPath 指安装器 Java，会实际探测并严格匹配原版元数据；游戏启动时可将相同路径设置到 JavaConfig。手动路径失败不会偷偷改用更高 Java。账号、实例隔离、窗口、日志和停止功能均复用既有 Launch。正版账号接口与原版相同，本阶段没有重新验证正版 Forge。

## 安装、校验与修复

1. 使用官方 Maven 版本列表；获取并校验安装器 `.sha1`，不修改安装器 JAR。
2. 检查归档路径、重复项、链接、条目/体积边界，以及 install_profile.json 的 spec 0/1、version.json 和请求组合。
3. 复用 Downloader 校验预取安装器/游戏依赖，复用 GameInstaller 准备原版。安装器在 SDK 专用缓存工作目录中通过官方 `--installClient`、`--offline` 和显式镜像执行，避免依赖失效的随机镜像列表；缺必要输入时明确失败，不跳过处理器。
4. 检查真实退出码、实际生成的 profile、所有声明的处理器输出 SHA-1 和内嵌文件，再导入受校验产物，最后通过原版流水线完成安装并发布 profile。取消/超时会停止安装器进程树。

生成依赖的空 URL 在统一版本模型中表示 `DownloadArtifact.IsLocalOnly=true`，Url 为 `about:blank`；Downloader.LocalOnly 仅验证和复用哈希匹配的本地文件，绝不访问该地址。其他必要生成文件通过 `VersionMetadata.GeneratedFiles`（JSON 扩展 fmcoreGeneratedFiles）进入同一安装/校验清单，不自动加入 Classpath，也不再维护另一套校验规则。

损坏或缺失普通远程文件可以用 GameInstaller.RepairAsync 修复。生成文件缺失时返回 DownloadErrorCode.LocalFileUnavailable，应重新调用同一 ForgeInstaller.InstallAsync 运行官方安装器恢复。重复安装若安装器来源、配置和全部文件有效，会复用校验结果，不重复执行处理器，此时 InstallerExitCode=null；工作缓存保留便于中断重试。安装日志位于 `.cache/loaders/work/forge/<组合哈希>/installer.filtered.log`，对 SDK 保存的日志先脱敏；官方安装器自己的原始日志留在该专用目录，未向其传入账号或令牌。

已有不同 profile 不覆盖；当前 SDK 所有的相同安装器配置可以重复安装和修复。同一根目录的这些安装器使用文件锁协调，实际游戏文件准备沿用 GameInstaller 的安装锁。外部程序仍须自行避免并发修改共享文件。安装器进程属于第三方 Java 程序，不是操作系统安全沙箱；只自动执行受校验的官方发布物。

## 错误与支持边界

LoaderException.Code 包括 InvalidMetadata、UnsupportedFormat、UnsupportedCombination、ProfileConflict、NotCached、InstallationBusy、UnsafePath、IoError、InvalidArchive、ProcessFailed、ProcessTimeout、MissingOutput、HashMismatch、DownloadUnavailable。ExitCode 是实际退出码，可为 0（例如安装器表面成功但未生成产物）。不会从“退出码 0”直接推断安装完整。

底层下载、Java、版本和安装错误保留原有结构化类型；取消抛 OperationCanceledException。CacheOnly 仅限制元数据获取，缺少安装文件时仍可能下载；完全离线启动需预先安装后设置 AutoInstall=false。

支持具有现代 version.json 和客户端处理器的 spec 0/1 安装器、ModLauncher/BootstrapLauncher 启动。旧 versionInfo/universal-copy/LaunchWrapper 等格式明确 UnsupportedFormat；不以能查询到版本代表所有历史版本均可安装。真实支持组合与未实机验证的格式分开记录。

## 实际验证

2026-10-21：Windows x64 / **Minecraft 1.16.5 / Forge 36.2.42 / Java 1.8.0_482 / 离线身份**。真实官方处理器运行，安装器返回 0；校验 **2,747** 个安装文件（含 46 个附加支持文件）通过，游戏音频/纹理和窗口初始化后通过 SDK 停止，StoppedByCaller=true，退出码 -1。重复安装零下载、零 SDK HTTP 请求。

测试另覆盖模拟安装器子进程的失败、超时、取消、缺产物、哈希错误、路径/归档边界、生成文件修复和启动集成。未验证真实 Mod、世界、联机、正版 Forge 或其他平台；其他游戏/Forge 组合未实机验收。

协议来源：[官方安装器](https://github.com/MinecraftForge/Installer)、[官方客户端安装动作](https://github.com/MinecraftForge/Installer/blob/2.0/src/main/java/net/minecraftforge/installer/actions/ClientInstall.java)、[官方 Maven](https://maven.minecraftforge.net/net/minecraftforge/forge/maven-metadata.xml)。

## 独立调用示例

- [查询加载器版本](examples/forge-versions.cs)
- [安装加载器](examples/forge-install.cs)
- [安装高级配置（高级）](examples/forge-advanced.cs)
