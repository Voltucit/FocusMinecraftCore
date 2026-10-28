# 查找 Java

使用 `FMCore.Java` 查看已有 Java 的路径和版本。没有合适的 Java 时，可以[安装 Java](java-installation.md)。

## 查看一个 Java

```csharp
using FMCore.Java;

var java = await JavaReader.GetJavaInfoAsync("C:/Java/jdk-25/bin/java.exe");
Console.WriteLine($"Java {java.Version}：{java.Executable}");
```

路径由用户或启动器提供。这个方法会实际运行所选 Java 来获取信息。返回的 `MajorVersion` 是主版本号，如 25；`Architecture` 是 `x86`、`x64`、`arm64`，无法识别时为 `null`。

## 列出可用 Java

```csharp
var javas = await JavaReader.GetJavasAsync();
foreach (var java in javas)
    Console.WriteLine($"Java {java.MajorVersion}：{java.Executable}");
```

会查看环境变量、用户的 `.jdks` 和 Windows 中登记的 Java。安装在其他位置时，可以增加搜索目录：

```csharp
var javas = await JavaReader.GetJavasAsync(searchPaths: ["D:/Java"]);
```

只检查指定目录及附近安装目录，不扫描整个磁盘，不修改系统设置。单个候选 Java 读取失败时继续查找；可用 `onError` 接收出错路径和原因。每次 Java 查询最多等待 10 秒。

Java 能运行，不代表它能运行所有游戏；具体游戏要求在[启动时](launch.md)检查。完整参数见[接口参考](public-api.md#fmcorejava)。
