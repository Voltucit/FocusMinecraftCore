using FMCore.Launch;

var config = new LaunchConfig { Java = new() { MinMemoryMb = 0, MaxMemoryMb = 2048, Arguments = ["-Dfile.encoding=UTF-8"] } };
Console.WriteLine(config);
