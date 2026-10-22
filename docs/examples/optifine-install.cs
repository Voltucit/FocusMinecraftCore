using FMCore.Loaders;

var installed = await new OptiFineInstaller(".minecraft").InstallAsync("1.16.5");
Console.WriteLine(installed.VersionId);
