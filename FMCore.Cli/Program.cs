namespace FMCore.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"])
        {
            Console.WriteLine("FMCore - Minecraft 启动核心命令行入口");
            Console.WriteLine("用法: FMCore.Cli [--help | --version]");
            Console.WriteLine("下载、版本、Java、安装、账号及原版启动可通过 SDK 使用，CLI 游戏命令尚未实现。");
            return 0;
        }

        if (args is ["--version"])
        {
            Console.WriteLine(typeof(FmCore).Assembly.GetName().Version?.ToString());
            return 0;
        }

        Console.Error.WriteLine("暂不支持此命令，请使用 --help 查看帮助。");
        return 2;
    }
}
