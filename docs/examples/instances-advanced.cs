using FMCore.Instances;

var path = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请设置 FMCORE_JAVA。");
var instance = await new InstanceManager(".minecraft").CreateAsync(new InstanceConfig { Name = "自定义实例", VersionId = "1.16.5", JavaPath = path, MinMemoryMb = 0, MaxMemoryMb = 2048, Window = new() { Width = 1280, Height = 720 } });
Console.WriteLine(instance.Id);
