using System.Collections;
using FluentAssertions;
using fNbt.Tags;

namespace fNbt.Tests;

public sealed class CompoundTests : TestBase
{
    [Fact]
    public void InitializingCompoundFromCollectionTest()
    {
        NbtTag[] allNamed =
        [
            new NbtShort("allNamed1", 1),
            new NbtLong("allNamed2", 2),
            new NbtInt("allNamed3", 3)
        ];

        NbtTag[] someUnnamed =
        [
            new NbtInt("someUnnamed1", 1),
            new NbtInt(2),
            new NbtInt("someUnnamed3", 3)
        ];

        NbtTag?[] someNull =
        [
            new NbtInt("someNull1", 1),
            null,
            new NbtInt("someNull3", 3)
        ];

        NbtTag[] dupeNames =
        [
            new NbtInt("dupeNames1", 1),
            new NbtInt("dupeNames2", 2),
            new NbtInt("dupeNames1", 3)
        ];

        // null collection, should throw
        Invoking(() => new NbtCompound("nullTest", null)).Should().Throw<ArgumentNullException>();

        // proper initialization
        NbtCompound? allNamedTest = null;
        Invoking(() => allNamedTest = new NbtCompound("allNamedTest", allNamed)).Should().NotThrow();
        allNamedTest.Should().BeEquivalentTo(allNamed);

        // some tags are unnamed, should throw
        Invoking(() => new NbtCompound("someUnnamedTest", someUnnamed)).Should().Throw<ArgumentException>();

        // some tags are null, should throw
        Invoking(() => new NbtCompound("someNullTest", someNull)).Should().Throw<ArgumentNullException>();

        // some tags have same names, should throw
        Invoking(() => new NbtCompound("dupeNamesTest", dupeNames)).Should().Throw<ArgumentException>();
    }


