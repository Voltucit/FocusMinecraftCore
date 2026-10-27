# 游戏资源补全 · FMCore.Installer

通过 GameResourceCompleter 补全已有 Minecraft 实例的资源索引、资源对象和日志配置，并校验已有文件。普通调用方填写版本与根目录，再调用 CompleteAsync。需要同时检查客户端与依赖时，使用 [GameFileCompleter](game-files.md)。

基础补全与再次复用已由用户在 1.16.5 样本上验收。保留备份后修复已通过本地 HTTP 样本及 1.21.10 Fabric 真实资源补全验收，重复执行可全部复用。

## 最简用法

命名空间：FMCore.Installer。下面是引用核心项目后的完整控制台入口：

```csharp
using FMCore.Installer;

var completer = new GameResourceCompleter(
    "1.16.5",
    ".minecraft");

ResourceCompletionResult result =
    await completer.CompleteAsync();

Console.WriteLine($"检查文件：{result.TotalFiles}");
Console.WriteLine($"新增下载：{result.DownloadedFiles}");
Console.WriteLine($"校验复用：{result.ReusedFiles}");
```

版本必须已能从所选根目录读取。版本、根目录和并发上限由调用方选择。此功能不执行 Java，不补全客户端 JAR 或依赖库，也不安装加载器。

## 构造方法

```text
public GameResourceCompleter(
    string versionId,
    string minecraftRoot,
    int maxConcurrency = 4,
    bool replaceInvalidFiles = true)
```

| 参数 | 含义 |
| --- | --- |
| versionId | 已有实例 JSON 中的版本 ID，支持同根继承 |
| minecraftRoot | 所选 Minecraft 根目录 |
| maxConcurrency | 正整数，默认 4；分别用于元数据和对象批次 |
| replaceInvalidFiles | 默认 true；不匹配文件下载并校验后，保留旧文件备份再替换；false 时保留并报错 |

构造时检查参数并规范化根目录，不创建目录、不读取版本 JSON、不下载。相对路径按构造时的工作目录确定。

## 下载源

可在对象初始化器设置 `Source = DownloadSource.BmclApi`，需引用 FMCore.Downloads。默认 Official；当前索引与日志保留原来源，资源对象可走镜像。规则和来源标注见 [原版安装](installation.md#下载源)。

## CompleteAsync

```text
public Task<ResourceCompletionResult> CompleteAsync(
    Action<ResourceCompletionProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

SDK 内部完成清单读取、下载、重试和校验。调用方无需手动拼装 DownloadRequest。元数据批次结束后按已验证索引展开对象批次；同一哈希去重，声明大小冲突则报错。

同一实例同时只能执行一次操作；重复调用失败，原操作继续。结束后可再次使用。失败或取消会等待已开始的下载收尾，已成功文件保留，不做整个目录的回滚。每个下载请求的超时独立计算，整个补全任务由调用方令牌控制。

只有任务成功返回，才表示本次范围内的文件已完成大小和 SHA-1 校验；这不代表全部游戏文件完整或游戏已启动。

## 进度与结果

```text
public enum ResourceCompletionStage
{
    ReadingMetadata,
    CompletingMetadata,
    ReadingAssetObjects,
    CompletingAssetObjects,
    Completed
}

public sealed record ResourceCompletionProgress(
    ResourceCompletionStage Stage,
    string Message,
    DownloadBatchProgress? Download = null)

public sealed record ResourceCompletionResult(
    int MetadataFiles,
    int AssetFiles,
    int DownloadedFiles,
    int ReusedFiles)

public int TotalFiles { get; }
public IReadOnlyList<ResourceFileBackup> Backups { get; init; }
public int RepairedFiles { get; }

public sealed record ResourceFileBackup(
    string FilePath,
    string BackupPath)
```

| 成员 | 含义 |
| --- | --- |
| Stage / Message | 当前步骤和说明 |
| Download | 可选的当前批次进度，包含字节、速度和剩余时间 |
| MetadataFiles | 资源索引与可选日志配置数量 |
| AssetFiles | 按哈希去重的资源对象文件数 |
| DownloadedFiles | 本次成功下载并正式落盘的文件数 |
| ReusedFiles | 已存在且通过校验的复用文件数 |
| TotalFiles | MetadataFiles 与 AssetFiles 之和 |
| Backups | 本次成功替换的文件与备份路径；不包含旧操作留下的备份 |
| RepairedFiles | Backups.Count，是 DownloadedFiles 的一部分 |

Download.Percentage 对应当前批次，元数据与资源对象分别计数。速度、复用和剩余时间的规则见 [下载手册](downloads.md#速度与剩余时间)。

进度在后台执行，不保证界面线程。界面自行调度更新，回调应及时返回。回调错误会传播；Completed 回调发生时文件已完成校验，若回调抛错，方法仍向调用方报告错误。

## 进度与取消示例

```csharp
using FMCore.Installer;

using CancellationTokenSource canceled = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    canceled.Cancel();
};

var completer = new GameResourceCompleter("1.16.5", ".minecraft");

ResourceCompletionResult result = await completer.CompleteAsync(
    progress =>
    {
        Console.WriteLine($"[{progress.Stage}] {progress.Message}");

        if (progress.Download is { } download)
            Console.WriteLine($"{download.Percentage:F1}%");
    },
    canceled.Token);

Console.WriteLine($"完成文件：{result.TotalFiles}");
```

## 文件处理与限制

资源索引请求位于内部元数据清单首项。索引声明大小须为 1 字节至 16 MiB，读取同一份原始字节后再次检查大小和 SHA-1，再解析 JSON。对象按前两位哈希和完整哈希确定路径，逻辑资源名称不直接用作输出路径。

支持标准 objects 布局，virtual 和 map_to_resources 为 true 时暂不支持。索引 ID 与日志文件 ID 必须是安全的单个名称；对象哈希为 40 位十六进制字符串。

默认修复内容不匹配的文件。旧文件在新文件下载与大小、SHA-1 校验期间保留；验证成功后使用 File.Replace 生成同目录唯一备份并替换目标。备份名为原文件名加 .fmcore-backup-<随机标识>.bak，由调用方决定何时处理，SDK 不自动删除。传入 replaceInvalidFiles: false 可改为保留不匹配目标并报错。

下载或校验失败不会进入替换。文件系统替换失败时会报告目标和备份候选位置；某些系统错误可能已将旧文件移到备份，不能保证错误后原路径仍存在。失败与取消仅清理本次临时文件，成功文件、备份和已创建目录保留。下载期间若其他操作已补好目标，再次校验后复用该目标，不产生新备份。

常见异常包括参数、版本或父版本缺失、无效元数据、JSON 语法、重复声明冲突、目录或权限、HTTP、取消、超时和 AggregateException。底层下载与清理异常规则见 [下载手册](downloads.md#取消超时和异常)。索引解析在后台执行，部分同步解析步骤不能立即被取消。

## 验收范围

用户已在 1.16.5 样本上反馈基础补全和再次复用成功。该索引有 2615 条记录，去重后 2596 个资源对象，加上索引与日志共 2598 个文件。验收步骤包含补全缺失日志配置、校验已有对象及再次全部复用，没有启动游戏。

用户反馈保留备份后修复阶段成功：本地 HTTP 样本先达到 503 重试上限并保留旧文件，随后下载成功、核对新文件及旧文件备份，再次调用复用；1.21.10 Fabric 的不匹配资源索引也完成备份修复及再次复用。该反馈不能代替取消、权限或文件系统替换故障路径的单独实测。

实际接口见 [公开 API](public-api.md)，内部结构见 [源码结构](architecture.md)。
