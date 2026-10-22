using FMCore.Modpacks;

var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var packs = new ModpackInstaller(root, id);
var updateId = Guid.Parse(Environment.GetEnvironmentVariable("FMCORE_UPDATE_ID") ?? throw new InvalidOperationException("请设置 FMCORE_UPDATE_ID。"));
await packs.RollbackAsync(updateId);
