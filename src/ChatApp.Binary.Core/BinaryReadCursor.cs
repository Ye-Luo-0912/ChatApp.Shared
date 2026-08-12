using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace ChatApp.Binary.Core;

/// <summary>
/// Stack-only canonical reader over contiguous input. Borrowed spans never outlive the caller's
/// source; native pointers stay inside the root <see cref="BinaryCodec"/> fixed scope.
/// </summary>
public unsafe ref struct BinaryReadCursor
{
    private readonly ReadOnlySpan<byte> _source;
    private readonly byte* _nativeStart;
    private readonly BinaryLimits _limits;
    private int _offset;
    private int _fieldCount;
    private int _lastFieldNumber;

    internal BinaryReadCursor(
        ReadOnlySpan<byte> source,
        byte* nativeStart,
        BinaryLimits limits)
    {
        _source = source;
        _nativeStart = nativeStart;
        _limits = limits;
        _offset = 0;
        _fieldCount = 0;
        _lastFieldNumber = 0;
        Status = source.Length <= limits.MaxMessageBytes
            ? BinaryStatus.Done
            : BinaryStatus.MessageTooLarge;
    }

    /// <summary>Creates a portable bounded reader without pinning the source.</summary>
    public BinaryReadCursor(
        ReadOnlySpan<byte> source,
        BinaryLimits limits)
        : this(source, null, limits)
    {
        limits.Validate(nameof(limits));
    }

    public readonly int Consumed => _offset;

    public readonly int Remaining => _source.Length - _offset;

    public readonly bool End => _offset == _source.Length;

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

    /// <summary>Returns validated borrowed UTF-8 bytes tied to the source span lifetime.</summary>
    public bool TryReadUtf8(BinaryWireType wireType, out ReadOnlySpan<byte> value)
    {
        value = default;
        if (!TryReadLengthDelimited(
                wireType,
                _limits.MaxStringBytes,
                BinaryStatus.StringTooLarge,
                out ReadOnlySpan<byte> bytes))
        {
            return false;
        }

        if (!BinaryUtf8.IsValid(bytes))
        {
            return Fail(BinaryStatus.InvalidUtf8);
        }

        value = bytes;
        return true;
    }

    /// <summary>Returns borrowed bytes tied to the source span lifetime.</summary>
    public bool TryReadBytes(BinaryWireType wireType, out ReadOnlySpan<byte> value) =>
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
            if (_offset >= _source.Length)
            {
                return Fail(BinaryStatus.Truncated);
            }

            byte current = _nativeStart == null
                ? _source[_offset++]
                : *(_nativeStart + _offset++);
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
        out ReadOnlySpan<byte> value)
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
        if (length > Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        value = _source.Slice(_offset, length);
        _offset += length;
        return true;
    }

    private bool TryReadFixed32Core(out uint value)
    {
        value = 0;
        if (sizeof(uint) > Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        if (_nativeStart != null)
        {
            byte* current = _nativeStart + _offset;
            value = current[0]
                | (uint)current[1] << 8
                | (uint)current[2] << 16
                | (uint)current[3] << 24;
        }
        else
        {
            value = BinaryPrimitives.ReadUInt32LittleEndian(_source[_offset..]);
        }

        _offset += sizeof(uint);
        return true;
    }

    private bool TryReadFixed64Core(out ulong value)
    {
        value = 0;
        if (sizeof(ulong) > Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        if (_nativeStart != null)
        {
            byte* current = _nativeStart + _offset;
            value = current[0]
                | (ulong)current[1] << 8
                | (ulong)current[2] << 16
                | (ulong)current[3] << 24
                | (ulong)current[4] << 32
                | (ulong)current[5] << 40
                | (ulong)current[6] << 48
                | (ulong)current[7] << 56;
        }
        else
        {
            value = BinaryPrimitives.ReadUInt64LittleEndian(_source[_offset..]);
        }

        _offset += sizeof(ulong);
        return true;
    }

    private bool TryAdvance(int byteCount)
    {
        if (byteCount > Remaining)
        {
            return Fail(BinaryStatus.Truncated);
        }

        _offset += byteCount;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
