using FMCore.Installer;

var installer = new GameInstaller(".minecraft");
var version = await new FMCore.Versions.VersionManager(new FMCore.Downloader.DownloadManager(".minecraft")).ResolveAsync("1.16.5");
var plan = installer.CreatePlan(version);
Console.WriteLine(plan.Client.RelativePath);
