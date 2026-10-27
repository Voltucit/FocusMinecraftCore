# 文件下载 · FMCore.Downloads

通过 `FileDownloader.DownloadAsync` 下载一个清单已知的文件，按声明大小与 SHA-1 校验，并返回正式目标路径。支持已有文件校验复用、调用方取消和操作超时。

单文件、多文件并发、总体字节进度和再次复用已通过用户 CLI 功能验收。有限重试已通过本地 HTTP 样本验收；平均速度与剩余传输时间已通过本地慢速 HTTP 样本的 CLI 验收。资源索引与日志配置的内部下载清单已通过两个真实实例的 CLI 验收，GameResourceCompleter 的资源对象与补全执行也已通过 1.16.5 基础验收，见 [游戏资源补全](resources.md)。

## 最简用法

创建下载请求，再调用下载方法：

```csharp
using FMCore.Downloads;

DownloadRequest request = new()
{
    Url = "https://piston-data.mojang.com/v1/objects/39384bd14c0606d812afec88d8aff595b2587dd9/client-1.21.2.xml",
    FilePath = "downloads/client-1.21.2.xml",
    Size = 1073,
    Sha1 = "39384bd14c0606d812afec88d8aff595b2587dd9"
};

DownloadResult result = await FileDownloader.DownloadAsync(
    request,
    progress => Console.WriteLine(
        $"{progress.Percentage:F1}%"));

Console.WriteLine(result.FilePath);
Console.WriteLine($"AlreadyPresent: {result.AlreadyPresent}");
```

示例可放入已引用核心的 .NET 10 控制台程序。目标路径由调用方选择；相对路径按调用时工作目录规范化。示例没有写入 Minecraft 安装目录。

## DownloadRequest

命名空间：`FMCore.Downloads`。使用无参构造和对象初始化器：

| 属性 | 类型 | 要求 / 默认值 | 用途 |
| --- | --- | --- | --- |
| Url | string | required | 完整 HTTP 或 HTTPS 地址 |
| FilePath | string | required | 正式目标文件路径 |
| Size | long | required，非负 | 清单声明的字节数，允许空文件 |
| Sha1 | string | required | 40 位十六进制 SHA-1，接受大小写 |
| Timeout | TimeSpan | 5 分钟 | 全部尝试与等待共用；必须为正且处于计时器范围 |
| MaxRetries | int | 2 | 额外重试次数，范围 0 到 10 |
| RetryDelay | TimeSpan | 1 秒 | 基础退避等待，范围 0 到 30 秒 |
| ReplaceInvalidFiles | bool | false | true 时下载并校验新内容，保留旧文件备份后替换 |

数据模型的成员为 init。填写请求本身不会创建目录或联网。参数、路径和目标占用检查在下载调用中执行。

## FileDownloader 方法

静态类，无需实例化。以下是接口声明：

```text
public static Task<DownloadResult> DownloadAsync(
    DownloadRequest request,
    Action<DownloadProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

| 参数 | 用途 |
| --- | --- |
| request | 调用方明确选择的 URL、目标文件、大小、SHA-1 和超时 |
| onProgress | 可选传输字节进度回调 |
| cancellationToken | 取消校验或网络与文件 I/O |

共享 HttpClient 复用连接。每次响应、网络流、文件流和计时器在操作结束时释放。

## 进度与返回值

```text
public sealed record DownloadProgress(
    string FilePath,
    long DownloadedBytes,
    long TotalBytes)
{
    public double Percentage { get; }
    public int Attempt { get; init; } = 1;
    public bool AlreadyPresent { get; init; }
    public long TransferredBytes { get; init; }
    public double BytesPerSecond { get; init; }
    public TimeSpan? RemainingTime { get; init; }
}

