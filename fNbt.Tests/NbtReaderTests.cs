using FluentAssertions;
using fNbt.Tags;
using Xunit.Abstractions;

namespace fNbt.Tests;

public sealed class NbtReaderTests(ITestOutputHelper testOutputHelper) : TestBase
{
    [Fact]
    public void PrintBigFileUncompressed()
    {
        using var fs = File.OpenRead(TestFiles.Big);
        var reader = new NbtReader(fs);
        fs.Should().BeSameAs(reader.BaseStream);
        while (reader.ReadToFollowing()) testOutputHelper.WriteLine($"@{reader.TagStartOffset} {reader}");
        reader.RootName.Should().Be("Level");
    }


    [Fact]
    public void PrintBigFileUncompressedNoSkip()
    {
        using var fs = File.OpenRead(TestFiles.Big);
        var reader = new NbtReader(fs)
        {
            SkipEndTags = false
        };
        fs.Should().BeSameAs(reader.BaseStream);
        while (reader.ReadToFollowing()) testOutputHelper.WriteLine($"@{reader.TagStartOffset} {reader}");
        reader.RootName.Should().Be("Level");
    }


    [Fact]
    public void CacheTagValuesTest()
    {
        var testData = new NbtFile(TestFiles.MakeValueTest()).SaveToBuffer(NbtCompression.None);
        var reader = new NbtReader(new MemoryStream(testData));
        reader.CacheTagValues.Should().BeFalse();
        reader.CacheTagValues = true;
        reader.ReadToFollowing().Should().BeTrue(); // root

        reader.ReadToFollowing().Should().BeTrue(); // byte
        reader.ReadValue().Should().Be(1);
        reader.ReadValue().Should().Be(1);
        reader.ReadToFollowing().Should().BeTrue(); // short
        reader.ReadValue().Should().Be(2);
        reader.ReadValue().Should().Be(2);
        reader.ReadToFollowing().Should().BeTrue(); // int
        reader.ReadValue().Should().Be(3);
        reader.ReadValue().Should().Be(3);
        reader.ReadToFollowing().Should().BeTrue(); // long
        reader.ReadValue().Should().Be(4L);
        reader.ReadValue().Should().Be(4L);
        reader.ReadToFollowing().Should().BeTrue(); // float
        reader.ReadValue().Should().Be(5f);
        reader.ReadValue().Should().Be(5f);
        reader.ReadToFollowing().Should().BeTrue(); // double
        reader.ReadValue().Should().Be(6d);
        reader.ReadValue().Should().Be(6d);
        reader.ReadToFollowing().Should().BeTrue(); // byteArray
        ((byte[])reader.ReadValue()).Should().BeEquivalentTo(new byte[] { 10, 11, 12 });
        ((byte[])reader.ReadValue()).Should().BeEquivalentTo(new byte[] { 10, 11, 12 });
        reader.ReadToFollowing().Should().BeTrue(); // intArray
        ((int[])reader.ReadValue()).Should().BeEquivalentTo([20, 21, 22]);
        ((int[])reader.ReadValue()).Should().BeEquivalentTo([20, 21, 22]);
        reader.ReadToFollowing().Should().BeTrue();
        ((long[])reader.ReadValue()).Should().BeEquivalentTo(new long[] { 200, 210, 220 });
        ((long[])reader.ReadValue()).Should().BeEquivalentTo(new long[] { 200, 210, 220 });
        reader.ReadToFollowing().Should().BeTrue(); // string
        reader.ReadValue().Should().Be("123");
        reader.ReadValue().Should().Be("123");
    }


    [Fact]
    public void NestedListTest()
    {
        var root = new NbtCompound("root")
        {
            new NbtList("OuterList")
            {
                new NbtList
                {
                    new NbtByte()
                },
                new NbtList
                {
                    new NbtShort()
                },
                new NbtList
                {
                    new NbtInt()
                }
            }
        };
        var testData = new NbtFile(root).SaveToBuffer(NbtCompression.None);
        using var ms = new MemoryStream(testData);
        var reader = new NbtReader(ms);
        while (reader.ReadToFollowing()) testOutputHelper.WriteLine(reader.ToString(true));
    }


