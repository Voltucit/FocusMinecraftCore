namespace FMCore.Launch;

public enum LaunchStage
{
    ReadingVersion,
    DetectingJava,
    PreparingGame,
    StartingJava,
    JavaStarted,
    GameExited
}

public sealed record LaunchProgress(
    LaunchStage Stage,
    string Message);
