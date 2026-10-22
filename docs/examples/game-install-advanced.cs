using FMCore.Installer;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var progress = new Progress<InstallationProgress>(p => Console.WriteLine($"{p.Stage}: {p.CompletedFiles}/{p.TotalFiles}"));
try { await new GameInstaller(".minecraft").InstallAsync("1.16.5", progress: progress, cancellationToken: ct); }
catch (OperationCanceledException) { Console.WriteLine("已取消，有效文件可复用。"); }
catch (InstallationException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