    [Fact]
    public void PropertiesTest()
    {
        var reader = new NbtReader(TestFiles.MakeReaderTest());
        reader.Depth.Should().Be(0);
        reader.TagsRead.Should().Be(0);

        reader.ReadToFollowing().Should().BeTrue();
        reader.TagName.Should().Be("root");
        reader.TagType.Should().Be(NbtTagType.Compound);
        reader.ListType.Should().Be(NbtTagType.Unknown);
        reader.HasValue.Should().BeFalse();
        reader.IsCompound.Should().BeTrue();
        reader.IsList.Should().BeFalse();
        reader.IsListElement.Should().BeFalse();
        reader.HasLength.Should().BeFalse();
        reader.ListIndex.Should().Be(0);
        reader.Depth.Should().Be(1);
        reader.ParentName.Should().BeNull();
        reader.ParentTagType.Should().Be(NbtTagType.Unknown);
        reader.ParentTagLength.Should().Be(0);
        reader.TagLength.Should().Be(0);
        reader.TagsRead.Should().Be(1);

        reader.ReadToFollowing().Should().BeTrue();
        reader.TagName.Should().Be("first");
        reader.TagType.Should().Be(NbtTagType.Int);
        reader.ListType.Should().Be(NbtTagType.Unknown);
        reader.HasValue.Should().BeTrue();
        reader.IsCompound.Should().BeFalse();
        reader.IsList.Should().BeFalse();
        reader.IsListElement.Should().BeFalse();
        reader.HasLength.Should().BeFalse();
        reader.ListIndex.Should().Be(0);
        reader.Depth.Should().Be(2);
        reader.ParentName.Should().Be("root");
        reader.ParentTagType.Should().Be(NbtTagType.Compound);
        reader.ParentTagLength.Should().Be(0);
        reader.TagLength.Should().Be(0);
        reader.TagsRead.Should().Be(2);

        reader.ReadToFollowing("fourth-list").Should().BeTrue();
        reader.TagName.Should().Be("fourth-list");
        reader.TagType.Should().Be(NbtTagType.List);
        reader.ListType.Should().Be(NbtTagType.List);
        reader.HasValue.Should().BeFalse();
        reader.IsCompound.Should().BeFalse();
        reader.IsList.Should().BeTrue();
        reader.IsListElement.Should().BeFalse();
        reader.HasLength.Should().BeTrue();
        reader.ListIndex.Should().Be(0);
        reader.Depth.Should().Be(2);
        reader.ParentName.Should().Be("root");
        reader.ParentTagType.Should().Be(NbtTagType.Compound);
        reader.ParentTagLength.Should().Be(0);
        reader.TagLength.Should().Be(3);
        reader.TagsRead.Should().Be(8);

        reader.ReadToFollowing().Should().BeTrue();
        reader.TagName.Should().BeNull();
        reader.TagType.Should().Be(NbtTagType.List);
        reader.ListType.Should().Be(NbtTagType.Compound);
        reader.HasValue.Should().BeFalse();
        reader.IsCompound.Should().BeFalse();
        reader.IsList.Should().BeTrue();
        reader.IsListElement.Should().BeTrue();
        reader.HasLength.Should().BeTrue();
        reader.ListIndex.Should().Be(0);
        reader.Depth.Should().Be(3);
        reader.ParentName.Should().Be("fourth-list");
        reader.ParentTagType.Should().Be(NbtTagType.List);
        reader.ParentTagLength.Should().Be(3);
        reader.TagLength.Should().Be(1);
        reader.TagsRead.Should().Be(9);

        reader.ReadToFollowing().Should().BeTrue();
        reader.TagName.Should().BeNull();
        reader.TagType.Should().Be(NbtTagType.Compound);
        reader.ListType.Should().Be(NbtTagType.Unknown);
        reader.HasValue.Should().BeFalse();
        reader.IsCompound.Should().BeTrue();
        reader.IsList.Should().BeFalse();
        reader.IsListElement.Should().BeTrue();
        reader.HasLength.Should().BeFalse();
        reader.ListIndex.Should().Be(0);
        reader.Depth.Should().Be(4);
        reader.ParentName.Should().BeNull();
        reader.ParentTagType.Should().Be(NbtTagType.List);
        reader.ParentTagLength.Should().Be(1);
        reader.TagLength.Should().Be(0);
        reader.TagsRead.Should().Be(10);


        reader.ReadToFollowing("fifth").Should().BeTrue();
        reader.TagName.Should().Be("fifth");
        reader.TagType.Should().Be(NbtTagType.Int);
        reader.ListType.Should().Be(NbtTagType.Unknown);
        reader.HasValue.Should().BeTrue();
        reader.IsCompound.Should().BeFalse();
        reader.IsList.Should().BeFalse();
        reader.IsListElement.Should().BeFalse();
        reader.HasLength.Should().BeFalse();
        reader.ListIndex.Should().Be(0);
        reader.Depth.Should().Be(2);
        reader.ParentName.Should().Be("root");
        reader.ParentTagType.Should().Be(NbtTagType.Compound);
        reader.ParentTagLength.Should().Be(0);
        reader.TagLength.Should().Be(0);
        reader.TagsRead.Should().Be(18);

        reader.ReadToFollowing().Should().BeTrue();
        reader.TagName.Should().Be("hugeArray");
        reader.TagType.Should().Be(NbtTagType.ByteArray);
        reader.ListType.Should().Be(NbtTagType.Unknown);
        reader.HasValue.Should().BeTrue();
        reader.IsCompound.Should().BeFalse();
        reader.IsList.Should().BeFalse();
        reader.IsListElement.Should().BeFalse();
        reader.HasLength.Should().BeTrue();
        reader.ListIndex.Should().Be(0);
        reader.Depth.Should().Be(2);
        reader.ParentName.Should().Be("root");
        reader.ParentTagType.Should().Be(NbtTagType.Compound);
        reader.ParentTagLength.Should().Be(0);
        reader.TagLength.Should().Be(1024 * 1024);
        reader.TagsRead.Should().Be(19);
    }


