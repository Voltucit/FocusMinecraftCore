using System.Text.Json.Nodes;
using FMCore.Versions;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.VersionTestContext;

namespace FMCore.Tests.Unit;

public sealed class VersionInheritanceTests
{
    [Fact]
    public async Task MergesLibrariesArgumentsClientAssetsJavaLoggingAndUnknownFields()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        await context.SaveAsync(Fixture("inherit-child.json"));
        var resolved = await context.Manager.ResolveAsync("test-child", VersionCachePolicy.CacheOnly, Ct);
        var metadata = resolved.Metadata;
        Assert.Equal("org.example.LoaderMain", metadata.MainClass);
        Assert.Equal(["-Dbase=true", "-cp", "${classpath}", "-Dchild=true"], metadata.Arguments.GetJvmArguments(RuleContext.Current));
        Assert.Equal(["--username", "${auth_player_name}", "--child"], metadata.Arguments.GetGameArguments(RuleContext.Current));
        Assert.Contains(metadata.Libraries, l => l.Name == "org.ow2.asm:asm:9.7.1");
        Assert.DoesNotContain(metadata.Libraries, l => l.Name == "org.ow2.asm:asm:9.6");
        Assert.Equal(2, metadata.Libraries.Count(l => l.Coordinate.Artifact == "keep"));
        Assert.Equal("test-base", resolved.ClientJarId);
        Assert.Equal(100, resolved.ClientDownload!.Size);
        Assert.Equal("17", metadata.AssetIndex!.Id);
        Assert.Equal(17, metadata.Java!.MajorVersion);
        Assert.Equal("client.xml", metadata.Logging!.FileId);
        Assert.Equal(["test-base", "test-child"], resolved.InheritanceChain);
        Assert.Equal(1, metadata.ToJson()["futureField"]!["keep"]!.GetValue<int>());
        Assert.Equal(2, metadata.ToJson()["futureField"]!["added"]!.GetValue<int>());
        Assert.Equal("child", metadata.ToJson()["futureField"]!["overridden"]!.GetValue<string>());
        var native = metadata.Libraries.Single(l => l.Coordinate.Artifact == "native");
        Assert.Equal("natives-windows-64", native.GetNativeClassifier(new("windows", "amd64", "10")));
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task ResolvesRealFabricInheritanceWithoutInstallingALoader()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("1.21.1.json"));
        await context.SaveAsync(Fixture("fabric-1.21.1.json"));
        var resolved = await context.Manager.ResolveAsync("fabric-loader-0.16.10-1.21.1", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal("net.fabricmc.loader.impl.launch.knot.KnotClient", resolved.Metadata.MainClass);
        Assert.Equal("1.21.1", resolved.ClientJarId);
        Assert.Equal(21, resolved.Metadata.Java!.MajorVersion);
        Assert.Contains(resolved.Metadata.Libraries, l => l.Coordinate.Artifact == "fabric-loader");
        Assert.Contains(resolved.Metadata.Libraries, l => l.Coordinate.Artifact == "lwjgl");
        Assert.Contains("-DFabricMcEmu= net.minecraft.client.main.Main ", resolved.Metadata.Arguments.GetJvmArguments(RuleContext.Current));
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task ChildClientAndExplicitJarReferencesChooseTheirActualDownload()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        var child = JsonNode.Parse(Fixture("inherit-child.json"))!.AsObject();
        child["downloads"] = JsonNode.Parse("""{"client":{"url":"https://metadata.example.test/new.jar","size":222}}""");
        await context.SaveAsync(child.ToJsonString());
        var own = await context.Manager.ResolveAsync("test-child", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal("test-child", own.ClientJarId);
        Assert.Equal(222, own.ClientDownload!.Size);
        Assert.Null(own.Metadata.Downloads["client"].Sha1);
        child["jar"] = "test-base";
        await context.SaveAsync(child.ToJsonString());
        var referenced = await context.Manager.ResolveAsync("test-child", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal("test-base", referenced.ClientJarId);
        Assert.Equal(100, referenced.ClientDownload!.Size);
    }

    [Theory]
    [InlineData("inheritsFrom")]
    [InlineData("jar")]
    public async Task DetectsCyclesInInheritanceAndJarReferences(string relationship)
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(new JsonObject { ["id"] = "a", [relationship] = "b" }.ToJsonString());
        await context.SaveAsync(new JsonObject { ["id"] = "b", [relationship] = "a" }.ToJsonString());
        var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.ResolveAsync("a", VersionCachePolicy.CacheOnly, Ct));
        Assert.Equal(VersionErrorCode.InheritanceCycle, error.Code);
    }

    [Fact]
    public async Task RejectsExcessivelyDeepInheritance()
    {
        using var context = new VersionTestContext();
        for (var index = 0; index < 33; index++)
        {
            var document = new JsonObject { ["id"] = "depth-" + index };
            if (index < 32) document["inheritsFrom"] = "depth-" + (index + 1);
            await context.SaveAsync(document.ToJsonString());
        }
        var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.ResolveAsync("depth-0", VersionCachePolicy.CacheOnly, Ct));
        Assert.Equal(VersionErrorCode.InheritanceTooDeep, error.Code);
    }

    [Fact]
    public async Task HandlesTransitionsBetweenLegacyAndModernGameArguments()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync("""{"id":"legacy","minecraftArguments":"--name \"hello world\""}""");
        await context.SaveAsync("""{"id":"modern","inheritsFrom":"legacy","arguments":{"game":["--child"]}}""");
        var modern = await context.Manager.ResolveAsync("modern", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal(["--name", "hello world", "--child"], modern.Metadata.Arguments.GetGameArguments(RuleContext.Current));
        await context.SaveAsync("""{"id":"override","inheritsFrom":"modern","minecraftArguments":"--replacement yes"}""");
        var legacy = await context.Manager.ResolveAsync("override", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal(["--replacement", "yes"], legacy.Metadata.Arguments.GetGameArguments(RuleContext.Current));
    }

    [Fact]
    public async Task GrandchildCanOverrideAssetsJavaAndLoggingWithoutLosingOtherFields()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        await context.SaveAsync(Fixture("inherit-child.json"));
        await context.SaveAsync("""
            {"id":"grandchild","inheritsFrom":"test-child","assets":"18",
             "javaVersion":{"component":"java-runtime-delta","majorVersion":21},
             "assetIndex":{"id":"18","url":"https://metadata.example.test/assets18.json","size":20},
             "logging":{"client":{"argument":"-Dnew=${path}","file":{"id":"new.xml","url":"https://metadata.example.test/new.xml","size":50}}}}
            """);
        var resolved = await context.Manager.ResolveAsync("grandchild", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal(21, resolved.Metadata.Java!.MajorVersion);
        Assert.Equal("18", resolved.Metadata.AssetIndex!.Id);
        Assert.Null(resolved.Metadata.AssetIndex.Download.Sha1);
        Assert.Null(resolved.Metadata.AssetIndex.TotalSize);
        Assert.Equal("new.xml", resolved.Metadata.Logging!.FileId);
        Assert.Equal("log4j2-xml", resolved.Metadata.Logging.Type);
        Assert.Equal("org.example.LoaderMain", resolved.Metadata.MainClass);
        Assert.Equal("test-base", resolved.ClientJarId);
        Assert.Equal(["test-base", "test-child", "grandchild"], resolved.InheritanceChain);
    }
}
