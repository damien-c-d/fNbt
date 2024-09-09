﻿using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace fNbt;

// Class used to count bytes read-from/written-to non-seekable streams.
internal class ByteCountingStream : Stream
{
    private readonly Stream _baseStream;

    // These are necessary to avoid counting bytes twice if Read/Write call ReadByte/WriteByte internally.
    private bool _readingManyBytes;

    // These are necessary to avoid counting bytes twice if ReadByte/WriteByte call Read/Write internally.
    private bool _readingOneByte;
    private bool _writingManyBytes;
    private bool _writingOneByte;


    public ByteCountingStream([NotNull] Stream stream)
    {
        Debug.Assert(stream != null);
        _baseStream = stream;
    }


    public override bool CanRead => _baseStream.CanRead;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => _baseStream.CanWrite;

    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public long BytesRead { get; private set; }
    public long BytesWritten { get; private set; }


    public override void Flush()
    {
        _baseStream.Flush();
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        return _baseStream.Seek(offset, origin);
    }


    public override void SetLength(long value)
    {
        _baseStream.SetLength(value);
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        _readingManyBytes = true;
        var bytesActuallyRead = _baseStream.Read(buffer, offset, count);
        _readingManyBytes = false;
        if (!_readingOneByte) BytesRead += bytesActuallyRead;
        return bytesActuallyRead;
    }


    public override void Write(byte[] buffer, int offset, int count)
    {
        _writingManyBytes = true;
        _baseStream.Write(buffer, offset, count);
        _writingManyBytes = false;
        if (!_writingOneByte) BytesWritten += count;
    }


    public override int ReadByte()
    {
        _readingOneByte = true;
        var value = base.ReadByte();
        _readingOneByte = false;
        if (value >= 0 && !_readingManyBytes) BytesRead++;
        return value;
    }


    public override void WriteByte(byte value)
    {
        _writingOneByte = true;
        base.WriteByte(value);
        _writingOneByte = false;
        if (!_writingManyBytes) BytesWritten++;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _baseStream.Dispose();
    }

    public override ValueTask DisposeAsync()
    {
        _baseStream.Dispose();
        return base.DisposeAsync();
    }
}