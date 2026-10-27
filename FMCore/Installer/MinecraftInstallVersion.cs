namespace FMCore.Installer;

public sealed record MinecraftInstallVersion(
    string Id,
    string Type,
    DateTimeOffset ReleaseTime);
