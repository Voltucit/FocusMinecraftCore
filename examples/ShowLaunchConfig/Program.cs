using System.Globalization;
using FMCore.Accounts;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

if (args.Length is < 4 or > 6)
{
    Console.Error.WriteLine("用法：ShowLaunchConfig <版本ID> <java.exe路径> <用户名> <内存MB> [Minecraft根目录] [工作目录]");
    return 2;
}
MinecraftVersion version = new MinecraftVersionReader().GetVersion(args[0], args.ElementAtOrDefault(4));
JavaInfo java = await JavaReader.GetJavaInfoAsync(args[1]);
OfflineAccount account = OfflineAccount.Create(args[2]);
LaunchConfig config = new(version, java, account, int.Parse(args[3], CultureInfo.InvariantCulture),
    args.ElementAtOrDefault(5));
Console.WriteLine($"启动版本：{config.Version.Id}\nJava：{config.Java.Executable}\n用户名：{config.Account.Username}\n最大内存 MB：{config.MaxMemoryMb}\n游戏目录：{config.GameDirectory}");
return 0;
