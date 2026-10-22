using FMCore.Installer;

var result = await new GameInstaller(".minecraft").RepairAsync("1.16.5");
Console.WriteLine(result);
