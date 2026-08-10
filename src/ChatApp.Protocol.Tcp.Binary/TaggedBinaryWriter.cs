using System.Buffers;
using System.Text;

namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>
/// Bounded tagged writer over <see cref="IBufferWriter{T}"/>. It uses checked span lengths and
/// confines native-pointer access to the already-bounded fixed-width helper. Variable-length data,
/// limits, and buffer ownership remain on the safe span path.
/// </summary>
public ref struct TaggedBinaryWriter
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private readonly IBufferWriter<byte> _destination;
    private readonly TcpBinaryLimits _limits;
    private int _written;
    private int _fieldCount;

    public TaggedBinaryWriter(IBufferWriter<byte> destination, TcpBinaryLimits limits)
    {
        ArgumentNullException.ThrowIfNull(destination);
        _destination = destination;
        _limits = limits;
        _written = 0;
        _fieldCount = 0;
    }

    public readonly int WrittenCount => _written;

    public void WriteBool(int fieldNumber, bool value) =>
        WriteVarIntField(fieldNumber, value ? 1UL : 0UL);

    public void WriteInt32(int fieldNumber, int value) =>
        WriteVarIntField(fieldNumber, ZigZagEncode(value));

    public void WriteUInt32(int fieldNumber, uint value) =>
        WriteVarIntField(fieldNumber, value);

    public void WriteInt64(int fieldNumber, long value) =>
        WriteVarIntField(fieldNumber, ZigZagEncode(value));

    public void WriteUInt64(int fieldNumber, ulong value) =>
        WriteVarIntField(fieldNumber, value);

    public void WriteSingle(int fieldNumber, float value) =>
        WriteFixed32Field(fieldNumber, unchecked((uint)BitConverter.SingleToInt32Bits(value)));

    public void WriteDouble(int fieldNumber, double value) =>
        WriteFixed64Field(fieldNumber, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));

    public void WriteString(int fieldNumber, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var byteCount = StrictUtf8.GetByteCount(value);
        EnsureFieldLength(byteCount, _limits.MaxStringBytes);
        var tag = CreateTag(fieldNumber, TaggedWireType.LengthDelimited);
        var prefixLength = GetVarIntSize(tag) + GetVarIntSize((ulong)byteCount);
        var totalLength = checked(prefixLength + byteCount);
        var destination = ReserveField(totalLength);
        var offset = WriteVarInt(destination, tag);
        offset += WriteVarInt(destination[offset..], (ulong)byteCount);
        var encoded = StrictUtf8.GetBytes(value.AsSpan(), destination.Slice(offset, byteCount));
        if (encoded != byteCount)
        {
            throw new InvalidDataException("The string encoder wrote an unexpected number of bytes.");
        }

        _destination.Advance(totalLength);
        _written += totalLength;
    }

    public void WriteBytes(int fieldNumber, ReadOnlySpan<byte> value)
    {
        EnsureFieldLength(value.Length, _limits.MaxByteArrayBytes);
        var tag = CreateTag(fieldNumber, TaggedWireType.LengthDelimited);
        var prefixLength = GetVarIntSize(tag) + GetVarIntSize((ulong)value.Length);
        var totalLength = checked(prefixLength + value.Length);
        var destination = ReserveField(totalLength);
        var offset = WriteVarInt(destination, tag);
        offset += WriteVarInt(destination[offset..], (ulong)value.Length);
        value.CopyTo(destination[offset..]);
        _destination.Advance(totalLength);
        _written += totalLength;
    }

    private void WriteVarIntField(int fieldNumber, ulong value)
    {
        var tag = CreateTag(fieldNumber, TaggedWireType.VarInt);
        var totalLength = GetVarIntSize(tag) + GetVarIntSize(value);
        var destination = ReserveField(totalLength);
        var offset = WriteVarInt(destination, tag);
        _ = WriteVarInt(destination[offset..], value);
        _destination.Advance(totalLength);
        _written += totalLength;
    }

    private void WriteFixed32Field(int fieldNumber, uint value)
    {
        var tag = CreateTag(fieldNumber, TaggedWireType.Fixed32);
        var tagLength = GetVarIntSize(tag);
        var totalLength = tagLength + sizeof(uint);
        var destination = ReserveField(totalLength);
        _ = WriteVarInt(destination, tag);
        NativeFixedWidth.WriteUInt32LittleEndian(destination[tagLength..], value);
        _destination.Advance(totalLength);
        _written += totalLength;
    }

    private void WriteFixed64Field(int fieldNumber, ulong value)
    {
        var tag = CreateTag(fieldNumber, TaggedWireType.Fixed64);
        var tagLength = GetVarIntSize(tag);
        var totalLength = tagLength + sizeof(ulong);
        var destination = ReserveField(totalLength);
        _ = WriteVarInt(destination, tag);
        NativeFixedWidth.WriteUInt64LittleEndian(destination[tagLength..], value);
        _destination.Advance(totalLength);
        _written += totalLength;
    }

    private Span<byte> ReserveField(int byteCount)
    {
        if (++_fieldCount > _limits.MaxFields)
        {
            throw new TcpBinaryLimitException(
                TcpBinaryDecodeError.TooManyFields,
                $"The payload exceeds the configured field limit of {_limits.MaxFields}.");
        }

        if (byteCount > _limits.MaxMessageBytes - _written)
        {
            throw new TcpBinaryLimitException(
                TcpBinaryDecodeError.MessageTooLarge,
                $"The payload exceeds the configured message limit of {_limits.MaxMessageBytes} bytes.");
        }

        return _destination.GetSpan(byteCount)[..byteCount];
    }

    private void EnsureFieldLength(int byteCount, int typeLimit)
    {
        if (byteCount < 0 || byteCount > typeLimit || byteCount > _limits.MaxFieldBytes)
        {
            throw new TcpBinaryLimitException(
                TcpBinaryDecodeError.FieldTooLarge,
                $"The field exceeds its configured limit ({byteCount} bytes).");
        }
    }

    private static ulong CreateTag(int fieldNumber, TaggedWireType wireType)
    {
        if (fieldNumber is <= 0 or > TcpBinaryLimits.MaximumFieldNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldNumber));
        }

        return ((ulong)(uint)fieldNumber << 3) | (byte)wireType;
    }

    private static int GetVarIntSize(ulong value)
    {
        var size = 1;
        while (value >= 0x80)
        {
            value >>= 7;
            size++;
        }

        return size;
    }

    private static int WriteVarInt(Span<byte> destination, ulong value)
    {
        var index = 0;
        while (value >= 0x80)
        {
            destination[index++] = (byte)(value | 0x80);
            value >>= 7;
        }

        destination[index++] = (byte)value;
        return index;
    }

    private static uint ZigZagEncode(int value) =>
        unchecked((uint)((value << 1) ^ (value >> 31)));

    private static ulong ZigZagEncode(long value) =>
        unchecked((ulong)((value << 1) ^ (value >> 63)));
}
