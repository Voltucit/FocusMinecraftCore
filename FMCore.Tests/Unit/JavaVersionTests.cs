using FMCore.Java;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.JavaTestContext;

namespace FMCore.Tests.Unit;

public sealed class JavaVersionTests
{
    [Theory]
    [InlineData("1.8.0_482-b08", 8, 482, 8, false)]
    [InlineData("1.7.0_80", 7, 80, 0, false)]
    [InlineData("17.0.9", 17, 9, 0, false)]
    [InlineData("21.0.6+7-LTS", 21, 6, 7, false)]
    [InlineData("25-ea+12-123", 25, 0, 12, true)]
    [InlineData("26", 26, 0, 0, false)]
    public void ParsesLegacyModernAndEarlyAccessVersions(string text, int major, int security, int build, bool ea)
    {
        var version = JavaVersion.Parse(text);
        Assert.Equal(major, version.Major);
        Assert.Equal(security, version.Security);
        Assert.Equal(build, version.Build);
        Assert.Equal(ea, version.IsEarlyAccess);
        Assert.Equal(text, version.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-java")]
    [InlineData("0.1")]
    [InlineData("21.0.6\nsecret")]
    [InlineData("999999999999999999999")]
    [InlineData("21.9999999999999999999")]
    public void RejectsInvalidVersionsWithoutReflectingInput(string text)
    {
        var error = Assert.Throws<JavaException>(() => JavaVersion.Parse(text));
        Assert.Equal(JavaErrorCode.InvalidOutput, error.Code);
        Assert.DoesNotContain("secret", error.ToString());
    }

    [Theory]
    [InlineData("17.0.12", "17.0.9")]
    [InlineData("1.8.0_482", "1.8.0_99")]
    [InlineData("21+10", "21+9")]
    [InlineData("25", "25-ea+99")]
    public void OrdersNumericallyAndPrefersStableRelease(string newer, string older) =>
        Assert.True(JavaVersion.Parse(newer).CompareTo(JavaVersion.Parse(older)) > 0);

    [Theory]
    [InlineData("java8-properties.txt", 8, JavaArchitecture.X64, false)]
    [InlineData("java21-properties.txt", 21, JavaArchitecture.X64, false)]
    [InlineData("java25-ea-arm64-properties.txt", 25, JavaArchitecture.Arm64, true)]
    public void ParsesSanitizedPropertyFixtures(string fixture, int major, JavaArchitecture arch, bool ea)
    {
        var text = Fixture(fixture).Replace("/fixture/", Path.GetTempPath(), StringComparison.Ordinal);
        var runtime = JavaProbe.Parse(Path.Combine(Path.GetTempPath(), ExecutableName), text);
        Assert.Equal(major, runtime.Version.Major);
        Assert.Equal(arch, runtime.Architecture);
        Assert.Equal(ea, runtime.Version.IsEarlyAccess);
        Assert.NotEmpty(runtime.Vendor!);
    }

    [Theory]
    [InlineData("i686", "32", JavaArchitecture.X86)]
    [InlineData("armv7l", "32", JavaArchitecture.Arm32)]
    [InlineData("ARM64", "64", JavaArchitecture.Arm64)]
    [InlineData("mystery", "64", JavaArchitecture.Unknown)]
    public void IdentifiesArchitectureWithoutAssumingHostBitness(string arch, string bits, JavaArchitecture expected)
    {
        var runtime = JavaProbe.Parse("unused", $"java.version = 21\njava.home = {Path.GetTempPath()}\nos.arch = {arch}\nsun.arch.data.model = {bits}");
        Assert.Equal(expected, runtime.Architecture);
    }

    [Theory]
    [InlineData("java.version = 21")]
    [InlineData("java.version = 21\njava.home = relative\nos.arch = amd64")]
    [InlineData("java.version = 21\njava.version = 17\njava.home = {home}\nos.arch = amd64")]
    [InlineData("java.version = 21\njava.home = {home}\nos.arch = amd64\nsun.arch.data.model = 32")]
    public void RejectsIncompleteConflictingOrInconsistentProperties(string text) =>
        Assert.Equal(JavaErrorCode.InvalidOutput, Assert.Throws<JavaException>(() =>
            JavaProbe.Parse("unused", text.Replace("{home}", Path.GetTempPath(), StringComparison.Ordinal))).Code);

    [Fact]
    public void RuleContextUsesSelectedJavaArchitectureAndCallerFeatures()
    {
        var context = Runtime(arch: JavaArchitecture.X86).GetRuleContext(new Dictionary<string, bool> { ["is_demo_user"] = true });
        Assert.Equal("x86", context.Architecture);
        Assert.False(context.Is64Bit);
        Assert.True(context.Features!["is_demo_user"]);
        Assert.Throws<JavaException>(() => Runtime(arch: JavaArchitecture.Unknown).GetRuleContext());
    }
}
