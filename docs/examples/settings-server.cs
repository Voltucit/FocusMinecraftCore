using FMCore.Launch;

var config = new LaunchConfig { Server = new() { Host = "play.example.com", Port = 25565 } };
Console.WriteLine(config);
