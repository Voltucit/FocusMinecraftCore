namespace FMCore.Logger;

public enum GameFileIssueKind
{
    Missing,
    SizeMismatch,
    Sha1Mismatch,
    MetadataMissing
}

public sealed record GameFileIssue(
    string Name,
    string FilePath,
    GameFileIssueKind Kind,
    string Message,
    bool IsLocallyGenerated = false);

public sealed record GameFileCheckProgress(
    int CheckedFiles,
    int TotalKnownFiles,
    string? FilePath);

public sealed record GameFileCheckResult(
    int CheckedFiles,
    int VerifiedFiles,
    bool AssetObjectsEnumerated,
    IReadOnlyList<GameFileIssue> Issues)
{
    public int MissingFiles =>
        Issues.Count(item => item.Kind == GameFileIssueKind.Missing);

    public int InvalidFiles => Issues.Count(item => item.Kind is
        GameFileIssueKind.SizeMismatch or GameFileIssueKind.Sha1Mismatch);

    public int UnverifiableFiles =>
        Issues.Count(item => item.Kind == GameFileIssueKind.MetadataMissing);

    public bool FileContentVerified =>
        AssetObjectsEnumerated && Issues.Count == 0
                               && CheckedFiles == VerifiedFiles;
}
