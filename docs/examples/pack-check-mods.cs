using FMCore.Modpacks;

var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var packs = new ModpackInstaller(root, id);
var updates = await packs.CheckModUpdatesAsync();
foreach (var update in updates) Console.WriteLine($"{update.Installed.ProjectId}: {update.VersionNumber}, 锁定 {update.IsLocked}");
