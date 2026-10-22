using FMCore.Modpacks;

var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var packs = new ModpackInstaller(root, id);
var project = Environment.GetEnvironmentVariable("FMCORE_PROJECT") ?? throw new InvalidOperationException("请设置 FMCORE_PROJECT。");
var result = await packs.UpdateModAsync(project, authorize: true);
Console.WriteLine(result.UpdateId);
