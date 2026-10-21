using System.Text.Json.Nodes;
using FMCore.Versions;

namespace FMCore.Tests.Unit;

public sealed class VersionRulesTests
{
    private static readonly string Rules = """
        [{"action":"allow","os":{"name":"windows","arch":"x86_64","version":"^10\\."},"features":{"has_custom_resolution":true}}]
        """;

    [Theory]
    [InlineData("windows", "amd64", "10.0.26100", true, true)]
    [InlineData("windows", "x86_64", "10.0", true, true)]
    [InlineData("windows", "x86", "10.0", true, false)]
    [InlineData("windows", "amd64", "6.1", true, false)]
    [InlineData("windows", "amd64", "10.0", false, false)]
    [InlineData("linux", "amd64", "10.0", true, false)]
    public void CombinesOsArchitectureVersionAndFeatureConditions(string os, string arch, string version, bool feature, bool expected)
    {
        var document = new JsonObject
        {
            ["id"] = "rules",
            ["libraries"] = new JsonArray(new JsonObject { ["name"] = "g:a:1", ["rules"] = JsonNode.Parse(Rules) }),
            ["arguments"] = new JsonObject { ["game"] = new JsonArray(new JsonObject { ["rules"] = JsonNode.Parse(Rules), ["value"] = new JsonArray("--width", "${resolution_width}") }) }
        };
        var metadata = VersionParser.Parse(document.ToJsonString());
        var context = new RuleContext(os, arch, version, new Dictionary<string, bool> { ["has_custom_resolution"] = feature });
        Assert.Equal(expected ? 1 : 0, metadata.GetLibraries(context).Count);
        Assert.Equal(expected ? 2 : 0, metadata.Arguments.GetGameArguments(context).Count);
    }

    [Fact]
    public void LastMatchingRuleWinsAndMissingFeaturesAreFalse()
    {
        var metadata = VersionParser.Parse("""
            {"id":"rules","arguments":{"jvm":[
              {"rules":[{"action":"allow"},{"action":"disallow","os":{"name":"windows"}},{"action":"allow","features":{"special":true}}],"value":"-Done"},
              {"rules":[{"action":"allow","features":{"unknown":false}}],"value":"-Dtwo"},
              {"rules":[],"value":"-Dnever"}, "-Dalways"
            ]}}
            """);
        Assert.Equal(["-Dtwo", "-Dalways"], metadata.Arguments.GetJvmArguments(new("windows", "amd64", "10")));
        Assert.Equal(["-Done", "-Dtwo", "-Dalways"], metadata.Arguments.GetJvmArguments(new("windows", "amd64", "10", new Dictionary<string, bool> { ["special"] = true })));
    }

    [Fact]
    public void ArchitectureRegexDoesNotAccidentallyMatchAnX86Prefix()
    {
        var metadata = VersionParser.Parse("""{"id":"rules","arguments":{"jvm":[{"rules":[{"action":"allow","os":{"arch":"x86"}}],"value":"-Xss1M"}]}}""");
        Assert.Empty(metadata.Arguments.GetJvmArguments(new("windows", "x86_64", "10")));
        Assert.Single(metadata.Arguments.GetJvmArguments(new("windows", "i386", "10")));
    }
}
