using FMCore.Loaders;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var options = new FabricInstallOptions();
var progress = new Progress<FabricInstallationProgress>(p => Console.WriteLine(p.Stage));
try { var result = await new FabricInstaller(".minecraft").InstallAsync("1.16.5", options: options, progress: progress, cancellationToken: ct); Console.WriteLine(result.VersionId); }
catch (OperationCanceledException) { Console.WriteLine("已取消。"); }
catch (LoaderException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
