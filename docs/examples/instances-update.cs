using FMCore.Instances;

var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var instance = await new InstanceManager(root).GetAsync(id);
var updated = await new InstanceManager(root).UpdateAsync(instance, instance.Config with { Name = "新的名称" });
Console.WriteLine(updated.Revision);
