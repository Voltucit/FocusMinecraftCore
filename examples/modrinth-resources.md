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

## 自动回退到官方来源

已有兼容镜像时，可以通过源映射开启自动回退。下面使用示例域名，使用前需要替换成实际兼容 Modrinth API 与文件路径的服务。

```csharp
using FMCore.Downloads;
using FMCore.ModsResources;

var automaticSource = new DownloadSource("社区资源自动选择", original =>
{
    var uri = new Uri(original);
    return uri.Host == "api.modrinth.com"
        ? "https://mirror.example.com/modrinth" + uri.PathAndQuery
        : uri.Host == "cdn.modrinth.com"
            ? "https://mirror.example.com" + uri.PathAndQuery
            : null;
}, automaticSelection: true);
var automaticApi = new ModrinthClient { Source = automaticSource };
var results = await automaticApi.SearchAsync(new ModrinthSearchOptions { Query = "Mod Menu" });
```

映射地址暂时不可用时，元数据请求会尝试原始官方地址；单个来源最多等待 12 秒。文件下载可根据实际传输表现调整候选顺序，仍保留大小与哈希校验。主动取消会停止整个请求，不继续切换来源。

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

## 一次下载多个已选择的文件

启动器解析完必需依赖后，可以先准备全部请求，再使用核心批量下载。下面沿用前文的已选文件；实际使用时向列表中加入其他已选择文件及各自的目标路径。

```csharp
using FMCore.Downloads;

var requests = new List<DownloadRequest>
{
    api.CreateDownloadRequest(file, destinationFile)
};
await FileDownloader.DownloadManyAsync(requests, 8, progress =>
    Console.WriteLine($"{progress.CompletedFiles}/{progress.TotalFiles}，{progress.BytesPerSecond / 1024 / 1024:F2} MiB/s"));
```

`8` 是同时处理的文件数。大文件默认最多 4 个分段，服务器不支持分段时自动使用普通下载；文件仍须通过完整大小和 SHA-1 检查。这里不执行资源替换、依赖决策或整合包安装。

[筛选、中文分类、详细返回内容和限制](../docs/modrinth.md)
