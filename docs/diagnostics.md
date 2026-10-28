# 检查哪里出问题

提供两个独立功能：一个查游戏文件，一个查启动设置。它们适合接到“检查文件”和“检查设置”按钮，不要求每次启动都重复执行。

## 检查文件

使用 `FMCore.Logger`，不需要 Java 或账号：

```csharp
using FMCore.Logger;

var result = await new GameFileChecker("1.21.10", ".minecraft").CheckAsync();
foreach (var issue in result.Issues)
    Console.WriteLine($"{issue.FilePath}：{issue.Message}");
```

只读取本地文件，不联网、下载、修复或创建目录。要修复问题，另用[文件补全](game-files.md)。

| 返回内容 | 含义 |
| --- | --- |
| `CheckedFiles`、`VerifiedFiles` | 查了多少文件，其中多少通过 |
| `MissingFiles` | 缺少的文件数 |
| `InvalidFiles` | 大小或内容不符的文件数 |
| `UnverifiableFiles` | 本地没有足够信息，无法判断的文件数 |
| `Issues` | 每个文件的问题、路径和说明 |
| `AssetObjectsEnumerated` | 是否成功读取了完整资源文件列表 |
| `FileContentVerified` | 资源列表已读取，所有已知文件检查通过且没有问题 |

文件存在却没有大小或校验值说明时，问题是 `MetadataMissing`，表示“无法确认”，不等于文件损坏。资源列表本身缺失或损坏时，无法继续列出全部声音和贴图，因此结果不会显示全部通过。

`issue.IsLocallyGenerated` 表示文件由加载器安装时生成。它缺失或损坏时，应重新运行对应加载器安装器。

## 检查启动设置

已有[启动配置](launch.md)中的 `config` 时：

```csharp
using FMCore.Launch;

var result = await new MinecraftLauncher(config).CheckAsync();
if (!result.ConfigurationVerified)
    Console.WriteLine(result.Issue!.Message);
```

会实际运行 Java 来读取版本，并检查已有启动设置和所需路径。不启动 Minecraft、不下载文件、不解压原生库，也不完整扫描所有文件内容。

`result.Java` 是读取到的 Java；`Issue` 是第一个阻止检查继续的问题，通过时为空。Microsoft 账号只查已知过期时间，外置账号只查本地代理配置，不替你在线登录或恢复账号。

## 怎么理解结果

检查文件通过，不代表 Java、账号和 Mod 都可用；检查设置通过，也不代表所有文件内容都完整。两种检查都不表示游戏已经打开。

一般启动流程直接“补全后启动”即可，不需要把两次检查都加进去，见[组合示例](../examples/launcher-workflow.md)。

这些方法都可以传入显示进度的方法和取消参数。检查资源列表之前，文件总数还可能增加，应以方法返回为准。同一个启动器对象同时只能执行一次检查或启动。参数、路径、文件读取及取消等错误的详细处理见[接口参考](public-api.md#fmcorelogger)。

[检查文件示例](../examples/file-check.md) · [检查设置示例](../examples/launch-check.md)
