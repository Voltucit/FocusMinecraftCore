using FMCore.Authentication;
using FMCore.Instances;
using FMCore.Launch;

using var accounts = new AccountManager(".fmcore/accounts");
var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var instance = await new InstanceManager(root).GetAsync(id);
var identity = await accounts.LoginOfflineAsync("Player");
await using var launcher = new MinecraftLauncher(instance.CreateLaunchConfig(identity));
var result = await launcher.LaunchAsync();
if (result.Success) await launcher.WaitForExitAsync();
else Console.WriteLine(result.Error);
