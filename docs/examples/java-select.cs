using FMCore.Java;
using FMCore.Versions;
using FMCore.Downloader;

var path = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请设置 FMCORE_JAVA。");
var version = await new VersionManager(new DownloadManager(".minecraft")).ResolveAsync("1.16.5");
var java = await new JavaManager().SelectAsync(version.Metadata, customPath: path);
Console.WriteLine(java.ExecutablePath);
