using FMCore.Versions;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
try { var catalog = await GameCoreUtil.GetAvailableVersionsAsync(".minecraft", VersionCachePolicy.Refresh, ct); Console.WriteLine(catalog.LatestRelease); }
catch (OperationCanceledException) { Console.WriteLine("已取消。"); }
catch (VersionException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
