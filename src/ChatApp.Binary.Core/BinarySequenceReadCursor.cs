using System.Buffers;
using System.Buffers.Binary;

namespace ChatApp.Binary.Core;

/// <summary>
/// Stack-only bounded reader over segmented input. It never coalesces the message; byte payloads
/// remain borrowed sequence slices and strings allocate only their final owning value.
/// </summary>
public ref struct BinarySequenceReadCursor
{
    private SequenceReader<byte> _reader;
    private readonly BinaryLimits _limits;
    private int _fieldCount;
    private int _lastFieldNumber;

    public BinarySequenceReadCursor(
        in ReadOnlySequence<byte> source,
        BinaryLimits limits)
    {
        limits.Validate(nameof(limits));
        _reader = new SequenceReader<byte>(source);
        _limits = limits;
        _fieldCount = 0;
        _lastFieldNumber = 0;
        Status = source.Length <= limits.MaxMessageBytes
            ? BinaryStatus.Done
            : BinaryStatus.MessageTooLarge;
    }

    public readonly long Consumed => _reader.Consumed;

    public readonly long Remaining => _reader.Remaining;

    public readonly bool End => _reader.End;

    public BinaryStatus Status { get; private set; }

    public bool TryReadFieldHeader(out int fieldNumber, out BinaryWireType wireType)
    {
        fieldNumber = 0;
        wireType = default;
        if (Status != BinaryStatus.Done || End)
        {
            return false;
        }

        if (_fieldCount >= _limits.MaxFields)
        {
            return Fail(BinaryStatus.TooManyFields);
        }

        if (!TryReadRawVarUInt(out ulong tag))
        {
            return false;
        }

        ulong rawFieldNumber = tag >> 3;
        if (rawFieldNumber is 0 or > BinaryLimits.MaximumFieldNumber)
        {
            return Fail(BinaryStatus.InvalidFieldNumber);
        }

        byte rawWireType = (byte)(tag & 0x07);
        bool supported = rawWireType is ((byte)BinaryWireType.VarInt)
            or ((byte)BinaryWireType.Fixed64)
            or ((byte)BinaryWireType.LengthDelimited)
            or ((byte)BinaryWireType.Fixed32);
        if (!supported)
        {
            return Fail(BinaryStatus.UnsupportedWireType);
        }

        fieldNumber = (int)rawFieldNumber;
        if (fieldNumber == _lastFieldNumber)
        {
            return Fail(BinaryStatus.DuplicateField);
        }

        if (fieldNumber < _lastFieldNumber)
        {
            return Fail(BinaryStatus.FieldsOutOfOrder);
        }

        _fieldCount++;
        _lastFieldNumber = fieldNumber;
        wireType = (BinaryWireType)rawWireType;
        return true;
    }

    public bool TryReadBool(BinaryWireType wireType, out bool value)
    {
        value = false;
        if (!RequireWireType(wireType, BinaryWireType.VarInt)
            || !TryReadRawVarUInt(out ulong raw))
        {
            return false;
        }

        if (raw > 1)
        {
            return Fail(BinaryStatus.ValueOutOfRange);
        }

        value = raw != 0;
        return true;
    }

    public bool TryReadInt32(BinaryWireType wireType, out int value)
    {
        value = 0;
        if (!RequireWireType(wireType, BinaryWireType.VarInt)
            || !TryReadRawVarUInt(out ulong raw))
        {
            return false;
        }

        if (raw > uint.MaxValue)
        {
            return Fail(BinaryStatus.ValueOutOfRange);
        }

        value = BinaryEncoding.ZigZagDecode((uint)raw);
        return true;
    }

    public bool TryReadUInt32(BinaryWireType wireType, out uint value)
    {
        value = 0;
        if (!RequireWireType(wireType, BinaryWireType.VarInt)
            || !TryReadRawVarUInt(out ulong raw))
        {
            return false;
        }

        if (raw > uint.MaxValue)
        {
            return Fail(BinaryStatus.ValueOutOfRange);
        }

        value = (uint)raw;
        return true;
    }

    public bool TryReadInt64(BinaryWireType wireType, out long value)
    {
        value = 0;
        if (!RequireWireType(wireType, BinaryWireType.VarInt)
            || !TryReadRawVarUInt(out ulong raw))
        {
            return false;
        }

        value = BinaryEncoding.ZigZagDecode(raw);
        return true;
    }

    public bool TryReadUInt64(BinaryWireType wireType, out ulong value)
    {
        value = 0;
        return RequireWireType(wireType, BinaryWireType.VarInt)
            && TryReadRawVarUInt(out value);
    }

    public bool TryReadSingle(BinaryWireType wireType, out float value)
    {
        value = 0;
        if (!RequireWireType(wireType, BinaryWireType.Fixed32)
            || !TryReadFixed32Core(out uint bits))
        {
            return false;
        }

        value = BitConverter.Int32BitsToSingle(unchecked((int)bits));
        return true;
    }

    public bool TryReadDouble(BinaryWireType wireType, out double value)
    {
        value = 0;
        if (!RequireWireType(wireType, BinaryWireType.Fixed64)
            || !TryReadFixed64Core(out ulong bits))
        {
            return false;
        }

        value = BitConverter.Int64BitsToDouble(unchecked((long)bits));
        return true;
    }

    public bool TryReadFixed32(BinaryWireType wireType, out uint value)
    {
        value = 0;
        return RequireWireType(wireType, BinaryWireType.Fixed32)
            && TryReadFixed32Core(out value);
    }

    public bool TryReadFixed64(BinaryWireType wireType, out ulong value)
    {
        value = 0;
        return RequireWireType(wireType, BinaryWireType.Fixed64)
            && TryReadFixed64Core(out value);
    }

    /// <summary>Returns validated borrowed UTF-8 bytes tied to the source sequence lifetime.</summary>
    public bool TryReadUtf8(
        BinaryWireType wireType,
        out ReadOnlySequence<byte> value)
    {
        value = default;
        if (!TryReadLengthDelimited(
                wireType,
                _limits.MaxStringBytes,
                BinaryStatus.StringTooLarge,
                out ReadOnlySequence<byte> bytes))
        {
            return false;
        }

        if (!BinaryUtf8.IsValid(in bytes))
        {
            return Fail(BinaryStatus.InvalidUtf8);
        }

        value = bytes;
        return true;
    }

    /// <summary>Returns a borrowed slice tied to the source sequence lifetime.</summary>
    public bool TryReadBytes(
        BinaryWireType wireType,
        out ReadOnlySequence<byte> value) =>
        TryReadLengthDelimited(
            wireType,
            _limits.MaxByteArrayBytes,
            BinaryStatus.ByteArrayTooLarge,
            out value);

    public bool TrySkip(BinaryWireType wireType) => wireType switch
    {
        BinaryWireType.VarInt => TryReadRawVarUInt(out _),
        BinaryWireType.Fixed64 => TryAdvance(sizeof(ulong)),
        BinaryWireType.LengthDelimited => TryReadLengthDelimited(
            wireType,
            _limits.MaxFieldBytes,
            BinaryStatus.FieldTooLarge,
            out _),
        BinaryWireType.Fixed32 => TryAdvance(sizeof(uint)),
        _ => Fail(BinaryStatus.UnsupportedWireType)
    };

    private bool TryReadRawVarUInt(out ulong value)
    {
        value = 0;
        if (Status != BinaryStatus.Done)
        {
            return false;
        }

        for (var index = 0; index < 10; index++)
        {
            if (!_reader.TryRead(out byte current))
            {
                return Fail(BinaryStatus.Truncated);
            }

            if (index == 9 && current > 1)
            {
                return Fail(BinaryStatus.MalformedVarInt);
            }

            value |= (ulong)(current & 0x7F) << (index * 7);
            if ((current & 0x80) == 0)
            {
                if (index > 0 && current == 0)
                {
                    return Fail(BinaryStatus.MalformedVarInt);
                }

                return true;
            }
        }

        return Fail(BinaryStatus.MalformedVarInt);
    }

    private bool TryReadLengthDelimited(
        BinaryWireType wireType,
        int typeLimit,
        BinaryStatus typeLimitStatus,
        out ReadOnlySequence<byte> value)
    {
        value = default;
        if (!RequireWireType(wireType, BinaryWireType.LengthDelimited)
            || !TryReadRawVarUInt(out ulong rawLength))
        {
            return false;
        }

        if (rawLength > int.MaxValue || rawLength > (ulong)_limits.MaxFieldBytes)
        {
            return Fail(BinaryStatus.FieldTooLarge);
        }

        if (rawLength > (ulong)typeLimit)
        {
            return Fail(typeLimitStatus);
        }

        int length = (int)rawLength;
        if (length > _reader.Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        value = _reader.UnreadSequence.Slice(0, length);
        _reader.Advance(length);
        return true;
    }

    private bool TryReadFixed32Core(out uint value)
    {
        value = 0;
        if (sizeof(uint) > _reader.Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        if (_reader.UnreadSpan.Length >= sizeof(uint))
        {
            value = BinaryPrimitives.ReadUInt32LittleEndian(_reader.UnreadSpan);
            _reader.Advance(sizeof(uint));
            return true;
        }

        for (var shift = 0; shift < 32; shift += 8)
        {
            _reader.TryRead(out byte current);
            value |= (uint)current << shift;
        }

        return true;
    }

    private bool TryReadFixed64Core(out ulong value)
    {
        value = 0;
        if (sizeof(ulong) > _reader.Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        if (_reader.UnreadSpan.Length >= sizeof(ulong))
        {
            value = BinaryPrimitives.ReadUInt64LittleEndian(_reader.UnreadSpan);
            _reader.Advance(sizeof(ulong));
            return true;
        }

        for (var shift = 0; shift < 64; shift += 8)
        {
            _reader.TryRead(out byte current);
            value |= (ulong)current << shift;
        }

        return true;
    }

    private bool TryAdvance(int byteCount)
    {
        if (byteCount > _reader.Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        _reader.Advance(byteCount);
        return true;
    }

    private bool RequireWireType(BinaryWireType actual, BinaryWireType expected) =>
        actual == expected || Fail(BinaryStatus.WireTypeMismatch);

    private bool Fail(BinaryStatus status)
    {
        if (Status == BinaryStatus.Done)
        {
            Status = status;
        }

        return false;
    }
}
