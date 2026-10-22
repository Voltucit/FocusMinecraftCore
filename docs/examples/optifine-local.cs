using FMCore.Loaders;

var path = Environment.GetEnvironmentVariable("FMCORE_OPTIFINE_JAR") ?? throw new InvalidOperationException("请设置 FMCORE_OPTIFINE_JAR。");
var result = await new OptiFineInstaller(".minecraft").InstallLocalAsync(path);
Console.WriteLine(result.VersionId);
