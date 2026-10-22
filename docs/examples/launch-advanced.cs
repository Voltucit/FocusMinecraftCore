using FMCore.Authentication;
using FMCore.Java;
using FMCore.Launch;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
using var accounts = new AccountManager(".fmcore/accounts");
var path = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请设置 FMCORE_JAVA。");
var java = await JavaUtil.GetJavaInfoAsync(path);
var identity = await accounts.LoginOfflineAsync("Player");
var config = LaunchConfig.Create("1.16.5", ".minecraft", identity, java, maxMemoryMb: 2048);
await using var launcher = new MinecraftLauncher(config);
launcher.StateChanged += (_, e) => Console.WriteLine(e.State);
launcher.OutputReceived += (_, e) => Console.WriteLine(e.Text);
launcher.ErrorReceived += (_, e) => Console.Error.WriteLine(e.Text);
launcher.Exited += (_, e) => Console.WriteLine($"退出码 {e.Result.ExitCode}");
var progress = new Progress<LaunchProgress>(p => Console.WriteLine(p.Stage));
try
{
    var result = await launcher.LaunchAsync(progress, ct);
    if (!result.Success) { Console.WriteLine(result.Error); return; }
    await launcher.WaitForExitAsync(ct);
}
catch (OperationCanceledException) { await launcher.StopAsync(); }
finally { await launcher.Logger.SaveAsync("game.jsonl"); }
