using FMCore.Instances;

var instance = await new InstanceManager(".minecraft").CreateAsync("我的实例", "1.16.5");
Console.WriteLine(instance.Id);
