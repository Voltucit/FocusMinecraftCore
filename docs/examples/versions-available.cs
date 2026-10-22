using FMCore.Versions;

var catalog = await GameCoreUtil.GetAvailableVersionsAsync();
foreach (var v in catalog.Versions) Console.WriteLine($"{v.Id} {v.Type}");