public sealed record DownloadResult(
    string FilePath,
    long Size,
    string Sha1,
    bool AlreadyPresent)
{
    public int Attempts { get; init; } = 1;
    public string? BackupPath { get; init; }
}
```

| 成员 | 含义 |
| --- | --- |
| DownloadedBytes | 本次传输已写入临时文件的字节数；复用成功时报告完整大小 |
| TotalBytes | 请求中的声明大小 |
| Percentage | 字节比例，声明大小为 0 时为 100 |
| result.FilePath | 规范化后的正式目标路径 |
| result.Size | 已校验的大小 |
| result.Sha1 | 已校验的 SHA-1，小写形式 |
| result.AlreadyPresent | 是否复用了原本存在且通过校验的目标 |
| progress.Attempt | 当前第几次 SDK 尝试，从 1 开始 |
| result.Attempts | 成功使用的 SDK 尝试次数 |
| result.BackupPath | 本次成功替换时的旧文件备份路径，否则为 null |

传输到 100% 不代表哈希和最终落盘已经完成。只有任务成功返回 DownloadResult，才能按成功下载或复用处理。

回调同步调用，不保证界面线程，应及时返回。回调异常向调用方传播；正在下载时会触发本次临时文件清理。界面更新由启动器自行调度。

## 多文件下载

命名空间：`FMCore.Downloads`。DownloadManyAsync 先固定请求并检查参数、重复实际目标及文件与父目录冲突，再复用单文件下载。

```csharp
using FMCore.Downloads;

DownloadRequest[] requests =
[
    new()
    {
        Url = "https://piston-data.mojang.com/v1/objects/39384bd14c0606d812afec88d8aff595b2587dd9/client-1.21.2.xml",
        FilePath = "downloads/client-1.21.2.xml",
        Size = 1073,
        Sha1 = "39384bd14c0606d812afec88d8aff595b2587dd9"
    },
    new()
    {
        Url = "https://piston-data.mojang.com/v1/objects/bd65e7d2e3c237be76cfbef4c2405033d7f91521/client-1.12.xml",
        FilePath = "downloads/client-1.12.xml",
        Size = 888,
        Sha1 = "bd65e7d2e3c237be76cfbef4c2405033d7f91521"
    }
];

IReadOnlyList<DownloadResult> results =
    await FileDownloader.DownloadManyAsync(
        requests,
        maxConcurrency: 2,
        onProgress: progress => Console.WriteLine(
            $"{progress.Percentage:F1}% " +
            $"{progress.CompletedFiles}/{progress.TotalFiles}"));

