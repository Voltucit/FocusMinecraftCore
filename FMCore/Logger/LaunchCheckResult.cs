using FMCore.Java;
using FMCore.Launch;

namespace FMCore.Logger;

public sealed record LaunchCheckIssue(
    LaunchStage Stage,
    string Message,
    string ExceptionType);

public sealed record LaunchCheckResult(
    string VersionId,
    JavaInfo? Java,
    bool? UsesModernArguments,
    LaunchCheckIssue? Issue)
{
    public bool ConfigurationVerified => Issue is null;
}
