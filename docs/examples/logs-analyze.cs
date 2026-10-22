using FMCore.Logging;

var logger = new LauncherLogger();
var path = Environment.GetEnvironmentVariable("FMCORE_LOG") ?? throw new InvalidOperationException("请设置 FMCORE_LOG。");
using var reader = File.OpenText(path);
while (await reader.ReadLineAsync() is { } line) logger.Write(LogLevel.Information, line, LogSource.StandardError);
var report = GameDiagnostics.Analyze(logger.GetEntries());
Console.WriteLine(report.Summary);
