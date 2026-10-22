using FMCore.Downloader;

var hash = Environment.GetEnvironmentVariable("FMCORE_SHA256") ?? throw new InvalidOperationException("请设置 FMCORE_SHA256。");
var result = await new DownloadManager("downloads").VerifyAsync(new(new Uri("https://example.com/file.bin"), "file.bin") { ExpectedHash = hash, HashAlgorithm = DownloadHashAlgorithm.Sha256 });
Console.WriteLine(result.State);
