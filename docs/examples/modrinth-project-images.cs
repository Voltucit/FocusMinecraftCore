using FMCore.Resources;

var project = await new ModrinthClient().GetProjectAsync("sodium");
Console.WriteLine(project.IconUrl);
foreach (var image in project.Gallery)
    Console.WriteLine($"{image.Title}: {image.Url}；推荐展示={image.Featured}；排序={image.Ordering}");
// Gallery 可为空；SDK 不下载或持久化图片。
