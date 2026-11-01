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

外部导入或此前安装的加载器版本可能借用原版的文件。已有上面的 `reader`、`version` 和同一根目录的 `versions` 时：

```csharp
var chain = reader.GetInheritanceChain(version, versions);
string mainClass = reader.ResolveMainClass(version, versions);
```

`chain` 从所选版本排到父版本；`mainClass` 是最终要运行的游戏入口名称。父版本只在同一个根目录里查找。缺父版本、名字重复或父子关系形成循环时会报错。

界面中可改用 `GetVersionsAsync()` 和 `GetVersionAsync()`，参数顺序相同。启动游戏时无需手动执行这些准备步骤，直接使用 [MinecraftLauncher](launch.md)。

[可运行示例](../examples/README.md) · [完整接口](public-api.md#fmcoreversions)

## 让已有版本独立启动

新安装的 Fabric、Forge、NeoForge 和 OptiFine 会自动生成独立版本。已有版本使用 `FMCore.Versions`：

```csharp
using FMCore.Versions;

var version = await new MinecraftVersionConverter(
    "1.21.10-Fabric-0.17.3", ".minecraft").MakeIndependentAsync();
```

转换时需要原有父版本仍然存在。核心合并完整配置，把校验通过的客户端复制到当前版本目录，并备份原配置。成功后可移除普通版目录，公共 `assets` 和 `libraries` 仍需保留。存档、Mod、资源包和玩家设置不会被搬动。已经独立的版本直接复用，不反复生成备份。

默认游戏工作目录是各自的 `versions/<目录名>`，所以存档、Mod 和设置彼此独立；显式指定 `GameDirectory` 时仍使用调用方选择的目录。取消、缺失父版本、文件校验失败或已有客户端内容不同会报错，已有数据保留。
