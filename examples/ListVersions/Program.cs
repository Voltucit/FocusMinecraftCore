using FMCore.Versions;

MinecraftVersionReader reader = new();
foreach (MinecraftVersion version in reader.GetVersions(args.FirstOrDefault()))
    Console.WriteLine($"{version.Id}\t{version.Directory}");
