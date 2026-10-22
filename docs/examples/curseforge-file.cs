using FMCore.Resources;

var key = Environment.GetEnvironmentVariable("FMCORE_CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("请设置 FMCORE_CURSEFORGE_API_KEY。");
var fileId = int.Parse(Environment.GetEnvironmentVariable("FMCORE_FILE_ID") ?? throw new InvalidOperationException("请设置 FMCORE_FILE_ID。"));
using var client = new CurseForgeClient(key);
var file = await client.GetFileAsync(306612, fileId);
Console.WriteLine($"{file.Id}: {file.FileName}");
