using FMCore.Instances;

var instances = await new InstanceManager(".minecraft").GetInstancesAsync();
foreach (var instance in instances) Console.WriteLine(instance.Id);
