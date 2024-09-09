namespace fNbt.Tests;

internal class NonSeekableStream(Stream baseStream) : Stream
{
    public override bool CanRead => baseStream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => baseStream.CanWrite;


    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }


    public override void Flush()
    {
        baseStream.Flush();
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        return baseStream.Read(buffer, offset, count);
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }


    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }


    public override void Write(byte[] buffer, int offset, int count)
    {
        baseStream.Write(buffer, offset, count);
    }
}