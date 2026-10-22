using FMCore.Java;

var path = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请设置 FMCORE_JAVA。");
var java = await JavaUtil.GetJavaInfoAsync(path);
Console.WriteLine($"{java.Version.Text} {java.Architecture}");
