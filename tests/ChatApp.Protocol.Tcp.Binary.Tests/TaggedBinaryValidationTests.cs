using System.Buffers;
using System.Buffers.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class TaggedBinaryValidationTests
{
    [Fact]
    public void PrimitiveWriterMatchesTaggedV1GoldenBytes()
    {
        var destination = new ArrayBufferWriter<byte>();
        var writer = new TaggedBinaryWriter(destination, TcpBinaryLimits.Default);
        writer.WriteInt32(1, -1);
        writer.WriteUInt32(2, 150);
        writer.WriteString(3, "A");
        writer.WriteSingle(4, 1F);

        Assert.Equal("08011096011A0141250000803F", Convert.ToHexString(destination.WrittenSpan));
    }

    [Fact]
    public void NativeFixedWidthPathMatchesPortableLittleEndianBytes()
    {
        uint[] singleBits = [0, 1, uint.MaxValue, 0x7FC0_0001, 0x8000_0000];
        foreach (uint bits in singleBits)
        {
            var destination = new ArrayBufferWriter<byte>();
            var writer = new TaggedBinaryWriter(destination, TcpBinaryLimits.Default);
            writer.WriteSingle(1, BitConverter.Int32BitsToSingle(unchecked((int)bits)));

            var expected = new byte[5];
            expected[0] = 0x0D;
            BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(1), bits);
            Assert.Equal(expected, destination.WrittenSpan.ToArray());

            var reader = new TaggedBinaryReader(
                SequenceFactory.OneByteSegments(destination.WrittenSpan.ToArray()),
                TcpBinaryLimits.Default);
            Assert.True(reader.TryReadFieldHeader(out _, out TaggedWireType wireType));
            Assert.True(reader.TryReadSingle(wireType, out float decoded));
            Assert.Equal(bits, unchecked((uint)BitConverter.SingleToInt32Bits(decoded)));
        }

        ulong[] doubleBits = [0, 1, ulong.MaxValue, 0x7FF8_0000_0000_0001, 0x8000_0000_0000_0000];
        foreach (ulong bits in doubleBits)
        {
            var destination = new ArrayBufferWriter<byte>();
            var writer = new TaggedBinaryWriter(destination, TcpBinaryLimits.Default);
            writer.WriteDouble(1, BitConverter.Int64BitsToDouble(unchecked((long)bits)));

            var expected = new byte[9];
            expected[0] = 0x09;
            BinaryPrimitives.WriteUInt64LittleEndian(expected.AsSpan(1), bits);
            Assert.Equal(expected, destination.WrittenSpan.ToArray());

            var reader = new TaggedBinaryReader(
                SequenceFactory.OneByteSegments(destination.WrittenSpan.ToArray()),
                TcpBinaryLimits.Default);
            Assert.True(reader.TryReadFieldHeader(out _, out TaggedWireType wireType));
            Assert.True(reader.TryReadDouble(wireType, out double decoded));
            Assert.Equal(bits, unchecked((ulong)BitConverter.DoubleToInt64Bits(decoded)));
        }
    }

    [Fact]
    public void FieldZeroIsRejected()
    {
        AssertDecodeError([0x00], TcpBinaryDecodeError.InvalidFieldNumber);
    }

    [Fact]
    public void UnsupportedWireTypeIsRejected()
    {
        AssertDecodeError([0x0B], TcpBinaryDecodeError.UnsupportedWireType);
    }

    [Theory]
    [InlineData(new byte[] { 0x80, 0x00 })]
    [InlineData(new byte[] { 0x81, 0x00 })]
    [InlineData(new byte[] { 0x88, 0x00 })]
    public void NonCanonicalVarIntsAreRejected(byte[] payload)
    {
        AssertDecodeError(payload, TcpBinaryDecodeError.MalformedVarInt);
    }

    [Fact]
    public void NonCanonicalValueAndLengthVarIntsAreRejected()
    {
        var valueReader = new TaggedBinaryReader(
            new ReadOnlySequence<byte>(new byte[] { 0x08, 0x81, 0x00 }),
            TcpBinaryLimits.Default);
        Assert.True(valueReader.TryReadFieldHeader(out _, out TaggedWireType valueWire));
        Assert.False(valueReader.TryReadInt32(valueWire, out _));
        Assert.Equal(TcpBinaryDecodeError.MalformedVarInt, valueReader.Error);

        var lengthReader = new TaggedBinaryReader(
            new ReadOnlySequence<byte>(new byte[] { 0x0A, 0x81, 0x00, 0x00 }),
            TcpBinaryLimits.Default);
        Assert.True(lengthReader.TryReadFieldHeader(out _, out TaggedWireType lengthWire));
        Assert.False(lengthReader.TrySkip(lengthWire));
        Assert.Equal(TcpBinaryDecodeError.MalformedVarInt, lengthReader.Error);
    }

    [Fact]
    public void TenByteOverflowAndUnterminatedVarIntsAreRejected()
    {
        AssertDecodeError(
            [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02],
            TcpBinaryDecodeError.MalformedVarInt);
        AssertDecodeError(
            [0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80],
            TcpBinaryDecodeError.MalformedVarInt);
    }

    [Fact]
    public void TruncatedValuesAreRejectedAcrossSegments()
    {
        byte[][] payloads =
        [
            [0x08, 0x80],
            [0x0D, 0x01, 0x02, 0x03],
            [0x09, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07],
            [0x0A, 0x03, 0x01, 0x02]
        ];

        foreach (byte[] payload in payloads)
        {
            var reader = new TaggedBinaryReader(
                SequenceFactory.OneByteSegments(payload),
                TcpBinaryLimits.Default);
            Assert.True(reader.TryReadFieldHeader(out _, out TaggedWireType wireType));
            Assert.False(reader.TrySkip(wireType));
            Assert.Equal(TcpBinaryDecodeError.Truncated, reader.Error);
        }
    }

    [Fact]
    public void MessageFieldAndFieldCountLimitsAreEnforced()
    {
        var tightMessage = new TcpBinaryLimits(
            maxMessageBytes: 1,
            maxFieldBytes: 1,
            maxStringBytes: 1,
            maxByteArrayBytes: 1,
            maxFields: 1,
            maxDepth: 1);
        var oversizedReader = new TaggedBinaryReader(
            new ReadOnlySequence<byte>(new byte[] { 0x08, 0x01 }),
            tightMessage);
        Assert.Equal(TcpBinaryDecodeError.MessageTooLarge, oversizedReader.Error);

        var fieldLimited = new TcpBinaryLimits(
            maxMessageBytes: 16,
            maxFieldBytes: 4,
            maxStringBytes: 4,
            maxByteArrayBytes: 4,
            maxFields: 1,
            maxDepth: 1);
        var reader = new TaggedBinaryReader(
            new ReadOnlySequence<byte>(new byte[] { 0x08, 0x01, 0x10, 0x01 }),
            fieldLimited);
        Assert.True(reader.TryReadFieldHeader(out _, out TaggedWireType firstWire));
        Assert.True(reader.TrySkip(firstWire));
        Assert.False(reader.TryReadFieldHeader(out _, out _));
        Assert.Equal(TcpBinaryDecodeError.TooManyFields, reader.Error);

        var fieldLengthReader = new TaggedBinaryReader(
            new ReadOnlySequence<byte>(new byte[] { 0x0A, 0x05 }),
            fieldLimited);
        Assert.True(fieldLengthReader.TryReadFieldHeader(out _, out TaggedWireType lengthWire));
        Assert.False(fieldLengthReader.TrySkip(lengthWire));
        Assert.Equal(TcpBinaryDecodeError.FieldTooLarge, fieldLengthReader.Error);
    }

    [Fact]
    public void WriterRejectsInvalidNumbersAndConfiguredLimitsBeforeAdvancing()
    {
        var destination = new ArrayBufferWriter<byte>();
        var limits = new TcpBinaryLimits(
            maxMessageBytes: 4,
            maxFieldBytes: 2,
            maxStringBytes: 2,
            maxByteArrayBytes: 2,
            maxFields: 1,
            maxDepth: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => WriteInvalidFieldNumber(destination, limits));
        TcpBinaryLimitException exception = Assert.Throws<TcpBinaryLimitException>(
            () => WriteOversizedString(destination, limits));
        Assert.Equal(TcpBinaryDecodeError.FieldTooLarge, exception.Error);
        Assert.Equal(0, destination.WrittenCount);
    }

    [Fact]
    public void WrongWireTypeAndInvalidUtf8AreRejected()
    {
        Assert.False(BinaryContractFixtureDescriptor.Codec.TryDecode(
            new ReadOnlySequence<byte>(new byte[] { 0x0A, 0x00 }),
            out _,
            out TcpBinaryDecodeError wireError));
        Assert.Equal(TcpBinaryDecodeError.WireTypeMismatch, wireError);

        Assert.False(BinaryContractFixtureDescriptor.Codec.TryDecode(
            new ReadOnlySequence<byte>(new byte[] { 0x2A, 0x01, 0xFF }),
            out _,
            out TcpBinaryDecodeError utf8Error));
        Assert.Equal(TcpBinaryDecodeError.InvalidUtf8, utf8Error);
    }

    [Fact]
    public void KnownSingularFieldsRejectDuplicates()
    {
        Assert.False(BinaryContractFixtureDescriptor.Codec.TryDecode(
            new ReadOnlySequence<byte>([0x08, 0x02, 0x08, 0x04]),
            out _,
            out TcpBinaryDecodeError error));
        Assert.Equal(TcpBinaryDecodeError.DuplicateField, error);
    }

    [Fact]
    public void RequiredFieldsMustBePresentOnTheWire()
    {
        Assert.False(RequiredBinaryContractFixtureDescriptor.Codec.TryDecode(
            ReadOnlySequence<byte>.Empty,
            out _,
            out TcpBinaryDecodeError missingError));
        Assert.Equal(TcpBinaryDecodeError.MissingRequiredField, missingError);

        var destination = new ArrayBufferWriter<byte>();
        RequiredBinaryContractFixtureDescriptor.Codec.Encode(
            destination,
            new RequiredBinaryContractFixture { RequiredId = 0 });
        Assert.True(RequiredBinaryContractFixtureDescriptor.Codec.TryDecode(
            new ReadOnlySequence<byte>(destination.WrittenMemory),
            out RequiredBinaryContractFixture? decoded,
            out TcpBinaryDecodeError presentError));
        Assert.Equal(TcpBinaryDecodeError.None, presentError);
        Assert.Equal(0, decoded!.RequiredId);
    }

    [Fact]
    public void BoundedDecoderNeverThrowsForDeterministicArbitraryInput()
    {
        var limits = new TcpBinaryLimits(
            maxMessageBytes: 256,
            maxFieldBytes: 64,
            maxStringBytes: 64,
            maxByteArrayBytes: 64,
            maxFields: 16,
            maxDepth: 1);
        var random = new Random(0x43_54_42_31);

        for (var iteration = 0; iteration < 10_000; iteration++)
        {
            var payload = new byte[random.Next(0, limits.MaxMessageBytes + 1)];
            random.NextBytes(payload);
            ReadOnlySequence<byte> sequence = iteration % 97 == 0
                ? SequenceFactory.OneByteSegments(payload)
                : new ReadOnlySequence<byte>(payload);

            bool decoded = BinaryContractFixtureDescriptor.Codec.TryDecode(
                sequence,
                limits,
                out _,
                out TcpBinaryDecodeError error);

            Assert.Equal(decoded, error == TcpBinaryDecodeError.None);
        }
    }

    private static void AssertDecodeError(byte[] payload, TcpBinaryDecodeError expected)
    {
        var reader = new TaggedBinaryReader(new ReadOnlySequence<byte>(payload), TcpBinaryLimits.Default);
        Assert.False(reader.TryReadFieldHeader(out _, out _));
        Assert.Equal(expected, reader.Error);
    }

    private static void WriteInvalidFieldNumber(
        IBufferWriter<byte> destination,
        TcpBinaryLimits limits)
    {
        var writer = new TaggedBinaryWriter(destination, limits);
        writer.WriteUInt32(0, 1);
    }

    private static void WriteOversizedString(
        IBufferWriter<byte> destination,
        TcpBinaryLimits limits)
    {
        var writer = new TaggedBinaryWriter(destination, limits);
        writer.WriteString(1, "oversize");
    }
}
