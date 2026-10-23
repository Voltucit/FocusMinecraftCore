using FMCore.Versions;

var root = Environment.GetEnvironmentVariable("FMCORE_MINECRAFT_ROOT") ?? throw new InvalidOperationException("请指定已有 Minecraft 根目录。");
// 这里只读扫描，不复制/搬动目录，不创建根目录，也不导入或执行任何 Java 代码。
foreach (var version in await GameCoreUtil.GetGameCoresAsync(root))
{
    Console.WriteLine($"目录={version.Id}；JSON id={version.MetadataId}；基础版本={version.MinecraftVersion}；错误={version.ErrorCode}");
    Console.WriteLine(version.MetadataPath);
    Console.WriteLine(version.ClientJarPath);
    foreach (var loader in version.Loaders) Console.WriteLine($"{loader.Name} {loader.Version}");
    foreach (var warning in version.Warnings) Console.WriteLine(warning);
}