    [Fact]
    public void ReadToSiblingTest()
    {
        var reader = new NbtReader(TestFiles.MakeReaderTest());
        reader.ReadToFollowing().Should().BeTrue();
        reader.TagName.Should().Be("root");
        reader.ReadToFollowing().Should().BeTrue();
        reader.TagName.Should().Be("first");
        reader.ReadToNextSibling("third-comp").Should().BeTrue();
        reader.TagName.Should().Be("third-comp");
        reader.ReadToNextSibling().Should().BeTrue();
        reader.TagName.Should().Be("fourth-list");
        reader.ReadToNextSibling().Should().BeTrue();
        reader.TagName.Should().Be("fifth");
        reader.ReadToNextSibling().Should().BeTrue();
        reader.TagName.Should().Be("hugeArray");
        reader.ReadToNextSibling().Should().BeFalse();
        // Test twice, since we hit different paths through the code
        reader.ReadToNextSibling().Should().BeFalse();
    }


    [Fact]
    public void ReadToSiblingTest2()
    {
        var reader = new NbtReader(TestFiles.MakeReaderTest());
        reader.ReadToFollowing("inComp1").Should().BeTrue();
        // Expect all siblings to be read while we search for a non-existent one
        reader.ReadToNextSibling("no such tag").Should().BeFalse();
        // Expect to pop out of "third-comp" by now
        reader.TagName.Should().Be("fourth-list");
    }


    [Fact]
    public void ReadToFollowingNotFound()
    {
        var reader = new NbtReader(TestFiles.MakeReaderTest());
        reader.ReadToFollowing().Should().BeTrue(); // at "root"
        reader.ReadToFollowing("no such tag").Should().BeFalse();
        reader.ReadToFollowing("not this one either").Should().BeFalse();
        reader.IsAtStreamEnd.Should().BeTrue();
    }


    [Fact]
    public void ReadToDescendantTest()
    {
        var reader = new NbtReader(TestFiles.MakeReaderTest());
        reader.ReadToDescendant("third-comp").Should().BeTrue();
        reader.TagName.Should().Be("third-comp");
        reader.ReadToDescendant("inComp2").Should().BeTrue();
        reader.TagName.Should().Be("inComp2");
        reader.ReadToDescendant("derp").Should().BeFalse();
        reader.TagName.Should().Be("inComp3");
        reader.ReadToFollowing(); // at fourth-list
        reader.ReadToDescendant("inList2").Should().BeTrue();
        reader.TagName.Should().Be("inList2");

        // Read through the rest of the file until we run out of tags in a compound
        reader.ReadToDescendant("*").Should().BeFalse();

        // Ensure ReadToDescendant returns false when at end-of-stream
        while (reader.ReadToFollowing())
        {
        }

        reader.ReadToDescendant("*").Should().BeFalse();

        // Ensure that this works even on the root
        new NbtReader(TestFiles.MakeReaderTest()).ReadToDescendant("*").Should().BeFalse();
    }


