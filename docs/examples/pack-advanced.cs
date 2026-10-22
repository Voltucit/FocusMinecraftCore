using FMCore.Modpacks;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var archive = Environment.GetEnvironmentVariable("FMCORE_ARCHIVE") ?? throw new InvalidOperationException("请设置 FMCORE_ARCHIVE。");
var packs = new ModpackInstaller(root, id, new() { PreserveModifiedConfigurations = true, AutoCleanupAfterConfirmation = false });
var progress = new Progress<ModpackProgress>(p => Console.WriteLine($"{p.Stage}: {p.CompletedFiles}/{p.TotalFiles}"));
try { var result = await packs.UpdateAsync(archive, progress, ct); Console.WriteLine(result.UpdateId); }
catch (OperationCanceledException) { Console.WriteLine("已取消，保留恢复所需备份。"); }
catch (ModpackException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
