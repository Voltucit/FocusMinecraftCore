using FMCore.Versions;

var versions = await GameCoreUtil.GetGameCoresAsync(".minecraft");
foreach (var v in versions) Console.WriteLine($"{v.Id}: {v.ErrorCode}");
