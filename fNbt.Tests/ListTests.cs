using System.Collections;
using FluentAssertions;
using fNbt.Tags;

namespace fNbt.Tests;

public sealed class ListTests : TestBase
{
    [Fact]
    public void InterfaceImplementation()
    {
        // prepare our test lists
        var referenceList = new List<NbtTag>
        {
            new NbtInt(1),
            new NbtInt(2),
            new NbtInt(3)
        };
        var testTag = new NbtInt(4);
        var originalList = new NbtList(referenceList);

        // check IList implementation
        IList iList = originalList;
        referenceList.Should().BeEquivalentTo(iList.Cast<NbtTag>());

        // check IList<NbtTag> implementation
        IList<NbtTag?> iGenericList = originalList;
        referenceList.Should().BeEquivalentTo(iGenericList);
        iGenericList.IsReadOnly.Should().BeFalse();

        // check IList.Add
        referenceList.Add(testTag);
        iList.Add(testTag);
        referenceList.Should().BeEquivalentTo(iList.Cast<NbtTag>());

        // check IList.IndexOf
        referenceList.IndexOf(testTag).Should().Be(iList.IndexOf(testTag));
        iList.IndexOf(null).Should().BeLessThan(0);

        // check IList<NbtTag>.IndexOf
        referenceList.IndexOf(testTag).Should().Be(iGenericList.IndexOf(testTag));
        iGenericList.IndexOf(null).Should().BeLessThan(0);

        // check IList.Contains
        iList.Contains(testTag).Should().BeTrue();
        iList.Contains(null).Should().BeFalse();

        // check IList.Remove
        iList.Remove(testTag);
        iList.Contains(testTag).Should().BeFalse();

        // check IList.Insert
        iList.Insert(0, testTag);
        iList.IndexOf(testTag).Should().Be(0);

        // check IList.RemoveAt
        iList.RemoveAt(0);
        iList.Contains(testTag).Should().BeFalse();

        // check misc IList properties
        iList.IsFixedSize.Should().BeFalse();
        iList.IsReadOnly.Should().BeFalse();
        iList.IsSynchronized.Should().BeFalse();
        iList.SyncRoot.Should().NotBeNull();

        // check IList.CopyTo
        var exportTest = new NbtInt[iList.Count];
        iList.CopyTo(exportTest, 0);
        iList.Should().BeEquivalentTo(exportTest);

        // check IList.this[int]
        for (var i = 0; i < iList.Count; i++)
        {
            originalList[i].Should().Be(iList[i]);
            iList[i] = new NbtInt(i);
        }

        // check IList.Clear
        iList.Clear();
        iList.Count.Should().Be(0);
        iList.IndexOf(testTag).Should().BeLessThan(0);
    }


    [Fact]
    public void IndexerTest()
    {
        var ourTag = new NbtByte(1);
        var secondList = new NbtList
        {
            new NbtByte()
        };

        var testList = new NbtList();
        // Trying to set an out-of-range element
        Invoking(() => testList[0] = new NbtByte(1)).Should().Throw<ArgumentOutOfRangeException>();

        // Make sure that setting did not affect ListType
        testList.ListType.Should().Be(NbtTagType.Unknown);
        testList.Count.Should().Be(0);
        testList.Add(ourTag);

        // set a tag to null
        Invoking(() => testList[0] = null).Should().Throw<ArgumentNullException>();

        // set a tag to itself
        Invoking(() => testList[0] = testList).Should().Throw<ArgumentException>();

        // give a named tag where an unnamed tag was expected
        Invoking(() => testList[0] = new NbtByte("NamedTag")).Should().Throw<ArgumentException>();

        // give a tag of wrong type
        Invoking(() => testList[0] = new NbtInt(0)).Should().Throw<ArgumentException>();

        // give an unnamed tag that already has a parent
        Invoking(() => testList[0] = secondList[0]).Should().Throw<ArgumentException>();

        // Make sure that none of the failed insertions went through
        ourTag.Should().Be(testList[0]);
    }


