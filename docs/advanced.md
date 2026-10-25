# 异步、取消与错误处理

读取本地版本提供同步和异步两种入口，复用相同的元数据解析；Java 探测使用异步入口。普通调用可省略 CancellationToken，纯内存的离线身份和继承解析不需要 Async 后缀。

## 可取消的版本查询

```csharp
using System.Text.Json;
using FMCore.Versions;

using CancellationTokenSource canceled = new();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; canceled.Cancel(); };
try
{
    IReadOnlyList<MinecraftVersion> versions = await new MinecraftVersionReader()
        .GetVersionsAsync(args.FirstOrDefault(), canceled.Token);
    Console.WriteLine($"读取到 {versions.Count} 个版本。");
}
catch (OperationCanceledException) { Console.Error.WriteLine("已取消。"); }
catch (JsonException error) { Console.Error.WriteLine($"JSON 语法错误：{error.Message}"); }
catch (InvalidDataException error) { Console.Error.WriteLine($"版本数据错误：{error.Message}"); }
catch (UnauthorizedAccessException error) { Console.Error.WriteLine($"无权读取目录：{error.Message}"); }
catch (IOException error) { Console.Error.WriteLine($"文件读取失败：{error.Message}"); }
catch (ArgumentException error) { Console.Error.WriteLine($"参数错误：{error.Message}"); }
```

## 可取消的指定 Java 探测

```csharp
using FMCore.Java;

if (args.Length != 1)
    throw new ArgumentException("请提供 java.exe 路径。");
using CancellationTokenSource canceled = new(TimeSpan.FromSeconds(5));
try
{
    JavaInfo java = await JavaReader.GetJavaInfoAsync(args[0], canceled.Token);
    Console.WriteLine(java.Version);
}
catch (OperationCanceledException) { Console.Error.WriteLine("调用方取消了查询。"); }
catch (TimeoutException) { Console.Error.WriteLine("Java 自身探测超时。"); }
catch (InvalidDataException error) { Console.Error.WriteLine(error.Message); }
catch (IOException error) { Console.Error.WriteLine(error.Message); }
catch (System.ComponentModel.Win32Exception error) { Console.Error.WriteLine(error.Message); }
catch (InvalidOperationException error) { Console.Error.WriteLine(error.Message); }
```

SDK 对取消和自身 10 秒超时分别报告。取消或超时后终止探测进程，释放进程和重定向流；不因取消而尝试其他 Java。查询结果直接返回，不需要订阅事件。

当前没有下载进度、游戏日志事件或账号令牌。后续增加这些功能时必须同步提供中文手册和独立示例，不把规划中的 API 当成可用接口。
