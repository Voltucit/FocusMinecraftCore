using FMCore.Modpacks;

var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var packs = new ModpackInstaller(root, id);
var archive = Environment.GetEnvironmentVariable("FMCORE_ARCHIVE") ?? throw new InvalidOperationException("请设置 FMCORE_ARCHIVE。");
var result = await packs.InstallAsync(archive);
Console.WriteLine(result);
