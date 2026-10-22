using FMCore.Java;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var options = new JavaRuntimeOptions { Distribution = JavaDistribution.Zulu, Architecture = JavaArchitecture.X64, InstallationDirectory = "runtimes", QueryTimeout = TimeSpan.FromSeconds(30) };
var progress = new Progress<JavaRuntimeProgress>(p => Console.WriteLine($"{p.Stage}: {p.CompletedFiles}/{p.TotalFiles}, {p.Download?.BytesReceived}"));
try { var java = await JavaRuntimeUtil.InstallAsync(21, options, progress, ct); Console.WriteLine(java.ExecutablePath); }
catch (OperationCanceledException) { Console.WriteLine("已取消。"); }
catch (JavaException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
