﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using fNbt.Tags;
using JetBrains.Annotations;

namespace fNbt;

/// <summary>
///     An efficient writer for writing NBT data directly to streams.
///     Each instance of NbtWriter writes one complete file.
///     NbtWriter enforces all constraints of the NBT file format
///     EXCEPT checking for duplicate tag names within a compound.
/// </summary>
public sealed class NbtWriter
{
    private const int MaxStreamCopyBufferSize = 8 * 1024;
    private readonly NbtBinaryWriter _writer;
    private int _listIndex;
    private int _listSize;
    private NbtTagType _listType;
    private Stack<NbtWriterNode> _nodes;
    private NbtTagType _parentType;


    /// <summary> Initializes a new instance of the NbtWriter class. </summary>
    /// <param name="stream"> Stream to write to. </param>
    /// <param name="rootTagName"> Name to give to the root tag (written immediately). </param>
    /// <remarks> Assumes that data in the stream should be Big-Endian encoded. </remarks>
    /// <exception cref="ArgumentNullException"> <paramref name="stream" /> or <paramref name="rootTagName" /> is <c>null</c>. </exception>
    /// <exception cref="ArgumentException"> <paramref name="stream" /> is not writable. </exception>
    public NbtWriter([NotNull] Stream stream, [NotNull] string rootTagName)
        : this(stream, rootTagName, true)
    {
    }


    /// <summary> Initializes a new instance of the NbtWriter class. </summary>
    /// <param name="stream"> Stream to write to. </param>
    /// <param name="rootTagName"> Name to give to the root tag (written immediately). </param>
    /// <param name="bigEndian"> Whether NBT data should be in Big-Endian encoding. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="stream" /> or <paramref name="rootTagName" /> is <c>null</c>. </exception>
    /// <exception cref="ArgumentException"> <paramref name="stream" /> is not writable. </exception>
    public NbtWriter([NotNull] Stream stream, [NotNull] string rootTagName, bool bigEndian)
    {
        ArgumentNullException.ThrowIfNull(rootTagName);
        _writer = new NbtBinaryWriter(stream, bigEndian);
        _writer.Write((byte)NbtTagType.Compound);
        _writer.Write(rootTagName);
        _parentType = NbtTagType.Compound;
    }


    /// <summary>
    ///     Gets whether the root tag has been closed.
    ///     No more tags may be written after the root tag has been closed.
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary> Gets the underlying stream of the NbtWriter. </summary>
    [NotNull]
    public Stream BaseStream => _writer.BaseStream;


    /// <summary>
    ///     Writes a NbtTag object, and all of its child tags, to stream.
    ///     Use this method sparingly with NbtWriter -- constructing NbtTag objects defeats the purpose of this class.
    ///     If you already have lots of NbtTag objects, you might as well use NbtFile to write them all at once.
    /// </summary>
    /// <param name="tag"> Tag to write. Must not be null. </param>
    /// <exception cref="NbtFormatException"> No more tags can be written -OR- given tag is unacceptable at this time. </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="tag" /> is null </exception>
    public void WriteTag([NotNull] NbtTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        EnforceConstraints(tag.Name, tag.TagType);
        if (tag.Name != null)
            tag.WriteTag(_writer);
        else
            tag.WriteData(_writer);
    }


    /// <summary>
    ///     Ensures that file has been written in its entirety, with no tags left open.
    ///     This method is for verification only, and does not actually write any data.
    ///     Calling this method is optional (but probably a good idea, to catch any usage errors).
    /// </summary>
    /// <exception cref="NbtFormatException"> Not all tags have been closed yet. </exception>
    public void Finish()
    {
        if (!IsDone) throw new NbtFormatException("Cannot finish: not all tags have been closed yet.");
    }

    private void GoDown(NbtTagType thisType)
    {
        _nodes ??= new Stack<NbtWriterNode>();
        var newNode = new NbtWriterNode
        {
            ParentType = _parentType,
            ListType = _listType,
            ListSize = _listSize,
            ListIndex = _listIndex
        };
        _nodes.Push(newNode);

        _parentType = thisType;
        _listType = NbtTagType.Unknown;
        _listSize = 0;
        _listIndex = 0;
    }

