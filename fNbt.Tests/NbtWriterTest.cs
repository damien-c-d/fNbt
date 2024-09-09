using System.Text;
using FluentAssertions;
using fNbt.Tags;
using Xunit.Abstractions;

namespace fNbt.Tests;

public class NbtWriterTest(ITestOutputHelper testOutputHelper) : TestBase
{
    [Fact]
    public void ValueTest()
    {
        // write one named tag for every value type, and read it back
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, "root");
        ms.Should().BeSameAs(writer.BaseStream);
        {
            writer.WriteByte("byte", 1);
            writer.WriteShort("short", 2);
            writer.WriteInt("int", 3);
            writer.WriteLong("long", 4L);
            writer.WriteFloat("float", 5f);
            writer.WriteDouble("double", 6d);
            writer.WriteByteArray("byteArray", [10, 11, 12]);
            writer.WriteIntArray("intArray", [20, 21, 22]);
            writer.WriteLongArray("longArray", [200, 210, 220]);
            writer.WriteString("string", "123");
        }
        writer.IsDone.Should().BeFalse();
        writer.EndCompound();
        writer.IsDone.Should().BeTrue();
        writer.Finish();

        ms.Position = 0;
        var file = new NbtFile();
        file.LoadFromStream(ms, NbtCompression.None);

