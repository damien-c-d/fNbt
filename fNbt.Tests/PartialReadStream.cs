namespace fNbt.Tests;

internal class PartialReadStream(Stream baseStream, int increment) : Stream
{
    private readonly Stream _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

    public PartialReadStream(Stream baseStream)
        : this(baseStream, 1)
    {
    }


    public override bool CanRead => true;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => false;

    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }


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
        var bytesToRead = Math.Min(increment, count);
        return _baseStream.Read(buffer, offset, bytesToRead);
    }


    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}