using FMCore.Java;

var versions = await JavaRuntimeUtil.GetAvailableVersionsAsync(21);
for (var i = 0; i < versions.Count; i++) Console.WriteLine($"{i}: {versions[i]}");
Console.Write("输入安装索引：");
if (!int.TryParse(Console.ReadLine(), out var index) || index < 0 || index >= versions.Count) return;
var java = await JavaRuntimeUtil.InstallAsync(versions[index]);
Console.WriteLine(java.ExecutablePath);
