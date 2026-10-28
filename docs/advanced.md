# 显示进度、取消操作和处理错误

最短示例可以直接使用。要做进度条、取消按钮或错误提示，再加下面这些内容。

## 显示进度

大多数安装和补全方法的第一个可选参数，是一个接收进度的方法：

```csharp
await installer.InstallAsync(progress => Console.WriteLine(progress.Message));
```

这里的 `installer` 是已创建的安装器。`progress => ...` 表示“收到新进度时执行这段代码”。`Message` 是文字说明；下载百分比等字段见对应功能页。

进度可能在后台传来。WPF 程序要用窗口的 `Dispatcher` 更新控件，例如已有文本控件 `StatusText` 时：

```csharp
await installer.InstallAsync(progress =>
    Dispatcher.Invoke(() => StatusText.Text = progress.Message));
```

传入的方法应及时返回。它自己报错时，本次操作也会失败。处理多批下载时，每批百分比会重新计算，100% 不能代替整个方法成功返回。

## 接上取消按钮

操作开始前创建取消开关，传给正在执行的方法：

```csharp
using CancellationTokenSource cancel = new();
await installer.InstallAsync(cancellationToken: cancel.Token);
```

把 `cancel` 保存在按钮能够访问的位置，用户点击取消时调用 `cancel.Cancel()`。同一个开关可以依次用于“补全文件”和“启动游戏”。

`await` 表示等待结果，不会让正确使用的界面按钮一直卡住。`MinecraftLauncher.LaunchAsync()` 已在内部安排准备工作，不需要再加 `Task.Run`。

游戏运行中取消会强制关闭本次游戏，不能代替游戏里的保存退出。下载取消后，已完成的正式文件会保留，本次没完成的临时文件会清理。

## 显示错误

已有安装器和上面的取消开关时：

```csharp
try
{
    await installer.InstallAsync(cancellationToken: cancel.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("已取消。");
}
catch (Exception error)
{
    Console.WriteLine(error.Message);
}
```

界面程序把输出换成自己的错误提示即可。不要出错后继续执行启动，也不要输出密码或登录凭据。

`TimeoutException` 表示等得太久；`AggregateException` 表示有多个问题，例如下载出错的同时，清理文件也失败。需要详细日志时查看其 `InnerExceptions`。

## 几个容易误会的地方

- `LaunchAsync()` 返回的是游戏退出码，通常要等到游戏关闭后才得到它。
- `JavaStarted` 只说明 Java 已运行，游戏可能还没加载完成。
- 游戏的错误输出里也可能有普通提示，不能见到 `onError` 就显示“崩溃”。
- 同一个安装器或启动器对象不要同时执行多次操作，等上一次结束后再用。
- 下载速度和剩余时间是估算，网络变化后可能变慢，文件个数也不等于文件大小。
- 下载失败后保留已完成文件；再次执行可继续使用，不会回滚整个安装。

路径、支持范围和各方法的返回内容见[文档目录](README.md)，完整参数见[接口参考](public-api.md)。
