# 原版安装 · FMCore.Installer

填写版本号和 Minecraft 根目录，调用 InstallAsync。SDK 读取官方版本清单，下载并校验版本 JSON，再完成客户端、依赖、适用的旧式原生库归档、资源索引、资源对象和日志配置。

## 最简安装

```csharp
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft");
MinecraftInstallResult result = await installer.InstallAsync();
Console.WriteLine(result.VersionDirectory);
Console.WriteLine($"下载：{result.Files.DownloadedFiles}");
Console.WriteLine($"复用：{result.Files.ReusedFiles}");
```

Java 路径在启动游戏时提供，安装阶段不需要。数据写入调用方指定的根目录：versions/<版本号> 保存版本文件，assets 与 libraries 共享。核心不自动添加随机测试根目录。

## 查询在线版本

```csharp
using FMCore.Installer;

IReadOnlyList<MinecraftInstallVersion> versions =
    await MinecraftInstaller.GetVersionsAsync();

foreach (var version in versions.Where(item => item.Type == "release"))
    Console.WriteLine($"{version.Id} {version.ReleaseTime:yyyy-MM-dd}");
```

结果包含 Id、Type、ReleaseTime，按发布时间降序排列。类型来自官方清单，当前包括 release、snapshot、old_beta 和 old_alpha；调用方按需筛选。查询不需要游戏目录，不创建文件，也不表示清单中每个历史版本都已通过安装兼容性验收。

## 构造方法与主方法

```text
public MinecraftInstaller(
    string versionId,
    string minecraftRoot,
    int maxConcurrency = 4,
    System.Runtime.InteropServices.Architecture? targetArchitecture = null)

public DownloadSource Source { get; init; } = DownloadSource.Official;

public Task<MinecraftInstallResult> InstallAsync(
    Action<MinecraftInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)

public static Task<IReadOnlyList<MinecraftInstallVersion>> GetVersionsAsync(
    CancellationToken cancellationToken = default)
```

| 参数 | 含义 |
| --- | --- |
| versionId | 官方清单中的版本 ID，必须是安全的单个目录名称 |
| minecraftRoot | 调用方选择的 Minecraft 根目录 |
| maxConcurrency | 文件批次的最大并发数，默认 4，须大于 0 |
| targetArchitecture | 默认当前操作系统架构；Windows 支持 x86、x64、arm64，具体文件取决于版本声明 |
| onProgress | 安装阶段及可选的当前下载批次；回调不保证 UI 线程 |
| cancellationToken | 取消当前操作，已完成文件保留 |

同一安装器对象同时只允许一次安装，结束后可以再次调用。构造规范化路径，不执行安装；InstallAsync 在后台编排。相对路径按构造时的工作目录确定。

## 下载源

```csharp
using FMCore.Downloads;
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft")
{
    Source = DownloadSource.BmclApi
};

MinecraftInstallResult result = await installer.InstallAsync();
Console.WriteLine(result.VersionDirectory);
```

默认 Official。BmclApi 用于已识别的客户端、Mojang/Fabric Maven 依赖及资源对象地址；版本清单、版本 JSON、资源索引、日志配置和 Maven 元数据查询保留原来源。未知地址不转换。

镜像发生 HTTP 或网络错误时，在剩余重试预算内切回原地址；总超时不重置，校验标准不变，不关闭 TLS 校验。校验、磁盘、回调、清理错误和取消不会被当作镜像故障吞掉。完整边界见 [下载手册](downloads.md#安装与补全的下载源)。

使用 BMCLAPI 的启动器需在下载界面等可见位置标明来源，例如“下载源：BMCLAPI（bangbang93）”。[BMCLAPI 使用说明](https://bmclapidoc.bangbang93.com/)

## 进度、结果与取消

```csharp
using FMCore.Installer;

using CancellationTokenSource canceled = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    canceled.Cancel();
};

var installer = new MinecraftInstaller("1.21.10", ".minecraft");
MinecraftInstallResult result = await installer.InstallAsync(
    progress =>
    {
        Console.WriteLine($"[{progress.Stage}] {progress.Message}");
        if (progress.Download is { } download)
            Console.WriteLine($"文件 {download.CompletedFiles}/{download.TotalFiles}，字节 {download.Percentage:F1}%");
    },
    canceled.Token);

Console.WriteLine($"已安装：{result.VersionId}");
Console.WriteLine($"版本目录：{result.VersionDirectory}");
```

阶段依次为 ReadingManifest、DownloadingVersionJson、CompletingFiles、Completed。Download 只代表当前批次，文件数量进度与字节百分比可以不同；速度为最近约 5 秒的正文写入速度。任务成功返回后才表示安装范围内的文件已完成校验。

MinecraftInstallResult 返回 VersionId、MinecraftRoot、VersionDirectory 和 Files。Files 是 GameFileCompletionResult，包含各类文件数量、下载与复用数量及本次备份修复路径，详见 [文件补全](game-files.md)。

## 异常与限制

元数据 HTTP 单次超时 30 秒，响应上限 8 MiB；下载使用各请求的超时与有限重试。参数、元数据、网络、文件系统、回调、取消和超时错误按实际原因传播。失败保留已完成文件，不回滚整个安装目录。

已有版本 JSON 内容不匹配时保留并报错；客户端、依赖与资源按补全策略备份修复。此入口安装官方原版，不安装 Java、加载器或 Mod，不启动游戏。旧式 virtual/map_to_resources 资源布局仍不支持。实际启动通过 MinecraftLauncher，Java、账号和内存仍由调用方选择。

用户已完成原版 1.21.10 的官方源安装和真实启动，以及 BMCLAPI 安装和真实启动；在线正式版与快照列表查询也已验收。其他版本、架构、节点及故障分支不能据此当作全部实测。

Java 运行时可通过独立的 [JavaInstaller](java-installation.md) 安装。它返回的 Java.Executable 可直接填入启动配置，原版安装器仍不要求 Java 路径。
