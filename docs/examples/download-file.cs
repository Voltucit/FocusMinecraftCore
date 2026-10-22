using FMCore.Downloader;

var url = Environment.GetEnvironmentVariable("FMCORE_URL") ?? throw new InvalidOperationException("请设置 FMCORE_URL。");
var result = await DownloadManager.DownloadFileAsync(url, "downloads/file.bin");
Console.WriteLine(result.FullPath);
