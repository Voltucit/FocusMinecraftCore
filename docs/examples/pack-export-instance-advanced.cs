using FMCore.Modpacks;

var root = Environment.GetEnvironmentVariable("FMCORE_ROOT") ?? ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var selected = Environment.GetEnvironmentVariable("FMCORE_EXPORT_OVERRIDES") ?? throw new InvalidOperationException("请设置要导出的相对文件路径，用分号分隔，例如 config/example.toml;options.txt。");
using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
Console.CancelKeyPress += cancel;
try
{
    await new ModpackInstaller(root, id).ExportAsync("instance-with-config.zip", new ModpackExportOptions
    {
        Name = "我的整合包",
        Version = "1.0.0",
        IncludeOverrides = true,
        OverridePaths = selected.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    }, cancellation.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("导出已取消。");
}
catch (ModpackException error)
{
    Console.Error.WriteLine($"{error.Code}: {error.Message}");
    Environment.ExitCode = 1;
}
finally
{
    Console.CancelKeyPress -= cancel;
}