    [Fact]
    public void SkipTest()
    {
        var reader = new NbtReader(TestFiles.MakeReaderTest());
        reader.ReadToFollowing(); // at root
        reader.ReadToFollowing(); // at first
        reader.ReadToFollowing(); // at second
        reader.ReadToFollowing(); // at third-comp
        reader.ReadToFollowing(); // at inComp1
        reader.TagName.Should().Be("inComp1");
        reader.Skip().Should().Be(2);
        reader.TagName.Should().Be("fourth-list");
        reader.Skip().Should().Be(11);
        reader.ReadToFollowing().Should().BeFalse();
        reader.Skip().Should().Be(0);
    }


    [Fact]
    public void ReadAsTagTest1()
    {
        // read various lists/compounds as tags
        var reader = new NbtReader(TestFiles.MakeReaderTest());
        reader.ReadToFollowing(); // skip root
        while (!reader.IsAtStreamEnd) reader.ReadAsTag();
        Invoking(() => reader.ReadAsTag()).Should().Throw<EndOfStreamException>();
    }


    [Fact]
    public void ReadAsTagTest2()
    {
        // read the whole thing as one tag
        var testData = new NbtFile(TestFiles.MakeValueTest()).SaveToBuffer(NbtCompression.None);
        {
            var reader = new NbtReader(new MemoryStream(testData));
            var root = (NbtCompound)reader.ReadAsTag();
            TestFiles.AssertValueTest(new NbtFile(root));
        }
        {
            // Try the same thing but with end tag skipping disabled
            var reader = new NbtReader(new MemoryStream(testData))
            {
                SkipEndTags = false
            };
            var root = (NbtCompound)reader.ReadAsTag();
            TestFiles.AssertValueTest(new NbtFile(root));
        }
    }


    [Fact]
    public void ReadAsTagTest3()
    {
        // read values as tags
        var testData = new NbtFile(TestFiles.MakeValueTest()).SaveToBuffer(NbtCompression.None);
        var reader = new NbtReader(new MemoryStream(testData));
        var root = new NbtCompound("root");

        // skip root
        reader.ReadToFollowing();
        reader.ReadToFollowing();

        while (!reader.IsAtStreamEnd) root.Add(reader.ReadAsTag());

        TestFiles.AssertValueTest(new NbtFile(root));
    }


    [Fact]
    public void ReadAsTagTest4()
    {
        // read a bunch of lists as tags
        var testData = new NbtFile(TestFiles.MakeListTest()).SaveToBuffer(NbtCompression.None);

        // first, read everything all-at-once
        {
            var reader = new NbtReader(new MemoryStream(testData));
            while (!reader.IsAtStreamEnd)
                //Console.WriteLine(reader.ReadAsTag());
                testOutputHelper.WriteLine(reader.ReadAsTag().ToString());
        }

        // next, read each list individually
        {
            var reader = new NbtReader(new MemoryStream(testData));
            reader.ReadToFollowing(); // read to root
            reader.ReadToFollowing(); // read to first list tag
            while (!reader.IsAtStreamEnd)
                //Console.WriteLine(reader.ReadAsTag());
                testOutputHelper.WriteLine(reader.ReadAsTag().ToString());
        }
    }


