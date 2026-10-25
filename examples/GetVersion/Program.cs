using FMCore.Versions;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("用法：GetVersion <版本ID> [Minecraft根目录]");
    return 2;
}
MinecraftVersion version = new MinecraftVersionReader().GetVersion(args[0], args.ElementAtOrDefault(1));
Console.WriteLine($"版本：{version.Id}\n目录：{version.Directory}\n父版本：{version.InheritsFrom ?? "无"}");
return 0;
