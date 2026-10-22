using FMCore.Downloader;
using FMCore.Versions;

var manager = new VersionManager(new DownloadManager(".minecraft"));
var result = await manager.ResolveAsync("1.16.5");
Console.WriteLine(result);
