using FMCore.Resources;

var client = new ModrinthClient();
var page = await client.SearchAsync("sodium");
foreach (var mod in page.Hits)
    Console.WriteLine($"{mod.Title}: {mod.IconUrl?.AbsoluteUri ?? "无图标"}");
// IconUrl 来自同一次搜索响应；宿主自行选择 UI 图片控件与缓存策略。
