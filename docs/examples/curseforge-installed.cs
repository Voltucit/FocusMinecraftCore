using FMCore.Resources;
using FMCore.Instances;

var key = Environment.GetEnvironmentVariable("FMCORE_CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("请设置 FMCORE_CURSEFORGE_API_KEY。");
using var client = new CurseForgeClient(key);
var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var instance = await new InstanceManager(root).GetAsync(id);
var mods = new CurseForgeInstaller(instance, client);
var result = await mods.GetInstalledAsync();
Console.WriteLine(result);
