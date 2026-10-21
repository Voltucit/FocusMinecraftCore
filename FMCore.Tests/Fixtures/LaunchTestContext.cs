using System.Diagnostics;
using System.Text;
using FMCore.Authentication;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

namespace FMCore.Tests.Fixtures;

internal sealed class LaunchTestContext : IDisposable
{
    internal InstallationTestContext Files { get; } = new();
    internal AccountManager Accounts { get; }
    internal string Root => Files.Root;
    internal ProcessStartInfo? CapturedStart { get; private set; }
    internal JavaManager Javas { get; }
    internal static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal LaunchTestContext()
    {
        Accounts = new(Path.Combine(Root, "accounts"));
        Javas = new(JavaTestContext.EmptyOptions, (_, ct) =>
        { ct.ThrowIfCancellationRequested(); return Task.FromResult(JavaTestContext.Runtime("1.8.0_482")); });
    }

    internal async Task SeedAsync(bool legacy = false, bool virtualAssets = false)
    {
        var document = await Files.SeedAsync(virtualAssets, virtualAssets);
        document["javaVersion"] = new System.Text.Json.Nodes.JsonObject { ["majorVersion"] = 8 };
        if (legacy)
            document["minecraftArguments"] = "--username ${auth_player_name} --session ${auth_session} --gameDir ${game_directory} --assetsDir ${game_assets}";
        else
            document["arguments"] = new System.Text.Json.Nodes.JsonObject
            {
                ["game"] = new System.Text.Json.Nodes.JsonArray("--username", "${auth_player_name}", "--uuid", "${auth_uuid}", "--accessToken", "${auth_access_token}", "--gameDir", "${game_directory}")
            };
        await Files.SaveAsync(document);
    }

    internal async Task<LaunchConfig> ConfigAsync() => new()
    {
        GameCore = new() { RootDirectory = Root, VersionId = "fixture", CachePolicy = VersionCachePolicy.CacheOnly },
        Account = await Accounts.LoginOfflineAsync("Player", Ct),
        Java = new() { JavaPath = Path.Combine(Root, "java", "bin", JavaTestContext.ExecutableName), Selection = new() { TargetArchitecture = JavaArchitecture.X64 } }
    };

    internal MinecraftLauncher Launcher(LaunchConfig config, string behavior = "echo", JavaManager? javas = null) =>
        new(config, Files.Downloads, javas ?? Javas, start =>
        {
            CapturedStart = start;
            var fixture = JavaProcessFixture.Start("game", [behavior, .. start.ArgumentList]);
            fixture.WorkingDirectory = start.WorkingDirectory;
            fixture.UseShellExecute = false;
            fixture.CreateNoWindow = true;
            fixture.RedirectStandardOutput = true;
            fixture.RedirectStandardError = true;
            fixture.StandardOutputEncoding = Encoding.UTF8;
            fixture.StandardErrorEncoding = Encoding.UTF8;
            fixture.Environment.Clear();
            foreach (var variable in start.Environment) fixture.Environment[variable.Key] = variable.Value;
            return fixture;
        });

    public void Dispose() { Accounts.Dispose(); Files.Dispose(); }
}
