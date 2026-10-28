# Modrinth 资源示例

## 搜索

```csharp
using FMCore.ModsResources;

var api = new ModrinthClient();
var page = await api.SearchAsync(new ModrinthSearchOptions
{
    ResourceType = ModrinthResourceType.Mod,
    Query = "Mod Menu",
    GameVersion = "1.21.10",
    Loader = "fabric"
});
foreach (var item in page.Hits)
    Console.WriteLine($"{item.Title}：{item.Author}");
```

将 `ResourceType` 换成 `ResourcePack`、`DataPack`、`Shader` 或 `Modpack` 即可查询其他类型，不需要另建客户端。

## 详情和历史版本

```csharp
var project = await api.GetProjectAsync("modmenu");
var versions = await api.GetVersionsAsync("modmenu");
```

详情提供介绍、作者、图片、时间、下载量、关注数和中文标签。版本列表供用户选择，不应不看兼容条件就直接安装第一个版本。

## 下载选中的文件

`selectedVersionId` 来自用户选中的版本，`destinationFile` 是启动器决定的目标文件路径：

```csharp
var selected = await api.GetVersionAsync(selectedVersionId);
var file = selected.PrimaryFile
    ?? throw new InvalidOperationException("这个版本没有下载文件。");
await api.DownloadAsync(file, destinationFile);
```

下载会检查文件，放入实例、安装依赖和启用资源由启动器处理，整个整合包系统也由启动器实现。

[筛选、中文分类、详细返回内容和限制](../docs/modrinth.md)
