# 查询本地游戏

使用 `FMCore.Versions` 读取已经安装的版本。查询不会下载文件或创建目录。

## 列出一个目录中的游戏

```csharp
using FMCore.Versions;

var reader = new MinecraftVersionReader();
var versions = reader.GetVersions(".minecraft");
foreach (var version in versions)
    Console.WriteLine($"{version.Id}：{version.Directory}");
```

不传目录时，会查看当前工作目录和 `%APPDATA%` 下的 `.minecraft`。它不会扫描所有磁盘。

## 找到一个版本

```csharp
var version = reader.GetVersion("1.21.10", ".minecraft");
Console.WriteLine(version.Directory);
```

版本号在前，根目录在后。找不到会报错；同一个 ID 对应多个目录时，也会报错，避免启动错游戏。

目录应当是 `根目录/versions/文件夹/文件夹.json`。`Id` 取自 JSON，可以与文件夹名称不同。

## 查询父版本

Fabric 等版本可能借用原版的文件。已有上面的 `reader`、`version` 和同一根目录的 `versions` 时：

```csharp
var chain = reader.GetInheritanceChain(version, versions);
string mainClass = reader.ResolveMainClass(version, versions);
```

`chain` 从所选版本排到父版本；`mainClass` 是最终要运行的游戏入口名称。父版本只在同一个根目录里查找。缺父版本、名字重复或父子关系形成循环时会报错。

界面中可改用 `GetVersionsAsync()` 和 `GetVersionAsync()`，参数顺序相同。启动游戏时无需手动执行这些准备步骤，直接使用 [MinecraftLauncher](launch.md)。

[可运行示例](../examples/README.md) · [完整接口](public-api.md#fmcoreversions)
