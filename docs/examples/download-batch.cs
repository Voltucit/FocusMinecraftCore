using FMCore.Downloader;

var url = Environment.GetEnvironmentVariable("FMCORE_URL") ?? throw new InvalidOperationException("请设置 FMCORE_URL。");
var result = await new DownloadManager("downloads").DownloadManyAsync([new(new Uri(url), "first.bin"), new(new Uri(url), "second.bin")]);
Console.WriteLine(result.Count);
