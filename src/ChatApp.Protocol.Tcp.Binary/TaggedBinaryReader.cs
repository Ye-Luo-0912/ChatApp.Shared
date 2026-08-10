using System.Buffers;
using System.Text;

namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>Bounded, allocation-conscious reader over segmented sequences.</summary>
public ref struct TaggedBinaryReader
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private SequenceReader<byte> _reader;
    private readonly TcpBinaryLimits _limits;
    private int _fieldCount;

    public TaggedBinaryReader(in ReadOnlySequence<byte> payload, TcpBinaryLimits limits)
    {
        _reader = new SequenceReader<byte>(payload);
        _limits = limits;
        _fieldCount = 0;
        Error = payload.Length > limits.MaxMessageBytes
            ? TcpBinaryDecodeError.MessageTooLarge
            : TcpBinaryDecodeError.None;
    }

    public TcpBinaryDecodeError Error { get; private set; }

    public readonly bool End => _reader.End;

    public bool TryReadFieldHeader(out int fieldNumber, out TaggedWireType wireType)
    {
        fieldNumber = 0;
        wireType = default;
        if (Error != TcpBinaryDecodeError.None || _reader.End)
        {
            return false;
        }

        if (++_fieldCount > _limits.MaxFields)
        {
            return Fail(TcpBinaryDecodeError.TooManyFields);
        }

        if (!TryReadVarIntCore(out var tag))
        {
            return false;
        }

        var rawFieldNumber = tag >> 3;
        if (rawFieldNumber is 0 or > TcpBinaryLimits.MaximumFieldNumber)
        {
            return Fail(TcpBinaryDecodeError.InvalidFieldNumber);
        }

        var rawWireType = (byte)(tag & 0x07);
        if (rawWireType is not ((byte)TaggedWireType.VarInt)
            and not ((byte)TaggedWireType.Fixed64)
            and not ((byte)TaggedWireType.LengthDelimited)
            and not ((byte)TaggedWireType.Fixed32))
        {
            return Fail(TcpBinaryDecodeError.UnsupportedWireType);
        }

        fieldNumber = (int)rawFieldNumber;
        wireType = (TaggedWireType)rawWireType;
        return true;
    }

    public bool TryReadBool(TaggedWireType wireType, out bool value)
    {
        value = false;
        if (!RequireWireType(wireType, TaggedWireType.VarInt)
            || !TryReadVarIntCore(out var raw))
        {
            return false;
        }

        if (raw > 1)
        {
            return Fail(TcpBinaryDecodeError.ValueOutOfRange);
        }

        value = raw != 0;
        return true;
    }

    public bool TryReadInt32(TaggedWireType wireType, out int value)
    {
        value = 0;
        if (!RequireWireType(wireType, TaggedWireType.VarInt)
            || !TryReadVarIntCore(out var raw))
        {
            return false;
        }

        if (raw > uint.MaxValue)
        {
            return Fail(TcpBinaryDecodeError.ValueOutOfRange);
        }

        value = ZigZagDecode((uint)raw);
        return true;
    }

    public bool TryReadUInt32(TaggedWireType wireType, out uint value)
    {
        value = default;
        if (!RequireWireType(wireType, TaggedWireType.VarInt)
            || !TryReadVarIntCore(out var raw))
        {
            return false;
        }

        if (raw > uint.MaxValue)
        {
            return Fail(TcpBinaryDecodeError.ValueOutOfRange);
        }

        value = (uint)raw;
        return true;
    }

    public bool TryReadInt64(TaggedWireType wireType, out long value)
    {
        value = default;
        if (!RequireWireType(wireType, TaggedWireType.VarInt)
            || !TryReadVarIntCore(out var raw))
        {
            return false;
        }

        value = ZigZagDecode(raw);
        return true;
    }

    public bool TryReadUInt64(TaggedWireType wireType, out ulong value)
    {
        value = default;
        return RequireWireType(wireType, TaggedWireType.VarInt)
               && TryReadVarIntCore(out value);
    }

    public bool TryReadSingle(TaggedWireType wireType, out float value)
    {
        value = default;
        if (!RequireWireType(wireType, TaggedWireType.Fixed32)
            || !TryReadFixed32(out var raw))
        {
            return false;
        }

        value = BitConverter.Int32BitsToSingle(unchecked((int)raw));
        return true;
    }

    public bool TryReadDouble(TaggedWireType wireType, out double value)
    {
        value = default;
        if (!RequireWireType(wireType, TaggedWireType.Fixed64)
            || !TryReadFixed64(out var raw))
        {
            return false;
        }

        value = BitConverter.Int64BitsToDouble(unchecked((long)raw));
        return true;
    }

    public bool TryReadString(TaggedWireType wireType, out string? value)
    {
        value = default;
        if (!TryReadLengthDelimited(wireType, _limits.MaxStringBytes, out var bytes))
        {
            return false;
        }

        try
        {
            value = DecodeUtf8(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return Fail(TcpBinaryDecodeError.InvalidUtf8);
        }
    }

    public bool TryReadBytes(TaggedWireType wireType, out byte[]? value)
    {
        value = default;
        if (!TryReadLengthDelimited(wireType, _limits.MaxByteArrayBytes, out var bytes))
        {
            return false;
        }

        if (bytes.IsEmpty)
        {
            value = Array.Empty<byte>();
            return true;
        }

        value = GC.AllocateUninitializedArray<byte>(checked((int)bytes.Length));
        bytes.CopyTo(value);
        return true;
    }

    public bool TrySkip(TaggedWireType wireType) => wireType switch
    {
        TaggedWireType.VarInt => TryReadVarIntCore(out _),
        TaggedWireType.Fixed64 => TryAdvance(sizeof(ulong)),
        TaggedWireType.LengthDelimited => TryReadLengthDelimited(
            wireType,
            _limits.MaxFieldBytes,
            out _),
        TaggedWireType.Fixed32 => TryAdvance(sizeof(uint)),
        _ => Fail(TcpBinaryDecodeError.UnsupportedWireType)
    };

    private bool TryReadLengthDelimited(
        TaggedWireType actualWireType,
        int typeLimit,
        out ReadOnlySequence<byte> value)
    {
        value = default;
        if (!RequireWireType(actualWireType, TaggedWireType.LengthDelimited)
            || !TryReadVarIntCore(out var rawLength))
        {
            return false;
        }

        if (rawLength > int.MaxValue
            || rawLength > (ulong)typeLimit
            || rawLength > (ulong)_limits.MaxFieldBytes)
        {
            return Fail(TcpBinaryDecodeError.FieldTooLarge);
        }

        var length = (int)rawLength;
        if (_reader.Remaining < length)
        {
            return Fail(TcpBinaryDecodeError.Truncated);
        }

        value = _reader.Sequence.Slice(_reader.Position, length);
        _reader.Advance(length);
        return true;
    }

    private bool TryReadVarIntCore(out ulong value)
    {
        value = 0;
        for (var index = 0; index < 10; index++)
        {
            if (!_reader.TryRead(out var current))
            {
                return Fail(TcpBinaryDecodeError.Truncated);
            }

            if (index == 9 && current > 1)
            {
                return Fail(TcpBinaryDecodeError.MalformedVarInt);
            }

            value |= (ulong)(current & 0x7F) << (index * 7);
            if ((current & 0x80) == 0)
            {
                if (index > 0 && current == 0)
                {
                    return Fail(TcpBinaryDecodeError.MalformedVarInt);
                }

                return true;
            }
        }

        return Fail(TcpBinaryDecodeError.MalformedVarInt);
    }

    private bool TryReadFixed32(out uint value)
    {
        value = default;
        if (_reader.UnreadSpan.Length >= sizeof(uint))
        {
            value = NativeFixedWidth.ReadUInt32LittleEndian(_reader.UnreadSpan);
            _reader.Advance(sizeof(uint));
            return true;
        }

        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        if (!_reader.TryCopyTo(bytes))
        {
            return Fail(TcpBinaryDecodeError.Truncated);
        }

        _reader.Advance(sizeof(uint));
        value = NativeFixedWidth.ReadUInt32LittleEndian(bytes);
        return true;
    }

    private bool TryReadFixed64(out ulong value)
    {
        value = default;
        if (_reader.UnreadSpan.Length >= sizeof(ulong))
        {
            value = NativeFixedWidth.ReadUInt64LittleEndian(_reader.UnreadSpan);
            _reader.Advance(sizeof(ulong));
            return true;
        }

        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        if (!_reader.TryCopyTo(bytes))
        {
            return Fail(TcpBinaryDecodeError.Truncated);
        }

        _reader.Advance(sizeof(ulong));
        value = NativeFixedWidth.ReadUInt64LittleEndian(bytes);
        return true;
    }

    private bool TryAdvance(int byteCount)
    {
        if (_reader.Remaining < byteCount)
        {
            return Fail(TcpBinaryDecodeError.Truncated);
        }

        _reader.Advance(byteCount);
        return true;
    }

    private bool RequireWireType(TaggedWireType actual, TaggedWireType expected) =>
        actual == expected || Fail(TcpBinaryDecodeError.WireTypeMismatch);

    private bool Fail(TcpBinaryDecodeError error)
    {
        if (Error == TcpBinaryDecodeError.None)
        {
            Error = error;
        }

        return false;
    }

    private static string DecodeUtf8(in ReadOnlySequence<byte> bytes)
    {
        if (bytes.IsSingleSegment)
        {
            return StrictUtf8.GetString(bytes.FirstSpan);
        }

        var length = checked((int)bytes.Length);
        if (length <= 256)
        {
            Span<byte> temporary = stackalloc byte[length];
            bytes.CopyTo(temporary);
            return StrictUtf8.GetString(temporary);
        }

        var rented = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            bytes.CopyTo(rented);
            return StrictUtf8.GetString(rented, 0, length);
        }
        finally
        {
            rented.AsSpan(0, length).Clear();
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static int ZigZagDecode(uint value) =>
        unchecked((int)((value >> 1) ^ (uint)-(int)(value & 1)));

    private static long ZigZagDecode(ulong value) =>
        unchecked((long)((value >> 1) ^ (ulong)-(long)(value & 1)));
}
