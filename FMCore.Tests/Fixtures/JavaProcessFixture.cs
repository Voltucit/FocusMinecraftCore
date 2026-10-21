using System.Diagnostics;

namespace FMCore.Tests.Fixtures;

/// <summary>Child-process fixture for pipe/timeout tests. Normal execution delegates to the xUnit runner.</summary>
internal static class JavaProcessFixture
{
    public static async Task<int> Main(string[] args)
    {
        if (args is not ["--java-process-fixture", var mode, ..])
            return await Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args);
        switch (mode)
        {
            case "streams":
                // Exceeds a pipe buffer on both streams; sequential ReadToEnd would deadlock.
                for (var i = 0; i < 100; i++)
                {
                    Console.Out.Write(new string('O', 1024));
                    Console.Error.Write(new string('E', 1024));
                }
                return 0;
            case "exit":
                Console.Error.Write("synthetic-sensitive-value");
                return 23;
            case "unicode":
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.Write("Java 目录/运行时");
                return 0;
            case "hang":
                await File.WriteAllTextAsync(args[2], Environment.ProcessId.ToString(), CancellationToken.None);
                await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
                return 0;
            case "game":
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.WriteLine("fixture-started");
                if (args[2] == "tree")
                {
                    var childStart = Start("game", "hold");
                    childStart.UseShellExecute = false;
                    childStart.CreateNoWindow = true;
                    using var child = Process.Start(childStart)!;
                    Console.WriteLine("fixture-child:" + child.Id);
                    await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
                }
                if (args[2] == "hold") await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
                if (args[2] == "flood")
                {
                    for (var i = 0; i < 80; i++)
                    {
                        Console.Out.WriteLine(new string('O', 2048));
                        Console.Error.WriteLine(new string('E', 2048));
                    }
                    Console.Out.WriteLine(new string('X', 20000));
                }
                foreach (var argument in args.Skip(3))
                {
                    Console.Out.WriteLine(argument);
                    Console.Error.WriteLine(argument);
                }
                Console.Out.Write("fixture-final-line");
                return args[2] == "exit" ? 23 : 0;
            default:
                return 2;
        }
    }

    internal static ProcessStartInfo Start(string mode, params string[] extra)
    {
        var executable = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "FMCore.Tests.exe" : "FMCore.Tests");
        var start = new ProcessStartInfo(executable);
        start.ArgumentList.Add("--java-process-fixture");
        start.ArgumentList.Add(mode);
        foreach (var argument in extra) start.ArgumentList.Add(argument);
        return start;
    }
}
