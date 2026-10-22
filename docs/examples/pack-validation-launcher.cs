using FMCore.Modpacks;
using FMCore.Authentication;
using FMCore.Launch;

var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var packs = new ModpackInstaller(root, id);
using var accounts = new AccountManager(".fmcore/accounts");
var identity = await accounts.LoginOfflineAsync("Player");
await using var launcher = await packs.CreateValidationLauncherAsync(identity);
var result = await launcher.LaunchAsync();
if (result.Success) await launcher.WaitForExitAsync();
else Console.WriteLine(result.Error);
