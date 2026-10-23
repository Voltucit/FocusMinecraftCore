using FMCore.Modpacks;

var root = Environment.GetEnvironmentVariable("FMCORE_ROOT") ?? ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE 为已有实例目录名。");
await new ModpackInstaller(root, id).ExportAsync("instance.zip");
