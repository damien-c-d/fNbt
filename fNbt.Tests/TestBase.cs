using FluentAssertions;
using JetBrains.Annotations;

namespace fNbt.Tests;

public abstract class TestBase
{
    /// <summary>
    ///     Invokes the specified action so that you can assert that it throws an exception.
    /// </summary>
    [Pure]
    public static Action Invoking(Action action)
    {
        return FluentActions.Invoking(action);
    }

    /// <summary>
    ///     Invokes the specified action so that you can assert that it throws an exception.
    /// </summary>
    [Pure]
    protected Func<T> Invoking<T>(Func<T> func)
    {
        return FluentActions.Invoking(func);
    }


    protected FileToTest TestFile(string fName)
    {
        return new FileToTest { Path = fName };
    }

    protected StreamToTest TestStream(Stream stream)
    {
        return new StreamToTest { Stream = stream };
    }
}

public class FileAssertion
{
    public required FileToTest File { get; init; }

    public void NotExist(string because = "", params object[] reasonArgs)
    {
        System.IO.File.Exists(File.Path).Should().BeFalse(because, reasonArgs);
    }

    public void Exist(string because = "", params object[] reasonArgs)
    {
        System.IO.File.Exists(File.Path).Should().BeTrue(because, reasonArgs);
    }

    public void BeEquivalentTo(string otherPath, string because = "", params object[] reasonArgs)
    {
        System.IO.File.ReadAllBytes(File.Path).Should()
            .BeEquivalentTo(System.IO.File.ReadAllBytes(otherPath), because, reasonArgs);
    }
}

public class StreamAssertion
{
    public required StreamToTest Stream { get; init; }

    public void BeEquivalentTo(Stream otherStream, string because = "", params object[] reasonArgs)
    {
        Stream.Stream.Position = 0;
        otherStream.Position = 0;

        // This doesn't work
        //Stream.Stream.Should().BeEquivalentTo(otherStream, because, reasonArgs);

        // We need to compare byte for byte
        while (true)
        {
            var thisByte = Stream.Stream.ReadByte();
            var otherByte = otherStream.ReadByte();

            thisByte.Should().Be(otherByte, because, reasonArgs);
            if (thisByte == -1)
                break;
        }
    }
}

public class FileToTest
{
    public required string Path;
}

public class StreamToTest
{
    public required Stream Stream;
}

public static class AssertionExtensions
{
    public static FileAssertion Should(this FileToTest file)
    {
        return new FileAssertion { File = file };
    }

    public static StreamAssertion Should(this StreamToTest stream)
    {
        return new StreamAssertion { Stream = stream };
    }
}