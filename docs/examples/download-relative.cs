using FMCore.Downloader;

var url = Environment.GetEnvironmentVariable("FMCORE_URL") ?? throw new InvalidOperationException("请设置 FMCORE_URL。");
var result = await new DownloadManager("downloads").DownloadAsync(url, "file.bin");
Console.WriteLine(result.FullPath);
