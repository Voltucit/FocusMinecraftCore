# 游戏文件补全 · FMCore.Installer

GameFileCompleter 检查已有实例声明的客户端 JAR、普通 artifact 依赖、资源索引、资源对象与日志配置。填写版本与 Minecraft 根目录，再调用 CompleteAsync。SDK 内部按目标系统架构完成清单读取、Maven 信息补齐、校验、下载和备份修复。

当前已由用户在 1.21.10 Fabric 配合 Java 25 上验收完整校验与再次复用：4476 个文件均通过校验，两次均未下载或修复文件。这证明该样本的校验复用编排正常，不代替缺失 JAR、损坏 JAR、取消和故障路径的独立验收。

## 最简用法

引用核心项目后的完整控制台入口：

```csharp
using FMCore.Installer;

var completer = new GameFileCompleter(
    "1.21.10",
    ".minecraft");

GameFileCompletionResult result =
    await completer.CompleteAsync();

Console.WriteLine($"已校验：{result.TotalFiles}");
Console.WriteLine($"下载：{result.DownloadedFiles}");
Console.WriteLine($"复用：{result.ReusedFiles}");
Console.WriteLine($"备份修复：{result.RepairedFiles}");
```

示例中的版本必须已存在于所选根目录。根目录与可选目标架构由启动器选择；此入口不会安装版本 JSON、Java、加载器或 Mod，也不会启动游戏。只需要资源文件时，可使用 [GameResourceCompleter](resources.md)。

## 构造方法

```text
public GameFileCompleter(
    string versionId,
    string minecraftRoot,
    int maxConcurrency = 4,
    bool replaceInvalidFiles = true,
    System.Runtime.InteropServices.Architecture? targetArchitecture = null)
```

| 参数 | 含义 |
| --- | --- |
| versionId | 已有实例的版本 ID，支持同根继承 |
| minecraftRoot | Minecraft 根目录，公共文件位于其 libraries 和 assets 下 |
| targetArchitecture | 可选的 Windows 目标架构；默认当前操作系统架构，支持 x86、x64 和 arm64 |
| maxConcurrency | 正整数，默认 4；各文件批次依次执行，共用该上限 |
| replaceInvalidFiles | 默认 true；不匹配文件下载并校验后保留备份再替换，false 时保留并报错 |

构造时检查参数并确定实际路径，不联网、不执行 Java 或读取版本 JSON。相对路径按构造时的工作目录确定。

## CompleteAsync

```text
public Task<GameFileCompletionResult> CompleteAsync(
    Action<GameFileCompletionProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

方法按目标架构读取并补齐客户端与依赖下载声明，确认声明齐全后执行文件批次，再复用 GameResourceCompleter 完成索引、对象与日志配置。已有文件必须通过大小和 SHA-1 校验才能复用；文件存在不等于内容匹配。

每个对象同时只允许一次操作，结束后可再次调用。方法在后台执行；回调不保证界面线程，界面自行调度更新。回调应及时返回，错误继续传播。文件完成后若最终回调抛错，方法仍会失败。

Maven 缺失字段查询目前不持久化，重复补全仍可能联网读取 .sha1 与 HEAD 信息；这不代表重新下载 JAR。每个待查询依赖共用 30 秒超时，文件下载按各请求的超时与重试规则执行。调用方令牌控制整个补全过程。

## 进度与结果

```text
public enum GameFileCompletionStage
{
    SelectingArchitecture,
    ReadingMetadata,
    CompletingClientAndLibraries,
    CompletingResources,
    Completed
}

public sealed record GameFileCompletionProgress(
    GameFileCompletionStage Stage,
    string Message,
    DownloadBatchProgress? Download = null)

public sealed record GameFileBackup(
    string FilePath,
    string BackupPath)

