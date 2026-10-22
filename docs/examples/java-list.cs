using FMCore.Java;

var javas = await JavaUtil.GetJavasAsync();
foreach (var java in javas) Console.WriteLine($"{java.Version.Text}: {java.ExecutablePath}");
