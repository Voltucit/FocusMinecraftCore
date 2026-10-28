# 下载普通文件

用 `FMCore.Downloads` 下载已知地址的文件，并检查大小和 SHA-1。SHA-1 是文件的校验值，可以用来检查内容是否与发布方提供的一致。

安装游戏用[安装入口](installation.md)，补全游戏用[补全入口](game-files.md)。它们已经包含下载步骤，无需自己拆成数千个请求。

## 下载一个文件

下面的日志配置提供了真实的下载地址、大小和校验值：

```csharp
using FMCore.Downloads;

DownloadRequest request = new()
{
    Url = "https://piston-data.mojang.com/v1/objects/39384bd14c0606d812afec88d8aff595b2587dd9/client-1.21.2.xml",
    FilePath = ".minecraft/assets/log_configs/client-1.21.2.xml",
    Size = 1073,
    Sha1 = "39384bd14c0606d812afec88d8aff595b2587dd9"
};

var result = await FileDownloader.DownloadAsync(request);
```

这四项是必填项。`FilePath` 由启动器选择；创建请求不会下载，调用 `DownloadAsync()` 才会执行。文件已存在且检查通过时直接使用，`result.AlreadyPresent` 为 `true`。

## 显示下载速度和进度

```csharp
var result = await FileDownloader.DownloadAsync(request, progress =>
{
    Console.WriteLine($"{progress.Percentage:F1}%");
    Console.WriteLine($"{progress.BytesPerSecond / 1024 / 1024:F2} MiB/s");
});
```

`RemainingTime` 是预计剩余时间，无法估算时为空。速度按最近约 5 秒计算；网速变化时，剩余时间也会变化。100% 后还可能需要检查内容、保存正式文件，应等方法成功返回。

## 同时下载多个文件

已有一组 `DownloadRequest`，名为 `requests` 时：

```csharp
var results = await FileDownloader.DownloadManyAsync(requests);
```

默认同时处理 4 个文件。要显示总体进度：

```csharp
var results = await FileDownloader.DownloadManyAsync(
    requests,
    onProgress: progress => Console.WriteLine(
        $"文件 {progress.CompletedFiles}/{progress.TotalFiles}，{progress.Percentage:F1}%"));
```

百分比按文件总字节计算，文件数按个数计算，两者不一定一起增长。已有文件的检查、连接等待和处理最后几个大文件时，也可能出现低速或估算变化。

## 按需调整

| `DownloadRequest` 设置 | 默认值 | 作用 |
| --- | --- | --- |
| `Timeout` | 5 分钟 | 这一个文件的下载、重试等待及检查共用的最长时间 |
| `MaxRetries` | 2 | 网络暂时出错后，额外尝试多少次；可设 0 到 10 |
| `RetryDelay` | 1 秒 | 重试前的基本等待时间；可设 0 到 30 秒 |
| `ReplaceInvalidFiles` | `false` | 设为 `true` 时，保留旧文件备份后换成检查通过的新文件 |

一般不需要修改。大小不符、校验值不符、磁盘错误或你提供的方法出错，不会被当作普通断网不断重试。

默认遇到已存在的坏文件会保留它并报错。开启替换后，成功返回的 `result.BackupPath` 是备份位置。取消或失败时清理本次临时下载；已完成的正式文件和已有备份保留。

多个文件中有一个失败时，会停止其余操作并等待清理结束，再报告错误。取消方式见[通用说明](advanced.md)。

## 安装与补全的下载源

安装器和补全器通过 `Source` 选择下载源。普通 `DownloadRequest.Url` 则使用你明确填写的地址，需要时可先调用 `source.GetUrl(originalUrl)` 转换。完整用法见[下载源](download-sources.md)。

安装与补全下载遇到网络或 HTTP 错误时，可以在原有重试次数和时间内尝试原始地址；不会因此接受不同的文件内容。下载说明查询失败时不保证自动换源。

完整进度字段、方法参数和错误类型见[接口参考](public-api.md#fmcoredownloads)。
