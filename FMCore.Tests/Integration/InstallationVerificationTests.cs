using FMCore.Downloader;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.InstallationTestContext;

namespace FMCore.Tests.Integration;

public sealed class InstallationVerificationTests
{
    [Fact]
    public async Task ChecksMissingAndSameSizeCorruptionWithoutNetworkThenRepairsOnlyDamagedFiles()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        var installed = await context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct);
        var client = await File.ReadAllBytesAsync(installed.ClientJarPath, Ct);
        client[0] ^= 0xff;
        await File.WriteAllBytesAsync(installed.ClientJarPath, client, Ct);
        File.Delete(installed.LoggingPath!);
        context.Requests.Clear();
        var check = await context.Installer.VerifyAsync("fixture", Options, cancellationToken: Ct);
        Assert.False(check.IsValid);
        Assert.Contains(check.Problems, p => p.State == FileVerificationState.HashMismatch);
        Assert.Contains(check.Problems, p => p.State == FileVerificationState.Missing);
        Assert.Empty(context.Requests);
        var repaired = await context.Installer.RepairAsync("fixture", Options, cancellationToken: Ct);
        Assert.Equal(2, repaired.DownloadedFiles);
        Assert.Equal(2, context.Requests.Values.Sum());
        Assert.True((await context.Installer.VerifyAsync("fixture", Options, cancellationToken: Ct)).IsValid);
    }

    [Fact]
    public async Task VerifiesAndRepairsVirtualAndResourcesCopies()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync(virtualAssets: true, mapToResources: true);
        var options = Options with { GameDirectory = "instances/legacy" };
        await context.Installer.InstallAsync("fixture", options, cancellationToken: Ct);
        File.Delete(context.At("assets/virtual/fixture-assets/sounds/first.ogg"));
        File.Delete(context.At("instances/legacy/resources/sounds/first.ogg"));
        context.Requests.Clear();
        Assert.Equal(2, (await context.Installer.VerifyAsync("fixture", options, cancellationToken: Ct)).Problems.Count);
        Assert.Equal(0, (await context.Installer.RepairAsync("fixture", options, cancellationToken: Ct)).DownloadedFiles);
        Assert.True((await context.Installer.VerifyAsync("fixture", options, cancellationToken: Ct)).IsValid);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task MissingAssetIndexCannotBeReportedAsACompleteCheck()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        var check = await context.Installer.VerifyAsync("fixture", Options, cancellationToken: Ct);
        Assert.False(check.AssetsChecked);
        Assert.False(check.IsValid);
        Assert.Empty(context.Requests);
    }
}
