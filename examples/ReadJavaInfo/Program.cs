using FMCore.Java;

if (args.Length != 1)
{
    Console.Error.WriteLine("用法：ReadJavaInfo <java.exe完整路径>");
    return 2;
}
JavaInfo java = await JavaReader.GetJavaInfoAsync(args[0]);
Console.WriteLine($"Java {java.MajorVersion}，完整版本：{java.Version}，架构：{java.Architecture ?? "未知"}\n{java.Executable}");
return 0;
