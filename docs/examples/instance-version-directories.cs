using FMCore.Instances;

// 由启动器明确选择根目录；请在新的测试根目录中运行，同名实例不会被覆盖。
var root = ".minecraft";
var manager = new InstanceManager(root);
var standard = await manager.CreateAsync("默认实例", "1.20.4");
var survival = await manager.CreateAsync(
    new InstanceConfig { Name = "生存实例", VersionId = "1.20.4" }, "Survival");
var creative = await manager.CreateAsync(
    new InstanceConfig { Name = "创造实例", VersionId = "1.20.4" }, "Creative");

Console.WriteLine(standard.GameDirectory); // <root>/versions/1.20.4
Console.WriteLine(survival.GameDirectory); // <root>/versions/Survival
Console.WriteLine(creative.GameDirectory); // <root>/versions/Creative
Console.WriteLine(survival.Config.VersionId); // 启动版本仍是 1.20.4