    [Fact]
    public void InitializingListFromCollection()
    {
        // auto-detecting list type
        var test1 = new NbtList("Test1", [
            new NbtInt(1),
            new NbtInt(2),
            new NbtInt(3)
        ]);
        test1.ListType.Should().Be(NbtTagType.Int);

        // check pre-conditions
        Invoking(() => new NbtList((NbtTag[])null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtList(null, null)).Should().Throw<ArgumentNullException>();
        Invoking(() => new NbtList((string)null, NbtTagType.Unknown)).Should().NotThrow();
        Invoking(() => new NbtList((NbtTag[])null, NbtTagType.Unknown)).Should().Throw<ArgumentNullException>();

        // correct explicitly-given list type
        Invoking(() => new NbtList("Test2", [
            new NbtInt(1),
            new NbtInt(2),
            new NbtInt(3)
        ], NbtTagType.Int)).Should().NotThrow();

        // wrong explicitly-given list type
        Invoking(() => new NbtList("Test3", [
            new NbtInt(1),
            new NbtInt(2),
            new NbtInt(3)
        ], NbtTagType.Float)).Should().Throw<ArgumentException>();

        // auto-detecting mixed list given
        Invoking(() => new NbtList("Test4", [
            new NbtFloat(1),
            new NbtByte(2),
            new NbtInt(3)
        ])).Should().Throw<ArgumentException>();

        // using AddRange
        Invoking(() => new NbtList().AddRange([
            new NbtInt(1),
            new NbtInt(2),
            new NbtInt(3)
        ])).Should().NotThrow();

        Invoking(() => new NbtList().AddRange(null)).Should().Throw<ArgumentNullException>();
    }


    [Fact]
    public void ManipulatingList()
    {
        var sameTags = new NbtTag[]
        {
            new NbtInt(0),
            new NbtInt(1),
            new NbtInt(2)
        };

        var list = new NbtList("Test1", sameTags);

        // testing enumerator, indexer, Contains, and IndexOf
        var j = 0;
        foreach (var tag in list)
        {
            list.Contains(sameTags[j]).Should().BeTrue();
            sameTags[j].Should().Be(tag);
            j.Should().Be(list.IndexOf(tag));
            j++;
        }

        // adding an item of correct type
        list.Add(new NbtInt(3));
        list.Insert(3, new NbtInt(4));

        // adding an item of wrong type
        Invoking(() => list.Add(new NbtString())).Should().Throw<ArgumentException>();
        Invoking(() => list.Insert(3, new NbtString())).Should().Throw<ArgumentException>();
        Invoking(() => list.Insert(3, null)).Should().Throw<ArgumentNullException>();

        // testing array contents
        for (var i = 0; i < sameTags.Length; i++)
        {
            sameTags[i].Should().BeSameAs(list[i]);
            ((NbtInt)list[i]).Value.Should().Be(i);
        }

        // test removal
        list.Remove(new NbtInt(5)).Should().BeFalse();
        list.Remove(sameTags[0]).Should().BeTrue();
        Invoking(() => list.Remove(null)).Should().Throw<ArgumentNullException>();
        list.RemoveAt(0);
        Invoking(() => list.RemoveAt(10)).Should().Throw<ArgumentOutOfRangeException>();

        // Test some failure scenarios for Add:
        // adding a list to itself
        var loopList = new NbtList();
        loopList.ListType.Should().Be(NbtTagType.Unknown);
        Invoking(() => loopList.Add(loopList)).Should().Throw<ArgumentException>();

        // adding same tag to multiple lists
        Invoking(() => loopList.Add(list[0])).Should().Throw<ArgumentException>();
        Invoking(() => loopList.Insert(0, list[0])).Should().Throw<ArgumentException>();

        // adding null tag
        Invoking(() => loopList.Add(null)).Should().Throw<ArgumentNullException>();

        // make sure that all those failed adds didn't affect the tag
        loopList.Count.Should().Be(0);
        loopList.ListType.Should().Be(NbtTagType.Unknown);

        // try creating a list with invalid tag type
        Invoking(() => new NbtList((NbtTagType)200)).Should().Throw<ArgumentOutOfRangeException>();
    }


    [Fact]
    public void ChangingListTagType()
    {
        var list = new NbtList();

        // changing list type to an out-of-range type
        Invoking(() => list.ListType = (NbtTagType)200).Should().Throw<ArgumentOutOfRangeException>();

        // failing to add or insert a tag should not change ListType
        Invoking(() => list.Insert(-1, new NbtInt())).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => list.Add(new NbtInt("namedTagWhereUnnamedIsExpected"))).Should().Throw<ArgumentException>();
        list.ListType.Should().Be(NbtTagType.Unknown);

        // changing the type of an empty list to "End" is allowed, see https://github.com/fragmer/fNbt/issues/12
        Invoking(() => list.ListType = NbtTagType.End).Should().NotThrow();
        list.ListType.Should().Be(NbtTagType.End);

        // changing the type of an empty list back to "Unknown" is allowed too!
        Invoking(() => list.ListType = NbtTagType.Unknown).Should().NotThrow();
        list.ListType.Should().Be(NbtTagType.Unknown);

        // adding the first element should set the tag type
        list.Add(new NbtInt());
        list.ListType.Should().Be(NbtTagType.Int);

        // setting correct type for a non-empty list
        Invoking(() => list.ListType = NbtTagType.Int).Should().NotThrow();

        // changing list type to an incorrect type
        Invoking(() => list.ListType = NbtTagType.Short).Should().Throw<ArgumentException>();

        // after the list is cleared, we should once again be allowed to change its TagType
        list.Clear();
        Invoking(() => list.ListType = NbtTagType.Short).Should().NotThrow();
    }


