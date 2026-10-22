using FMCore.Resources;

var client = new ModrinthClient();
var project = await client.GetProjectAsync("fabric-api");
Console.WriteLine(project);
