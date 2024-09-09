using FluentAssertions;
using fNbt.Tags;

namespace fNbt.Tests;

public class MiscTests : TestBase
{
    [Fact]
    public void CopyConstructorTest()
    {
        var byteTag = new NbtByte("byteTag", 1);
        var byteTagClone = (NbtByte)byteTag.Clone();
        byteTag.Should().NotBeSameAs(byteTagClone);
        byteTag.Name.Should().Be(byteTagClone.Name);
        byteTag.Value.Should().Be(byteTagClone.Value);
        Invoking(() => new NbtByte((NbtByte)null)).Should().Throw<ArgumentNullException>();

        var byteArrTag = new NbtByteArray("byteArrTag", [1, 2, 3, 4]);
        var byteArrTagClone = (NbtByteArray)byteArrTag.Clone();
        byteArrTag.Should().NotBeSameAs(byteArrTagClone);
        byteArrTag.Name.Should().Be(byteArrTagClone.Name);
        byteArrTag.Value.Should().NotBeSameAs(byteArrTagClone.Value);
        byteArrTag.Value.Should().BeEquivalentTo(byteArrTagClone.Value);
        Invoking(() => new NbtByteArray((NbtByteArray)null)).Should().Throw<ArgumentNullException>();

        var compTag = new NbtCompound("compTag", [new NbtByte("innerTag", 1)]);
        var compTagClone = (NbtCompound)compTag.Clone();
        compTag.Should().NotBeSameAs(compTagClone);
        compTag.Name.Should().Be(compTagClone.Name);
        compTag["innerTag"].Should().NotBeSameAs(compTagClone["innerTag"]);
        compTag["innerTag"].Name.Should().Be(compTagClone["innerTag"].Name);
        compTag["innerTag"].ByteValue.Should().Be(compTagClone["innerTag"].ByteValue);
        Invoking(() => new NbtCompound((NbtCompound)null)).Should().Throw<ArgumentNullException>();

        var doubleTag = new NbtDouble("doubleTag", 1);
        var doubleTagClone = (NbtDouble)doubleTag.Clone();
        doubleTag.Should().NotBeSameAs(doubleTagClone);
        doubleTag.Name.Should().Be(doubleTagClone.Name);
        doubleTag.Value.Should().Be(doubleTagClone.Value);
        Invoking(() => new NbtDouble((NbtDouble)null)).Should().Throw<ArgumentNullException>();

        var floatTag = new NbtFloat("floatTag", 1);
        var floatTagClone = (NbtFloat)floatTag.Clone();
        floatTag.Should().NotBeSameAs(floatTagClone);
        floatTag.Name.Should().Be(floatTagClone.Name);
        floatTag.Value.Should().Be(floatTagClone.Value);
        Invoking(() => new NbtFloat((NbtFloat)null)).Should().Throw<ArgumentNullException>();

        var intTag = new NbtInt("intTag", 1);
        var intTagClone = (NbtInt)intTag.Clone();
        intTag.Should().NotBeSameAs(intTagClone);
        intTag.Name.Should().Be(intTagClone.Name);
        intTag.Value.Should().Be(intTagClone.Value);
        Invoking(() => new NbtInt((NbtInt)null)).Should().Throw<ArgumentNullException>();

        var intArrTag = new NbtIntArray("intArrTag", [1, 2, 3, 4]);
        var intArrTagClone = (NbtIntArray)intArrTag.Clone();
        intArrTag.Should().NotBeSameAs(intArrTagClone);
        intArrTag.Name.Should().Be(intArrTagClone.Name);
        intArrTag.Value.Should().NotBeSameAs(intArrTagClone.Value);
        intArrTag.Value.Should().BeEquivalentTo(intArrTagClone.Value);
        Invoking(() => new NbtIntArray((NbtIntArray)null)).Should().Throw<ArgumentNullException>();

        var longArrTag = new NbtLongArray("longArrTag", [1, 2, 3, 4]);
        var longArrTagClone = (NbtLongArray)longArrTag.Clone();
        longArrTag.Should().NotBeSameAs(longArrTagClone);
        longArrTag.Name.Should().Be(longArrTagClone.Name);
        longArrTag.Value.Should().NotBeSameAs(longArrTagClone.Value);
        longArrTag.Value.Should().BeEquivalentTo(longArrTagClone.Value);
        Invoking(() => new NbtLongArray((NbtLongArray)null)).Should().Throw<ArgumentNullException>();

        var listTag = new NbtList("listTag", [new NbtByte(1)]);
        var listTagClone = (NbtList)listTag.Clone();
        listTag.Should().NotBeSameAs(listTagClone);
        listTag.Name.Should().Be(listTagClone.Name);
        listTag[0].Should().NotBeSameAs(listTagClone[0]);
        listTag[0].ByteValue.Should().Be(listTagClone[0].ByteValue);
        Invoking(() => new NbtList((NbtList)null)).Should().Throw<ArgumentNullException>();

        var longTag = new NbtLong("longTag", 1);
        var longTagClone = (NbtLong)longTag.Clone();
        longTag.Should().NotBeSameAs(longTagClone);
        longTag.Name.Should().Be(longTagClone.Name);
        longTag.Value.Should().Be(longTagClone.Value);
        Invoking(() => new NbtLong((NbtLong)null)).Should().Throw<ArgumentNullException>();

        var shortTag = new NbtShort("shortTag", 1);
        var shortTagClone = (NbtShort)shortTag.Clone();
        shortTag.Should().NotBeSameAs(shortTagClone);
        shortTag.Name.Should().Be(shortTagClone.Name);
        shortTag.Value.Should().Be(shortTagClone.Value);
        Invoking(() => new NbtShort((NbtShort)null)).Should().Throw<ArgumentNullException>();

        var stringTag = new NbtString("stringTag", "foo");
        var stringTagClone = (NbtString)stringTag.Clone();
        stringTag.Should().NotBeSameAs(stringTagClone);
        stringTag.Name.Should().Be(stringTagClone.Name);
        stringTag.Value.Should().Be(stringTagClone.Value);
        Invoking(() => new NbtString((NbtString)null)).Should().Throw<ArgumentNullException>();
    }


