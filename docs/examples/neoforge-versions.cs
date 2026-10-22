using FMCore.Loaders;

var versions = await new NeoForgeInstaller(".minecraft").GetVersionsAsync("1.20.4");
foreach (var v in versions) Console.WriteLine(v);
