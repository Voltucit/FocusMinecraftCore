using FMCore.Versions;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("用法：InheritanceChain <版本ID> [Minecraft根目录]");
    return 2;
}
MinecraftVersionReader reader = new();
string? root = args.ElementAtOrDefault(1);
MinecraftVersion selected = reader.GetVersion(args[0], root);
IReadOnlyList<MinecraftVersion> versions = reader.GetVersions(root);
foreach (MinecraftVersion version in reader.GetInheritanceChain(selected, versions))
    Console.WriteLine(version.Id);
return 0;
