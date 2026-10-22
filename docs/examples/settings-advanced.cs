using FMCore.Authentication;
using FMCore.Java;
using FMCore.Launch;

using var accounts = new AccountManager(".fmcore/accounts");
var path = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请设置 FMCORE_JAVA。");
var java = await JavaUtil.GetJavaInfoAsync(path);
var identity = await accounts.LoginOfflineAsync("Player");
var basic = LaunchConfig.Create("1.16.5", ".minecraft", identity, java, maxMemoryMb: 2048);
var custom = basic with { Window = new() { Width = 1280, Height = 720 }, Java = basic.Java with { Arguments = ["-Dfile.encoding=UTF-8"] } };
await using var launcher = new MinecraftLauncher(custom);
var result = await launcher.LaunchAsync();
if (result.Success) await launcher.WaitForExitAsync();
else Console.WriteLine(result.Error);
