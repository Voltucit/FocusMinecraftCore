using FMCore.Loaders;

var installed = await new FabricInstaller(".minecraft").InstallAsync("1.16.5");
Console.WriteLine(installed.VersionId);
