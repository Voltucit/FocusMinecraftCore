using FMCore.Loaders;

var versions = await new ForgeInstaller(".minecraft").GetVersionsAsync("1.16.5");
foreach (var v in versions) Console.WriteLine(v);
