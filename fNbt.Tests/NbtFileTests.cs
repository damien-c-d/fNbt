using FluentAssertions;
using fNbt.Tags;

namespace fNbt.Tests;

public class NbtFileTests : TestBase, IDisposable
{
    private const string TestDirName = "NbtFileTests";

    public NbtFileTests()
    {
        Directory.CreateDirectory(TestDirName);
    }


    public void Dispose()
    {
        if (!Directory.Exists(TestDirName)) return;

        Directory.Delete(TestDirName, true);
    }


    [Fact]
    public void TestNbtSmallFileSavingUncompressed()
    {
        var file = TestFiles.MakeSmallFile();
        var testFileName = Path.Combine(TestDirName, "test.nbt");
        file.SaveToFile(testFileName, NbtCompression.None);
        TestFile(testFileName).Should().BeEquivalentTo(TestFiles.Small);
    }


    [Fact]
    public void TestNbtSmallFileSavingUncompressedStream()
    {
        var file = TestFiles.MakeSmallFile();
        var nbtStream = new MemoryStream();
        Invoking(() => file.SaveToStream(null, NbtCompression.None)).Should().Throw<ArgumentNullException>();
        Invoking(() => file.SaveToStream(nbtStream, NbtCompression.AutoDetect)).Should().Throw<ArgumentException>();
        Invoking(() => file.SaveToStream(nbtStream, (NbtCompression)255)).Should().Throw<ArgumentOutOfRangeException>();
        file.SaveToStream(nbtStream, NbtCompression.None);
        var testFileStream = File.OpenRead(TestFiles.Small);
        TestStream(nbtStream).Should().BeEquivalentTo(testFileStream);
    }


    [Fact]
    public void ReloadFile()
    {
        ReloadFileInternal("bigtest.nbt", NbtCompression.None, true, true);
        ReloadFileInternal("bigtest.nbt.gz", NbtCompression.GZip, true, true);
        ReloadFileInternal("bigtest.nbt.z", NbtCompression.ZLib, true, true);
        ReloadFileInternal("bigtest.nbt", NbtCompression.None, false, true);
        ReloadFileInternal("bigtest.nbt.gz", NbtCompression.GZip, false, true);
        ReloadFileInternal("bigtest.nbt.z", NbtCompression.ZLib, false, true);
    }


    [Fact]
    public void ReloadFileUnbuffered()
    {
        ReloadFileInternal("bigtest.nbt", NbtCompression.None, true, false);
        ReloadFileInternal("bigtest.nbt.gz", NbtCompression.GZip, true, false);
        ReloadFileInternal("bigtest.nbt.z", NbtCompression.ZLib, true, false);
        ReloadFileInternal("bigtest.nbt", NbtCompression.None, false, false);
        ReloadFileInternal("bigtest.nbt.gz", NbtCompression.GZip, false, false);
        ReloadFileInternal("bigtest.nbt.z", NbtCompression.ZLib, false, false);
    }


    private void ReloadFileInternal(string fileName, NbtCompression compression, bool bigEndian, bool buffered)
    {
        var loadedFile = new NbtFile(Path.Combine(TestFiles.DirName, fileName))
        {
            BigEndian = bigEndian
        };
        if (!buffered) loadedFile.BufferSize = 0;
        var bytesWritten = loadedFile.SaveToFile(Path.Combine(TestDirName, fileName), compression);
        var bytesRead = loadedFile.LoadFromFile(Path.Combine(TestDirName, fileName), NbtCompression.AutoDetect,
            null);
        bytesWritten.Should().Be(bytesRead);
        TestFiles.AssertNbtBigFile(loadedFile);
    }


    [Fact]
    public void ReloadNonSeekableStream()
    {
        var loadedFile = new NbtFile(TestFiles.Big);
        using var ms = new MemoryStream();
        using var nss = new NonSeekableStream(ms);
        var bytesWritten = loadedFile.SaveToStream(nss, NbtCompression.None);
        ms.Position = 0;

        Invoking(() => loadedFile.LoadFromStream(nss, NbtCompression.AutoDetect)).Should()
            .Throw<NotSupportedException>();
        ms.Position = 0;

        Invoking(() => loadedFile.LoadFromStream(nss, NbtCompression.ZLib)).Should().Throw<InvalidDataException>();
        ms.Position = 0;
        var bytesRead = loadedFile.LoadFromStream(nss, NbtCompression.None);

        bytesWritten.Should().Be(bytesRead);
        TestFiles.AssertNbtBigFile(loadedFile);
    }


    [Fact]
    public void LoadFromStream()
    {
        LoadFromStreamInternal(TestFiles.Big, NbtCompression.None);
        LoadFromStreamInternal(TestFiles.BigGZip, NbtCompression.GZip);
        LoadFromStreamInternal(TestFiles.BigZLib, NbtCompression.ZLib);
    }


