using FMCore.Versions;

var version = await GameCoreUtil.GetGameCoreAsync("1.16.5", ".minecraft");
Console.WriteLine($"{version.Id}: {version.MainClass}, Java {version.JavaRequirement?.MajorVersion}");
