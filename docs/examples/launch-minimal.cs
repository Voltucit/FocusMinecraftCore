using FMCore.Authentication;
using FMCore.Java;
using FMCore.Launch;

using var accounts = new AccountManager(".fmcore/accounts");
var path = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请设置 FMCORE_JAVA。");
var java = await JavaUtil.GetJavaInfoAsync(path);
var identity = await accounts.LoginOfflineAsync("Player");
var config = LaunchConfig.Create("1.16.5", ".minecraft", identity, java, maxMemoryMb: 2048);
await using var launcher = new MinecraftLauncher(config);
var result = await launcher.LaunchAsync();
if (!result.Success) { Console.WriteLine(result.Error); return; }
var exit = await launcher.WaitForExitAsync();
Console.WriteLine(exit.ExitCode);