    private void LoadFromStreamInternal(string fileName, NbtCompression compression)
    {
        var file = new NbtFile();
        var fileBytes = File.ReadAllBytes(fileName);
        using var ms = new MemoryStream(fileBytes);
        file.LoadFromStream(ms, compression);
    }


    [Fact]
    public void SaveToBuffer()
    {
        var littleTag = new NbtCompound("Root");
        var testFile = new NbtFile(littleTag);

        var buffer1 = testFile.SaveToBuffer(NbtCompression.None);
        var buffer2 = new byte[buffer1.Length];
        testFile.SaveToBuffer(buffer2, 0, NbtCompression.None).Should().Be(buffer2.Length);
        buffer1.Should().Equal(buffer2);
    }


    [Fact]
    public void PrettyPrint()
    {
        var loadedFile = new NbtFile(TestFiles.Big);
        loadedFile.RootTag.ToString().Should().Be(loadedFile.ToString());
        loadedFile.RootTag.ToString("   ").Should().Be(loadedFile.ToString("   "));
        Invoking(() => loadedFile.ToString(null)).Should().Throw<ArgumentNullException>();
        Invoking(() => NbtTag.DefaultIndentString = null).Should().Throw<ArgumentNullException>();
    }


    [Fact]
    public void ReadRootTag()
    {
        Invoking(() => NbtFile.ReadRootTagName("NonExistentFile")).Should().Throw<FileNotFoundException>();

        ReadRootTagInternal(TestFiles.Big, NbtCompression.None);
        ReadRootTagInternal(TestFiles.BigGZip, NbtCompression.GZip);
        ReadRootTagInternal(TestFiles.BigZLib, NbtCompression.ZLib);
    }


    private void ReadRootTagInternal(string fileName, NbtCompression compression)
    {
        Invoking(() => NbtFile.ReadRootTagName(fileName, compression, true, -1)).Should()
            .Throw<ArgumentOutOfRangeException>();
        Invoking(() => NbtFile.ReadRootTagName(fileName, (NbtCompression)255, true, 0)).Should()
            .Throw<ArgumentOutOfRangeException>();

        NbtFile.ReadRootTagName(fileName).Should().Be("Level");
        NbtFile.ReadRootTagName(fileName, compression, true, 0).Should().Be("Level");

        var fileBytes = File.ReadAllBytes(fileName);
        using var ms = new MemoryStream(fileBytes);
        using var nss = new NonSeekableStream(ms);
        //    () => NbtFile.ReadRootTagName(nss, compression, true, -1));
        Invoking(() => NbtFile.ReadRootTagName(nss, compression, true, -1)).Should()
            .Throw<ArgumentOutOfRangeException>();
        NbtFile.ReadRootTagName(nss, compression, true, 0);
    }


    [Fact]
    public void GlobalsTest()
    {
        NbtFile.DefaultBufferSize.Should().Be(new NbtFile(new NbtCompound("Foo")).BufferSize);
        Invoking(() => NbtFile.DefaultBufferSize = -1).Should().Throw<ArgumentOutOfRangeException>();
        NbtFile.DefaultBufferSize = 12345;
        NbtFile.DefaultBufferSize.Should().Be(12345);

        // Newly-created NbtFiles should use default buffer size
        var tempFile = new NbtFile(new NbtCompound("Foo"));
        NbtFile.DefaultBufferSize.Should().Be(tempFile.BufferSize);
        Invoking(() => tempFile.BufferSize = -1).Should().Throw<ArgumentOutOfRangeException>();
        tempFile.BufferSize = 54321;
        tempFile.BufferSize.Should().Be(54321);

        // Changing default buffer size should not retroactively change already-existing NbtFiles' buffer size.
        NbtFile.DefaultBufferSize = 8192;
        tempFile.BufferSize.Should().Be(54321);
    }


    [Fact]
    public void HugeNbtFileTest()
    {
        // Tests writing byte arrays that exceed the max NbtBinaryWriter chunk size
        var val = new byte[5 * 1024 * 1024];
        var root = new NbtCompound("root")
        {
            new NbtByteArray("payload1")
            {
                Value = val
            }
        };
        var file = new NbtFile(root);
        file.SaveToStream(Stream.Null, NbtCompression.None);
    }


    [Fact]
    public void RootTagTest()
    {
        var oldRoot = new NbtCompound("defaultRoot");
        var newFile = new NbtFile(oldRoot);

        // Ensure that inappropriate tags are not accepted as RootTag
        Invoking(() => newFile.RootTag = null).Should().Throw<ArgumentNullException>();
        Invoking(() => newFile.RootTag = []).Should().Throw<ArgumentException>();

        // Ensure that the root has not changed
        newFile.RootTag.Should().BeSameAs(oldRoot);

        // Invalidate the root tag, and ensure that expected exception is thrown
        oldRoot.Name = null;
        Invoking(() => newFile.SaveToBuffer(NbtCompression.None)).Should().Throw<NbtFormatException>();
    }


