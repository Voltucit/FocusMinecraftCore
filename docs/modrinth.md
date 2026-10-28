# 搜索和下载 Modrinth 资源

使用 `FMCore.ModsResources` 搜索模组、资源包、数据包、光影和整合包。不需要 API Key。

## 最短搜索

```csharp
using FMCore.ModsResources;

var api = new ModrinthClient();
var page = await api.SearchAsync(new ModrinthSearchOptions
{
    Query = "Mod Menu",
    ResourceType = ModrinthResourceType.Mod
});
foreach (var item in page.Hits)
    Console.WriteLine($"{item.Title}：{item.Author}，下载 {item.Downloads}");
```

## 切换资源类别和显示中文标签

| `ModrinthResourceType` | 显示名称 |
| --- | --- |
| `Mod` | 模组 |
| `ResourcePack` | 资源包 |
| `DataPack` | 数据包 |
| `Shader` | 光影 |
| `Modpack` | 整合包 |

```csharp
var types = ModrinthClient.GetResourceTypes();
var categories = await api.GetCategoriesAsync(ModrinthResourceType.Mod);
foreach (var category in categories)
    Console.WriteLine($"{category.DisplayName}：{category.Id}");
```

`DisplayName` 给中文界面显示，`Id` 用于搜索筛选，不要把中文名称传回搜索。未知标签保留原文。项目的 `Tags` 也有这两个字段；介绍、作者名和更新日志保持原文。

## 按游戏版本、加载器和标签筛选

```csharp
var page = await api.SearchAsync(new ModrinthSearchOptions
{
    ResourceType = ModrinthResourceType.Mod,
    Query = "",
    GameVersion = "1.21.10",
    Loader = "fabric",
    Categories = ["optimization"],
    Sort = ModrinthSearchSort.Downloads,
    Limit = 20,
    Offset = 0
});
```

不需要的条件省略。多个标签表示要同时满足；`Query` 为空时用于浏览分类。`Limit` 默认 20，可设 1 到 100；下一页增加 `Offset`。`page.TotalHits` 是总结果数。

排序可用 `Relevance`（相关程度）、`Downloads`（下载量）、`Follows`（关注数）、`Newest`（新发布）、`Updated`（新更新）。搜索匹配的是项目，下载前仍需选择适合所用游戏版本的文件。

## 获取详情、图片和作者

```csharp
var project = await api.GetProjectAsync("modmenu");
Console.WriteLine(project.Title);
Console.WriteLine(project.Description);
Console.WriteLine(project.Body);
foreach (var author in project.Authors)
    Console.WriteLine(author.DisplayName);
foreach (var image in project.Gallery)
    Console.WriteLine(image.OriginalImageUrl);
```

| 展示内容 | 返回字段 |
| --- | --- |
| 名称、简介、完整介绍 | `Title`、`Description`、`Body` |
| 发布时间、更新时间 | `PublishedAt`、`UpdatedAt` |
| 下载量、关注数 | `Downloads`、`Followers` |
| 作者、团队成员 | `Authors`、`TeamMembers` |
| 图标、展示图片 | `IconUrl`、`Gallery` |
| 中文标签 | `Tags` |
| 支持版本、加载器 | `GameVersions`、`Loaders` |

`Followers` 是关注人数，不能当成另一个“收藏量”。图片返回地址，完整介绍返回 Markdown 文本，如何显示和缓存图片由启动器决定。

## 历史版本和更新日志

```csharp
var versions = await api.GetVersionsAsync(
    "modmenu", gameVersion: "1.21.10", loader: "fabric");
```

不传 `gameVersion` 和 `loader` 时列出全部公开版本，按发布时间从新到旧排列。每个版本有版本号、发布时间、下载量、文件列表和依赖说明。

用户选中一个版本后，读取其详情和更新日志：

```csharp
var selected = await api.GetVersionAsync(selectedVersionId);
Console.WriteLine(selected.Changelog);
```

`selectedVersionId` 来自版本列表中用户选中的 `Id`。列表不批量加载更新日志，选中后再取即可。

## 下载用户选中的文件

```csharp
var file = selected.PrimaryFile
    ?? throw new InvalidOperationException("这个版本没有下载文件。");
await api.DownloadAsync(file, destinationFile);
```

`destinationFile` 是启动器决定的完整目标文件路径。下载会检查大小和 SHA-1。需要进度时传入第三个参数，写法见[普通下载](downloads.md)。

核心负责获取资源和下载文件。放到哪个实例、启用或禁用、安装依赖、导入或导出整合包都由启动器作者完成。`Dependencies` 只是原平台给出的依赖说明，不会自动安装。

## 使用范围

可在 `ModrinthClient` 上设置 `Source`；内置 BMCLAPI 不转换 Modrinth 地址，自定义源可以，见[下载源](download-sources.md)。

数据包的某些返回字段可能仍标记为 `mod`，同时保留 `AllProjectTypes`；不要因此丢弃数据包搜索结果。类别目录按平台提供的方式读取。网络或平台错误会直接报出，取消参数见[接口参考](public-api.md#fmcoremodsresources)。

[调用示例](../examples/modrinth-resources.md) · [文档目录](README.md)
