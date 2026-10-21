# T01 下载管理

命名空间：`FMCore.Downloader`。核心没有新增第三方运行时依赖。

T06 补充 `new DownloadManager(root)` 默认网络入口和不联网的 `VerifyAsync`，原 HttpClient 构造与下载签名保留。最简/高级组合示例见 [P0 SDK](sdk.md)。

## 使用方式

| API | 主要参数 | 返回值 |
| --- | --- | --- |
| `new DownloadManager(root, options)` | 下载根目录；可选 `DownloadOptions` | 复用 SDK 网络客户端的下载器 |
| `new DownloadManager(http, root, options)` | 调用方持有的 HttpClient、同一根目录 | 使用自定义代理/网络策略的下载器 |
| `DownloadAsync(request, progress, ct)` | URL、相对路径、可选大小/哈希；可选进度和取消 | `DownloadResult`：目标、大小、是否跳过 |
| `DownloadManyAsync(requests, progress, ct)` | 请求集合，参数同单文件 | 按输入顺序的只读结果列表 |
| `VerifyAsync(request, ct)` | 与下载相同的请求元数据 | `FileVerificationResult`：Valid / Missing / SizeMismatch / HashMismatch / Unverified |

最简调用：`await new DownloadManager(root).DownloadAsync(request, cancellationToken: ct)`。`root` 是调用方选定的目录，`request` 必须来自可信来源；高级 HttpClient、并发和进度配置如下。所有任务方法的 `ct` 默认不取消，进度可省略；错误码与取消语义见文末。

调用方创建并复用 `HttpClient`，由调用方负责释放。建议一个下载根目录复用一个 `DownloadManager`；同一管理器的单文件和批量调用共享并发上限。

下面的方法展示单文件下载，`source`、`sha1`、`size` 应由可信元数据提供。下载管理器自身不获取 Minecraft 版本元数据。

```csharp
using FMCore.Downloader;

// 在应用启动时创建并复用；应用退出时再释放 http。
using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
var downloader = new DownloadManager(http, Path.Combine(AppContext.BaseDirectory, ".fmcore"),
    new DownloadOptions { MaxConcurrency = 8, MaxRetries = 3 });

Task<DownloadResult> DownloadClientAsync(Uri source, string sha1, long size, CancellationToken ct)
{
    var request = new DownloadRequest(source, "versions/example/client.jar")
    {
        ExpectedHash = sha1,
        HashAlgorithm = DownloadHashAlgorithm.Sha1,
        ExpectedSize = size
    };
    var progress = new Progress<DownloadProgress>(p =>
        Console.WriteLine($"{p.RelativePath}: {p.BytesReceived}/{p.TotalBytes} ({p.State})"));
    return downloader.DownloadAsync(request, progress, ct);
}
```

`DownloadManyAsync` 接受请求集合，按输入顺序返回结果。整个批次先检查参数和重复目标，再开始下载。批次失败时会取消仍在执行的工作，已完成文件保留；它不是整个安装流程的事务。

## 配置与进度

| 配置 | 默认值 | 含义 |
| --- | --- | --- |
| MaxConcurrency | 8 | 同一管理器的并发上限，允许 1–64 |
| MaxRetries | 3 | 首次请求之后的额外尝试次数，允许 0–10 |
| AttemptTimeout | 2 分钟 | 单次完整尝试的超时，覆盖响应头与响应体读取 |
| RetryDelay | 250 毫秒 | 指数退避的初始等待时间 |
| MaxRetryDelay | 30 秒 | 退避和 Retry-After 等待的上限，最多 5 分钟 |

`HttpClient.Timeout` 可以更早终止请求；建议设为无限，由 `AttemptTimeout` 统一控制。网络故障、超时、HTTP 408/429/5xx、大小或哈希不符会有限重试；其他 HTTP 状态和本地文件错误直接失败。不接受未请求的 HTTP 206 部分响应。

进度包括 Started、Downloading、Retrying、Completed、Skipped。Attempt 从 1 开始；重试时字节数重新计算；跳过文件的 Attempt 为 0。总大小未知时 TotalBytes 为 null。

Completed 在文件替换完成后报告。回调可能从不同工作线程并发调用，应保持简短、线程安全并避免抛异常；`Progress<T>` 自身的调度可能使回调晚于下载方法返回。

## 文件与安全边界

- RelativePath 必须位于构造时指定的下载根目录中；拒绝绝对路径、`..`、设备名称、备用数据流及其他不安全路径。
- 检查已有路径及祖先目录，拒绝符号链接和 Windows 目录联接。支持普通空格、中文文件名和子目录。
- 远程地址必须使用 HTTPS，HTTP 仅允许本机回环地址用于本地服务。URL 不允许内嵌用户名和密码；重定向后的最终地址也会检查。
- 请求对象的文本输出、进度及下载异常不包含来源 URL、响应正文或底层 HTTP 异常，避免暴露签名链接中的秘密。
- 使用同目录、独立命名的临时文件；校验和写入完成后才替换目标。失败、重试或取消时清理本次临时文件，不提前截断原文件。文件系统拒绝清理时返回文件错误，仍可能留有临时文件。
- ExpectedHash 支持 SHA-1、SHA-256、SHA-512；ExpectedSize 包括合法的 0 字节文件。提供两项时都必须匹配。
- 仅有大小时只能做大小检查，无法发现同大小的内容损坏，建议同时提供哈希。两项都未提供时，不根据“文件已存在”跳过下载。
- 同一管理器同时处理相同目标时返回 DestinationBusy；批次内重复目标在开始写入前被拒绝。不提供跨管理器、跨进程的目录锁。
- 本阶段重新尝试从头下载，不支持 HTTP Range 断点续传，也不解压文件。完整性检查与修复流程由后续 Installer 阶段实现。

路径检查面向普通下载操作，不宣称抵御另一个本地进程恶意竞态替换目录，也不提供断电后的文件系统事务保证。

## 错误处理

调用方取消通过 `OperationCanceledException` 传播，不按超时重试。下载业务失败使用 `DownloadException`，包含 Code、RelativePath、Attempt 和可选 StatusCode，不保留可能含 URL 的底层异常。

| Code | 含义 |
| --- | --- |
| InvalidRequest | 地址、选项、大小或哈希元数据无效 |
| UnsafePath | 目标路径不安全或经过链接 |
| DestinationBusy | 同一管理器正在处理该目标 |
| HttpError | HTTP 状态不符合下载要求 |
| NetworkError | 网络连接或响应体传输失败 |
| Timeout | 当前下载尝试超时 |
| HashMismatch | 内容哈希不符 |
| SizeMismatch | 大小不符或响应体不完整 |
| IoError | 本地文件访问、写入、替换或清理失败 |

## 验证范围

Windows / .NET 10 下共 64 项测试通过，无失败或跳过：

- Unit：三种哈希、进度、已有文件检查、错误重试、并发上限、目标冲突、超时、取消、原文件保护、临时文件清理。
- Security：路径穿越、设备路径、链接/目录联接、不安全地址、无效哈希、敏感 URL 过滤。
- Integration：通过本机 TCP/HTTP 服务测试真实的分块响应，以及连接中断后的完整重试。

模拟 Handler 与本机 HTTP 验证不等于公网 TLS、代理、Minecraft 服务或游戏安装验证。Linux/macOS 的行为尚未实际验证。

## 参考

- [Microsoft：ResponseHeadersRead 与内容读取超时](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpcompletionoption?view=net-10.0)
- [Microsoft：IncrementalHash](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.incrementalhash?view=net-10.0)
- [Microsoft：File.Move](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.move?view=net-10.0)