    private void GoUp()
    {
        if (_nodes == null || _nodes.Count == 0)
        {
            IsDone = true;
        }
        else
        {
            var oldNode = _nodes.Pop();
            _parentType = oldNode.ParentType;
            _listType = oldNode.ListType;
            _listSize = oldNode.ListSize;
            _listIndex = oldNode.ListIndex;
        }
    }

    private void EnforceConstraints([CanBeNull] string name, NbtTagType desiredType)
    {
        if (IsDone) throw new NbtFormatException("Cannot write any more tags: root tag has been closed.");
        if (_parentType == NbtTagType.List)
        {
            if (name != null)
                throw new NbtFormatException("Expecting an unnamed tag.");
            if (_listType != desiredType)
                throw new NbtFormatException("Unexpected tag type (expected: " + _listType + ", given: " +
                                             desiredType);
            if (_listIndex >= _listSize) throw new NbtFormatException("Given list size exceeded.");
            _listIndex++;
        }
        else if (name == null)
        {
            throw new NbtFormatException("Expecting a named tag.");
        }
    }

    private static void CheckArray([NotNull] Array data, int offset, int count)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "offset may not be negative.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count may not be negative.");
        if (data.Length - offset < count)
            throw new ArgumentException("count may not be greater than offset subtracted from the array length.");
    }

    private void WriteByteArrayFromStreamImpl([NotNull] Stream dataSource, int count, [NotNull] byte[] buffer)
    {
        Debug.Assert(dataSource != null);
        Debug.Assert(buffer != null);
        _writer.Write(count);
        var maxBytesToWrite = Math.Min(buffer.Length, NbtBinaryWriter.MaxWriteChunk);
        var bytesWritten = 0;
        while (bytesWritten < count)
        {
            var bytesToRead = Math.Min(count - bytesWritten, maxBytesToWrite);
            var bytesRead = dataSource.Read(buffer, 0, bytesToRead);
            _writer.Write(buffer, 0, bytesRead);
            bytesWritten += bytesRead;
        }
    }


    #region Compounds and Lists

    /// <summary> Begins an unnamed compound tag. </summary>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named compound tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void BeginCompound()
    {
        EnforceConstraints(null, NbtTagType.Compound);
        GoDown(NbtTagType.Compound);
    }


    /// <summary> Begins a named compound tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed compound tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void BeginCompound([NotNull] string tagName)
    {
        EnforceConstraints(tagName, NbtTagType.Compound);
        GoDown(NbtTagType.Compound);

        _writer.Write((byte)NbtTagType.Compound);
        _writer.Write(tagName);
    }


    /// <summary> Ends a compound tag. </summary>
    /// <exception cref="NbtFormatException"> Not currently in a compound. </exception>
    public void EndCompound()
    {
        if (IsDone || _parentType != NbtTagType.Compound) throw new NbtFormatException("Not currently in a compound.");
        GoUp();
        _writer.Write(NbtTagType.End);
    }


    /// <summary> Begins an unnamed list tag. </summary>
    /// <param name="elementType"> Type of elements of this list. </param>
    /// <param name="size"> Number of elements in this list. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named list tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="size" /> is negative -OR-
    ///     <paramref name="elementType" /> is not a valid NbtTagType.
    /// </exception>
    public void BeginList(NbtTagType elementType, int size)
    {
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "List size may not be negative.");
        if (elementType is < NbtTagType.Byte or > NbtTagType.LongArray)
            throw new ArgumentOutOfRangeException(nameof(elementType));
        EnforceConstraints(null, NbtTagType.List);
        GoDown(NbtTagType.List);
        _listType = elementType;
        _listSize = size;

        _writer.Write((byte)elementType);
        _writer.Write(size);
    }


    /// <summary> Begins an unnamed list tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="elementType"> Type of elements of this list. </param>
    /// <param name="size"> Number of elements in this list. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed list tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="size" /> is negative -OR-
    ///     <paramref name="elementType" /> is not a valid NbtTagType.
    /// </exception>
    public void BeginList([NotNull] string tagName, NbtTagType elementType, int size)
    {
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "List size may not be negative.");
        if (elementType is < NbtTagType.Byte or > NbtTagType.LongArray)
            throw new ArgumentOutOfRangeException(nameof(elementType));
        EnforceConstraints(tagName, NbtTagType.List);
        GoDown(NbtTagType.List);
        _listType = elementType;
        _listSize = size;

        _writer.Write((byte)NbtTagType.List);
        _writer.Write(tagName);
        _writer.Write((byte)elementType);
        _writer.Write(size);
    }


    /// <summary> Ends a list tag. </summary>
    /// <exception cref="NbtFormatException">
    ///     Not currently in a list -OR-
    ///     not all list elements have been written yet.
    /// </exception>
    public void EndList()
    {
        if (_parentType != NbtTagType.List || IsDone)
            throw new NbtFormatException("Not currently in a list.");
        if (_listIndex < _listSize)
            throw new NbtFormatException("Cannot end list: not all list elements have been written yet. " +
                                         "Expected: " + _listSize + ", written: " + _listIndex);
        GoUp();
    }

    #endregion


    #region Value Tags

    /// <summary> Writes an unnamed byte tag. </summary>
    /// <param name="value"> The unsigned byte to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named byte tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void WriteByte(byte value)
    {
        EnforceConstraints(null, NbtTagType.Byte);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed byte tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="value"> The unsigned byte to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed byte tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void WriteByte([NotNull] string tagName, byte value)
    {
        EnforceConstraints(tagName, NbtTagType.Byte);
        _writer.Write((byte)NbtTagType.Byte);
        _writer.Write(tagName);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed double tag. </summary>
    /// <param name="value"> The eight-byte floating-point value to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named double tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void WriteDouble(double value)
    {
        EnforceConstraints(null, NbtTagType.Double);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed byte tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="value"> The unsigned byte to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed byte tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void WriteDouble([NotNull] string tagName, double value)
    {
        EnforceConstraints(tagName, NbtTagType.Double);
        _writer.Write((byte)NbtTagType.Double);
        _writer.Write(tagName);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed float tag. </summary>
    /// <param name="value"> The four-byte floating-point value to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named float tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void WriteFloat(float value)
    {
        EnforceConstraints(null, NbtTagType.Float);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed float tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="value"> The four-byte floating-point value to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed float tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void WriteFloat([NotNull] string tagName, float value)
    {
        EnforceConstraints(tagName, NbtTagType.Float);
        _writer.Write((byte)NbtTagType.Float);
        _writer.Write(tagName);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed int tag. </summary>
    /// <param name="value"> The four-byte signed integer to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named int tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void WriteInt(int value)
    {
        EnforceConstraints(null, NbtTagType.Int);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed int tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="value"> The four-byte signed integer to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed int tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void WriteInt([NotNull] string tagName, int value)
    {
        EnforceConstraints(tagName, NbtTagType.Int);
        _writer.Write((byte)NbtTagType.Int);
        _writer.Write(tagName);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed long tag. </summary>
    /// <param name="value"> The eight-byte signed integer to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named long tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void WriteLong(long value)
    {
        EnforceConstraints(null, NbtTagType.Long);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed long tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="value"> The eight-byte signed integer to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed long tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void WriteLong([NotNull] string tagName, long value)
    {
        EnforceConstraints(tagName, NbtTagType.Long);
        _writer.Write((byte)NbtTagType.Long);
        _writer.Write(tagName);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed short tag. </summary>
    /// <param name="value"> The two-byte signed integer to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named short tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void WriteShort(short value)
    {
        EnforceConstraints(null, NbtTagType.Short);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed short tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="value"> The two-byte signed integer to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed short tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void WriteShort([NotNull] string tagName, short value)
    {
        EnforceConstraints(tagName, NbtTagType.Short);
        _writer.Write((byte)NbtTagType.Short);
        _writer.Write(tagName);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed string tag. </summary>
    /// <param name="value"> The string to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named string tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    public void WriteString([NotNull] string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnforceConstraints(null, NbtTagType.String);
        _writer.Write(value);
    }


    /// <summary> Writes an unnamed string tag. </summary>
    /// <param name="tagName"> Name to give to this compound tag. May not be null. </param>
    /// <param name="value"> The string to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed string tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    public void WriteString([NotNull] string tagName, [NotNull] string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnforceConstraints(tagName, NbtTagType.String);
        _writer.Write((byte)NbtTagType.String);
        _writer.Write(tagName);
        _writer.Write(value);
    }

    #endregion


    #region ByteArray, IntArray and LongArray

    /// <summary> Writes an unnamed byte array tag, copying data from an array. </summary>
    /// <param name="data"> A byte array containing the data to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named byte array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="data" /> is null </exception>
    public void WriteByteArray([NotNull] byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteByteArray(data, 0, data.Length);
    }


    /// <summary> Writes an unnamed byte array tag, copying data from an array. </summary>
    /// <param name="data"> A byte array containing the data to write. </param>
    /// <param name="offset"> The starting point in <paramref name="data" /> at which to begin writing. Must not be negative. </param>
    /// <param name="count"> The number of bytes to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named byte array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or
    ///     <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="data" /> is null </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="count" /> is greater than
    ///     <paramref name="offset" /> subtracted from the array length.
    /// </exception>
    public void WriteByteArray([NotNull] byte[] data, int offset, int count)
    {
        CheckArray(data, offset, count);
        EnforceConstraints(null, NbtTagType.ByteArray);
        _writer.Write(count);
        _writer.Write(data, offset, count);
    }


    /// <summary> Writes a named byte array tag, copying data from an array. </summary>
    /// <param name="tagName"> Name to give to this byte array tag. May not be null. </param>
    /// <param name="data"> A byte array containing the data to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed byte array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tagName" /> or
    ///     <paramref name="data" /> is null
    /// </exception>
    public void WriteByteArray([NotNull] string tagName, [NotNull] byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteByteArray(tagName, data, 0, data.Length);
    }


    /// <summary> Writes a named byte array tag, copying data from an array. </summary>
    /// <param name="tagName"> Name to give to this byte array tag. May not be null. </param>
    /// <param name="data"> A byte array containing the data to write. </param>
    /// <param name="offset"> The starting point in <paramref name="data" /> at which to begin writing. Must not be negative. </param>
    /// <param name="count"> The number of bytes to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed byte array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or
    ///     <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tagName" /> or
    ///     <paramref name="data" /> is null
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="count" /> is greater than
    ///     <paramref name="offset" /> subtracted from the array length.
    /// </exception>
    public void WriteByteArray([NotNull] string tagName, [NotNull] byte[] data, int offset, int count)
    {
        CheckArray(data, offset, count);
        EnforceConstraints(tagName, NbtTagType.ByteArray);
        _writer.Write((byte)NbtTagType.ByteArray);
        _writer.Write(tagName);
        _writer.Write(count);
        _writer.Write(data, offset, count);
    }


    /// <summary> Writes an unnamed byte array tag, copying data from a stream. </summary>
    /// <remarks>
    ///     A temporary buffer will be allocated, of size up to 8192 bytes.
    ///     To manually specify a buffer, use one of the other WriteByteArray() overloads.
    /// </remarks>
    /// <param name="dataSource"> A Stream from which data will be copied. </param>
    /// <param name="count"> The number of bytes to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named byte array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="count" /> is negative. </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="dataSource" /> is null. </exception>
    /// <exception cref="ArgumentException"> Given stream does not support reading. </exception>
    public void WriteByteArray([NotNull] Stream dataSource, int count)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        if (!dataSource.CanRead)
            throw new ArgumentException("Given stream does not support reading.", nameof(dataSource));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count may not be negative");
        var bufferSize = Math.Min(count, MaxStreamCopyBufferSize);
        var streamCopyBuffer = new byte[bufferSize];
        WriteByteArray(dataSource, count, streamCopyBuffer);
    }


    /// <summary> Writes an unnamed byte array tag, copying data from a stream. </summary>
    /// <param name="dataSource"> A Stream from which data will be copied. </param>
    /// <param name="count"> The number of bytes to write. Must not be negative. </param>
    /// <param name="buffer"> Buffer to use for copying. Size must be greater than 0. Must not be null. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named byte array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="count" /> is negative. </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="dataSource" /> is null. </exception>
    /// <exception cref="ArgumentException">
    ///     Given stream does not support reading -OR-
    ///     <paramref name="buffer" /> size is 0.
    /// </exception>
    public void WriteByteArray([NotNull] Stream dataSource, int count, [NotNull] byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(buffer);
        if (!dataSource.CanRead)
            throw new ArgumentException("Given stream does not support reading.", nameof(dataSource));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count may not be negative");
        if (buffer.Length == 0 && count > 0)
            throw new ArgumentException("buffer size must be greater than 0 when count is greater than 0",
                nameof(buffer));
        EnforceConstraints(null, NbtTagType.ByteArray);
        WriteByteArrayFromStreamImpl(dataSource, count, buffer);
    }


    /// <summary> Writes a named byte array tag, copying data from a stream. </summary>
    /// <remarks>
    ///     A temporary buffer will be allocated, of size up to 8192 bytes.
    ///     To manually specify a buffer, use one of the other WriteByteArray() overloads.
    /// </remarks>
    /// <param name="tagName"> Name to give to this byte array tag. May not be null. </param>
    /// <param name="dataSource"> A Stream from which data will be copied. </param>
    /// <param name="count"> The number of bytes to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed byte array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="count" /> is negative. </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="dataSource" /> is null. </exception>
    /// <exception cref="ArgumentException"> Given stream does not support reading. </exception>
    public void WriteByteArray([NotNull] string tagName, [NotNull] Stream dataSource, int count)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count may not be negative");
        var bufferSize = Math.Min(count, MaxStreamCopyBufferSize);
        var streamCopyBuffer = new byte[bufferSize];
        WriteByteArray(tagName, dataSource, count, streamCopyBuffer);
    }


    /// <summary> Writes an unnamed byte array tag, copying data from another stream. </summary>
    /// <param name="tagName"> Name to give to this byte array tag. May not be null. </param>
    /// <param name="dataSource"> A Stream from which data will be copied. </param>
    /// <param name="count"> The number of bytes to write. Must not be negative. </param>
    /// <param name="buffer"> Buffer to use for copying. Size must be greater than 0. Must not be null. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed byte array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="count" /> is negative. </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="dataSource" /> is null. </exception>
    /// <exception cref="ArgumentException">
    ///     Given stream does not support reading -OR-
    ///     <paramref name="buffer" /> size is 0.
    /// </exception>
    public void WriteByteArray([NotNull] string tagName, [NotNull] Stream dataSource, int count,
        [NotNull] byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(buffer);
        if (!dataSource.CanRead)
            throw new ArgumentException("Given stream does not support reading.", nameof(dataSource));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "count may not be negative");
        if (buffer.Length == 0 && count > 0)
            throw new ArgumentException("buffer size must be greater than 0 when count is greater than 0",
                nameof(buffer));
        EnforceConstraints(tagName, NbtTagType.ByteArray);
        _writer.Write((byte)NbtTagType.ByteArray);
        _writer.Write(tagName);
        WriteByteArrayFromStreamImpl(dataSource, count, buffer);
    }


    /// <summary> Writes an unnamed int array tag, copying data from an array. </summary>
    /// <param name="data"> An int array containing the data to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named int array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="data" /> is null </exception>
    public void WriteIntArray([NotNull] int[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteIntArray(data, 0, data.Length);
    }


    /// <summary> Writes an unnamed int array tag, copying data from an array. </summary>
    /// <param name="data"> An int array containing the data to write. </param>
    /// <param name="offset"> The starting point in <paramref name="data" /> at which to begin writing. Must not be negative. </param>
    /// <param name="count"> The number of elements to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named int array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or
    ///     <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="data" /> is null </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="count" /> is greater than
    ///     <paramref name="offset" /> subtracted from the array length.
    /// </exception>
    public void WriteIntArray([NotNull] int[] data, int offset, int count)
    {
        CheckArray(data, offset, count);
        EnforceConstraints(null, NbtTagType.IntArray);
        _writer.Write(count);
        for (var i = offset; i < count; i++) _writer.Write(data[i]);
    }


    /// <summary> Writes a named int array tag, copying data from an array. </summary>
    /// <param name="tagName"> Name to give to this int array tag. May not be null. </param>
    /// <param name="data"> An int array containing the data to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed int array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tagName" /> or
    ///     <paramref name="data" /> is null
    /// </exception>
    public void WriteIntArray([NotNull] string tagName, [NotNull] int[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteIntArray(tagName, data, 0, data.Length);
    }


    /// <summary> Writes a named int array tag, copying data from an array. </summary>
    /// <param name="tagName"> Name to give to this int array tag. May not be null. </param>
    /// <param name="data"> An int array containing the data to write. </param>
    /// <param name="offset"> The starting point in <paramref name="data" /> at which to begin writing. Must not be negative. </param>
    /// <param name="count"> The number of elements to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed int array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or
    ///     <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tagName" /> or
    ///     <paramref name="data" /> is null
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="count" /> is greater than
    ///     <paramref name="offset" /> subtracted from the array length.
    /// </exception>
    public void WriteIntArray([NotNull] string tagName, [NotNull] int[] data, int offset, int count)
    {
        CheckArray(data, offset, count);
        EnforceConstraints(tagName, NbtTagType.IntArray);
        _writer.Write((byte)NbtTagType.IntArray);
        _writer.Write(tagName);
        _writer.Write(count);
        for (var i = offset; i < count; i++) _writer.Write(data[i]);
    }

    /// <summary> Writes an unnamed long array tag, copying data from an array. </summary>
    /// <param name="data"> A long array containing the data to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named long array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="data" /> is null </exception>
    public void WriteLongArray([NotNull] long[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteLongArray(data, 0, data.Length);
    }


    /// <summary> Writes an unnamed long array tag, copying data from an array. </summary>
    /// <param name="data"> A long array containing the data to write. </param>
    /// <param name="offset"> The starting point in <paramref name="data" /> at which to begin writing. Must not be negative. </param>
    /// <param name="count"> The number of elements to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     a named long array tag was expected -OR- a tag of a different type was expected -OR-
    ///     the size of a parent list has been exceeded.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or
    ///     <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException"> <paramref name="data" /> is null </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="count" /> is greater than
    ///     <paramref name="offset" /> subtracted from the array length.
    /// </exception>
    public void WriteLongArray([NotNull] long[] data, int offset, int count)
    {
        CheckArray(data, offset, count);
        EnforceConstraints(null, NbtTagType.LongArray);
        _writer.Write(count);
        for (var i = offset; i < count; i++) _writer.Write(data[i]);
    }


    /// <summary> Writes a named long array tag, copying data from an array. </summary>
    /// <param name="tagName"> Name to give to this long array tag. May not be null. </param>
    /// <param name="data"> A long array containing the data to write. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed long array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tagName" /> or
    ///     <paramref name="data" /> is null
    /// </exception>
    public void WriteLongArray([NotNull] string tagName, [NotNull] long[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        WriteLongArray(tagName, data, 0, data.Length);
    }


    /// <summary> Writes a named long array tag, copying data from an array. </summary>
    /// <param name="tagName"> Name to give to this long array tag. May not be null. </param>
    /// <param name="data"> A long array containing the data to write. </param>
    /// <param name="offset"> The starting point in <paramref name="data" /> at which to begin writing. Must not be negative. </param>
    /// <param name="count"> The number of elements to write. Must not be negative. </param>
    /// <exception cref="NbtFormatException">
    ///     No more tags can be written -OR-
    ///     an unnamed long array tag was expected -OR- a tag of a different type was expected.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or
    ///     <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="tagName" /> or
    ///     <paramref name="data" /> is null
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="count" /> is greater than
    ///     <paramref name="offset" /> subtracted from the array length.
    /// </exception>
    public void WriteLongArray([NotNull] string tagName, [NotNull] long[] data, int offset, int count)
    {
        CheckArray(data, offset, count);
        EnforceConstraints(tagName, NbtTagType.LongArray);
        _writer.Write((byte)NbtTagType.LongArray);
        _writer.Write(tagName);
        _writer.Write(count);
        for (var i = offset; i < count; i++) _writer.Write(data[i]);
    }

    #endregion
}