    [Fact]
    public void ReadListAsArray()
    {
        var intList = TestFiles.MakeListTest();

        var ms = new MemoryStream();
        new NbtFile(intList).SaveToStream(ms, NbtCompression.None);
        ms.Seek(0, SeekOrigin.Begin);
        var reader = new NbtReader(ms);

        // attempt to read value before we're in a list
        Invoking(() => reader.ReadListAsArray<int>()).Should().Throw<InvalidOperationException>();

        // test byte values
        reader.ReadToFollowing("ByteList");
        var bytes = reader.ReadListAsArray<byte>();
        bytes.Should().BeEquivalentTo(new byte[] { 100, 20, 3 });

        // test double values
        reader.ReadToFollowing("DoubleList");
        var doubles = reader.ReadListAsArray<double>();
        doubles.Should().BeEquivalentTo([1d, 2000d, -3000000d]);

        // test float values
        reader.ReadToFollowing("FloatList");
        var floats = reader.ReadListAsArray<float>();
        floats.Should().BeEquivalentTo([1f, 2000f, -3000000f]);

        // test int values
        reader.ReadToFollowing("IntList");
        var ints = reader.ReadListAsArray<int>();
        ints.Should().BeEquivalentTo([1, 2000, -3000000]);

        // test long values
        reader.ReadToFollowing("LongList");
        var longs = reader.ReadListAsArray<long>();
        longs.Should().BeEquivalentTo([1L, 2000L, -3000000L]);

        // test short values
        reader.ReadToFollowing("ShortList");
        var shorts = reader.ReadListAsArray<short>();
        shorts.Should().BeEquivalentTo(new short[] { 1, 200, -30000 });

        // test short values
        reader.ReadToFollowing("StringList");
        var strings = reader.ReadListAsArray<string>();
        strings.Should().BeEquivalentTo("one", "two thousand", "negative three million");

        // try reading list of compounds (should fail)
        reader.ReadToFollowing("CompoundList");
        Invoking(() => reader.ReadListAsArray<NbtCompound>()).Should().Throw<InvalidOperationException>();

        // skip to the end of the stream
        while (reader.ReadToFollowing())
        {
        }

        Invoking(() => reader.ReadListAsArray<int>()).Should().Throw<EndOfStreamException>();
    }


    [Fact]
    public void ReadListAsArrayRecast()
    {
        var intList = TestFiles.MakeListTest();

        var ms = new MemoryStream();
        new NbtFile(intList).SaveToStream(ms, NbtCompression.None);
        ms.Seek(0, SeekOrigin.Begin);
        var reader = new NbtReader(ms);

        // test bytes as shorts
        reader.ReadToFollowing("ByteList");
        var bytes = reader.ReadListAsArray<short>();
        //                          new short[] {
        //                              100, 20, 3
        //                          });
        bytes.Should().BeEquivalentTo(new short[] { 100, 20, 3 });
    }


    [Fact]
    public void ReadValueTest()
    {
        var testData = new NbtFile(TestFiles.MakeValueTest()).SaveToBuffer(NbtCompression.None);
        var reader = new NbtReader(new MemoryStream(testData));

        reader.ReadToFollowing().Should().BeTrue(); // root

        reader.ReadToFollowing().Should().BeTrue(); // byte
        reader.ReadValue().Should().Be(1);
        reader.ReadToFollowing().Should().BeTrue(); // short
        reader.ReadValue().Should().Be(2);
        reader.ReadToFollowing().Should().BeTrue(); // int
        reader.ReadValue().Should().Be(3);
        reader.ReadToFollowing().Should().BeTrue(); // long
        reader.ReadValue().Should().Be(4L);
        reader.ReadToFollowing().Should().BeTrue(); // float
        reader.ReadValue().Should().Be(5f);
        reader.ReadToFollowing().Should().BeTrue(); // double
        reader.ReadValue().Should().Be(6d);
        reader.ReadToFollowing().Should().BeTrue(); // byteArray
        reader.ReadValue().Should().BeEquivalentTo(new byte[] { 10, 11, 12 });
        reader.ReadToFollowing().Should().BeTrue(); // intArray
        reader.ReadValue().Should().BeEquivalentTo(new[] { 20, 21, 22 });
        reader.ReadToFollowing().Should().BeTrue(); // longArray
        reader.ReadValue().Should().BeEquivalentTo(new long[] { 200, 210, 220 });
        reader.ReadToFollowing().Should().BeTrue(); // string
        reader.ReadValue().Should().Be("123");

        // Skip to the very end and make sure that we can't read any more values
        reader.ReadToFollowing();
        Invoking(() => reader.ReadValue()).Should().Throw<EndOfStreamException>();
    }


