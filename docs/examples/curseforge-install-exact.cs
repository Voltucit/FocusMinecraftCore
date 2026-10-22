using FMCore.Resources;
using FMCore.Instances;

var key = Environment.GetEnvironmentVariable("FMCORE_CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("请设置 FMCORE_CURSEFORGE_API_KEY。");
using var client = new CurseForgeClient(key);
var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var instance = await new InstanceManager(root).GetAsync(id);
var selected = int.Parse(Environment.GetEnvironmentVariable("FMCORE_FILE_ID") ?? throw new InvalidOperationException("请设置 FMCORE_FILE_ID。"));
var mods = new CurseForgeInstaller(instance, client);
var result = await mods.InstallAsync(306612, selected);
Console.WriteLine(result.Reused);
