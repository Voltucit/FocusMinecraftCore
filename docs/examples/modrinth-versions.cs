using FMCore.Resources;

var client = new ModrinthClient();
var result = await client.GetVersionsAsync("fabric-api", new("1.20.4", "fabric"));
Console.WriteLine(result);
