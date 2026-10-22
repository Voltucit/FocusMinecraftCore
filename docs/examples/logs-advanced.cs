using FMCore.Logging;

var logger = new LauncherLogger(new() { MaxEntries = 100 }, sensitiveValues: ["仅演示秘密"]);
logger.EntryReceived += (_, e) => Console.WriteLine(e.Message);
logger.Write(LogLevel.Information, "演示：仅演示秘密");
Console.WriteLine(logger.DroppedEntries);
