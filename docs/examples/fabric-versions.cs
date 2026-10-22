using FMCore.Loaders;

var versions = await new FabricInstaller(".minecraft").GetLoaderVersionsAsync("1.16.5");
foreach (var v in versions) Console.WriteLine(v);
