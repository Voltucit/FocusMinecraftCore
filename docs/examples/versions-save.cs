using FMCore.Downloader;
using FMCore.Versions;

var path = Environment.GetEnvironmentVariable("FMCORE_PROFILE") ?? throw new InvalidOperationException("请设置 FMCORE_PROFILE。");
var json = await File.ReadAllTextAsync(path);
await new VersionManager(new DownloadManager(".minecraft")).SaveLocalAsync(VersionParser.Parse(json));
