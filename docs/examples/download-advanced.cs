using FMCore.Downloader;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var url = Environment.GetEnvironmentVariable("FMCORE_URL") ?? throw new InvalidOperationException("请设置 FMCORE_URL。");
var hash = Environment.GetEnvironmentVariable("FMCORE_SHA256") ?? throw new InvalidOperationException("请设置 FMCORE_SHA256。");
var downloads = new DownloadManager("downloads", new() { MaxConcurrency = 4, MaxRetries = 2 });
var request = new DownloadRequest(new Uri(url), "file.bin") { ExpectedHash = hash, HashAlgorithm = DownloadHashAlgorithm.Sha256 };
var progress = new Progress<DownloadProgress>(p => Console.WriteLine($"{p.State}: {p.BytesReceived}/{p.TotalBytes}"));
try { var result = await downloads.DownloadAsync(request, progress, ct); Console.WriteLine(result.FullPath); }
catch (OperationCanceledException) { Console.WriteLine("已取消。"); }
catch (DownloadException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
