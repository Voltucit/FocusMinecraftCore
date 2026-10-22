using FMCore.Resources;

var key = Environment.GetEnvironmentVariable("FMCORE_CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("请设置 FMCORE_CURSEFORGE_API_KEY。");
using var client = new CurseForgeClient(key);
var result = await client.GetFilesAsync(306612, new("1.20.4", "fabric"));
Console.WriteLine(result);
