# 只补全声音、贴图和日志配置

通常直接使用[游戏文件补全](game-files.md)即可。如果只想处理资源文件，使用 `FMCore.Installer` 的 `GameResourceCompleter`。

## 最短调用

```csharp
using FMCore.Installer;

var completer = new GameResourceCompleter("1.21.10", ".minecraft");
var result = await completer.CompleteAsync();
```

先补齐资源列表和日志配置，再按列表检查、下载声音和贴图等文件。需要已有的版本 JSON，不处理客户端或依赖库，也不启动游戏。

## 进度和设置

```csharp
var result = await completer.CompleteAsync(
    progress => Console.WriteLine(progress.Message));
```

`progress.Download` 是当前这批文件的进度。处理顺序中会有多批文件，百分比会重新开始。`maxConcurrency` 默认 4，`replaceInvalidFiles` 默认 `true`；`Source` 可以[选择下载源](download-sources.md)。

已有文件检查通过后直接使用，坏文件修复前会留备份。资源列表读取失败时，不会假装所有资源都检查完了。失败后保留已完成文件，重新运行可以继续使用它们。

## 保存位置和范围

资源列表放在 `assets/indexes`，资源内容放在 `assets/objects`，日志配置放在 `assets/log_configs`。根目录由调用方决定，不额外生成测试目录。

不处理 Mod、玩家设置、存档或整合包。暂不支持 `virtual`、`map_to_resources` 和只有 `assets` 字段的旧资源存放方式。

返回字段、取消和错误类型见[接口参考](public-api.md#fmcoreinstaller)。一般启动器无需自己读取资源列表或拼下载请求。