    [Fact]
    public void ReadValueAsTest()
    {
        var testData = new NbtFile(TestFiles.MakeValueTest()).SaveToBuffer(NbtCompression.None);
        var reader = new NbtReader(new MemoryStream(testData));

        reader.ReadToFollowing().Should().BeTrue(); // root

        reader.ReadToFollowing().Should().BeTrue(); // byte
        reader.ReadValueAs<byte>().Should().Be(1);
        reader.ReadToFollowing().Should().BeTrue(); // short
        reader.ReadValueAs<short>().Should().Be(2);
        reader.ReadToFollowing().Should().BeTrue(); // int
        reader.ReadValueAs<int>().Should().Be(3);
        reader.ReadToFollowing().Should().BeTrue(); // long
        reader.ReadValueAs<long>().Should().Be(4L);
        reader.ReadToFollowing().Should().BeTrue(); // float
        reader.ReadValueAs<float>().Should().Be(5f);
        reader.ReadToFollowing().Should().BeTrue(); // double
        reader.ReadValueAs<double>().Should().Be(6d);
        reader.ReadToFollowing().Should().BeTrue(); // byteArray
        reader.ReadValueAs<byte[]>().Should().BeEquivalentTo(new byte[] { 10, 11, 12 });
        reader.ReadToFollowing().Should().BeTrue(); // intArray
        reader.ReadValueAs<int[]>().Should().BeEquivalentTo([20, 21, 22]);
        reader.ReadToFollowing().Should().BeTrue(); // longArray
        reader.ReadValueAs<long[]>().Should().BeEquivalentTo(new long[] { 200, 210, 220 });
        reader.ReadToFollowing().Should().BeTrue(); // string
        reader.ReadValueAs<string>().Should().Be("123");
    }


    [Fact]
    public void ErrorTest()
    {
        var root = new NbtCompound("root");
        var testData = new NbtFile(root).SaveToBuffer(NbtCompression.None);

        // creating NbtReader without a stream, or with a non-readable stream
        Invoking(() => new NbtReader(null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtReader(new NonReadableStream())).Should().Throw<ArgumentException>();

        // corrupt the data
        testData[0] = 123;
        var reader = new NbtReader(new MemoryStream(testData));

        // attempt to use ReadValue when not at value
        Invoking(() => reader.ReadValue()).Should().Throw<InvalidOperationException>();
        reader.CacheTagValues = true;
        Invoking(() => reader.ReadValue()).Should().Throw<InvalidOperationException>();

        // attempt to read a corrupt stream
        Invoking(() => reader.ReadToFollowing()).Should().Throw<NbtFormatException>();

        // make sure we've properly entered the error state
        reader.IsInErrorState.Should().BeTrue();
        reader.HasName.Should().BeFalse();
        Invoking(() => reader.ReadToFollowing()).Should().Throw<InvalidReaderStateException>();
        Invoking(() => reader.ReadListAsArray<int>()).Should().Throw<InvalidReaderStateException>();
        Invoking(() => reader.ReadToNextSibling()).Should().Throw<InvalidReaderStateException>();
        Invoking(() => reader.ReadToDescendant("derp")).Should().Throw<InvalidReaderStateException>();
        Invoking(() => reader.ReadAsTag()).Should().Throw<InvalidReaderStateException>();
        Invoking(() => reader.Skip()).Should().Throw<InvalidReaderStateException>();
    }


    [Fact]
    public void NonSeekableStreamSkip1()
    {
        var fileBytes = File.ReadAllBytes(TestFiles.Big);
        using var ms = new MemoryStream(fileBytes);
        using var nss = new NonSeekableStream(ms);
        var reader = new NbtReader(nss);
        reader.ReadToFollowing();
        reader.Skip();
    }


    [Fact]
    public void NonSeekableStreamSkip2()
    {
        using var ms = TestFiles.MakeReaderTest();
        using var nss = new NonSeekableStream(ms);
        var reader = new NbtReader(nss);
        reader.ReadToFollowing();
        reader.Skip();
    }


    [Fact]
    public void CorruptFileRead()
    {
        byte[] emptyFile = [];
        Invoking(() => TryReadBadFile(emptyFile)).Should().Throw<EndOfStreamException>();
        Invoking(() => new NbtFile().LoadFromBuffer(emptyFile, 0, emptyFile.Length, NbtCompression.None))
            .Should().Throw<EndOfStreamException>();
        Invoking(() => NbtFile.ReadRootTagName(new MemoryStream(emptyFile), NbtCompression.AutoDetect, true, 0))
            .Should().Throw<EndOfStreamException>();
        Invoking(() => NbtFile.ReadRootTagName(new MemoryStream(emptyFile), NbtCompression.None, true, 0))
            .Should().Throw<EndOfStreamException>();

        byte[] badHeader =
        [
            0x02, // TAG_Short ID (instead of TAG_Compound ID)
            0x00, 0x01, 0x66, // Root name: 'f'
            0x00 // end tag
        ];
        Invoking(() => TryReadBadFile(badHeader)).Should().Throw<NbtFormatException>();
        Invoking(() => new NbtFile().LoadFromBuffer(badHeader, 0, badHeader.Length, NbtCompression.None))
            .Should().Throw<NbtFormatException>();
        Invoking(() => NbtFile.ReadRootTagName(new MemoryStream(badHeader), NbtCompression.None, true, 0))
            .Should().Throw<NbtFormatException>();

        byte[] badStringLength =
        [
            0x0A, // Compound tag
            0xFF, 0xFF, 0x66, // Root name 'f' (with string length given as "-1")
            0x00 // end tag
        ];
        Invoking(() => TryReadBadFile(badStringLength)).Should().Throw<NbtFormatException>();
        Invoking(() => new NbtFile().LoadFromBuffer(badStringLength, 0, badStringLength.Length, NbtCompression.None))
            .Should().Throw<NbtFormatException>();
        Invoking(() => NbtFile.ReadRootTagName(new MemoryStream(badStringLength), NbtCompression.None, true, 0))
            .Should().Throw<NbtFormatException>();

        byte[] abruptStringEnd =
        [
            0x0A, // Compound tag
            0x00, 0xFF, 0x66, // Root name 'f' (string length given as 5)
            0x00 // premature end tag
        ];
        Invoking(() => TryReadBadFile(abruptStringEnd)).Should().Throw<EndOfStreamException>();
        Invoking(() => new NbtFile().LoadFromBuffer(abruptStringEnd, 0, abruptStringEnd.Length, NbtCompression.None))
            .Should().Throw<EndOfStreamException>();
        Invoking(() => NbtFile.ReadRootTagName(new MemoryStream(abruptStringEnd), NbtCompression.None, true, 0))
            .Should().Throw<EndOfStreamException>();

        byte[] badSecondTag =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0xFF, 0x01, 0x4E, 0x7F, 0xFF, // Short tag named 'N' with invalid tag ID (0xFF instead of 0x02)
            0x00 // end tag
        ];
        AssertBadFileFromBuffer(badSecondTag);

        byte[] badListType =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x09, // List tag
            0x00, 0x01, 0x67, // List tag name: 'g'
            0xFF // invalid list tag type (-1)
        ];
        AssertBadFileFromBuffer(badListType);

