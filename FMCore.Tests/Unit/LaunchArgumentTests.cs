using System.Text.Json.Nodes;
using FMCore.Assets;
using FMCore.Authentication;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Tests.Fixtures;
using FMCore.Versions;

namespace FMCore.Tests.Unit;

public sealed class LaunchArgumentTests
{
    [Theory]
    [InlineData("1.5.2")]
    [InlineData("1.6.4")]
    [InlineData("1.7.10")]
    [InlineData("1.16.5")]
    public void RealOfficialFormatsExpandLegacyAndModernCredentialsWithoutShellQuoting(string id)
    {
        using var context = new InstallationTestContext();
        var metadata = VersionParser.Parse(InstallationTestContext.Fixture(id + ".json"));
        var installation = Installation(context, metadata, new("windows", "amd64", "10.0"));
        var token = "fixture-launch-secret";
        var account = MicrosoftIdentity(token);
        var start = LaunchArguments.Build(Config(context.Root), JavaTestContext.Runtime("1.8.0_482"), installation, account);
        Assert.False(start.UseShellExecute);
        Assert.Equal(Path.Combine(context.Root, "instances", "with spaces"), start.WorkingDirectory);
        Assert.DoesNotContain(start.ArgumentList, a => a.Contains("${", StringComparison.Ordinal));
        Assert.Contains(metadata.MainClass!, start.ArgumentList);
        Assert.Contains(id is "1.5.2" or "1.6.4" ? account.LegacySession : token, start.ArgumentList);
        Assert.Contains(start.WorkingDirectory, start.ArgumentList);
        Assert.Contains("-Xmx3072M", start.ArgumentList);
        Assert.Contains("-Dsample=value with spaces", start.ArgumentList);
        Assert.Equal("", start.Arguments); // No concatenated command line is exposed by the builder.
        Assert.DoesNotContain("JAVA_TOOL_OPTIONS", start.Environment.Keys);
        Assert.DoesNotContain("JDK_JAVA_OPTIONS", start.Environment.Keys);
        Assert.DoesNotContain("_JAVA_OPTIONS", start.Environment.Keys);
        Assert.DoesNotContain("CLASSPATH", start.Environment.Keys);
    }

    [Theory]
    [InlineData("windows", "amd64", ';')]
    [InlineData("linux", "amd64", ':')]
    [InlineData("osx", "aarch64", ':')]
    [InlineData("windows", "x86", ';')]
    public void RulesAndClasspathFollowTheTargetJavaPlatform(string os, string arch, char separator)
    {
        using var context = new InstallationTestContext();
        var metadata = VersionParser.Parse(InstallationTestContext.Fixture("1.16.5.json"));
        var installation = Installation(context, metadata, new(os, arch, "10.0", new Dictionary<string, bool> { ["has_custom_resolution"] = true }));
        var start = LaunchArguments.Build(Config(context.Root), JavaTestContext.Runtime("1.8.0_482"), installation, MicrosoftIdentity("fixture-secret"));
        var args = start.ArgumentList.ToList();
        Assert.Equal(string.Join(separator, installation.ClasspathEntries), args[args.IndexOf("-cp") + 1]);
        Assert.Equal(os == "osx", args.Contains("-XstartOnFirstThread"));
        Assert.Equal(arch == "x86", args.Contains("-Xss1M"));
        Assert.Contains("--width", args);
        Assert.Contains("--fullscreen", args);
        Assert.DoesNotContain("--demo", args);
        Assert.Contains(installation.ClasspathEntries, a => a.Contains("text2speech-1.11.3.jar", StringComparison.Ordinal));
        if (os != "osx") Assert.Contains(installation.Plan.NativeArchives, a => a.RelativePath.Contains("text2speech", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("-cp")]
    [InlineData("--class-path=evil")]
    [InlineData("-jar")]
    [InlineData("-Xmx8G")]
    [InlineData("-Xms8G")]
    [InlineData("@arguments.txt")]
    public void CustomJvmArgumentsCannotReplaceTheControlledEntrypoint(string argument)
    {
        using var context = new InstallationTestContext();
        var metadata = VersionParser.Parse(InstallationTestContext.Fixture("1.16.5.json"));
        var config = Config(context.Root);
        var error = Assert.Throws<LaunchException>(() => LaunchArguments.Build(config with { Java = config.Java with { Arguments = [argument] } },
            JavaTestContext.Runtime(), Installation(context, metadata, RuleContext.Current), MicrosoftIdentity("fixture-secret")));
        Assert.Equal(LaunchErrorCode.InvalidArguments, error.Code);
    }

    [Theory]
    [InlineData("--accessToken=spoof")]
    [InlineData("--uuid")]
    [InlineData("--gameDir")]
    [InlineData("--username")]
    public void CustomGameArgumentsCannotOverrideAccountOrDirectories(string argument)
    {
        using var context = new InstallationTestContext();
        var metadata = VersionParser.Parse(InstallationTestContext.Fixture("1.16.5.json"));
        var error = Assert.Throws<LaunchException>(() => LaunchArguments.Build(Config(context.Root) with { GameArguments = [argument] },
            JavaTestContext.Runtime(), Installation(context, metadata, RuleContext.Current), MicrosoftIdentity("fixture-secret")));
        Assert.Equal(LaunchErrorCode.InvalidArguments, error.Code);
    }

    [Fact]
    public void UnknownPlaceholderFailsWithoutReturningItsSensitiveValue()
    {
        using var context = new InstallationTestContext();
        var document = VersionParser.Parse(InstallationTestContext.Fixture("1.16.5.json")).ToJson();
        document["arguments"]!["game"]!.AsArray().Add("${fixture_secret_placeholder}");
        var metadata = VersionParser.Parse(document.ToJsonString());
        var error = Assert.Throws<LaunchException>(() => LaunchArguments.Build(Config(context.Root), JavaTestContext.Runtime(),
            Installation(context, metadata, RuleContext.Current), MicrosoftIdentity("fixture-secret-token")));
        Assert.DoesNotContain("fixture_secret_placeholder", error.ToString());
        Assert.DoesNotContain("fixture-secret-token", error.ToString());
    }

    internal static LaunchIdentity MicrosoftIdentity(string token) => new(new(Guid.NewGuid(), AccountKind.Microsoft, "MockPlayer",
        Guid.Parse("12345678-1234-1234-1234-1234567890ab"), true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow), token,
        DateTimeOffset.UtcNow.AddHours(1), "11111111-2222-3333-4444-555555555555", "1234567890123");

    private static LaunchConfig Config(string root) => new()
    {
        GameCore = new() { RootDirectory = root, VersionId = "1.16.5" },
        Java = new() { MaxMemoryMb = 3072, Arguments = ["-Dsample=value with spaces"] },
        Window = new() { Width = 1280, Height = 720, Fullscreen = true }
    };

    private static InstallationResult Installation(InstallationTestContext context, VersionMetadata metadata, RuleContext rules)
    {
        var version = new ResolvedVersion(metadata, metadata.Id, metadata.Downloads["client"], [metadata.Id]);
        var plan = context.Installer.CreatePlan(version, new() { Context = rules, GameDirectory = "instances/with spaces" });
        return new(plan, context.At(plan.Client.RelativePath), plan.ClasspathEntries.Select(context.At).ToArray(), context.At(plan.NativeDirectory),
            new AssetLayoutResult(context.At("assets"), context.At("assets/virtual/legacy"), null),
            plan.Logging == null ? null : context.At(plan.Logging.RelativePath), 0, 0, 0);
    }
}
