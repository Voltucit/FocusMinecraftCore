# 补全和修复游戏文件

已有游戏缺文件或下载不完整时，使用 `FMCore.Installer` 的 `GameFileCompleter`。不用先生成下载列表，也不需要 Java 或账号。

## 最短调用

```csharp
using FMCore.Installer;

var completer = new GameFileCompleter("1.21.10", ".minecraft");
var result = await completer.CompleteAsync();
```

会检查客户端、依赖库、旧版原生库压缩包、资源列表、资源文件和日志配置。正确文件直接使用，缺失文件下载，坏文件先留备份再替换。

## 显示进度

```csharp
var result = await completer.CompleteAsync(
    progress => Console.WriteLine(progress.Message));
```

`progress.Download` 有值时是当前这批文件的下载进度。文件数量和下载字节百分比是两回事：几千个小文件处理完后，可能还剩几个大文件。应等待方法成功返回，不要把某一批的 100% 当作全部完成。

## 查看处理结果

| 返回内容 | 含义 |
| --- | --- |
| `TotalFiles` | 本次处理的文件总数 |
| `DownloadedFiles` | 新下载的文件数 |
| `ReusedFiles` | 已有且检查通过的下载文件数 |
| `VerifiedGeneratedFiles` | 加载器生成、且按本地说明检查通过的文件数 |
| `RepairedFiles` | 本次替换并保留备份的坏文件数 |
| `Backups` | 原文件和备份文件的路径 |

成功表示本次负责的文件已检查完成，不表示游戏已经启动或 Mod 一定兼容。

## 可选设置

在构造时可设置 `maxConcurrency`（同时处理数，默认 4）、`replaceInvalidFiles`（默认 `true`）和 `targetArchitecture`（默认当前系统位数）。若选用不同位数的 Java，应为那个 Java 选择文件，而不是只看 Windows 位数。

设置 `Source` 可[选择下载源](download-sources.md)。取消方式见[通用说明](advanced.md)，完整声明见[接口参考](public-api.md#fmcoreinstaller)。

## 哪些文件不能直接修复

Forge 等安装时生成的文件可能没有下载地址。存在大小和校验值说明时，FMCore 可以检查它；缺失或损坏时，需要重新运行对应加载器安装器。损坏文件需先备份移走，不能随意编一个地址下载。

资源列表坏了会先修复列表，再处理其中的资源。部分依赖缺少下载大小或校验值时，补全方法会尝试从对应 Maven 仓库获取。仅检查本地文件的 [GameFileChecker](diagnostics.md) 不会联网补取这些信息。

本方法不修复 Mod、存档、玩家设置或 Java，也不负责整合包导入。尚未支持的旧资源存放方式见[启动范围](launch.md#使用范围)。

## 接着启动

补全成功后直接调用已有 `launcher.LaunchAsync()` 即可。失败或取消时不要继续启动，也不用在成功补全后再扫描一遍全部文件，见[组合示例](../examples/launcher-workflow.md)。
