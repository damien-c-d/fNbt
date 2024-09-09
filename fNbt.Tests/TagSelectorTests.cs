using FluentAssertions;
using fNbt.Tags;

namespace fNbt.Tests;

public sealed class TagSelectorTests : TestBase
{
    [Fact]
    public void SkippingTagsOnFileLoad()
    {
        var loadedFile = new NbtFile();
        loadedFile.LoadFromFile(TestFiles.Big,
            NbtCompression.None,
            tag => tag.Name != "nested compound test");
        loadedFile.RootTag.Contains("nested compound test").Should().BeFalse();
        loadedFile.RootTag.Contains("listTest (long)").Should().BeTrue();

        loadedFile.LoadFromFile(TestFiles.Big,
            NbtCompression.None,
            tag => tag.TagType != NbtTagType.Float || tag.Parent.Name != "Level");
        loadedFile.RootTag.Contains("floatTest").Should().BeFalse();
        loadedFile.RootTag["nested compound test"]["ham"]["value"].FloatValue.Should().Be(0.75f);

        loadedFile.LoadFromFile(TestFiles.Big,
            NbtCompression.None,
            tag => tag.Name != "listTest (long)");
        loadedFile.RootTag.Contains("listTest (long)").Should().BeFalse();
        loadedFile.RootTag.Contains("byteTest").Should().BeTrue();

        loadedFile.LoadFromFile(TestFiles.Big,
            NbtCompression.None,
            tag => false);
        loadedFile.RootTag.Count.Should().Be(0);
    }


    [Fact]
    public void SkippingLists()
    {
        {
            var file = new NbtFile(TestFiles.MakeListTest());
            var savedFile = file.SaveToBuffer(NbtCompression.None);
            file.LoadFromBuffer(savedFile, 0, savedFile.Length, NbtCompression.None,
                tag => tag.TagType != NbtTagType.List);

            file.RootTag.Count.Should().Be(0);
        }
        {
            // Check list-compound interaction
            var comp = new NbtCompound("root")
            {
                new NbtCompound("compOfLists")
                {
                    new NbtList("listOfComps")
                    {
                        new NbtCompound
                        {
                            new NbtList("emptyList", NbtTagType.Compound)
                        }
                    }
                }
            };
            var file = new NbtFile(comp);
            var savedFile = file.SaveToBuffer(NbtCompression.None);
            file.LoadFromBuffer(savedFile, 0, savedFile.Length, NbtCompression.None,
                tag => tag.TagType != NbtTagType.List);

            file.RootTag.Count.Should().Be(1);
        }
    }


    [Fact]
    public void SkippingValuesInCompoundTest()
    {
        var root = TestFiles.MakeValueTest();
        var nestedComp = TestFiles.MakeValueTest();
        nestedComp.Name = "NestedComp";
        root.Add(nestedComp);

        var file = new NbtFile(root);
        var savedFile = file.SaveToBuffer(NbtCompression.None);
        file.LoadFromBuffer(savedFile, 0, savedFile.Length, NbtCompression.None, tag => false);
        file.RootTag.Count.Should().Be(0);
    }
}