using System.Net;

namespace FMCore.Downloader;

public enum DownloadErrorCode
{
    InvalidRequest,
    UnsafePath,
    DestinationBusy,
    HttpError,
    NetworkError,
    Timeout,
    HashMismatch,
    SizeMismatch,
    IoError,
    LocalFileUnavailable
}

/// <summary>Sanitized failure information; request URLs and underlying HTTP exceptions are omitted.</summary>
public sealed class DownloadException : Exception
{
    public DownloadErrorCode Code { get; }
    public string? RelativePath { get; }
    public int Attempt { get; }
    public HttpStatusCode? StatusCode { get; }
    internal bool Retryable { get; }
    internal TimeSpan? RetryAfter { get; }

    internal DownloadException(DownloadErrorCode code, string message, string? relativePath = null,
        int attempt = 0, HttpStatusCode? statusCode = null, bool retryable = false, TimeSpan? retryAfter = null)
        : base(message)
    {
        Code = code;
        RelativePath = relativePath;
        Attempt = attempt;
        StatusCode = statusCode;
        Retryable = retryable;
        RetryAfter = retryAfter;
    }
}
