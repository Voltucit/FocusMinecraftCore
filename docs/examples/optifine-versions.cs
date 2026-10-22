using FMCore.Loaders;

var versions = await new OptiFineInstaller(".minecraft").GetVersionsAsync("1.16.5");
foreach (var v in versions) Console.WriteLine(v);
