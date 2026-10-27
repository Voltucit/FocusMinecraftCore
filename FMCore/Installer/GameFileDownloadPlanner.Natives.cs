using System.Text.Json.Nodes;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

namespace FMCore.Installer;

internal static partial class GameFileDownloadPlanner
{
    private static GameDownloadFile? CreateLegacyNativeFile(
        JsonObject library,
        string minecraftRoot,
        string architecture)
    {
        // 外层已经按系统规则筛选，这里选择 Windows 分类。
        string? classifier =
            LibraryResolver.ReadNativeClassifier(
                library,
                "windows");

        if (classifier is null)
            return null;

        if (architecture is not ("x86" or "x64"))
        {
            throw new NotSupportedException(
                "旧式 Windows 原生库只支持 x86 或 x64 目标架构。");
        }

        classifier =
            LibraryResolver.ExpandNativeClassifierForArchitecture(
                classifier,
                architecture);

        string name =
            MinecraftVersionReader.ReadRequiredString(
                library,
                "name",
                "依赖缺少有效的 name。");

        if (library["downloads"] is not JsonObject downloads
            || downloads["classifiers"] is not JsonObject classifiers
            || classifiers[classifier] is not JsonObject declaration)
        {
            throw new InvalidDataException(
                $"依赖 {name} 缺少原生库分类下载声明：{classifier}");
        }

        string relativePath =
            MinecraftVersionReader.ReadRequiredString(
                declaration,
                "path",
                $"原生库分类 {classifier} 缺少有效的 path。");

        string filePath =
            LibraryResolver.ResolveLibraryPath(
                relativePath,
                minecraftRoot);

        return CreateFile(
                $"{name} [{classifier}]",
                filePath,
                declaration,
                repositoryUrl: false) with
            {
                IsLegacyNativeArchive = true
            };
    }
}