    [Fact]
    public void GettersAndSetters()
    {
        // construct a document for us to test.
        var nestedChild = new NbtCompound("NestedChild");
        var nestedInt = new NbtInt(1);
        var nestedChildList = new NbtList("NestedChildList")
        {
            nestedInt
        };
        var child = new NbtCompound("Child")
        {
            nestedChild,
            nestedChildList
        };
        var childList = new NbtList("ChildList")
        {
            new NbtInt(1)
        };
        var parent = new NbtCompound("Parent")
        {
            child,
            childList
        };

        // Accessing nested compound tags using indexers
        parent["Child"]["NestedChild"].Should().Be(nestedChild);
        parent["Child"]["NestedChildList"].Should().Be(nestedChildList);
        parent["Child"]["NestedChildList"][0].Should().Be(nestedInt);

        // Accessing nested compound tags using Get and Get<T>
        Invoking(() => parent.Get<NbtCompound>(null)).Should().Throw<ArgumentNullException>();
        parent.Get<NbtCompound>("NonExistingChild").Should().BeNull();
        nestedChild.Should().BeSameAs(parent.Get<NbtCompound>("Child").Get<NbtCompound>("NestedChild"));
        nestedChildList.Should().BeSameAs(parent.Get<NbtCompound>("Child").Get<NbtList>("NestedChildList"));
        nestedInt.Should().BeSameAs(parent.Get<NbtCompound>("Child").Get<NbtList>("NestedChildList")[0]);
        Invoking(() => parent.Get(null)).Should().Throw<ArgumentNullException>();
        parent.Get("NonExistingChild").Should().BeNull();
        parent.Get("Child").Should().BeOfType<NbtCompound>().Which.Get("NestedChild").Should().BeSameAs(nestedChild);
        parent.Get("Child").Should().BeOfType<NbtCompound>().Which.Get("NestedChildList").Should()
            .BeSameAs(nestedChildList);

        // Accessing with Get<T> and an invalid given type
        Invoking(() => parent.Get<NbtInt>("Child")).Should().Throw<InvalidCastException>();

        // Using TryGet and TryGet<T>
        NbtTag dummyTag;
        Invoking(() => parent.TryGet(null, out dummyTag)).Should().Throw<ArgumentNullException>();
        parent.TryGet("NonExistingChild", out dummyTag).Should().BeFalse();
        parent.TryGet("Child", out dummyTag).Should().BeTrue();
        NbtCompound dummyCompoundTag;
        Invoking(() => parent.TryGet(null, out dummyCompoundTag)).Should().Throw<ArgumentNullException>();
        parent.TryGet("NonExistingChild", out dummyCompoundTag).Should().BeFalse();
        parent.TryGet("Child", out dummyCompoundTag).Should().BeTrue();

        // Trying to use integer indexers on non-NbtList tags
        Invoking(() => parent[0] = nestedInt).Should().Throw<InvalidOperationException>();
        Invoking(() => nestedInt[0] = nestedInt).Should().Throw<InvalidOperationException>();

        // Trying to use string indexers on non-NbtCompound tags
        Invoking(() => dummyTag = childList["test"]).Should().Throw<InvalidOperationException>();
        Invoking(() => childList["test"] = nestedInt).Should().Throw<InvalidOperationException>();
        Invoking(() => nestedInt["test"] = nestedInt).Should().Throw<InvalidOperationException>();

        // Trying to get a non-existent element by name
        parent.Get<NbtTag>("NonExistentTag").Should().BeNull();
        parent["NonExistentTag"].Should().BeNull();

        // Null indices on NbtCompound
        Invoking(() => parent.Get<NbtTag>(null)).Should().Throw<ArgumentNullException>();
        Invoking(() => parent[null] = new NbtInt(1)).Should().Throw<ArgumentNullException>();
        Invoking(() => nestedInt = (NbtInt)parent[null]).Should().Throw<ArgumentNullException>();

        // Out-of-range indices on NbtList
        Invoking(() => nestedInt = (NbtInt)childList[-1]).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => childList[-1] = new NbtInt(1)).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => nestedInt = childList.Get<NbtInt>(-1)).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => nestedInt = (NbtInt)childList[childList.Count]).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => nestedInt = childList.Get<NbtInt>(childList.Count)).Should()
            .Throw<ArgumentOutOfRangeException>();

        // Using setter correctly
        parent["NewChild"] = new NbtByte("NewChild");

        // Using setter incorrectly
        object dummyObject;
        Invoking(() => parent["Child"] = null).Should().Throw<ArgumentNullException>();
        parent["Child"].Should().NotBeNull();
        Invoking(() => parent["Child"] = new NbtByte("NotChild")).Should().Throw<ArgumentException>();
        Invoking(() => dummyObject = parent[0]).Should().Throw<InvalidOperationException>();
        Invoking(() => parent[0] = new NbtByte("NewerChild")).Should().Throw<InvalidOperationException>();

        // Try adding tag to self
        var selfTest = new NbtCompound("SelfTest");
        Invoking(() => selfTest["SelfTest"] = selfTest).Should().Throw<ArgumentException>();

        // Try adding a tag that already has a parent
        Invoking(() => selfTest[child.Name] = child).Should().Throw<ArgumentException>();
    }


    [Fact]
    public void Renaming()
    {
        var tagToRename = new NbtInt("DifferentName", 1);
        var compound = new NbtCompound
        {
            new NbtInt("SameName", 1),
            tagToRename
        };

        // proper renaming, should not throw
        tagToRename.Name = "SomeOtherName";

        // attempting to use a duplicate name
        Invoking(() => tagToRename.Name = "SameName").Should().Throw<ArgumentException>();

        // assigning a null name to a tag inside a compound; should throw
        Invoking(() => tagToRename.Name = null).Should().Throw<ArgumentNullException>();

        // assigning a null name to a tag that's been removed; should not throw
        compound.Remove(tagToRename);
        tagToRename.Name = null;
    }


    [Fact]
    public void AddingAndRemoving()
    {
        var foo = new NbtInt("Foo");
        var test = new NbtCompound
        {
            foo
        };

        // adding duplicate object
        Invoking(() => test.Add(foo)).Should().Throw<ArgumentException>();

        // adding duplicate name
        Invoking(() => test.Add(new NbtByte("Foo"))).Should().Throw<ArgumentException>();

        // adding unnamed tag
        Invoking(() => test.Add(new NbtInt())).Should().Throw<ArgumentException>();

        // adding null
        Invoking(() => test.Add(null)).Should().Throw<ArgumentNullException>();

        // adding tag to self
        Invoking(() => test.Add(test)).Should().Throw<ArgumentException>();

        // contains existing name/object
        test.Contains("Foo").Should().BeTrue();
        test.Contains(foo).Should().BeTrue();
        Invoking(() => test.Contains((string)null)).Should().Throw<ArgumentNullException>();
        Invoking(() => test.Contains((NbtTag?)null)).Should().Throw<ArgumentNullException>();

        // contains non-existent name
        test.Contains("Bar").Should().BeFalse();

        // contains existing name / different object
        test.Contains(new NbtInt("Foo")).Should().BeFalse();

        // removing non-existent name
        Invoking(() => test.Remove((string)null)).Should().Throw<ArgumentNullException>();
        test.Remove("Bar").Should().BeFalse();

        // removing existing name
        test.Remove("Foo").Should().BeTrue();

        // removing non-existent name
        test.Remove("Foo").Should().BeFalse();

        // re-adding object
        test.Add(foo);

        // removing existing object
        Invoking(() => test.Remove((NbtTag?)null)).Should().Throw<ArgumentNullException>();
        test.Remove(foo).Should().BeTrue();
        test.Remove(foo).Should().BeFalse();

        // clearing an empty NbtCompound
        test.Count.Should().Be(0);
        test.Clear();

        // re-adding after clearing
        test.Add(foo);
        test.Count.Should().Be(1);

        // clearing a non-empty NbtCompound
        test.Clear();
        test.Count.Should().Be(0);
    }


    [Fact]
    public void UtilityMethods()
    {
        NbtTag[] testThings =
        [
            new NbtShort("Name1", 1),
            new NbtInt("Name2", 2),
            new NbtLong("Name3", 3)
        ];
        var compound = new NbtCompound();

        // add range
        compound.AddRange(testThings);

        // add range with duplicates
        Invoking(() => compound.AddRange(testThings)).Should().Throw<ArgumentException>();
    }


    [Fact]
    public void InterfaceImplementations()
    {
        NbtTag[] tagList =
        [
            new NbtByte("First", 1), new NbtShort("Second", 2), new NbtInt("Third", 3),
            new NbtLong("Fourth", 4L)
        ];

        // test NbtCompound(IEnumerable<NbtTag>) constructor
        var comp = new NbtCompound(tagList);

        // test .Names and .Tags collections
        comp.Names.Should().BeEquivalentTo("First", "Second", "Third", "Fourth");
        comp.Tags.Should().BeEquivalentTo(tagList);

        // test ICollection and ICollection<NbtTag> boilerplate properties
        ICollection<NbtTag> iGenCollection = comp;
        iGenCollection.IsReadOnly.Should().BeFalse();
        ICollection iCollection = comp;
        iCollection.SyncRoot.Should().NotBeNull();
        iCollection.IsSynchronized.Should().BeFalse();

        // test CopyTo()
        var tags = new NbtTag[iCollection.Count];
        iCollection.CopyTo(tags, 0);
        tags.Should().BeEquivalentTo(comp);

        // test non-generic GetEnumerator()
        var enumeratedTags = comp.ToList();
        enumeratedTags.Should().BeEquivalentTo(tagList);

        // test generic GetEnumerator()
        List<NbtTag> enumeratedTags2 = [];
        var enumerator = comp.GetEnumerator();
        while (enumerator.MoveNext()) enumeratedTags2.Add(enumerator.Current);
        enumeratedTags2.Should().BeEquivalentTo(tagList);
    }
}