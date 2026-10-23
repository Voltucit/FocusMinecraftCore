using FMCore.Versions;

var root = Environment.GetEnvironmentVariable("FMCORE_MINECRAFT_ROOT") ?? throw new InvalidOperationException("请指定已有根目录。");
var folder = Environment.GetEnvironmentVariable("FMCORE_VERSION_FOLDER") ?? throw new InvalidOperationException("请指定 versions 下的实际文件夹名称。");
var version = await GameCoreUtil.GetGameCoreAsync(folder, root);
Console.WriteLine($"启动查询 ID：{version.Id}；JSON id：{version.MetadataId}；主类：{version.MainClass}");
Console.WriteLine($"版本目录：{version.VersionDirectory}；客户端：{version.ClientJarPath}");
Console.WriteLine($"基础版本提示：{version.MinecraftVersion}；Java 要求：{version.JavaRequirement?.MajorVersion}");
foreach (var loader in version.Loaders) Console.WriteLine($"{loader.Name} {loader.Version}");
foreach (var warning in version.Warnings) Console.WriteLine(warning);