Console.WriteLine($"完成：{results.Count}");
```

示例可放入引用核心的 .NET 10 控制台程序。使用独立下载目录，不运行游戏。

### 方法与返回值

```text
public static Task<IReadOnlyList<DownloadResult>> DownloadManyAsync(
    IEnumerable<DownloadRequest> requests,
    int maxConcurrency = 4,
    Action<DownloadBatchProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

| 参数 / 返回 | 含义 |
| --- | --- |
| requests | 枚举一次并固定为本批次集合；可以为空 |
| maxConcurrency | 正整数，默认 4；限制当前批次同时处理的文件操作数 |
| onProgress | 可选总体进度回调 |
| cancellationToken | 取消调度与已开始的操作，并等待收尾 |
| 返回值 | 仅全部成功时返回，结果按原请求顺序排列 |

并发数包含已有文件的校验，不等于线程数或 HTTP 连接数；不同批次各有自己的上限。预检含同步步骤，不保证全部在后台执行。

### 批次进度

```text
public sealed record DownloadBatchProgress(
    int TotalFiles,
    int CompletedFiles,
    int ActiveFiles,
    int PeakActiveFiles,
    long ProcessedBytes,
    long TotalBytes)
{
    public double Percentage { get; }
    public long TransferredBytes { get; init; }
    public double BytesPerSecond { get; init; }
    public TimeSpan? RemainingTime { get; init; }
}
```

| 属性 | 含义 |
| --- | --- |
| TotalFiles | 请求数 |
| CompletedFiles | 已完成单文件校验、正式落盘或复用的数量 |
| ActiveFiles | 正在执行单文件操作的数量 |
| PeakActiveFiles | 已报告的最高同时处理数，不超过本批次上限 |
| ProcessedBytes | 各文件报告的已处理大小，包含通过校验的复用大小，不是纯网络流量 |
| TotalBytes | 全部声明大小之和；溢出 long 时在预检报错 |
| Percentage | 字节比例；总大小为 0 时按是否全部完成计算 0 或 100 |

字节到 100% 不代表校验或落盘成功，应以任务成功返回为准。空集合返回空结果、进度为 100%。成功时最后一份进度的 ActiveFiles 为 0。

同一批次统计与回调通过锁串行执行，网络传输在锁外。回调应及时返回，不保证 UI 线程；不同批次之间不共享这把锁。

### 失败与取消

单项失败会取消其余操作，并等待已开始的操作释放资源、清理临时文件。已完成的正式文件保留，不做整批回滚。非取消单项错误通过带目标路径上下文的 AggregateException 汇总，包含清理错误。

没有已记录的非取消错误时，调用方取消通常为 OperationCanceledException。初始和最终总体回调错误可直接传播；工作项内的回调错误进入批次汇总。失败或取消不保证报告 ActiveFiles 为 0 的最终进度。

参数、重复目标和父目录冲突在下载前检查，已有文件内容在对应单文件操作中校验。大小求和溢出为 OverflowException。

用户按本轮 CLI 步骤反馈三文件下载、最大并发数为 2 的统计及再次全部复用成功。尚未单独实测批次失败、取消和冲突路径。

## 速度与剩余时间

单文件与批次进度均提供 TransferredBytes、BytesPerSecond 和 RemainingTime。DownloadProgress 还提供 AlreadyPresent，标记已有文件通过校验后的复用。

| 成员 | 含义 |
| --- | --- |
| DownloadProgress.AlreadyPresent | 当前报告来自已存在且通过校验的目标；不增加传输量 |
| TransferredBytes | 本次调用累计成功写入临时文件的正文大小，包含失败尝试中的已报告数据，不包含已有文件复用 |
| BytesPerSecond | 最近约 5 秒内的正文写入速度，单位字节/秒；单文件与批次分别统计，开始不足 5 秒时使用已有样本 |
| RemainingTime | 用剩余完成字节数除以平均速度估算的传输时间；暂不可估算或超出 TimeSpan 范围时为 null，剩余字节为 0 时为 0 |

单文件的 DownloadedBytes、批次的 ProcessedBytes 用来表示完成进度，包含复用大小，重试时可能回退。TransferredBytes 用于传输统计，在一次调用内累计，不能直接除以声明总大小当作完成百分比。

近期平均速度会受到连接、重试等待、校验和回调耗时影响，不是瞬时速度。内部约每 250 毫秒记录一次样本；累计传输量仍用于 TransferredBytes。数据在进度回调时更新，等待期间没有独立的定期刷新。全部复用时传输量和速度均为 0。

下面是完整的单文件控制台示例：

```csharp
using FMCore.Downloads;

DownloadRequest request = new()
{
    Url = "https://piston-data.mojang.com/v1/objects/39384bd14c0606d812afec88d8aff595b2587dd9/client-1.21.2.xml",
    FilePath = "downloads/client-1.21.2.xml",
    Size = 1073,
    Sha1 = "39384bd14c0606d812afec88d8aff595b2587dd9"
};

DownloadResult result = await FileDownloader.DownloadAsync(
    request,
    progress =>
    {
        string remaining = progress.RemainingTime is TimeSpan time
            ? $"{Math.Ceiling(time.TotalSeconds):F0} 秒"
            : "计算中";

        Console.WriteLine(
            $"{progress.Percentage:F1}% " +
            $"{progress.BytesPerSecond / 1024d / 1024:F2} MiB/s " +
            $"预计剩余 {remaining}");
    });

Console.WriteLine($"校验完成：{result.FilePath}");
```

剩余时间只估算正文传输，不包含未来重试、等待、校验和正式落盘等耗时。0 秒与 100% 都不能代替任务成功返回。

用户反馈本地慢速 HTTP 样本的进度与结果验收成功，步骤覆盖单文件、一个复用加一个下载的混合批次、再次全部复用。该样本用于核对进度计算与复用统计，实际显示速度会随运行情况变化。

## 有限重试

DownloadAsync 在每次 DownloadOnceAsync 完成资源释放和临时文件清理后，再判断是否重试。DownloadManyAsync 继续复用同一入口；等待重试时仍占用一个批次活动项。

### 配置

| 属性 | 默认值 / 范围 | 含义 |
| --- | --- | --- |
| DownloadRequest.MaxRetries | 2；0 到 10 | 额外重试次数；默认最多尝试 3 次，0 表示不重试 |
| DownloadRequest.RetryDelay | 1 秒；0 到 30 秒 | 第一次重试的基础等待时间 |
| DownloadRequest.Timeout | 5 分钟 | 单文件所有尝试、校验和重试等待共用的超时 |
| DownloadProgress.Attempt | 1 起 | 当前 SDK 下载尝试次数 |
| DownloadResult.Attempts | 1 起 | 最终成功使用的尝试次数 |

下面是可直接放入已引用核心项目的控制台入口：

```csharp
using FMCore.Downloads;

DownloadRequest request = new()
{
    Url = "https://piston-data.mojang.com/v1/objects/39384bd14c0606d812afec88d8aff595b2587dd9/client-1.21.2.xml",
    FilePath = "downloads/client-1.21.2.xml",
    Size = 1073,
    Sha1 = "39384bd14c0606d812afec88d8aff595b2587dd9",
    MaxRetries = 2,
    RetryDelay = TimeSpan.FromSeconds(1),
    Timeout = TimeSpan.FromMinutes(5)
};

DownloadResult result = await FileDownloader.DownloadAsync(
    request,
    progress => Console.WriteLine(
        $"第 {progress.Attempt} 次：{progress.Percentage:F1}%"));

Console.WriteLine($"成功尝试次数：{result.Attempts}");
```

### 重试条件与等待

| 情况 | 行为 |
| --- | --- |
| DNS、建立连接失败、HTTP 响应提前结束 | 在次数与超时允许时重试 |
| HTTP 408、429、500、502、503、504 | 在次数与超时允许时重试 |
| 其他 HTTP 状态、TLS 握手或证书错误 | 直接传播 |
| 大小、SHA-1、参数或路径无效 | 直接传播 |
| 普通磁盘 I/O、权限或清理错误 | 直接传播；清理失败不重试 |
| 进度回调错误 | 直接传播，不把回调伪装成网络故障 |
| 调用方取消或总超时 | 停止，不启动下一次尝试 |

基础等待按 1、2、4 倍递增，基础退避最多 30 秒。有效 Retry-After 的秒数或日期要求更久时，采用服务器要求；已经过期的日期按 0 处理。服务器要求超过整个请求 Timeout 时保留本次 HTTP 错误，不提前重试；要求仍在整个配置范围内但剩余总预算不足时，由总超时终止等待。

重试从新的独立临时文件和 0 字节重新开始，不做断点续传。DownloadProgress.DownloadedBytes 和批次 ProcessedBytes 可能回退，不表示累计网络流量。已经成功的其他文件保留。最终失败传播最后一次错误；清理失败继续保留聚合错误。

### 验收范围

用户通过 CLI 本地 HTTP 样本验收：前两次 503、第三次 200 时成功，Attempts 为 3；连续三次 503 时达到上限停止。这里验证了 SDK 的 HTTP 状态重试与次数上限，没有据此宣称 TLS、DNS、传输中断、Retry-After 日期、取消或清理失败均已实测。

## 文件处理规则

| 情况 | 行为 |
| --- | --- |
| 目标不存在 | 按块写入同目录的唯一 .part 文件，校验后改为正式文件 |
| 目标存在且大小、SHA-1 匹配 | 读取并校验，返回 AlreadyPresent = true，不发起 HTTP 请求 |
| 已有文件不匹配 | 默认保留并报错；ReplaceInvalidFiles = true 时，下载校验后备份替换 |
| 目标被目录占用 | 报错，不覆盖目录 |
| 传输超出声明大小 | 中止并清理本次临时文件 |
| 传输结束但长度或 SHA-1 错误 | 报错并清理本次临时文件 |
| 正式落盘前目标被其他操作创建 | 严格模式报错；修复模式重新校验，匹配则复用，否则备份替换 |
| 下载失败或取消 | 清理本次创建的单个 .part 文件，不递归删除目录 |

读取缓冲区为 64 KiB，不将整份文件装入内存。服务器提供 Content-Length 时先检查它，正文读取期间仍统计实际长度。

目标父目录仅在需要写入时创建。空的已创建目录不会随失败递归回收。SHA-1 用于核对调用方提供的清单值，不代替对元数据来源的判断。

ReplaceInvalidFiles 默认 false。开启后，下载及校验成功才由 FileDownloader.Commit.cs 使用 File.Replace 替换。旧文件备份保留在目标目录，名称为原文件名加 .fmcore-backup-<随机标识>.bak；SDK 不自动删除备份。下载或校验失败不触碰旧文件。替换本身失败时报告目标及备份候选路径，系统可能已移动旧文件，因此不能保证原路径仍存在。DownloadResult.BackupPath 只记录本次成功替换的备份。

高层 GameResourceCompleter 的 replaceInvalidFiles 默认 true，会为内部请求开启该策略。其结果汇总成功备份路径，RepairedFiles 计入 DownloadedFiles。

## 取消、超时和异常

超时覆盖所有尝试中的已有文件校验、网络、读写和重试等待等可取消步骤。使用 ResponseHeadersRead 时，正文读取同样传入关联令牌。同步文件系统操作与同步回调不会被强制打断。

调用方取消抛 OperationCanceledException；仅操作超时时转换为 TimeoutException。已有目标不会因取消而删除。本次临时文件清理若也失败，通过 AggregateException 保留原错误与清理错误。

| 异常 | 常见原因 |
| --- | --- |
| ArgumentNullException / ArgumentException | 请求为空、URL 或 SHA-1 格式无效、路径无效 |
| ArgumentOutOfRangeException | 大小为负、超时无效或超出计时器范围 |
| InvalidDataException | 长度或 SHA-1 不匹配 |
| System.Net.Http.HttpRequestException | HTTP 失败、连接或证书等网络错误 |
| IOException / UnauthorizedAccessException | 路径占用、权限、磁盘或正式落盘失败 |
| OperationCanceledException / TimeoutException | 调用方取消或操作超时 |
| AggregateException | 原操作与临时清理同时失败 |

其他回调或文件系统异常按实际原因传播。正常下载、再次复用、HTTP 503 后恢复与次数上限已由用户验收。备份修复样本已验收下载失败保留旧文件、随后成功替换并核对备份及再次复用；1.21.10 Fabric 的资源索引修复也反馈成功。取消、超时、权限和清理错误路径仍无对应实测证据。

## 后续范围

Range 分段尚未实现。MinecraftInstaller 已提供原版安装并通过 1.21.10 真实安装与启动验收，见 [原版安装](installation.md)。资源索引与日志下载声明的内部读取只生成请求、不下载文件；GameResourceCompleter 已通过基础补全、备份修复及再次复用验收。客户端、普通依赖、Maven 元数据及旧式 Windows 原生库声明已接入 GameFileCompleter；新版实例曾连续两次校验复用 4476 个文件，1.8.9 已完成旧式原生库元数据验收。入口复用同一个下载器，见 [游戏文件补全](game-files.md)。清单中的存在性统计不代替内容校验，正式补全仍核对大小与 SHA-1。

实际接口索引见 [公开 API](public-api.md)，阶段安排见 [开发路线](roadmap.md)。用户指定的不完整旧版实例已用于清单验收，已继续通过实际资源补全与再次复用验收。

## 安装与补全的下载源

MinecraftInstaller、GameFileCompleter 和 GameResourceCompleter 提供可选的 Source 属性，默认 DownloadSource.Official。设为 DownloadSource.BmclApi 后，已识别的客户端、Mojang/Fabric Maven 依赖与资源对象地址使用 BMCLAPI；未知地址、版本和资源元数据、日志配置保留原来源。完整用法见 [原版安装](installation.md#下载源)。

镜像发生 HTTP 或网络错误时可在剩余重试预算内回退原地址；后续尝试使用原地址，总超时不重置。MaxRetries 为 0 时不回退。取消、已耗尽的总超时、校验、磁盘、回调和清理错误不触发源回退。大小及 SHA-1 标准始终来自下载声明，TLS 校验保持启用。下载源不是全局可变设置。

用户已反馈 BMCLAPI 安装并实际启动原版 1.21.10 成功；不据此宣称每个镜像节点或回退错误分支均已单独验收。近期速度与文件/字节分开显示也已通过本地 CLI 样本验收。
