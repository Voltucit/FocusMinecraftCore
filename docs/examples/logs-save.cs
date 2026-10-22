using FMCore.Logging;

var logger = new LauncherLogger();
logger.Write(LogLevel.Information, "启动器已就绪");
await logger.SaveAsync("launcher.jsonl");
