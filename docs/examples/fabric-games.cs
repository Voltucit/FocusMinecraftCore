using FMCore.Loaders;

var games = await new FabricInstaller(".minecraft").GetGameVersionsAsync();
foreach (var game in games) Console.WriteLine(game.Version);