    [Fact]
    public void ByteArrayIndexerTest()
    {
        // test getting/settings values of byte array tag via indexer
        var byteArray = new NbtByteArray("Test");
        byteArray.Value.Should().BeEmpty();
        byteArray.Value = [1, 2, 3];
        byteArray[0].Should().Be(1);
        byteArray[1].Should().Be(2);
        byteArray[2].Should().Be(3);
        byteArray[0] = 4;
        byteArray[0].Should().Be(4);
    }


    [Fact]
    public void IntArrayIndexerTest()
    {
        // test getting/settings values of int array tag via indexer
        var intArray = new NbtIntArray("Test");
        intArray.Value.Should().BeEmpty();
        intArray.Value = [1, 2000, -3000000];
        intArray[0].Should().Be(1);
        intArray[1].Should().Be(2000);
        intArray[2].Should().Be(-3000000);
        intArray[0] = 4;
        intArray[0].Should().Be(4);
    }


    [Fact]
    public void LongArrayIndexerTest()
    {
        var longArray = new NbtLongArray("Test");
        longArray.Value.Should().BeEmpty();
        longArray.Value = [1, long.MaxValue, long.MinValue];
        longArray[0].Should().Be(1);
        longArray[1].Should().Be(long.MaxValue);
        longArray[2].Should().Be(long.MinValue);
        longArray[0] = 4;
        longArray[0].Should().Be(4);
    }


    [Fact]
    public void DefaultValueTest()
    {
        // test default values of all value tags
        new NbtByte("test").Value.Should().Be(0);
        new NbtByteArray("test").Value.Should().BeEmpty();
        new NbtDouble("test").Value.Should().Be(0d);
        new NbtFloat("test").Value.Should().Be(0f);
        new NbtInt("test").Value.Should().Be(0);
        new NbtIntArray("test").Value.Should().BeEmpty();
        new NbtLongArray("test").Value.Should().BeEmpty();
        new NbtLong("test").Value.Should().Be(0L);
        new NbtShort("test").Value.Should().Be(0);
        new NbtString().Value.Should().BeEmpty();
    }


    [Fact]
    public void NullValueTest()
    {
        Invoking(() => new NbtByteArray().Value = null).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtIntArray().Value = null).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtLongArray().Value = null).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtString().Value = null).Should().Throw<ArgumentNullException>();
    }


    [Fact]
    public void PathTest()
    {
        // test NbtTag.Path property
        var testComp = new NbtCompound
        {
            new NbtCompound("Compound")
            {
                new NbtCompound("InsideCompound")
            },
            new NbtList("List")
            {
                new NbtCompound
                {
                    new NbtInt("InsideCompoundAndList")
                }
            }
        };

        // parent-less tag with no name has empty string for a path
        testComp.Path.Should().BeEmpty();
        testComp["Compound"].Path.Should().Be(".Compound");
        testComp["Compound"]["InsideCompound"].Path.Should().Be(".Compound.InsideCompound");
        testComp["List"].Path.Should().Be(".List");

        // tags inside lists have no name, but they do have an index
        testComp["List"][0].Path.Should().Be(".List[0]");
        testComp["List"][0]["InsideCompoundAndList"].Path.Should().Be(".List[0].InsideCompoundAndList");
    }


    [Fact]
    public void BadParamsTest()
    {
        Invoking(() => new NbtByteArray((byte[])null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtIntArray((int[])null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtLongArray((long[])null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtString((string)null)).Should().Throw<ArgumentNullException>();
    }
}