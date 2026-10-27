using System.Globalization;
using FMCore.Accounts;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

if (args.Length is < 4 or > 6)
{
    Console.Error.WriteLine(
        "用法：ShowLaunchConfig <版本ID> <java.exe路径> <用户名> <内存MB> [Minecraft根目录] [工作目录]");
    return 2;
}

MinecraftVersion version = new MinecraftVersionReader()
    .GetVersion(args[0], args.ElementAtOrDefault(4));

JavaInfo java = await JavaReader.GetJavaInfoAsync(args[1]);
OfflineAccount account = OfflineAccount.Create(args[2]);

string minecraftRoot = args.ElementAtOrDefault(4)
                       ?? Directory.GetParent(version.Directory)?.Parent?.FullName
                       ?? throw new InvalidOperationException("无法确定 Minecraft 根目录");

LaunchConfig config = new()
{
    Account = account,
    GameCoreConfig = new GameCoreConfig
    {
        Root = minecraftRoot,
        Version = version.Id,
        GameDirectory = args.ElementAtOrDefault(5)
    },
    JavaConfig = new JavaConfig
    {
        JavaPath = java.Executable,
        MaxMemoryMb = int.Parse(
            args[3],
            CultureInfo.InvariantCulture)
    }
};

Console.WriteLine(
    $"启动版本：{config.GameCoreConfig.Version}\n" +
    $"Java：{config.JavaConfig.JavaPath}\n" +
    $"用户名：{config.Account.Username}\n" +
    $"最大内存 MB：{config.JavaConfig.MaxMemoryMb}\n" +
    $"游戏目录：{config.GameCoreConfig.GameDirectory ?? config.GameCoreConfig.Root}");

return 0;
