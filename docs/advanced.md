# 异步、回调与取消

命名空间按功能使用 `FMCore.Launch`、`FMCore.Versions` 和 `FMCore.Java`。简单调用可以省略 CancellationToken；需要停止或取消时传入调用方的令牌。

## 统一启动入口

先按 [启动手册](launch.md) 构造分组配置。以下是已有 `LaunchConfig config` 后的异步方法体片段：

```csharp
using CancellationTokenSource canceled = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    canceled.Cancel();
};

var launcher = new FMCore.Launch.MinecraftLauncher(config);

try
{
    int exitCode = await launcher.LaunchAsync(
        progress => Console.WriteLine(progress.Message),
        onOutput: Console.WriteLine,
        onError: Console.Error.WriteLine,
        cancellationToken: canceled.Token);

    Console.WriteLine($"GameExitCode: {exitCode}");
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("本次操作已取消。");
}
catch (AggregateException error)
{
    foreach (Exception item in error.InnerExceptions)
        Console.Error.WriteLine(item.Message);
}
```

LaunchAsync 在后台处理同步准备，不需要界面调用方再包 Task.Run。任务持续至 Java 退出并完成日志收尾，结果是整数退出码。当前没有启动后立即返回的运行对象。

同一个 MinecraftLauncher 实例同一时间只允许一次操作；重复调用会失败，原来的操作继续运行。配置使用 init 成员；需要调整设置时创建新配置和启动器。

## 进度与日志

| 回调 | 内容 |
| --- | --- |
| onProgress | LaunchProgress，包含阶段枚举 Stage 和说明 Message |
| onOutput | Java 标准输出，每次一行 |
| onError | Java 标准错误，每次一行，可能包含普通提示 |

回调不保证处于 UI 线程，进度和日志也可能交错。界面更新需自行使用 Dispatcher 等调度方式。共享文件和集合由调用方处理同步；回调应及时返回。

回调异常会导致操作失败，进程仍运行时进入停止和收尾。GameExited 回调发生时进程及日志已完成收尾，若该回调抛错仍会向调用方传播。不要把回调异常视为 Minecraft 自身崩溃。

JavaStarted 只说明进程已创建。GameExited 也可能对应非零退出码。取消或异常不保证报告 GameExited。

## 取消与超时

准备阶段在相应步骤检查令牌；同步解析不保证立即中断。Java 创建后，取消会尝试强制结束本次进程树，不代替游戏菜单的保存并退出。

Java 探测固定超时为 10 秒。游戏正常退出后的日志排空最多 5 秒；失败或取消时，停止等待最多 10 秒、日志收尾最多 5 秒。这些时限不限制游戏运行总时长。

调用方取消通常抛 OperationCanceledException。主错误和收尾错误同时发生时，抛 AggregateException 并保留原始错误。控制台程序可以把取消映射成 130；这不是 SDK 的整数返回约定。

## 单独查询的取消

版本和 Java 查询仍保留独立入口，例如：

```csharp
using FMCore.Versions;

using CancellationTokenSource canceled = new();

IReadOnlyList<MinecraftVersion> versions =
    await new MinecraftVersionReader().GetVersionsAsync(
        cancellationToken: canceled.Token);
```

指定 Java 探测：

```csharp
using FMCore.Java;

if (args.Length != 1)
    throw new ArgumentException("请提供 Java 程序路径。");

using CancellationTokenSource canceled =
    new(TimeSpan.FromSeconds(5));

JavaInfo java = await JavaReader.GetJavaInfoAsync(
    args[0], canceled.Token);
```

调用方令牌和 Java 自身探测超时分别表现为取消和 TimeoutException。取消不触发更换候选；Java 搜索中的单个失败按其接口约定诊断并继续。详见 [Java 查询](java.md)。

## 编码和目录生命周期

游戏进程设置 UTF-8 编码并按 UTF-8 解码日志。仅在该子进程环境移除 JAVA_TOOL_OPTIONS、JDK_JAVA_OPTIONS 和 _JAVA_OPTIONS，不修改系统环境。

旧式解压失败会尝试清理本次创建内容，清理失败保留并报告。成功后的 natives 目录和后续启动失败时已准备好的目录暂时保留。进程仍运行时不应移除其原生库目录。

## 下载的取消与进度

单文件下载已提供 FileDownloader.DownloadAsync。调用方令牌用于取消校验、网络和文件 I/O；请求中的 Timeout 默认为 5 分钟，所有下载尝试、校验与重试等待共用这一个预算。超时转换为 TimeoutException，调用方取消通常为 OperationCanceledException，清理也失败时为 AggregateException。

