﻿using System;
using System.Text;
using JetBrains.Annotations;

namespace fNbt.Tags;

/// <summary> A tag containing an array of signed 32-bit integers. </summary>
public sealed class NbtIntArray : NbtTag
{
    [NotNull] private int[] _ints;


    /// <summary> Creates an unnamed NbtIntArray tag, containing an empty array of ints. </summary>
    public NbtIntArray()
        : this((string)null)
    {
    }


    /// <summary> Creates an unnamed NbtIntArray tag, containing the given array of ints. </summary>
    /// <param name="value"> Int array to assign to this tag's Value. May not be <c>null</c>. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="value" /> is <c>null</c>. </exception>
    /// <remarks>
    ///     Given int array will be cloned. To avoid unnecessary copying, call one of the other constructor
    ///     overloads (that do not take a int[]) and then set the Value property yourself.
    /// </remarks>
    public NbtIntArray([NotNull] int[] value)
        : this(null, value)
    {
    }


    /// <summary> Creates an NbtIntArray tag with the given name, containing an empty array of ints. </summary>
    /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
    public NbtIntArray([CanBeNull] string tagName)
    {
        name = tagName;
        _ints = [];
    }


    /// <summary> Creates an NbtIntArray tag with the given name, containing the given array of ints. </summary>
    /// <param name="tagName"> Name to assign to this tag. May be <c>null</c>. </param>
    /// <param name="value"> Int array to assign to this tag's Value. May not be <c>null</c>. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="value" /> is <c>null</c>. </exception>
    /// <remarks>
    ///     Given int array will be cloned. To avoid unnecessary copying, call one of the other constructor
    ///     overloads (that do not take a int[]) and then set the Value property yourself.
    /// </remarks>
    public NbtIntArray([CanBeNull] string tagName, [NotNull] int[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        name = tagName;
        _ints = (int[])value.Clone();
    }


    /// <summary> Creates a deep copy of given NbtIntArray. </summary>
    /// <param name="other"> Tag to copy. May not be <c>null</c>. </param>
    /// <exception cref="ArgumentNullException"> <paramref name="other" /> is <c>null</c>. </exception>
    /// <remarks> Int array of given tag will be cloned. </remarks>
    public NbtIntArray([NotNull] NbtIntArray other)
    {
        ArgumentNullException.ThrowIfNull(other);
        name = other.name;
        _ints = (int[])other.Value.Clone();
    }

    /// <summary> Type of this tag (ByteArray). </summary>
    public override NbtTagType TagType => NbtTagType.IntArray;

    /// <summary>
    ///     Value/payload of this tag (an array of signed 32-bit integers). Value is stored as-is and is NOT cloned. May
    ///     not be <c>null</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException"> <paramref name="value" /> is <c>null</c>. </exception>
    [NotNull]
    public int[] Value
    {
        get => _ints;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _ints = value;
        }
    }


    /// <summary> Gets or sets an integer at the given index. </summary>
    /// <param name="tagIndex"> The zero-based index of the element to get or set. </param>
    /// <returns> The integer at the specified index. </returns>
    /// <exception cref="IndexOutOfRangeException"> <paramref name="tagIndex" /> is outside the array bounds. </exception>
    public new int this[int tagIndex]
    {
        get => Value[tagIndex];
        set => Value[tagIndex] = value;
    }


    internal override bool ReadTag(NbtBinaryReader readStream)
    {
        var length = readStream.ReadInt32();
        if (length < 0) throw new NbtFormatException("Negative length given in TAG_Int_Array");

        if (readStream.Selector != null && !readStream.Selector(this))
        {
            readStream.Skip(length * sizeof(int));
            return false;
        }

        Value = new int[length];
        for (var i = 0; i < length; i++) Value[i] = readStream.ReadInt32();
        return true;
    }


    internal override void SkipTag(NbtBinaryReader readStream)
    {
        var length = readStream.ReadInt32();
        if (length < 0) throw new NbtFormatException("Negative length given in TAG_Int_Array");
        readStream.Skip(length * sizeof(int));
    }


    internal override void WriteTag(NbtBinaryWriter writeStream)
    {
        writeStream.Write(NbtTagType.IntArray);
        if (Name == null) throw new NbtFormatException("Name is null");
        writeStream.Write(Name);
        WriteData(writeStream);
    }


    internal override void WriteData(NbtBinaryWriter writeStream)
    {
        writeStream.Write(Value.Length);
        foreach (var data in Value) writeStream.Write(data);
    }


    /// <inheritdoc />
    public override object Clone()
    {
        return new NbtIntArray(this);
    }


    internal override void PrettyPrint(StringBuilder sb, string indentString, int indentLevel)
    {
        for (var i = 0; i < indentLevel; i++) sb.Append(indentString);
        sb.Append("TAG_Int_Array");
        if (!string.IsNullOrEmpty(Name)) sb.AppendFormat("(\"{0}\")", Name);
        sb.AppendFormat(": [{0} ints]", _ints.Length);
    }
}