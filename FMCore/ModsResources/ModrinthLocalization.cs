namespace FMCore.ModsResources;

public static class ModrinthLocalization
{
    private static readonly IReadOnlyDictionary<string, string> CategoryNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["128x"] = "128× 分辨率",
            ["16x"] = "16× 分辨率",
            ["256x"] = "256× 分辨率",
            ["32x"] = "32× 分辨率",
            ["48x"] = "48× 分辨率",
            ["512x+"] = "512× 及以上",
            ["64x"] = "64× 分辨率",
            ["8x-"] = "8× 及以下",
            ["adventure"] = "冒险",
            ["atmosphere"] = "氛围",
            ["audio"] = "音效",
            ["blocks"] = "方块",
            ["bloom"] = "泛光",
            ["cartoon"] = "卡通",
            ["challenging"] = "挑战",
            ["colored-lighting"] = "彩色光照",
            ["combat"] = "战斗",
            ["core-shaders"] = "原版核心着色器",
            ["cursed"] = "整活",
            ["decoration"] = "装饰",
            ["economy"] = "经济",
            ["entities"] = "实体",
            ["environment"] = "环境",
            ["equipment"] = "装备",
            ["fantasy"] = "奇幻",
            ["foliage"] = "植被",
            ["fonts"] = "字体",
            ["food"] = "食物",
            ["game-mechanics"] = "游戏机制",
            ["gui"] = "界面",
            ["high"] = "高性能开销",
            ["items"] = "物品",
            ["kitchen-sink"] = "大杂烩",
            ["library"] = "前置与库",
            ["lightweight"] = "轻量",
            ["locale"] = "语言本地化",
            ["low"] = "低性能开销",
            ["magic"] = "魔法",
            ["management"] = "管理",
            ["medium"] = "中等性能开销",
            ["minigame"] = "小游戏",
            ["mobs"] = "生物",
            ["modded"] = "模组扩展",
            ["models"] = "模型",
            ["multiplayer"] = "多人游戏",
            ["optimization"] = "性能优化",
            ["path-tracing"] = "路径追踪",
            ["pbr"] = "物理渲染（PBR）",
            ["potato"] = "极低性能开销",
            ["quests"] = "任务",
            ["realistic"] = "写实",
            ["reflections"] = "反射",
            ["screenshot"] = "截图级性能开销",
            ["semi-realistic"] = "半写实",
            ["shadows"] = "阴影",
            ["simplistic"] = "简约",
            ["social"] = "社交",
            ["storage"] = "存储",
            ["technology"] = "科技",
            ["themed"] = "主题",
            ["transportation"] = "交通运输",
            ["tweaks"] = "细节调整",
            ["utility"] = "实用工具",
            ["vanilla-like"] = "原版风格",
            ["worldgen"] = "世界生成",
            ["fabric"] = "Fabric",
            ["forge"] = "Forge",
            ["neoforge"] = "NeoForge",
            ["quilt"] = "Quilt",
            ["minecraft"] = "原版",
            ["datapack"] = "数据包",
            ["iris"] = "Iris",
            ["optifine"] = "OptiFine",
            ["liteloader"] = "LiteLoader",
            ["bukkit"] = "Bukkit",
            ["paper"] = "Paper",
            ["spigot"] = "Spigot",
            ["folia"] = "Folia",
            ["purpur"] = "Purpur",
        };

    public static string GetResourceTypeName(ModrinthResourceType type) => type switch
    {
        ModrinthResourceType.Mod => "模组",
        ModrinthResourceType.ResourcePack => "资源包",
        ModrinthResourceType.DataPack => "数据包",
        ModrinthResourceType.Shader => "光影",
        ModrinthResourceType.Modpack => "整合包",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static string GetCategoryName(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return CategoryNames.TryGetValue(id, out string? name) ? name : id;
    }

    public static string GetGroupName(string id) => id switch
    {
        "categories" => "类别",
        "features" => "功能",
        "resolutions" => "分辨率",
        "performance impact" => "性能开销",
        _ => id
    };

    public static string GetVersionTypeName(string id) => id switch
    {
        "release" => "正式版",
        "beta" => "测试版",
        "alpha" => "预览版",
        _ => id
    };

    internal static string ApiType(ModrinthResourceType type) => type switch
    {
        ModrinthResourceType.Mod => "mod",
        ModrinthResourceType.ResourcePack => "resourcepack",
        ModrinthResourceType.DataPack => "datapack",
        ModrinthResourceType.Shader => "shader",
        ModrinthResourceType.Modpack => "modpack",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    internal static IReadOnlyList<ModrinthLabel> Labels(IEnumerable<string> ids)
        => ids.Distinct(StringComparer.Ordinal)
            .Select(id => new ModrinthLabel(id, GetCategoryName(id))).ToArray();
}

