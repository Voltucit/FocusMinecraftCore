using FMCore.Resources;
using FMCore.Instances;

var client = new ModrinthClient();
var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var instance = await new InstanceManager(root).GetAsync(id);
var selected = Environment.GetEnvironmentVariable("FMCORE_RESOURCE_VERSION") ?? throw new InvalidOperationException("请设置 FMCORE_RESOURCE_VERSION。");
var mods = new ModrinthInstaller(instance, client);
var result = await mods.InstallAsync("fabric-api", selected);
Console.WriteLine(result.Reused);
