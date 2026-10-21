namespace FMCore.Authentication;

/// <summary>Host-provided secure storage on non-Windows platforms. Payloads contain secrets; store them only in a system vault or equivalent encrypted storage.</summary>
public interface IAccountTokenStore
{
    /// <summary>Returns a fresh, caller-owned plaintext buffer, or null when missing. The manager clears this buffer after reading.</summary>
    Task<byte[]?> ReadAsync(Guid accountId, CancellationToken cancellationToken = default);
    /// <summary>Atomically replaces the protected credential. Complete the secure write before returning; do not retain the supplied buffer.</summary>
    Task WriteAsync(Guid accountId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
    /// <summary>Removes the protected credential; a missing entry is already deleted.</summary>
    Task DeleteAsync(Guid accountId, CancellationToken cancellationToken = default);
}
