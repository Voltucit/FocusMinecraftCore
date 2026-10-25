using FMCore.Versions;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("用法：ResolveMainClass <版本ID> [Minecraft根目录]");
    return 2;
}
MinecraftVersionReader reader = new();
string? root = args.ElementAtOrDefault(1);
MinecraftVersion selected = reader.GetVersion(args[0], root);
Console.WriteLine(reader.ResolveMainClass(selected, reader.GetVersions(root)));
return 0;
