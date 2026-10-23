using FMCore.Modpacks;

var archive = Environment.GetEnvironmentVariable("FMCORE_ARCHIVE") ?? throw new InvalidOperationException("请设置 FMCORE_ARCHIVE。");
var info = await ModpackArchive.ReadAsync(archive);
Console.WriteLine($"{info.Name} {info.Version}: {info.Files.Count}");
Console.WriteLine($"{info.Format}: Minecraft {info.MinecraftVersion}, {info.Loader} {info.LoaderVersion}");
