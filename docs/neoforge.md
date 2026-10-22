# NeoForge 查询、安装与启动

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Loaders;

var versions = await new NeoForgeInstaller(".minecraft").GetVersionsAsync("1.20.4");
foreach (var v in versions) Console.WriteLine(v);
```


`FMCore.Loaders.NeoForgeInstaller` 包含于 1.0.0。复用 [Forge 手册](forge.md)中的 LoaderInstallOptions、LoaderInstallationProgress、LoaderInstallationResult、结构化错误、文件锁和安装器执行/校验流程，不另建启动引擎。

## 最简调用

独立、可编译的调用代码见[本模块示例](examples.md#neoforge)。

## API 与高级配置

| API | 参数及返回值 |
| --- | --- |
| `NeoForgeInstaller(rootDirectory = ".minecraft")` | 默认官方网络配置 |
| `NeoForgeInstaller(downloads, versions = null, mavenUri = null)` | 同根共享下载器/元数据，可自定义 HTTPS Maven 根地址 |
| `GetVersionsAsync(minecraftVersion, cachePolicy = Refresh, cancellationToken = default)` | `IReadOnlyList<MavenLoaderVersion>`，包含游戏版本、加载器版本、Maven 版本及预发布标记 |
| `InstallAsync(minecraftVersion, neoForgeVersion = null, options = null, progress = null, cancellationToken = default)` | `LoaderInstallationResult`；省略版本选官方 Maven 中最新非预发布版本 |

独立、可编译的调用代码见[本模块示例](examples.md#neoforge)。

示例实例首次创建后应通过 GetAsync 读取，避免重复创建同名实例。WPF/Avalonia/WinUI 自行调度进度到界面线程；SDK 不依赖 UI。正版身份使用同一 AccountManager，未重新实机验证正版 NeoForge。

## 安装与支持边界

支持 Minecraft **1.20.2–1.21.x** 对应的 `net.neoforged:neoforge` 数字 Maven 命名。旧 1.20.1 的 `net.neoforged:forge` 重命名格式、年号版本及未支持的安装规范明确返回 UnsupportedFormat，不推测对应关系。此范围表示协议处理能力，不代表其中每一组合均已实机通过。

NeoForge spec 1 可以没有 path 字段，自身坐标可能只存在于 install_profile 的 libraries。安装器同样验证请求的游戏、坐标、BootstrapLauncher 和实际产物。部分处理器没有预先发布的输出哈希：SDK 仍要求相关客户端输出实际存在、非空、JAR 可读取且路径安全，保存本地产物 SHA-1 供以后检查；不会把本地计算哈希称为作者签名。提供的预期哈希仍必须严格匹配。

Mojang mappings 由现有 Versions 中的 client_mappings 元数据和 Downloader 预取。官方处理器可能仍执行自己的网络检查；安装器标志和 SDK 请求统计不应视为第三方程序的操作系统网络沙箱。重复安装在全部清单有效时跳过进程，InstallerExitCode=null 表示本次没有新执行安装器。

额外支持文件统一保存在 VersionMetadata.GeneratedFiles，不自动加入 Classpath。缺失/损坏时重新调用 NeoForgeInstaller.InstallAsync；普通远程文件也可由 GameInstaller.RepairAsync 修复。工作目录、日志、超时/取消、冲突保护和异常见 [Forge](forge.md)。

BootstrapLauncher 的 ignoreList 会补上实际继承的原版 JAR 文件名，避免原版 JAR 与处理器生成的 Minecraft 模块重复；metadata 的 version_name 仍表示加载器 profile，其他参数保持原有含义。GameCoreUtil 能识别当前 SDK 安装的 NeoForge 及其实际加载器版本。

## 实际验证

2026-10-21：**Windows x64 / Minecraft 1.20.4 / NeoForge 20.4.253 / Java 17.0.9 / 离线身份**。真实官方安装器返回 0，3,936 个安装文件校验通过（含 43 个附加支持文件）。修复 BootstrapLauncher 重复模块后，音频/纹理和窗口初始化成功，通过 SDK 停止，退出码 -1，StoppedByCaller=true；重复安装零下载、零 SDK HTTP 请求。

另用真实格式 Fixtures 和模拟安装器子进程验证：没有 path 字段、没有预期产物哈希、缺产物失败、模块路径和继承启动。不把模拟通过视为更多组合实机通过。未验证真实 Mod、世界/多人游戏、正版身份及 Linux/macOS。

官方依据：[NeoForge 用户指南](https://docs.neoforged.net/user/docs/)、[官方安装器](https://github.com/neoforged/LegacyInstaller)、[官方 Maven](https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml)。

## 独立调用示例

- [查询加载器版本](examples/neoforge-versions.cs)
- [安装加载器](examples/neoforge-install.cs)
- [安装高级配置（高级）](examples/neoforge-advanced.cs)
