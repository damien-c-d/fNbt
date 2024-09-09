using System.Reflection;
using FluentAssertions;
using fNbt.Tags;

namespace fNbt.Tests;

public static class TestFiles
{
    public static readonly string DirName =
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "TestFiles");

    public static readonly string Small = Path.Combine(DirName, "test.nbt");
    public static readonly string SmallGZip = Path.Combine(DirName, "test.nbt.gz");
    public static readonly string SmallZLib = Path.Combine(DirName, "test.nbt.z");
    public static readonly string Big = Path.Combine(DirName, "bigtest.nbt");
    public static readonly string BigGZip = Path.Combine(DirName, "bigtest.nbt.gz");
    public static readonly string BigZLib = Path.Combine(DirName, "bigtest.nbt.z");


    // creates a compound containing lists of every kind of tag
    public static NbtCompound MakeListTest()
    {
        return new NbtCompound("Root")
        {
            new NbtList("ByteList")
            {
                new NbtByte(100),
                new NbtByte(20),
                new NbtByte(3)
            },
            new NbtList("DoubleList")
            {
                new NbtDouble(1d),
                new NbtDouble(2000d),
                new NbtDouble(-3000000d)
            },
            new NbtList("FloatList")
            {
                new NbtFloat(1f),
                new NbtFloat(2000f),
                new NbtFloat(-3000000f)
            },
            new NbtList("IntList")
            {
                new NbtInt(1),
                new NbtInt(2000),
                new NbtInt(-3000000)
            },
            new NbtList("LongList")
            {
                new NbtLong(1L),
                new NbtLong(2000L),
                new NbtLong(-3000000L)
            },
            new NbtList("ShortList")
            {
                new NbtShort(1),
                new NbtShort(200),
                new NbtShort(-30000)
            },
            new NbtList("StringList")
            {
                new NbtString("one"),
                new NbtString("two thousand"),
                new NbtString("negative three million")
            },
            new NbtList("CompoundList")
            {
                new NbtCompound(),
                new NbtCompound(),
                new NbtCompound()
            },
            new NbtList("ListList")
            {
                new NbtList(NbtTagType.List),
                new NbtList(NbtTagType.List),
                new NbtList(NbtTagType.List)
            },
            new NbtList("ByteArrayList")
            {
                new NbtByteArray([
                    1, 2, 3
                ]),
                new NbtByteArray([
                    11, 12, 13
                ]),
                new NbtByteArray([
                    21, 22, 23
                ])
            },
            new NbtList("IntArrayList")
            {
                new NbtIntArray([
                    1, -2, 3
                ]),
                new NbtIntArray([
                    1000, -2000, 3000
                ]),
                new NbtIntArray([
                    1000000, -2000000, 3000000
                ])
            },
            new NbtList("LongArrayList")
            {
                new NbtLongArray([
                    10, -20, 30
                ]),
                new NbtLongArray([
                    100, -200, 300
                ]),
                new NbtLongArray([
                    100, -200, 300
                ])
            }
        };
    }


    // creates a file with lots of compounds and lists, used to test NbtReader compliance
    public static Stream MakeReaderTest()
    {
        var root = new NbtCompound("root")
        {
            new NbtInt("first"),
            new NbtInt("second"),
            new NbtCompound("third-comp")
            {
                new NbtInt("inComp1"),
                new NbtInt("inComp2"),
                new NbtInt("inComp3")
            },
            new NbtList("fourth-list")
            {
                new NbtList
                {
                    new NbtCompound
                    {
                        new NbtCompound("inList1")
                    }
                },
                new NbtList
                {
                    new NbtCompound
                    {
                        new NbtCompound("inList2")
                    }
                },
                new NbtList
                {
                    new NbtCompound
                    {
                        new NbtCompound("inList3")
                    }
                }
            },
            new NbtInt("fifth"),
            new NbtByteArray("hugeArray", new byte[1024 * 1024])
        };
        var testData = new NbtFile(root).SaveToBuffer(NbtCompression.None);
        return new MemoryStream(testData);
    }


    // creates an NbtFile with contents identical to "test.nbt"
    public static NbtFile MakeSmallFile()
    {
        return new NbtFile(new NbtCompound("hello world")
        {
            new NbtString("name", "Bananrama")
        });
    }


    public static void AssertNbtSmallFile(NbtFile file)
    {
        file.RootTag.Should().BeOfType<NbtCompound>();

        var root = file.RootTag;
        root.Name.Should().Be("hello world");
        root.Count.Should().Be(1);
        root["name"].Should().BeOfType<NbtString>();

        var node = (NbtString)root["name"];
        node.Name.Should().Be("name");
        node.Value.Should().Be("Bananrama");
    }


    public static void AssertNbtBigFile(NbtFile file)
    {
        file.RootTag.Should().BeOfType<NbtCompound>();

        var root = file.RootTag;
        root.Name.Should().Be("Level");
        root.Count.Should().Be(13);

        root["longTest"].Should().BeOfType<NbtLong>();
        var node = root["longTest"];
        node.Name.Should().Be("longTest");
        ((NbtLong)node).Value.Should().Be(9223372036854775807);

        root["shortTest"].Should().BeOfType<NbtShort>();
        node = root["shortTest"];
        node.Name.Should().Be("shortTest");
        ((NbtShort)node).Value.Should().Be(32767);

        root["stringTest"].Should().BeOfType<NbtString>();
        node = root["stringTest"];
        node.Name.Should().Be("stringTest");
        ((NbtString)node).Value.Should().Be("HELLO WORLD THIS IS A TEST STRING ÅÄÖ!");

        root["floatTest"].Should().BeOfType<NbtFloat>();
        node = root["floatTest"];
        node.Name.Should().Be("floatTest");
        ((NbtFloat)node).Value.Should().Be(0.49823147f);

        root["intTest"].Should().BeOfType<NbtInt>();
        node = root["intTest"];
        node.Name.Should().Be("intTest");
        ((NbtInt)node).Value.Should().Be(2147483647);

        root["nested compound test"].Should().BeOfType<NbtCompound>();
        node = root["nested compound test"];
        node.Name.Should().Be("nested compound test");
        ((NbtCompound)node).Count.Should().Be(2);

        // First nested test
        node["ham"].Should().BeOfType<NbtCompound>();
        var subNode = (NbtCompound)node["ham"];
        subNode.Name.Should().Be("ham");
        subNode.Count.Should().Be(2);

        // Checking sub node values

        subNode["name"].Should().BeOfType<NbtString>();
        subNode["name"].Name.Should().Be("name");
        ((NbtString)subNode["name"]).Value.Should().Be("Hampus");


        subNode["value"].Should().BeOfType<NbtFloat>();
        subNode["value"].Name.Should().Be("value");
        ((NbtFloat)subNode["value"]).Value.Should().Be(0.75f);

        // End sub node

        // Second nested test
        node["egg"].Should().BeOfType<NbtCompound>();
        subNode = (NbtCompound)node["egg"];
        subNode.Name.Should().Be("egg");
        subNode.Count.Should().Be(2);

        // Checking sub node values

        subNode["name"].Should().BeOfType<NbtString>();
        subNode["name"].Name.Should().Be("name");
        ((NbtString)subNode["name"]).Value.Should().Be("Eggbert");


        subNode["value"].Should().BeOfType<NbtFloat>();
        subNode["value"].Name.Should().Be("value");
        ((NbtFloat)subNode["value"]).Value.Should().Be(0.5f);
        // End sub node

        root["listTest (long)"].Should().BeOfType<NbtList>();
        node = root["listTest (long)"];
        node.Name.Should().Be("listTest (long)");
        ((NbtList)node).Count.Should().Be(5);

        // The values should be: 11, 12, 13, 14, 15
        for (var nodeIndex = 0; nodeIndex < ((NbtList)node).Count; nodeIndex++)
        {
            node[nodeIndex].Should().BeOfType<NbtLong>();
            node[nodeIndex].Name.Should().BeNull();
            ((NbtLong)node[nodeIndex]).Value.Should().Be(nodeIndex + 11);
        }

        root["listTest (compound)"].Should().BeOfType<NbtList>();
        node = root["listTest (compound)"];
        node.Name.Should().Be("listTest (compound)");
        ((NbtList)node).Count.Should().Be(2);

        // First Sub Node
        node[0].Should().BeOfType<NbtCompound>();
        subNode = (NbtCompound)node[0];

        // First node in sub node

        subNode["name"].Should().BeOfType<NbtString>();
        subNode["name"].Name.Should().Be("name");
        ((NbtString)subNode["name"]).Value.Should().Be("Compound tag #0");

        // Second node in sub node

        subNode["created-on"].Should().BeOfType<NbtLong>();
        subNode["created-on"].Name.Should().Be("created-on");
        ((NbtLong)subNode["created-on"]).Value.Should().Be(1264099775885);

        // Second Sub Node
        node[1].Should().BeOfType<NbtCompound>();
        subNode = (NbtCompound)node[1];

        // First node in sub node

        subNode["name"].Should().BeOfType<NbtString>();
        subNode["name"].Name.Should().Be("name");
        ((NbtString)subNode["name"]).Value.Should().Be("Compound tag #1");

        // Second node in sub node

        subNode["created-on"].Should().BeOfType<NbtLong>();
        subNode["created-on"].Name.Should().Be("created-on");
        ((NbtLong)subNode["created-on"]).Value.Should().Be(1264099775885);

        root["byteTest"].Should().BeOfType<NbtByte>();
        node = root["byteTest"];

        node.Name.Should().Be("byteTest");
        ((NbtByte)node).Value.Should().Be(127);

        const string byteArrayName =
            "byteArrayTest (the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...))";
        root[byteArrayName].Should().BeOfType<NbtByteArray>();
        node = root[byteArrayName];

        node.Name.Should().Be(byteArrayName);
        ((NbtByteArray)node).Value.Length.Should().Be(1000);

        // Values are: the first 1000 values of (n*n*255+n*7)%100, starting with n=0 (0, 62, 34, 16, 8, ...)
        for (var n = 0; n < 1000; n++) ((NbtByteArray)node)[n].Should().Be((byte)((n * n * 255 + n * 7) % 100));

        root["doubleTest"].Should().BeOfType<NbtDouble>();
        node = root["doubleTest"];
        node.Name.Should().Be("doubleTest");
        ((NbtDouble)node).Value.Should().Be(0.4931287132182315);

        root["intArrayTest"].Should().BeOfType<NbtIntArray>();
        var intArrayTag = root.Get<NbtIntArray>("intArrayTest");
        intArrayTag.Should().NotBeNull();
        intArrayTag!.Value.Length.Should().Be(10);
        var rand = new Random(0);
        for (var i = 0; i < 10; i++) intArrayTag.Value[i].Should().Be(rand.Next());

        root["longArrayTest"].Should().BeOfType<NbtLongArray>();
        var longArrayTag = root.Get<NbtLongArray>("longArrayTest");
        longArrayTag.Should().NotBeNull();
        longArrayTag!.Value.Length.Should().Be(5);
        var rand2 = new Random(0);
        for (var i = 0; i < 5; i++) longArrayTag.Value[i].Should().Be(((long)rand2.Next() << 32) | (uint)rand2.Next());
    }


    #region Value test

    // creates an NbtCompound with one of tag of each value-type
    public static NbtCompound MakeValueTest()
    {
        return new NbtCompound("root")
        {
            new NbtByte("byte", 1),
            new NbtShort("short", 2),
            new NbtInt("int", 3),
            new NbtLong("long", 4L),
            new NbtFloat("float", 5f),
            new NbtDouble("double", 6d),
            new NbtByteArray("byteArray", [10, 11, 12]),
            new NbtIntArray("intArray", [20, 21, 22]),
            new NbtLongArray("longArray", [200, 210, 220]),
            new NbtString("string", "123")
        };
    }


    public static void AssertValueTest(NbtFile file)
    {
        file.RootTag.Should().BeOfType<NbtCompound>();

        var root = file.RootTag;
        root.Name.Should().Be("root");
        root.Count.Should().Be(10);

        root["byte"].Should().BeOfType<NbtByte>();
        var node = root["byte"];
        node.Name.Should().Be("byte");
        node.ByteValue.Should().Be(1);

        root["short"].Should().BeOfType<NbtShort>();
        node = root["short"];
        node.Name.Should().Be("short");
        node.ShortValue.Should().Be(2);

        root["int"].Should().BeOfType<NbtInt>();
        node = root["int"];
        node.Name.Should().Be("int");
        node.IntValue.Should().Be(3);

        root["long"].Should().BeOfType<NbtLong>();
        node = root["long"];
        node.Name.Should().Be("long");
        node.LongValue.Should().Be(4L);

        root["float"].Should().BeOfType<NbtFloat>();
        node = root["float"];
        node.Name.Should().Be("float");
        node.FloatValue.Should().Be(5f);

        root["double"].Should().BeOfType<NbtDouble>();
        node = root["double"];
        node.Name.Should().Be("double");
        node.DoubleValue.Should().Be(6d);

        root["byteArray"].Should().BeOfType<NbtByteArray>();
        node = root["byteArray"];
        node.Name.Should().Be("byteArray");
        node.ByteArrayValue.Should().Equal(10, 11, 12);

        root["intArray"].Should().BeOfType<NbtIntArray>();
        node = root["intArray"];
        node.Name.Should().Be("intArray");
        node.IntArrayValue.Should().Equal(20, 21, 22);

        root["longArray"].Should().BeOfType<NbtLongArray>();
        node = root["longArray"];
        node.Name.Should().Be("longArray");
        node.LongArrayValue.Should().Equal(200, 210, 220);

        root["string"].Should().BeOfType<NbtString>();
        node = root["string"];
        node.Name.Should().Be("string");
        node.StringValue.Should().Be("123");
    }

    #endregion
}