传输进度到 100% 后仍可能在校验或正式落盘时失败，应以任务成功返回为准。已有不匹配目标默认保留并报错；DownloadRequest.ReplaceInvalidFiles 为 true 时，在新内容下载及校验后保留备份再替换。只清理本次临时文件，已创建的空父目录和备份保留。回调不保证 UI 线程。当前正常下载和复用已验收，其他失败路径不能当作本轮已实测。

DownloadManyAsync 会取消调度及已开始的单项操作，并等待收尾。单项失败也会取消其余操作，已完成的正式文件保留。单项非取消错误以 AggregateException 汇总，取消与清理错误同时出现时保留清理错误。失败或取消不保证报告最终进度。

同一批次进度回调串行执行，应及时返回。ProcessedBytes 包含复用大小；字节进度不能代替已完成文件数。正常并发与再次全部复用已由用户验收；批次取消、失败和回调异常尚无单独实测证据。

单文件默认额外重试 2 次。退避等待传入同一个关联令牌，取消或总超时停止等待。只有特定网络分类和 HTTP 状态可重试；回调、校验、磁盘和清理错误直接传播。单文件失败不回滚其他已完成文件，批次汇总错误。用户已验收本地 503 后恢复和次数上限，其他失败路径不能据此当作已实测。

TransferredBytes 累计实际报告的正文写入量，包含失败尝试，不包含复用大小；完成进度仍用 DownloadedBytes 或 ProcessedBytes。速度使用最近约 5 秒的正文写入样本，剩余时间暂不可估算时为 null。速度和时间在进度回调时更新，等待期间没有独立刷新。剩余 0 秒表示预计传输结束，仍需等待校验与正式落盘。

资源元数据清单读取仅生成请求并统计文件是否存在，不下载、不验证目标内容，也不创建目录。用户已使用两个真实实例验收此内部步骤。GameResourceCompleter 已提供 CompleteAsync，执行补全时通过下载器核对大小与 SHA-1，索引原始字节在解析前再次验证。默认修复不匹配文件；replaceInvalidFiles: false 时保留并报错。成功替换的旧文件备份通过结果 Backups 返回，RepairedFiles 是 DownloadedFiles 的一部分。替换系统调用失败可能已将旧文件移到备份，错误中包含目标和备份候选路径。

资源补全按阶段报告进度，Download 为当前批次的可选统计；元数据和对象分别计算百分比。每实例有重复操作防护，结束后重置；回调不保证 UI 线程，取消和失败保留已完成文件。完整用法见 [游戏资源补全](resources.md)。

GameFileDownloadPlanner.ReadAsync 为内部只读清单步骤。客户端路径解析允许文件缺失，启动路径仍要求文件存在。清单保留缺失元数据，不使用本地文件大小或哈希补造校验标准。game-file-metadata 会探测指定 Java，但不会下载文件或启动 Minecraft。

添加 --resolve-maven 时，内部 ReadResolvedAsync 会查询声明中缺失的 Maven 元数据；已有声明保留。每个待查询依赖共用 30 秒超时，SHA-1 响应最多读取 1 KiB，HEAD 须提供有效大小；要求完整 HTTP 200 与未压缩响应。不使用本地文件补造校验标准，不持久化查询结果，元数据 HTTP 失败直接传播，文件下载器的重试不适用于这一步。用户已验收正常在线查询，未据此宣称超时与取消路径已实测。

GameFileCompleter 已提供完整的已有实例文件校验编排，内部依次执行客户端与依赖批次、资源元数据批次和对象批次。Download.Percentage 为当前批次字节进度，文件数比例可以不同；复用不计入网络传输速度。每个对象同时只允许一次操作。失败保留已经完成的文件与备份，最终回调抛错时方法仍会失败；整个流程不回滚实例。用户已连续两次校验复用 4476 个文件，尚未据此确认统一入口的缺失 JAR、损坏 JAR、取消或故障路径。

完整配置和处理规则见 [下载手册](downloads.md)，统一调用见 [游戏文件补全](game-files.md)，实际签名见 [公开 API](public-api.md)。

原版安装使用 MinecraftInstaller(versionId, minecraftRoot).InstallAsync，在线列表使用 MinecraftInstaller.GetVersionsAsync；两者均不要求 Java 路径。安装和文件补全按目标系统架构选择依赖，Java 在启动时提供。下载源和错误回退规则见 [原版安装](installation.md)。
