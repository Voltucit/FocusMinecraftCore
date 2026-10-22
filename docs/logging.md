# 日志与诊断

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Logging;

var logger = new LauncherLogger();
logger.Write(LogLevel.Information, "启动准备中");
foreach (var entry in logger.GetEntries()) Console.WriteLine(entry.Message);
```


命名空间：`FMCore.Logging`。本模块包含于 FocusMinecraftCore 1.0.0。

`LauncherLogger` 记录带时间、级别和来源的有界日志；`MinecraftLauncher.Logger` 自动汇集启动器状态及本次进程的标准输出/错误输出。默认只保留内存缓存，调用方明确调用 `SaveAsync` 才导出文件，不依赖 GUI 框架或日志容器。

## 最简示例：日志与证据分析

下面是人工构造日志的完整调用示例，不表示启动或认证成功：

独立、可编译的调用代码见[本模块示例](examples.md#logging)。

未知日志仍保留脱敏后的原文、时间和来源，不替换成凭空推断的原因。`GameDiagnostics.Analyze` 是有界同步分析，支持取消；文件读取在启动器退出诊断流程中异步处理。

## 自动采集与高级启动

以下完整示例通过现有 SDK 启动，默认需要匹配元数据的 Java，并可在首次准备时下载游戏文件：

独立、可编译的调用代码见[本模块示例](examples.md#logging)。

原有 `OutputReceived`、`ErrorReceived`、`Exited` 和停止 API 继续保留。`GameExitResult.Diagnostics` 与 `Exited` 中的同一退出结果提供诊断；准备失败时从 `LaunchResult.Diagnostics` 获取，退出码为 `null`。`LastDiagnostic` 返回最近一次完成结果，每次新的启动尝试都会清空旧缓存和诊断，避免混入上次错误。

GUI 事件在写入/后台线程触发，调用方自行调度到 UI，处理器不能同步等待启动器。订阅者异常会隔离；不要让慢日志接收器阻塞游戏管道。取消 `WaitForExitAsync` 只取消等待，停止游戏仍使用 `StopAsync`。

## API 与配置

| 入口 | 参数 / 返回值 | 说明 |
| --- | --- | --- |
| `new LauncherLogger(options = null, sensitiveValues = null)` | `LoggingOptions`、可选已知敏感值 | 单独供宿主日志使用；裸露的调用方凭据应提前通过 sensitiveValues 提供 |
| `Write(level, message, source = Launcher)` | → `LogEntry` | 先过滤，再存储和通知事件 |
| `EntryReceived` | `EventHandler<LogEntry>` | 带时间、级别、来源、文本、截断标记及可选报告相对路径 |
| `GetEntries()` / `DroppedEntries` | 不可变快照 / 已丢弃数量 | 缓存满时丢弃最旧记录，不无限增长 |
| `SaveAsync(path, cancellationToken)` | 显式文件路径 | 原子替换 UTF-8 JSONL 快照，拒绝路径上的符号链接/重解析点，不自动导出原报告 |
| `GameDiagnostics.Analyze(entries, exitCode, stoppedByCaller, launchError, cancellationToken)` | → `GameDiagnosticReport` | 无需启动游戏也可分析已捕获的日志；不会联网、下载或执行修复 |

`LoggingOptions` 默认保留 2000 条，最多配置 10000 条；单条消息超过 16 Ki 字符时整行省略，避免截断令牌后泄露半段值。报告扫描默认最多 512 个候选路径（最大 4096），最多读取 8 份报告（最大 32），每份默认 1 MiB（可配置 1 KiB–16 MiB）。退出报告读取受内部 5 秒取消预算约束，无法完成时返回警告和已捕获日志，不阻止释放实例锁。

## 当前会话报告与历史排除

在进程创建前记录当前实例的文件名基线。退出后仅检查：

- 该游戏目录 `crash-reports/` 的直接 `.txt` 文件；
- 该游戏目录中与本次进程 ID 一致的 `hs_err_pid<PID>.log`。

所有启动前已存在的路径都排除，即使之后内容或修改时间发生变化。新路径还需满足会话时间范围；允许 2 秒文件系统时间粒度误差。新文件带旧时间、其他 PID 的 HotSpot 日志、其他实例目录和任意临时目录均不纳入。`CrashReportInfo.ProcessIdMatched` 只说明文件名中的 PID 匹配；普通 Minecraft 报告仍是本轮新增候选，不保证它一定由该进程写入。

基线无法完整读取、包含不安全路径或超过候选上限时，禁用本轮自动报告归属判断，避免把未扫描的历史报告当作新文件。文件数量/大小上限、访问失败和缓存丢弃会明确体现在 `Warnings`、`IsTruncated` 和 `DroppedEntries` 中。

大报告仅读取有界前缀，并省略截断处的整条不完整行；超长单行同样整行省略。原始游戏文件保持原样，不复制、覆盖或删除它们；原文件可能包含凭据，导出应优先使用 SDK 的脱敏快照。自定义 `-XX:ErrorFile` 写到其他位置时，本模块不扩大扫描范围。

## 诊断语义与证据

| `DiagnosticCode` | 可识别信号 | 保守边界 |
| --- | --- | --- |
| OutOfMemory | `java.lang.OutOfMemoryError`、JVM 明确的分配失败文本 | 不仅可能是 Java 堆；不自动增大 Xmx |
| JavaIncompatible | 不支持的类文件版本、或已有 Java 模块的明确兼容性错误码 | 按元数据选择 Java，不默认最新版本兼容 |
| MissingDependency | `ClassNotFoundException`、部分 `NoClassDefFoundError` | 类初始化失败并不自动归类为缺依赖；仍需查上游异常 |
| NativeLibraryError | `UnsatisfiedLinkError` 等明确加载失败 | 可能涉及架构、系统库或驱动，不直接判定某个 DLL 缺失 |
| Unknown | 没有足够的已知信号 | 保留脱敏原文与退出信息，交由调用方人工分析 |

这些是排查候选，不等于最终退出原因。退出码 `-1`、`1`、访问异常码乃至 `0` 本身不会触发原因猜测；正常退出同样不能证明游戏完成了全部初始化。主动停止通过 `StoppedByCaller` 单独标明，避免把启动器终止进程当成未知崩溃。

每个 finding 附有支持它的原始脱敏 `Evidence` 和排查建议；完整报告还保留当前缓存证据与结构化准备错误。宿主自己写入的 Launcher 文本不被当作 JVM 错误证据。诊断不会自动调用修复，完整性检查见[检查与修复](integrity.md)。

过滤覆盖已知启动访问令牌/旧 session、常见 URL 转义、ANSI 颜色片段，以及命名的 token、刷新令牌、Bearer/Xbox 授权、API Key、客户端秘密、密码和 JWT 形式。过滤发生在实时事件、缓存、诊断和导出之前；任意自行改码、加密或无标签且未知的秘密不能保证自动识别，宿主须提前传入敏感值并避免记录凭据。

JVM 错误类型与日志约定依据官方资料：[UnsupportedClassVersionError](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/UnsupportedClassVersionError.html)、[NoClassDefFoundError](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/NoClassDefFoundError.html)、[UnsatisfiedLinkError](https://docs.oracle.com/en/java/javase/25/docs/api/java.base/java/lang/UnsatisfiedLinkError.html)、[HotSpot 错误日志](https://www.oracle.com/java/technologies/javase/felog.html)。文件归属、保留窗口和诊断候选是 FMCore 的保守实现策略，并非官方对所有异常原因的保证。

T08 本地测试使用真实文件、真实测试子进程和合成的 JVM 文本；验证历史文件排除、跨实例范围、读取上限、当前输出、退出结果和敏感值过滤。未重新运行真实 Minecraft，未在 Linux/macOS 实机验证。

## 独立调用示例

- [写入脱敏日志](examples/logs-write.cs)
- [日志独立导出](examples/logs-save.cs)
- [诊断已有日志](examples/logs-analyze.cs)
- [容量、过滤与事件（高级）](examples/logs-advanced.cs)