        byte[] badListSize =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x09, // List tag
            0x00, 0x01, 0x67, // List tag name: 'g'
            0x01, // List type: Byte
            0xFF, 0xFF, 0xFF, 0xFF // List size: -1
        ];
        AssertBadFileFromBuffer(badListSize);
    }


    [Fact]
    public void BadArraySize()
    {
        byte[] badByteArraySize =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x07, // ByteArray tag
            0x00, 0x01, 0x67, // ByteArray tag name: 'g'
            0xFF, 0xFF, 0xFF, 0xFF // array length: -1
        ];
        AssertBadFileFromBuffer(badByteArraySize);


        byte[] badIntArraySize =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x0b, // IntArray tag
            0x00, 0x01, 0x66, // IntArray tag name: 'g'
            0xFF, 0xFF, 0xFF, 0xFF // array length: -1
        ];
        AssertBadFileFromBuffer(badIntArraySize);

        byte[] badLongArraySize =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x0c, // LongArray tag
            0x00, 0x01, 0x66, // LongArray tag name: 'g'
            0xFF, 0xFF, 0xFF, 0xFF // array length: -1
        ];
        AssertBadFileFromBuffer(badLongArraySize);
    }


    [Fact]
    public void BadNestedArraySize()
    {
        byte[] badNestedByteArraySize =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x0A, // Child compound tag
            0x00, 0x01, 0x67, // Child name: 'g'
            0x07, // ByteArray tag
            0x00, 0x01, 0x68, // ByteArray tag name: 'h'
            0xFF, 0xFF, 0xFF, 0xFF // array length: -1
        ];
        AssertBadFileFromBuffer(badNestedByteArraySize);


        byte[] badNestedIntArraySize =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x0A, // Child compound tag
            0x00, 0x01, 0x67, // Child name: 'g'
            0x0b, // IntArray tag
            0x00, 0x01, 0x68, // IntArray tag name: 'h'
            0xFF, 0xFF, 0xFF, 0xFF // array length: -1
        ];
        AssertBadFileFromBuffer(badNestedIntArraySize);

        byte[] badNestedLongArraySize =
        [
            0x0A, // Compound tag
            0x00, 0x01, 0x66, // Root name: 'f'
            0x0A, // Child compound tag
            0x00, 0x01, 0x67, // Child name: 'g'
            0x0c, // LongArray tag
            0x00, 0x01, 0x68, // LongArray tag name: 'g'
            0xFF, 0xFF, 0xFF, 0xFF // array length: -1
        ];
        AssertBadFileFromBuffer(badNestedLongArraySize);
    }


    [Fact]
    public void PartialReadTest()
    {
        // read the whole thing as one tag, one byte at a time
        TestFiles.AssertValueTest(PartialReadTestInternal(new NbtFile(TestFiles.MakeValueTest())));
        TestFiles.AssertNbtSmallFile(PartialReadTestInternal(TestFiles.MakeSmallFile()));
        TestFiles.AssertNbtBigFile(PartialReadTestInternal(new NbtFile(TestFiles.Big)));
    }


    [Fact]
    public void PartialBatchReadTest()
    {
        // read the whole thing as one tag, in batches of 4 bytes
        // Verifies fix for https://github.com/fragmer/fNbt/issues/26
        TestFiles.AssertValueTest(PartialReadTestInternal(new NbtFile(TestFiles.MakeValueTest()), 4));
        TestFiles.AssertNbtSmallFile(PartialReadTestInternal(TestFiles.MakeSmallFile(), 4));
        TestFiles.AssertNbtBigFile(PartialReadTestInternal(new NbtFile(TestFiles.Big), 4));
    }


    [Fact]
    public void EndTagTest()
    {
        using MemoryStream ms = new();
        var root = new NbtCompound("root")
        {
            new NbtInt("test", 0)
        };
        new NbtFile(root).SaveToStream(ms, NbtCompression.None);
        ms.Position = 0;

        var reader = new NbtReader(ms) { SkipEndTags = false };
        reader.ReadToDescendant("test");
        reader.TagType.Should().Be(NbtTagType.Int);
        reader.ReadToNextSibling().Should().BeTrue();

        // should be at root's End tag now
        reader.TagType.Should().Be(NbtTagType.End);
        reader.IsInErrorState.Should().BeFalse();
        reader.IsAtStreamEnd.Should().BeFalse();
        reader.IsCompound.Should().BeFalse();
        reader.IsList.Should().BeFalse();
        reader.IsListElement.Should().BeFalse();
        reader.HasValue.Should().BeFalse();
        reader.HasName.Should().BeFalse();
        reader.HasLength.Should().BeFalse();
        Invoking(() => reader.ReadAsTag()).Should().Throw<InvalidOperationException>();

        // We done now
        reader.ReadToFollowing().Should().BeFalse();
        reader.IsAtStreamEnd.Should().BeTrue();
    }


    private static NbtFile PartialReadTestInternal(NbtFile comp, int increment = 1)
    {
        var testData = comp.SaveToBuffer(NbtCompression.None);
        var reader = new NbtReader(new PartialReadStream(new MemoryStream(testData), increment));
        var root = (NbtCompound)reader.ReadAsTag();
        return new NbtFile(root);
    }


    private void AssertBadFileFromBuffer(byte[] input)
    {
        Invoking(() => TryReadBadFile(input)).Should().Throw<NbtFormatException>();
        Invoking(() => new NbtFile().LoadFromBuffer(input, 0, input.Length, NbtCompression.None))
            .Should().Throw<NbtFormatException>();
        Invoking(() => new NbtFile().LoadFromBuffer(input, 0, input.Length, NbtCompression.None, tag => false))
            .Should().Throw<NbtFormatException>();
    }


    private static void TryReadBadFile(byte[] data)
    {
        using MemoryStream ms = new(data);
        var reader = new NbtReader(ms);
        try
        {
            while (reader.ReadToFollowing())
            {
            }
        }
        catch (Exception)
        {
            reader.IsInErrorState.Should().BeTrue();
            throw;
        }
    }


    private class NonReadableStream : MemoryStream
    {
        public override bool CanRead => false;


        public override int ReadByte()
        {
            throw new NotSupportedException();
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}