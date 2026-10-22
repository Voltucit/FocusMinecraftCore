using FMCore.Logging;

var logger = new LauncherLogger();
logger.Write(LogLevel.Information, "启动准备中");
foreach (var entry in logger.GetEntries()) Console.WriteLine(entry.Message);
