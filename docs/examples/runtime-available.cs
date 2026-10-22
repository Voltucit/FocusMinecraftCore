using FMCore.Java;

var versions = await JavaRuntimeUtil.GetAvailableVersionsAsync(21);
foreach (var v in versions) Console.WriteLine(v);