    [Fact]
    public void SerializingWithoutListType()
    {
        var root = new NbtCompound("root")
        {
            new NbtList("list")
        };
        var file = new NbtFile(root);

        using var ms = new MemoryStream();
        // list should throw NbtFormatException, because its ListType is Unknown
        Invoking(() => file.SaveToStream(ms, NbtCompression.None)).Should().Throw<NbtFormatException>();
    }


    [Fact]
    public void Serializing1()
    {
        // check the basics of saving/loading
        const NbtTagType expectedListType = NbtTagType.Int;
        const int elements = 10;

        // construct nbt file
        var writtenFile = new NbtFile(new NbtCompound("ListTypeTest"));
        var writtenList = new NbtList("Entities", null, expectedListType);
        for (var i = 0; i < elements; i++) writtenList.Add(new NbtInt(i));
        writtenFile.RootTag.Add(writtenList);

        // test saving
        var data = writtenFile.SaveToBuffer(NbtCompression.None);

        // test loading
        var readFile = new NbtFile();
        var bytesRead = readFile.LoadFromBuffer(data, 0, data.Length, NbtCompression.None);
        bytesRead.Should().Be(data.Length);

        // check contents of loaded file
        readFile.RootTag.Should().NotBeNull();
        readFile.RootTag["Entities"].Should().BeOfType<NbtList>();
        var readList = (NbtList)readFile.RootTag["Entities"];
        readList.ListType.Should().Be(writtenList.ListType);
        readList.Count.Should().Be(writtenList.Count);

        // check .ToArray
        readList.Should().BeEquivalentTo(readList.ToArray());
        readList.Should().BeEquivalentTo(readList.ToArray<NbtInt>());

        // check contents of loaded list
        for (var i = 0; i < elements; i++) readList.Get<NbtInt>(i).Value.Should().Be(writtenList.Get<NbtInt>(i).Value);
    }


    [Fact]
    public void Serializing2()
    {
        // check saving/loading lists of all possible value types
        var testFile = new NbtFile(TestFiles.MakeListTest());
        var buffer = testFile.SaveToBuffer(NbtCompression.None);
        var bytesRead = testFile.LoadFromBuffer(buffer, 0, buffer.Length, NbtCompression.None);
        bytesRead.Should().Be(buffer.Length);
    }


    [Fact]
    public void SerializingEmpty()
    {
        // check saving/loading lists of all possible value types
        var testFile = new NbtFile(new NbtCompound("root")
        {
            new NbtList("emptyList", NbtTagType.End),
            new NbtList("listyList", NbtTagType.List)
            {
                new NbtList(NbtTagType.End)
            }
        });
        var buffer = testFile.SaveToBuffer(NbtCompression.None);

        testFile.LoadFromBuffer(buffer, 0, buffer.Length, NbtCompression.None);

        var list1 = testFile.RootTag.Get<NbtList>("emptyList");
        list1.Count.Should().Be(0);
        list1.ListType.Should().Be(NbtTagType.End);

        var list2 = testFile.RootTag.Get<NbtList>("listyList");
        list2.Count.Should().Be(1);
        list2.ListType.Should().Be(NbtTagType.List);
        list2.Get<NbtList>(0).Count.Should().Be(0);
        list2.Get<NbtList>(0).ListType.Should().Be(NbtTagType.End);
    }


    [Fact]
    public void NestedListAndCompoundTest()
    {
        byte[] data;
        {
            var root = new NbtCompound("Root");
            var outerList = new NbtList("OuterList", NbtTagType.Compound);
            var outerCompound = new NbtCompound();
            var innerList = new NbtList("InnerList", NbtTagType.Compound);
            var innerCompound = new NbtCompound();

            innerList.Add(innerCompound);
            outerCompound.Add(innerList);
            outerList.Add(outerCompound);
            root.Add(outerList);

            var file = new NbtFile(root);
            data = file.SaveToBuffer(NbtCompression.None);
        }
        {
            var file = new NbtFile();
            var bytesRead = file.LoadFromBuffer(data, 0, data.Length, NbtCompression.None);

            bytesRead.Should().Be(data.Length);
            file.RootTag.Get<NbtList>("OuterList").Count.Should().Be(1);
            file.RootTag.Get<NbtList>("OuterList").Get<NbtCompound>(0).Name.Should().BeNull();
            file.RootTag.Get<NbtList>("OuterList").Get<NbtCompound>(0).Get<NbtList>("InnerList").Count.Should().Be(1);
            file.RootTag.Get<NbtList>("OuterList").Get<NbtCompound>(0).Get<NbtList>("InnerList").Get<NbtCompound>(0)
                .Name.Should().BeNull();
        }
    }


    [Fact]
    public void FirstInsertTest()
    {
        NbtList list = [];
        list.ListType.Should().Be(NbtTagType.Unknown);
        list.Insert(0, new NbtInt(123));
        // Inserting a tag should set ListType
        list.ListType.Should().Be(NbtTagType.Int);
    }
}