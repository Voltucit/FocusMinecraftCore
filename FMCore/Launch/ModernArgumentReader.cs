using System.Text.Json.Nodes;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

internal sealed record ModernArgumentTemplates(
    IReadOnlyList<string> Jvm,
    IReadOnlyList<string> Game);

internal static class ModernArgumentReader
{
    internal static ModernArgumentTemplates Read(
        MinecraftVersion version,
        string minecraftRoot,
        JavaInfo java,
        IReadOnlyDictionary<string, bool>? features = null)
    {
        ArgumentNullException.ThrowIfNull(java);

        IReadOnlyList<MinecraftVersion> chain =
            LibraryResolver.GetValidatedChain(
                version, minecraftRoot);

        List<string> jvm = [];
        List<string> game = [];
        bool found = false;

        // 新版数组按父版本到子版本的顺序追加。
        foreach (MinecraftVersion current in chain.Reverse())
        {
            JsonObject data =
                MinecraftVersionReader.ReadVersionJson(
                    current.Directory);

            if (!data.TryGetPropertyValue(
                    "arguments", out JsonNode? argumentsNode))
                continue;

            if (argumentsNode is not JsonObject arguments)
                throw new InvalidDataException(
                    $"版本 {current.Id} 的 arguments 必须是对象。");

            if (arguments.Any(pair =>
                    pair.Key is not ("jvm" or "game")))
                throw new NotSupportedException(
                    "arguments 中包含暂未支持的参数分类。");

            found = true;

            AppendSection(
                arguments, "jvm", jvm, java, features);

            AppendSection(
                arguments, "game", game, java, features);
        }

        if (!found)
            throw new NotSupportedException(
                "当前继承链没有新版 arguments 声明。");

        return new ModernArgumentTemplates(
            jvm.AsReadOnly(),
            game.AsReadOnly());
    }

    private static void AppendSection(
        JsonObject arguments,
        string section,
        List<string> destination,
        JavaInfo java,
        IReadOnlyDictionary<string, bool>? features)
    {
        if (!arguments.TryGetPropertyValue(
                section, out JsonNode? sectionNode))
            return;

        if (sectionNode is not JsonArray array)
            throw new InvalidDataException(
                $"arguments.{section} 必须是列表。");

        foreach (JsonNode? node in array)
        {
            if (node is JsonValue)
            {
                AddString(node, destination);
                continue;
            }

            if (node is not JsonObject conditional)
                throw new InvalidDataException(
                    "参数项必须是字符串或条件对象。");

            if (conditional.Any(pair =>
                    pair.Key is not ("rules" or "value")))
                throw new NotSupportedException(
                    "条件参数包含暂未支持的字段。");

            if (!conditional.ContainsKey("rules"))
                throw new InvalidDataException(
                    "条件参数缺少 rules。");

            if (!LaunchRules.IsAllowed(
                    conditional, java, features))
                continue;

            if (conditional["value"] is JsonArray values)
            {
                foreach (JsonNode? value in values)
                    AddString(value, destination);
            }
            else
            {
                AddString(conditional["value"], destination);
            }
        }
    }

    private static void AddString(
        JsonNode? node,
        List<string> destination)
    {
        if (node is not JsonValue value
            || !value.TryGetValue<string>(out string? text)
            || text is null)
            throw new InvalidDataException(
                "参数值必须是字符串。");

        if (text.Contains('\0'))
            throw new InvalidDataException(
                "参数不能包含空字符。");

        // 保留整个字符串，不进行 Split 或 Trim。
        destination.Add(text);
    }
}