        TestFiles.AssertValueTest(file);
    }


    [Fact]
    public void HugeNbtWriterTest()
    {
        // Tests writing byte arrays that exceed the max NbtBinaryWriter chunk size
        using var bs = new BufferedStream(Stream.Null);
        var writer = new NbtWriter(bs, "root");
        writer.WriteByteArray("payload4", new byte[5 * 1024 * 1024]);
        writer.EndCompound();
        writer.Finish();
    }


    [Fact]
    public void ByteArrayFromStream()
    {
        var data = new byte[64 * 1024];
        for (var i = 0; i < data.Length; i++) data[i] = unchecked((byte)i);

        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, "root");
        {
            var buffer = new byte[1024];
            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray("byteArray1", dataStream, data.Length);
            }

            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray("byteArray2", dataStream, data.Length, buffer);
            }

            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray("byteArray3", dataStream, 1);
            }

            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray("byteArray4", dataStream, 1, buffer);
            }

            writer.BeginList("innerLists", NbtTagType.ByteArray, 4);
            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray(dataStream, data.Length);
            }

            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray(dataStream, data.Length, buffer);
            }

            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray(dataStream, 1);
            }

            using (var dataStream = new NonSeekableStream(new MemoryStream(data)))
            {
                writer.WriteByteArray(dataStream, 1, buffer);
            }

            writer.EndList();
        }
        writer.EndCompound();
        writer.Finish();

        ms.Position = 0;
        var file = new NbtFile();
        file.LoadFromStream(ms, NbtCompression.None);
        data.Should().Equal(file.RootTag["byteArray1"].ByteArrayValue);
        data.Should().Equal(file.RootTag["byteArray2"].ByteArrayValue);
        file.RootTag["byteArray3"].ByteArrayValue.Length.Should().Be(1);
        file.RootTag["byteArray3"].ByteArrayValue[0].Should().Be(data[0]);
        file.RootTag["byteArray4"].ByteArrayValue.Length.Should().Be(1);
        file.RootTag["byteArray4"].ByteArrayValue[0].Should().Be(data[0]);


        data.Should().Equal(file.RootTag["innerLists"][0].ByteArrayValue);
        data.Should().Equal(file.RootTag["innerLists"][1].ByteArrayValue);
        file.RootTag["innerLists"][2].ByteArrayValue.Length.Should().Be(1);
        file.RootTag["innerLists"][2].ByteArrayValue[0].Should().Be(data[0]);
        file.RootTag["innerLists"][3].ByteArrayValue.Length.Should().Be(1);
        file.RootTag["innerLists"][3].ByteArrayValue[0].Should().Be(data[0]);
    }


    [Fact]
    public void CompoundListTest()
    {
        // test writing various combinations of compound tags and list tags
        const string testString = "Come on and slam, and welcome to the jam.";
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, "Test");
        {
            writer.BeginCompound("EmptyCompy");
            {
            }
            writer.EndCompound();

            writer.BeginCompound("OuterNestedCompy");
            {
                writer.BeginCompound("InnerNestedCompy");
                {
                    writer.WriteInt("IntTest", 123);
                    writer.WriteString("StringTest", testString);
                }
                writer.EndCompound();
            }
            writer.EndCompound();

            writer.BeginList("ListOfInts", NbtTagType.Int, 3);
            {
                writer.WriteInt(1);
                writer.WriteInt(2);
                writer.WriteInt(3);
            }
            writer.EndList();

            writer.BeginCompound("CompoundOfListsOfCompounds");
            {
                writer.BeginList("ListOfCompounds", NbtTagType.Compound, 1);
                {
                    writer.BeginCompound();
                    {
                        writer.WriteInt("TestInt", 123);
                    }
                    writer.EndCompound();
                }
                writer.EndList();
            }
            writer.EndCompound();


            writer.BeginList("ListOfEmptyLists", NbtTagType.List, 3);
            {
                writer.BeginList(NbtTagType.List, 0);
                {
                }
                writer.EndList();
                writer.BeginList(NbtTagType.List, 0);
                {
                }
                writer.EndList();
                writer.BeginList(NbtTagType.List, 0);
                {
                }
                writer.EndList();
            }
            writer.EndList();
        }
        writer.EndCompound();
        writer.Finish();

        ms.Seek(0, SeekOrigin.Begin);
        var file = new NbtFile();
        file.LoadFromStream(ms, NbtCompression.None);
        testOutputHelper.WriteLine(file.ToString());
    }


    [Fact]
    public void ListTest()
    {
        // write short (1-element) lists of every possible kind
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, "Test");
        writer.BeginList("LotsOfLists", NbtTagType.List, 12);
        {
            writer.BeginList(NbtTagType.Byte, 1);
            writer.WriteByte(1);
            writer.EndList();

            writer.BeginList(NbtTagType.ByteArray, 1);
            writer.WriteByteArray([
                1
            ]);
            writer.EndList();

            writer.BeginList(NbtTagType.Compound, 1);
            writer.BeginCompound();
            writer.EndCompound();
            writer.EndList();

            writer.BeginList(NbtTagType.Double, 1);
            writer.WriteDouble(1);
            writer.EndList();

            writer.BeginList(NbtTagType.Float, 1);
            writer.WriteFloat(1);
            writer.EndList();

            writer.BeginList(NbtTagType.Int, 1);
            writer.WriteInt(1);
            writer.EndList();

            writer.BeginList(NbtTagType.IntArray, 1);
            writer.WriteIntArray([
                1
            ]);
            writer.EndList();

            writer.BeginList(NbtTagType.List, 1);
            writer.BeginList(NbtTagType.List, 0);
            writer.EndList();
            writer.EndList();

            writer.BeginList(NbtTagType.Long, 1);
            writer.WriteLong(1);
            writer.EndList();

            writer.BeginList(NbtTagType.Short, 1);
            writer.WriteShort(1);
            writer.EndList();

            writer.BeginList(NbtTagType.String, 1);
            writer.WriteString("ponies");
            writer.EndList();

            writer.BeginList(NbtTagType.LongArray, 1);
            writer.WriteLongArray([
                1L
            ]);
            writer.EndList();
        }
        writer.EndList();
        writer.IsDone.Should().BeFalse();
        writer.EndCompound();
        writer.IsDone.Should().BeTrue();
        writer.Finish();

        ms.Position = 0;
        var reader = new NbtReader(ms);
        reader.Invoking(r => r.ReadAsTag()).Should().NotThrow();
    }


    [Fact]
    public void WriteTagTest()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, "root");
        {
            foreach (var tag in TestFiles.MakeValueTest().Tags) writer.WriteTag(tag);

            writer.EndCompound();

            writer.IsDone.Should().BeTrue();
            writer.Finish();
        }
        ms.Position = 0;
        var file = new NbtFile();
        var bytesRead = file.LoadFromBuffer(ms.ToArray(), 0, (int)ms.Length, NbtCompression.None);
        bytesRead.Should().Be(ms.Length);
        TestFiles.AssertValueTest(file);
    }


    [Fact]
    public void ErrorTest()
    {
        byte[] dummyByteArray = [1, 2, 3, 4, 5];
        int[] dummyIntArray = [1, 2, 3, 4, 5];
        long[] dummyLongArray = [1, 2, 3, 4, 5];
        var dummyStream = new MemoryStream(dummyByteArray);

        using var ms = new MemoryStream();
        // null constructor parameters, or a non-writable stream
        Invoking(() => new NbtWriter(null, "root")).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtWriter(ms, null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtWriter(new NonWritableStream(), "root")).Should().Throw<ArgumentException>();

        var writer = new NbtWriter(ms, "root");
        {
            // use negative list size

            Invoking(() => writer.BeginList("list", NbtTagType.Int, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            writer.BeginList("listOfLists", NbtTagType.List, 1);

            Invoking(() => writer.BeginList(NbtTagType.Int, -1)).Should().Throw<ArgumentOutOfRangeException>();
            writer.BeginList(NbtTagType.Int, 0);
            writer.EndList();
            writer.EndList();

            writer.BeginList("list", NbtTagType.Int, 1);

            // invalid list type


            Invoking(() => writer.BeginList(NbtTagType.End, 0)).Should().Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.BeginList("list", NbtTagType.End, 0)).Should()
                .Throw<ArgumentOutOfRangeException>();

            // call EndCompound when not in a compound

            Invoking(writer.EndCompound).Should().Throw<NbtFormatException>();

            // end list before all elements have been written

            Invoking(writer.EndList).Should().Throw<NbtFormatException>();

            // write the wrong kind of tag inside a list

            Invoking(() => writer.WriteShort(0)).Should().Throw<NbtFormatException>();

            // write a named tag where an unnamed tag is expected

            Invoking(() => writer.WriteInt("NamedInt", 0)).Should().Throw<NbtFormatException>();

            // write too many list elements
            writer.WriteTag(new NbtInt());

            Invoking(() => writer.WriteInt(0)).Should().Throw<NbtFormatException>();
            writer.EndList();

            // write a null tag

            Invoking(() => writer.WriteTag(null)).Should().Throw<ArgumentNullException>();

            // write an unnamed tag where a named tag is expected


            Invoking(() => writer.WriteTag(new NbtInt())).Should().Throw<NbtFormatException>();
            Invoking(() => writer.WriteInt(0)).Should().Throw<NbtFormatException>();

            // end a list when not in a list

            Invoking(writer.EndList).Should().Throw<NbtFormatException>();

            // unacceptable nulls: WriteString

            Invoking(() => writer.WriteString(null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteString("NullString", null)).Should().Throw<ArgumentNullException>();

            // unacceptable nulls: WriteByteArray from array

            Invoking(() => writer.WriteByteArray(null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray(null, 0, 5)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray("NullByteArray", null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray("NullByteArray", null, 0, 5)).Should()
                .Throw<ArgumentNullException>();

            // unacceptable nulls: WriteByteArray from stream

            Invoking(() => writer.WriteByteArray(null, 5)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray(null, 5, null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray(dummyStream, 5, null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray("NullBuffer", dummyStream, 5, null)).Should()
                .Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray("NullStream", null, 5)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteByteArray("NullStream", null, 5, dummyByteArray)).Should()
                .Throw<ArgumentNullException>();

            // unacceptable nulls: WriteIntArray

            Invoking(() => writer.WriteIntArray(null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteIntArray(null, 0, 5)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteIntArray("NullIntArray", null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteIntArray("NullIntArray", null, 0, 5)).Should()
                .Throw<ArgumentNullException>();

            // unacceptable nulls: WriteIntArray

            Invoking(() => writer.WriteLongArray(null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteLongArray(null, 0, 5)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteLongArray("NullLongArray", null)).Should().Throw<ArgumentNullException>();
            Invoking(() => writer.WriteLongArray("NullLongArray", null, 0, 5)).Should()
                .Throw<ArgumentNullException>();

            // non-readable streams are unacceptable

            Invoking(() => writer.WriteByteArray(new NonReadableStream(), 0)).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteByteArray(new NonReadableStream(), 0, new byte[10])).Should()
                .Throw<ArgumentException>();
            Invoking(() => writer.WriteByteArray("NonReadableStream", new NonReadableStream(), 0)).Should()
                .Throw<ArgumentException>();

            // trying to write array with out-of-range offset/count

            Invoking(() => writer.WriteByteArray(dummyByteArray, -1, 5)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray(dummyByteArray, 0, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray(dummyByteArray, 0, 6)).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteByteArray(dummyByteArray, 1, 5)).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteByteArray("OutOfRangeByteArray", dummyByteArray, -1, 5)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray("OutOfRangeByteArray", dummyByteArray, 0, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray("OutOfRangeByteArray", dummyByteArray, 0, 6)).Should()
                .Throw<ArgumentException>();
            Invoking(() => writer.WriteByteArray("OutOfRangeByteArray", dummyByteArray, 1, 5)).Should()
                .Throw<ArgumentException>();


            Invoking(() => writer.WriteIntArray(dummyIntArray, -1, 5)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteIntArray(dummyIntArray, 0, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteIntArray(dummyIntArray, 0, 6)).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteIntArray(dummyIntArray, 1, 5)).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteIntArray("OutOfRangeIntArray", dummyIntArray, -1, 5)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteIntArray("OutOfRangeIntArray", dummyIntArray, 0, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteIntArray("OutOfRangeIntArray", dummyIntArray, 0, 6)).Should()
                .Throw<ArgumentException>();
            Invoking(() => writer.WriteIntArray("OutOfRangeIntArray", dummyIntArray, 1, 5)).Should()
                .Throw<ArgumentException>();


            Invoking(() => writer.WriteLongArray(dummyLongArray, -1, 5)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteLongArray(dummyLongArray, 0, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteLongArray(dummyLongArray, 0, 6)).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteLongArray(dummyLongArray, 1, 5)).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteLongArray("OutOfRangeLongArray", dummyLongArray, -1, 5)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteLongArray("OutOfRangeLongArray", dummyLongArray, 0, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteLongArray("OutOfRangeLongArray", dummyLongArray, 0, 6)).Should()
                .Throw<ArgumentException>();
            Invoking(() => writer.WriteLongArray("OutOfRangeLongArray", dummyLongArray, 1, 5)).Should()
                .Throw<ArgumentException>();

            // out-of-range values for stream-reading overloads of WriteByteArray


            Invoking(() => writer.WriteByteArray(dummyStream, -1)).Should().Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray("BadLength", dummyStream, -1)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray(dummyStream, -1, dummyByteArray)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray("BadLength", dummyStream, -1, dummyByteArray)).Should()
                .Throw<ArgumentOutOfRangeException>();
            Invoking(() => writer.WriteByteArray(dummyStream, 5, [])).Should().Throw<ArgumentException>();
            Invoking(() => writer.WriteByteArray("BadLength", dummyStream, 5, [])).Should()
                .Throw<ArgumentException>();

            // trying to read from non-readable stream


            Invoking(() => writer.WriteByteArray("ByteStream", new NonReadableStream(), 0)).Should()
                .Throw<ArgumentException>();

            // finish too early

            Invoking(writer.Finish).Should().Throw<NbtFormatException>();

            writer.EndCompound();
            writer.Finish();

            // write tag after finishing

            Invoking(() => writer.WriteTag(new NbtInt())).Should().Throw<NbtFormatException>();
        }
    }


    // Ensure that Unicode strings of arbitrary size and content are written/read properly
    [Fact]
    public void ComplexStringsTest()
    {
        // Use a fixed seed for repeatability of this test
        var rand = new Random(0);

        // Generate random Unicode strings
        const int numStrings = 1024;
        List<string> writtenStrings = [];
        for (var i = 0; i < numStrings; i++) writtenStrings.Add(GenRandomUnicodeString(rand));

        using var ms = new MemoryStream();
        // Write a list of strings
        var writer = new NbtWriter(ms, "test");
        writer.BeginList("stringList", NbtTagType.String, numStrings);
        foreach (var s in writtenStrings) writer.WriteString(s);

        writer.EndList();
        writer.EndCompound();
        writer.Finish();

        // Rewind!
        ms.Position = 0;

        // Let's read what we have written, and check contents
        var file = new NbtFile();
        file.LoadFromStream(ms, NbtCompression.None);
        var readStrings =
            file.RootTag.Get<NbtList>("stringList")
                .ToArray<NbtString>()
                .Select(tag => tag.StringValue);

        // Make sure that all read/written strings match exactly
        writtenStrings.Should().Equal(readStrings);
    }


    [Fact]
    public void MissingNameTest()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, "test");
        // All tags (aside from list elements) must be named
        Invoking(() => writer.WriteTag(new NbtByte(123))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtShort(123))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtInt(123))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtLong(123))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtFloat(123))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtDouble(123))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtString("value"))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtByteArray([]))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtIntArray([]))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtLongArray([]))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtList(NbtTagType.Byte))).Should().Throw<NbtFormatException>();
        Invoking(() => writer.WriteTag(new NbtCompound())).Should().Throw<NbtFormatException>();
    }


    private static string GenRandomUnicodeString(Random rand)
    {
        // String length is limited by number of bytes, not characters.
        // Most bytes per char in UTF8 is 4, so max string length is therefore short.MaxValue/4
        var len = rand.Next(8, short.MaxValue / 4);
        var sb = new StringBuilder();

        // Generate one char at a time until we filled up the StringBuilder
        while (sb.Length < len)
        {
            var ch = (char)rand.Next(0, 0xFFFF);
            if (char.IsControl(ch) || char.IsSurrogate(ch) || (ch >= 0xE000 && ch <= 0xF8FF))
                // exclude control characters, surrogates, and private range
                continue;

            sb.Append(ch);
        }

        return sb.ToString();
    }


    private class NonWritableStream : MemoryStream
    {
        public override bool CanWrite => false;


        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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