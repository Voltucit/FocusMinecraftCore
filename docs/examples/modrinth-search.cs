using FMCore.Resources;

var client = new ModrinthClient();
var result = await client.SearchAsync("fabric api", new() { MinecraftVersion = "1.20.4", Loader = "fabric" });
Console.WriteLine(result);