public sealed record GameFileCompletionResult(
    int ClientFiles,
    int LibraryFiles,
    int ResourceMetadataFiles,
    int AssetFiles,
    int DownloadedFiles,
    int ReusedFiles)
{
    public int ClientAndLibraryFiles { get; }
    public int TotalFiles { get; }
    public IReadOnlyList<GameFileBackup> Backups { get; init; }
    public int RepairedFiles { get; }
}
```

| 成员 | 含义 |
| --- | --- |
| Stage / Message | 当前步骤与说明 |
| Download | 可选的当前文件批次进度 |
| ClientFiles | 客户端文件数，当前为 1 |
| LibraryFiles | 按 Windows 默认功能规则及目标架构选中的依赖和旧式原生库归档数 |
| ResourceMetadataFiles | 资源索引与可选日志配置文件数 |
| AssetFiles | 按哈希去重的资源对象数 |
| ClientAndLibraryFiles | ClientFiles + LibraryFiles |
| TotalFiles | 客户端、依赖、资源元数据与对象数量之和 |
| DownloadedFiles | 本次下载并落盘的文件数，包含成功修复文件 |
| ReusedFiles | 已存在且通过校验的复用文件数 |
| Backups | 本次成功替换的旧文件备份路径 |
| RepairedFiles | Backups.Count，是 DownloadedFiles 的一部分 |

Download.Percentage 按当前批次字节计算；它与已完成文件数的比例可以不同。客户端与依赖、资源元数据、对象分别计数。完全复用时网络传输量与速度为 0，不能将该速度理解为磁盘校验速度。百分比达到 100% 后仍须等待校验与落盘结束。

## 进度与取消示例

```csharp
using FMCore.Installer;

using CancellationTokenSource canceled = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    canceled.Cancel();
};

var completer = new GameFileCompleter(
    "1.21.10",
    ".minecraft");

GameFileCompletionResult result = await completer.CompleteAsync(
    progress =>
    {
        Console.WriteLine($"[{progress.Stage}] {progress.Message}");

        if (progress.Download is { } download)
            Console.WriteLine($"{download.Percentage:F1}%");
    },
    canceled.Token);

foreach (GameFileBackup backup in result.Backups)
    Console.WriteLine($"{backup.FilePath} -> {backup.BackupPath}");
```

## 失败处理与限制

不匹配文件在新内容下载与校验期间保留。成功替换时生成同目录唯一备份，SDK 不自动删除。文件系统替换失败可能已把旧文件移到备份，错误包含备份候选路径，不能保证旧文件仍在原位置。具体规则见 [文件下载](downloads.md#文件处理规则)。

失败或取消会等待当前下载批次收尾；已成功文件、备份与已创建目录保留，不执行整个实例回滚。方法失败时不返回成功结果，因此不能用之前的某个进度事件判断整个操作成功。

当前支持已有版本 JSON 的客户端、普通 artifact、Maven 声明和旧式 Windows natives 分类归档，包括新版 artifact 形式的原生库。旧式原生库清单已用原版 1.8.9 验收：客户端 1、依赖及原生库 35，共 36 项。该结果证明声明齐全，不代替旧版完整补全的单独实测。此入口不安装版本 JSON、Java、加载器或 Mod，不启动游戏；原版安装使用 [MinecraftInstaller](installation.md)。旧式 virtual/map_to_resources 资源布局仍不支持。

参数或路径错误、缺失版本、无效 JSON、缺失或不支持的下载声明、HTTP、权限与磁盘错误会传播。取消通常为 OperationCanceledException，操作超时为 TimeoutException，批次或清理失败可能为 AggregateException。部分同步解析、文件系统操作和同步回调不能立即被取消。

## 验收范围

用户通过 complete-game-files 在同一个 1.21.10 Fabric 实例连续执行两次，结果均为：

- 客户端 1、依赖 76、资源元数据 2、资源对象 4397，共 4476 个文件。
- DownloadedFiles 为 0、ReusedFiles 为 4476、RepairedFiles 为 0。
- FileContentVerified 为 true、GameStarted 为 false。

该验收没有启动 Minecraft，也没有通过删除或损坏现有 JAR 制造下载场景。底层下载与资源修复已有其他验收记录，不能把那些记录当作本入口每种故障都已实测。

实际签名见 [公开 API](public-api.md)，内部实现见 [源码结构](architecture.md)。

## 下载源

对象初始化器可设置 `Source = DownloadSource.BmclApi`，需要引用 `FMCore.Downloads`。默认 Official；客户端与资源补全沿用该选择。规则及来源标注要求见 [原版安装](installation.md#下载源)。安装与补全均不要求 Java 路径，Java 在启动时由调用方提供。
