using System.IO.Compression;

namespace FMCore.Modpacks;

// Central-directory lengths are untrusted. Bound actual decompressed bytes as well as declared sizes.
internal sealed class ArchiveEntryStream : Stream
{
    private readonly Stream input;
    private readonly long expected;
    private long count;
    internal ArchiveEntryStream(ZipArchiveEntry entry) { input = entry.Open(); expected = entry.Length; }
    private int Checked(int length)
    {
        count += length;
        if (count > expected) throw PackArchive.Limit();
        if (length == 0 && count != expected) throw PackArchive.Invalid();
        return length;
    }
    public override int Read(byte[] buffer, int offset, int count) => Checked(input.Read(buffer, offset, (int)Math.Min(count, expected - this.count + 1)));
    public override int Read(Span<byte> buffer) => Checked(input.Read(buffer[..(int)Math.Min(buffer.Length, expected - count + 1)]));
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        Checked(await input.ReadAsync(buffer[..(int)Math.Min(buffer.Length, expected - count + 1)], cancellationToken).ConfigureAwait(false));
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    protected override void Dispose(bool disposing) { if (disposing) input.Dispose(); base.Dispose(disposing); }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => expected;
    public override long Position { get => count; set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
