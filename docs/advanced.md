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

下载与资源补全属于第 3 阶段，当前没有已验收的公开下载 API。完整实际签名见 [公开 API](public-api.md)。
