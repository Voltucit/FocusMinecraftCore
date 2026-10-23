using FMCore.Resources;

var key = Environment.GetEnvironmentVariable("CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("需要合法 API Key。");
using var client = new CurseForgeClient(key);
var project = await client.GetProjectAsync(238222);
Console.WriteLine(project.IconUrl);
foreach (var image in project.Gallery)
    Console.WriteLine($"{image.Title}: {image.Url}；缩略图={image.ThumbnailUrl}");
// Gallery 映射官方 screenshots；部分项目没有图片，返回空列表。
