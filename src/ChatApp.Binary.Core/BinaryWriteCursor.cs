using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChatApp.Binary.Core;

/// <summary>
/// Stack-only writer over one pre-sized contiguous span. Native pointers are internal and remain
/// valid only while <see cref="BinaryCodec"/> owns the enclosing fixed scope.
/// </summary>
public unsafe ref struct BinaryWriteCursor
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private readonly Span<byte> _destination;
    private readonly byte* _nativeStart;
    private readonly BinaryLimits _limits;
    private int _offset;
    private int _fieldCount;
    private int _lastFieldNumber;
    private int _collectionFieldNumber;
    private int _collectionElementCount;
    private int _nestingDepth;

    internal BinaryWriteCursor(
        Span<byte> destination,
        byte* nativeStart,
        BinaryLimits limits)
        : this(destination, nativeStart, limits, limits.CurrentNestingDepth)
    {
    }

    private BinaryWriteCursor(
        Span<byte> destination,
        byte* nativeStart,
        BinaryLimits limits,
        int nestingDepth)
    {
        _destination = destination;
        _nativeStart = nativeStart;
        _limits = limits;
        _offset = 0;
        _fieldCount = 0;
        _lastFieldNumber = 0;
        _collectionFieldNumber = 0;
        _collectionElementCount = 0;
        _nestingDepth = nestingDepth;
        Status = BinaryStatus.Done;
    }

    /// <summary>Creates the portable path for direct callers and native/portable differential tests.</summary>
    public BinaryWriteCursor(
        Span<byte> destination,
        BinaryLimits limits)
        : this(destination, null, limits)
    {
        limits.Validate(nameof(limits));
    }

    public readonly int WrittenCount => _offset;

    public readonly int Remaining => _destination.Length - _offset;

    public BinaryStatus Status { get; private set; }

    public bool TryAddCollectionElement(int fieldNumber)
    {
        if (Status != BinaryStatus.Done)
        {
            return false;
        }

        if (_collectionFieldNumber != fieldNumber)
        {
            _collectionFieldNumber = fieldNumber;
            _collectionElementCount = 0;
        }

        if (_collectionElementCount >= _limits.MaxCollectionElements)
        {
            Fail(BinaryStatus.CollectionTooLarge);
            return false;
        }

        _collectionElementCount++;
        return true;
    }

    public BinaryStatus WriteBool(int fieldNumber, bool value) =>
        WriteVarIntField(fieldNumber, value ? 1UL : 0UL, allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedBool(int fieldNumber, bool value) =>
        WriteVarIntField(fieldNumber, value ? 1UL : 0UL, allowRepeatedFieldNumber: true);

    public BinaryStatus WriteInt32(int fieldNumber, int value) =>
        WriteVarIntField(fieldNumber, BinaryEncoding.ZigZagEncode(value), allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedInt32(int fieldNumber, int value) =>
        WriteVarIntField(fieldNumber, BinaryEncoding.ZigZagEncode(value), allowRepeatedFieldNumber: true);

    public BinaryStatus WriteUInt32(int fieldNumber, uint value) =>
        WriteVarIntField(fieldNumber, value, allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedUInt32(int fieldNumber, uint value) =>
        WriteVarIntField(fieldNumber, value, allowRepeatedFieldNumber: true);

    public BinaryStatus WriteInt64(int fieldNumber, long value) =>
        WriteVarIntField(fieldNumber, BinaryEncoding.ZigZagEncode(value), allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedInt64(int fieldNumber, long value) =>
        WriteVarIntField(fieldNumber, BinaryEncoding.ZigZagEncode(value), allowRepeatedFieldNumber: true);

    public BinaryStatus WriteUInt64(int fieldNumber, ulong value) =>
        WriteVarIntField(fieldNumber, value, allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedUInt64(int fieldNumber, ulong value) =>
        WriteVarIntField(fieldNumber, value, allowRepeatedFieldNumber: true);

    public BinaryStatus WriteSingle(int fieldNumber, float value) =>
        WriteFixed32Core(fieldNumber, unchecked((uint)BitConverter.SingleToInt32Bits(value)), allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedSingle(int fieldNumber, float value) =>
        WriteFixed32Core(fieldNumber, unchecked((uint)BitConverter.SingleToInt32Bits(value)), allowRepeatedFieldNumber: true);

    public BinaryStatus WriteDouble(int fieldNumber, double value) =>
        WriteFixed64Core(fieldNumber, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)), allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedDouble(int fieldNumber, double value) =>
        WriteFixed64Core(fieldNumber, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)), allowRepeatedFieldNumber: true);

    public BinaryStatus WriteString(int fieldNumber, string value)
        => WriteStringCore(fieldNumber, value, allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedString(int fieldNumber, string value) =>
        WriteStringCore(fieldNumber, value, allowRepeatedFieldNumber: true);

    private BinaryStatus WriteStringCore(int fieldNumber, string value, bool allowRepeatedFieldNumber)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Status != BinaryStatus.Done)
        {
            return Status;
        }

        // UTF-8 is never shorter than its UTF-16 source in code units. Reject
        // an obviously over-budget value before the strict encoder scans it.
        if (value.Length > _limits.MaxStringBytes)
        {
            return Fail(BinaryStatus.StringTooLarge);
        }

        int byteCount;
        try
        {
            byteCount = StrictUtf8.GetByteCount(value);
        }
        catch (EncoderFallbackException)
        {
            return Fail(BinaryStatus.InvalidUtf8);
        }

        if (byteCount > _limits.MaxStringBytes)
        {
            return Fail(BinaryStatus.StringTooLarge);
        }

        BinaryStatus status = BeginLengthDelimited(fieldNumber, byteCount, allowRepeatedFieldNumber);
        if (status != BinaryStatus.Done)
        {
            return status;
        }

        Span<byte> output = _destination.Slice(_offset, byteCount);
        try
        {
            int encoded = StrictUtf8.GetBytes(value.AsSpan(), output);
            _offset += encoded;
        }
        catch (EncoderFallbackException)
        {
            return Fail(BinaryStatus.InvalidUtf8);
        }

        return BinaryStatus.Done;
    }

    public BinaryStatus WriteBytes(int fieldNumber, ReadOnlySpan<byte> value)
        => WriteBytesCore(fieldNumber, value, allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedBytes(int fieldNumber, ReadOnlySpan<byte> value) =>
        WriteBytesCore(fieldNumber, value, allowRepeatedFieldNumber: true);

    private BinaryStatus WriteBytesCore(int fieldNumber, ReadOnlySpan<byte> value, bool allowRepeatedFieldNumber)
    {
        if (value.Length > _limits.MaxByteArrayBytes)
        {
            return Fail(BinaryStatus.ByteArrayTooLarge);
        }

        BinaryStatus status = BeginLengthDelimited(fieldNumber, value.Length, allowRepeatedFieldNumber);
        if (status != BinaryStatus.Done)
        {
            return status;
        }

        value.CopyTo(_destination[_offset..]);
        _offset += value.Length;
        return BinaryStatus.Done;
    }

    /// <summary>
    /// Encodes one nested length-delimited message in-place. The child is written once into a
    /// reserved length prefix and the prefix is compacted after the child finishes.
    /// </summary>
    public BinaryStatus WriteNested<TEncoder, T>(
        int fieldNumber,
        in T value,
        bool allowRepeatedFieldNumber = false)
        where TEncoder : IBinaryEncoder<TEncoder, T>
    {
        if (Status != BinaryStatus.Done)
        {
            return Status;
        }

        if (_nestingDepth >= _limits.MaxNestingDepth)
        {
            return Fail(BinaryStatus.NestingTooDeep);
        }

        if (fieldNumber is <= 0 or > BinaryLimits.MaximumFieldNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldNumber));
        }

        if (_fieldCount >= _limits.MaxFields)
        {
            return Fail(BinaryStatus.TooManyFields);
        }

        if (fieldNumber == _lastFieldNumber && !allowRepeatedFieldNumber)
        {
            return Fail(BinaryStatus.DuplicateField);
        }

        if (fieldNumber < _lastFieldNumber)
        {
            return Fail(BinaryStatus.FieldsOutOfOrder);
        }

        ulong tag = BinaryEncoding.CreateTag(fieldNumber, BinaryWireType.LengthDelimited);
        int tagBytes = BinaryEncoding.VarIntSize(tag);
        const int ReservedLengthBytes = 5;
        if (Remaining < tagBytes + ReservedLengthBytes)
        {
            return Fail(BinaryStatus.DestinationTooSmall);
        }

        if (tagBytes + ReservedLengthBytes > _limits.MaxMessageBytes - _offset)
        {
            return Fail(BinaryStatus.MessageTooLarge);
        }

        _fieldCount++;
        _lastFieldNumber = fieldNumber;
        WriteVarInt(tag);
        int lengthOffset = _offset;
        _offset += ReservedLengthBytes;
        int childOffset = _offset;

        var child = new BinaryWriteCursor(
            _destination[childOffset..],
            _nativeStart == null ? null : _nativeStart + childOffset,
            _limits.ForNestedMessage(),
            _nestingDepth + 1);
        BinaryStatus status = TEncoder.Write(ref child, in value);
        if (status != BinaryStatus.Done || child.Status != BinaryStatus.Done)
        {
            return Fail(status != BinaryStatus.Done ? status : child.Status);
        }

        int childBytes = child.WrittenCount;
        if (childBytes > _limits.MaxFieldBytes)
        {
            return Fail(BinaryStatus.FieldTooLarge);
        }

        int lengthBytes = BinaryEncoding.VarIntSize((ulong)childBytes);
        int compactBytes = ReservedLengthBytes - lengthBytes;
        Span<byte> destination = _destination;
        if (compactBytes > 0)
        {
            destination.Slice(childOffset, childBytes)
                .CopyTo(destination.Slice(lengthOffset + lengthBytes, childBytes));
        }

        WriteVarIntAt(destination.Slice(lengthOffset, lengthBytes), (ulong)childBytes);
        _offset = checked(childOffset + childBytes - compactBytes);
        return BinaryStatus.Done;
    }

    private BinaryStatus WriteVarIntField(int fieldNumber, ulong value, bool allowRepeatedFieldNumber)
    {
        BinaryStatus status = BeginField(
            fieldNumber,
            BinaryWireType.VarInt,
            BinaryEncoding.VarIntSize(value),
            allowRepeatedFieldNumber);
        if (status != BinaryStatus.Done)
        {
            return status;
        }

        WriteVarInt(value);
        return BinaryStatus.Done;
    }

    public BinaryStatus WriteFixed32(int fieldNumber, uint value) =>
        WriteFixed32Core(fieldNumber, value, allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedFixed32(int fieldNumber, uint value) =>
        WriteFixed32Core(fieldNumber, value, allowRepeatedFieldNumber: true);

    private BinaryStatus WriteFixed32Core(int fieldNumber, uint value, bool allowRepeatedFieldNumber)
    {
        BinaryStatus status = BeginField(fieldNumber, BinaryWireType.Fixed32, sizeof(uint), allowRepeatedFieldNumber);
        if (status != BinaryStatus.Done)
        {
            return status;
        }

        if (_nativeStart != null)
        {
            byte* current = _nativeStart + _offset;
            current[0] = (byte)value;
            current[1] = (byte)(value >> 8);
            current[2] = (byte)(value >> 16);
            current[3] = (byte)(value >> 24);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_destination[_offset..], value);
        }

        _offset += sizeof(uint);
        return BinaryStatus.Done;
    }

    public BinaryStatus WriteFixed64(int fieldNumber, ulong value) =>
        WriteFixed64Core(fieldNumber, value, allowRepeatedFieldNumber: false);

    public BinaryStatus WriteRepeatedFixed64(int fieldNumber, ulong value) =>
        WriteFixed64Core(fieldNumber, value, allowRepeatedFieldNumber: true);

    private BinaryStatus WriteFixed64Core(int fieldNumber, ulong value, bool allowRepeatedFieldNumber)
    {
        BinaryStatus status = BeginField(fieldNumber, BinaryWireType.Fixed64, sizeof(ulong), allowRepeatedFieldNumber);
        if (status != BinaryStatus.Done)
        {
            return status;
        }

        if (_nativeStart != null)
        {
            byte* current = _nativeStart + _offset;
            current[0] = (byte)value;
            current[1] = (byte)(value >> 8);
            current[2] = (byte)(value >> 16);
            current[3] = (byte)(value >> 24);
            current[4] = (byte)(value >> 32);
            current[5] = (byte)(value >> 40);
            current[6] = (byte)(value >> 48);
            current[7] = (byte)(value >> 56);
        }
        else
        {
            BinaryPrimitives.WriteUInt64LittleEndian(_destination[_offset..], value);
        }

        _offset += sizeof(ulong);
        return BinaryStatus.Done;
    }

    private BinaryStatus BeginLengthDelimited(
        int fieldNumber,
        int byteCount,
        bool allowRepeatedFieldNumber = false)
    {
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount));
        }

        if (byteCount > _limits.MaxFieldBytes)
        {
            return Fail(BinaryStatus.FieldTooLarge);
        }

        int lengthSize = BinaryEncoding.VarIntSize((ulong)byteCount);
        int payloadSize;
        try
        {
            payloadSize = checked(lengthSize + byteCount);
        }
        catch (OverflowException)
        {
            return Fail(BinaryStatus.ArithmeticOverflow);
        }

        BinaryStatus status = BeginField(
            fieldNumber,
            BinaryWireType.LengthDelimited,
            payloadSize,
            allowRepeatedFieldNumber);
        if (status != BinaryStatus.Done)
        {
            return status;
        }

        WriteVarInt((ulong)byteCount);
        return BinaryStatus.Done;
    }

    private BinaryStatus BeginField(
        int fieldNumber,
        BinaryWireType wireType,
        int payloadSize,
        bool allowRepeatedFieldNumber = false)
    {
        if (Status != BinaryStatus.Done)
        {
            return Status;
        }

        if (fieldNumber is <= 0 or > BinaryLimits.MaximumFieldNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldNumber));
        }

        if (_fieldCount >= _limits.MaxFields)
        {
            return Fail(BinaryStatus.TooManyFields);
        }

        if (fieldNumber == _lastFieldNumber && !allowRepeatedFieldNumber)
        {
            return Fail(BinaryStatus.DuplicateField);
        }

        if (fieldNumber < _lastFieldNumber)
        {
            return Fail(BinaryStatus.FieldsOutOfOrder);
        }

        ulong tag = BinaryEncoding.CreateTag(fieldNumber, wireType);
        int required;
        try
        {
            required = checked(BinaryEncoding.VarIntSize(tag) + payloadSize);
        }
        catch (OverflowException)
        {
            return Fail(BinaryStatus.ArithmeticOverflow);
        }

        if (required > Remaining)
        {
            return Fail(BinaryStatus.DestinationTooSmall);
        }

        if (required > _limits.MaxMessageBytes - _offset)
        {
            return Fail(BinaryStatus.MessageTooLarge);
        }

        _fieldCount++;
        _lastFieldNumber = fieldNumber;
        WriteVarInt(tag);
        return BinaryStatus.Done;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteVarInt(ulong value)
    {
        if (_nativeStart != null)
        {
            byte* current = _nativeStart + _offset;
            while (value >= 0x80)
            {
                *current++ = (byte)(value | 0x80);
                value >>= 7;
            }

            *current++ = (byte)value;
            _offset = checked((int)(current - _nativeStart));
            return;
        }

        while (value >= 0x80)
        {
            _destination[_offset++] = (byte)(value | 0x80);
            value >>= 7;
        }

        _destination[_offset++] = (byte)value;
    }

    private BinaryStatus Fail(BinaryStatus status)
    {
        if (Status == BinaryStatus.Done)
        {
            Status = status;
        }

        return Status;
    }

    private static void WriteVarIntAt(Span<byte> destination, ulong value)
    {
        int offset = 0;
        while (value >= 0x80)
        {
            destination[offset++] = (byte)(value | 0x80);
            value >>= 7;
        }

        destination[offset] = (byte)value;
    }
}
