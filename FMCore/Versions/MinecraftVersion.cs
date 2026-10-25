namespace FMCore.Versions;

/// <summary>本地版本的基础元数据；不表示游戏文件已完整或能够启动。</summary>
public sealed record MinecraftVersion(
    string Id,
    string? VersionType,
    string Directory,
    string? InheritsFrom = null,
    string? MainClass = null);
