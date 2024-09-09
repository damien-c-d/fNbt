using System.Globalization;
using FluentAssertions;
using fNbt.Tags;

namespace fNbt.Tests;

public class ShortcutTests : TestBase
{
    [Fact]
    public void NbtByteTest()
    {
        object dummy;
        NbtTag test = new NbtByte(250);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        test.ByteValue.Should().Be(250);
        test.DoubleValue.Should().Be(250D);
        test.FloatValue.Should().Be(250F);
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        test.IntValue.Should().Be(250);
        test.LongValue.Should().Be(250L);
        test.ShortValue.Should().Be(250);
        test.StringValue.Should().Be("250");
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtByteArrayTest()
    {
        object dummy;
        byte[] bytes = [1, 2, 3, 4, 5];
        NbtTag test = new NbtByteArray(bytes);
        test.ByteArrayValue.Should().Equal(bytes);
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.DoubleValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.FloatValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.StringValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtCompoundTest()
    {
        object dummy;
        NbtTag test = new NbtCompound("Derp");
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.DoubleValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.FloatValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.StringValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeFalse();
    }


    [Fact]
    public void NbtDoubleTest()
    {
        object dummy;
        NbtTag test = new NbtDouble(0.4931287132182315);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        test.DoubleValue.Should().Be(0.4931287132182315);
        test.FloatValue.Should().Be(0.4931287132182315F);
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        test.StringValue.Should().Be(0.4931287132182315.ToString(CultureInfo.InvariantCulture));
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtFloatTest()
    {
        object dummy;
        NbtTag test = new NbtFloat(0.49823147f);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        test.DoubleValue.Should().Be(0.49823147f);
        test.FloatValue.Should().Be(0.49823147f);
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        test.StringValue.Should().Be(0.49823147f.ToString(CultureInfo.InvariantCulture));
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtIntTest()
    {
        object dummy;
        NbtTag test = new NbtInt(2147483647);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        test.DoubleValue.Should().Be(2147483647D);
        test.FloatValue.Should().Be(2147483647F);
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        test.IntValue.Should().Be(2147483647);
        test.LongValue.Should().Be(2147483647L);
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        test.StringValue.Should().Be("2147483647");
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtIntArrayTest()
    {
        object dummy;
        int[] ints = [1111, 2222, 3333, 4444, 5555];
        NbtTag test = new NbtIntArray(ints);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.DoubleValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.FloatValue).Should().Throw<InvalidCastException>();
        test.IntArrayValue.Should().Equal(ints);
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.StringValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtListTest()
    {
        object dummy;
        NbtTag test = new NbtList("Derp");
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.DoubleValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.FloatValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.StringValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeFalse();
    }


    [Fact]
    public void NbtLongTest()
    {
        object dummy;
        NbtTag test = new NbtLong(9223372036854775807);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        test.DoubleValue.Should().Be(9223372036854775807D);
        test.FloatValue.Should().Be(9223372036854775807F);
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        test.LongValue.Should().Be(9223372036854775807L);
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        test.StringValue.Should().Be("9223372036854775807");
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtShortTest()
    {
        object dummy;
        NbtTag test = new NbtShort(32767);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        test.DoubleValue.Should().Be(32767D);
        test.FloatValue.Should().Be(32767F);
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        test.IntValue.Should().Be(32767);
        test.LongValue.Should().Be(32767L);
        test.ShortValue.Should().Be(32767);
        test.StringValue.Should().Be("32767");
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtStringTest()
    {
        object dummy;
        NbtTag test = new NbtString("HELLO WORLD THIS IS A TEST STRING ÅÄÖ!");
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.DoubleValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.FloatValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        test.StringValue.Should().Be("HELLO WORLD THIS IS A TEST STRING ÅÄÖ!");
        Invoking(() => dummy = test.LongArrayValue).Should().Throw<InvalidCastException>();
        test.HasValue.Should().BeTrue();
    }


    [Fact]
    public void NbtLongArrayTest()
    {
        object dummy;
        long[] longs = [1111, 2222, 3333, 4444, 5555];
        NbtTag test = new NbtLongArray(longs);
        Invoking(() => dummy = test.ByteArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ByteValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.DoubleValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.FloatValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntArrayValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.IntValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.LongValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.ShortValue).Should().Throw<InvalidCastException>();
        Invoking(() => dummy = test.StringValue).Should().Throw<InvalidCastException>();
        test.LongArrayValue.Should().Equal(longs);
        test.HasValue.Should().BeTrue();
    }
}