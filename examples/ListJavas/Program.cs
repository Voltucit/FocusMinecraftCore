using FMCore.Java;

IReadOnlyList<JavaInfo> javas = await JavaReader.GetJavasAsync(args,
    (path, error) => Console.Error.WriteLine($"探测失败：{path}；{error.Message}"));
foreach (JavaInfo java in javas)
    Console.WriteLine($"Java {java.MajorVersion}，{java.Version}，{java.Architecture ?? "未知"}\n{java.Executable}");