    [Fact]
    public void NullParameterTest()
    {
        Invoking(() => new NbtFile((NbtCompound)null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtFile((string)null)).Should().Throw<ArgumentNullException>();

        var file = new NbtFile();
        Invoking(() => file.LoadFromBuffer(null, 0, 1, NbtCompression.None)).Should().Throw<ArgumentNullException>();
        Invoking(() => file.LoadFromBuffer(null, 0, 1, NbtCompression.None, tag => true)).Should()
            .Throw<ArgumentNullException>();
        Invoking(() => file.LoadFromFile(null)).Should().Throw<ArgumentNullException>();
        Invoking(() => file.LoadFromFile(null, NbtCompression.None, tag => true)).Should()
            .Throw<ArgumentNullException>();
        Invoking(() => file.LoadFromStream(null, NbtCompression.AutoDetect)).Should().Throw<ArgumentNullException>();
        Invoking(() => file.LoadFromStream(null, NbtCompression.AutoDetect, tag => true)).Should()
            .Throw<ArgumentNullException>();

        Invoking(() => file.SaveToBuffer(null, 0, NbtCompression.None)).Should().Throw<ArgumentNullException>();
        Invoking(() => file.SaveToFile(null, NbtCompression.None)).Should().Throw<ArgumentNullException>();
        Invoking(() => file.SaveToStream(null, NbtCompression.None)).Should().Throw<ArgumentNullException>();

        //    () => NbtFile.ReadRootTagName((Stream)null, NbtCompression.None, true, 0));
        Invoking(() => NbtFile.ReadRootTagName(null)).Should().Throw<ArgumentNullException>();
        Invoking(() => NbtFile.ReadRootTagName((Stream)null, NbtCompression.None, true, 0)).Should()
            .Throw<ArgumentNullException>();
    }


    #region Loading Small Nbt Test File

    [Fact]
    public void TestNbtSmallFileLoadingUncompressed()
    {
        var file = new NbtFile(TestFiles.Small);
        file.FileName.Should().Be(TestFiles.Small);
        file.FileCompression.Should().Be(NbtCompression.None);
        TestFiles.AssertNbtSmallFile(file);
    }


    [Fact]
    public void LoadingSmallFileGZip()
    {
        var file = new NbtFile(TestFiles.SmallGZip);
        file.FileName.Should().Be(TestFiles.SmallGZip);
        file.FileCompression.Should().Be(NbtCompression.GZip);
        TestFiles.AssertNbtSmallFile(file);
    }


    [Fact]
    public void LoadingSmallFileZLib()
    {
        var file = new NbtFile(TestFiles.SmallZLib);
        file.FileName.Should().Be(TestFiles.SmallZLib);
        file.FileCompression.Should().Be(NbtCompression.ZLib);
        TestFiles.AssertNbtSmallFile(file);
    }

    #endregion


    #region Loading Big Nbt Test File

    [Fact]
    public void LoadingBigFileUncompressed()
    {
        var file = new NbtFile();
        var length = file.LoadFromFile(TestFiles.Big);
        TestFiles.AssertNbtBigFile(file);
        length.Should().Be(new FileInfo(TestFiles.Big).Length);
    }


    [Fact]
    public void LoadingBigFileGZip()
    {
        var file = new NbtFile();
        var length = file.LoadFromFile(TestFiles.BigGZip);
        TestFiles.AssertNbtBigFile(file);
        length.Should().Be(new FileInfo(TestFiles.BigGZip).Length);
    }


    [Fact]
    public void LoadingBigFileZLib()
    {
        var file = new NbtFile();
        var length = file.LoadFromFile(TestFiles.BigZLib);
        TestFiles.AssertNbtBigFile(file);
        length.Should().Be(new FileInfo(TestFiles.BigZLib).Length);
    }


    [Fact]
    public void LoadingBigFileBuffer()
    {
        var fileBytes = File.ReadAllBytes(TestFiles.Big);
        var file = new NbtFile();

        Invoking(() => file.LoadFromBuffer(null, 0, fileBytes.Length, NbtCompression.AutoDetect, null))
            .Should().Throw<ArgumentNullException>();

        var length = file.LoadFromBuffer(fileBytes, 0, fileBytes.Length, NbtCompression.AutoDetect, null);
        TestFiles.AssertNbtBigFile(file);
        length.Should().Be(new FileInfo(TestFiles.Big).Length);
    }


    [Fact]
    public void LoadingBigFileStream()
    {
        var fileBytes = File.ReadAllBytes(TestFiles.Big);
        using var ms = new MemoryStream(fileBytes);
        using var nss = new NonSeekableStream(ms);
        var file = new NbtFile();
        var length = file.LoadFromStream(nss, NbtCompression.None, null);
        TestFiles.AssertNbtBigFile(file);

        length.Should().Be(new FileInfo(TestFiles.Big).Length);
    }

    #endregion
}