using FMCore.Loaders;

var installed = await new NeoForgeInstaller(".minecraft").InstallAsync("1.20.4");
Console.WriteLine(installed.VersionId);
