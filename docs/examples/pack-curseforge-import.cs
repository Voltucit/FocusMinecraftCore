using FMCore.Modpacks;
using FMCore.Resources;

var key = Environment.GetEnvironmentVariable("FMCORE_CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("请设置 FMCORE_CURSEFORGE_API_KEY。");
using var client = new CurseForgeClient(key);
var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var archive = Environment.GetEnvironmentVariable("FMCORE_ARCHIVE") ?? throw new InvalidOperationException("请设置 FMCORE_ARCHIVE。");
var packs = new ModpackInstaller(root, id, new() { CurseForge = client });
var result = await packs.ImportAsync(archive);
Console.WriteLine(result.UpdateId);
