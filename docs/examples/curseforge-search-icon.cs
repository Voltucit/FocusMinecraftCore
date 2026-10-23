using FMCore.Resources;

var key = Environment.GetEnvironmentVariable("CURSEFORGE_API_KEY") ?? throw new InvalidOperationException("需要合法 API Key。");
using var client = new CurseForgeClient(key);
var page = await client.SearchAsync("jei");
foreach (var mod in page.Mods)
    Console.WriteLine($"{mod.Name}: {mod.IconUrl?.AbsoluteUri ?? "无图标"}");
// Key 仅由 API 客户端放在认证请求头，图标地址不附加 Key。
