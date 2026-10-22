using FMCore.Resources;
using FMCore.Instances;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var key = Environment.GetEnvironmentVariable("FMCORE_CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("请设置 FMCORE_CURSEFORGE_API_KEY。");
using var client = new CurseForgeClient(key);
var root = ".minecraft";
var id = Environment.GetEnvironmentVariable("FMCORE_INSTANCE") ?? throw new InvalidOperationException("请设置 FMCORE_INSTANCE。");
var instance = await new InstanceManager(root).GetAsync(id);
var mods = new CurseForgeInstaller(instance, client);
var progress = new Progress<ResourceProgress>(p => Console.WriteLine($"{p.Stage}: {p.Download?.BytesReceived}"));
try { await mods.UpdateAsync(306612, progress, ct); }
catch (OperationCanceledException) { Console.WriteLine("已取消，旧文件由事务恢复保护。"); }
catch (ResourceException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
