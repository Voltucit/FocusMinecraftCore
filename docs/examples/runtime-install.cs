using FMCore.Java;

var java = await JavaRuntimeUtil.InstallAsync(21);
Console.WriteLine(java.ExecutablePath);
