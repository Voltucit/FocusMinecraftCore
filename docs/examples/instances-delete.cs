using FMCore.Instances;

var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
await new InstanceManager(root).DeleteAsync(id);
