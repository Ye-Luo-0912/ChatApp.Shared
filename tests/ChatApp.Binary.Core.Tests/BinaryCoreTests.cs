using System.Buffers;
using ChatApp.Binary.Core;
using Xunit;

namespace ChatApp.Binary.Core.Tests;

public sealed class BinaryCoreTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    [Fact]
    public void SinglePassEncoderProducesCanonicalGoldenBytes()
    {
        var value = new Fixture(-1, 300, -0.0F, [1, 2, 3]);
        Span<byte> destination = stackalloc byte[64];

        BinaryStatus status = BinaryCodec.TryEncode<FixtureEncoder, Fixture>(
            in value,
            destination,
            Limits,
            out int written);

        Assert.Equal(BinaryStatus.Done, status);
        Assert.Equal(15, written);
        Assert.Equal(
            [
                0x08, 0x01,
                0x10, 0xAC, 0x02,
                0x1D, 0x00, 0x00, 0x00, 0x80,
                0x22, 0x03, 0x01, 0x02, 0x03
            ],
            destination[..written].ToArray());
    }

    [Fact]
    public void NativeAndPortableWritersProduceIdenticalBytes()
    {
        var value = new Fixture(-123, ulong.MaxValue, 1.25F, [4, 5, 6, 7]);
        Span<byte> nativeBytes = stackalloc byte[64];
        Span<byte> portableBytes = stackalloc byte[64];

        Assert.Equal(
            BinaryStatus.Done,
            BinaryCodec.TryEncode<FixtureEncoder, Fixture>(
                in value,
                nativeBytes,
                Limits,
                out int nativeLength));

        var writer = new BinaryWriteCursor(portableBytes, Limits);
        Assert.Equal(BinaryStatus.Done, FixtureEncoder.Write(ref writer, in value));

        Assert.Equal(nativeLength, writer.WrittenCount);
        Assert.True(nativeBytes[..nativeLength].SequenceEqual(portableBytes[..writer.WrittenCount]));
    }

    [Fact]
    public void EncodingFailureDoesNotReportPartialBytes()
    {
        var value = new Fixture(-123, 17, 1.25F, [4, 5, 6, 7]);
        Span<byte> destination = stackalloc byte[2];

        BinaryStatus status = BinaryCodec.TryEncode<FixtureEncoder, Fixture>(
            in value,
            destination,
            Limits,
            out int written);

        Assert.Equal(BinaryStatus.DestinationTooSmall, status);
        Assert.Equal(0, written);
    }

    [Fact]
    public void EncodingPreservesRawIeeeBits()
    {
        var value = new Fixture(0, 0, -0.0F, []);
        Span<byte> destination = stackalloc byte[32];

        Assert.Equal(
            BinaryStatus.Done,
            BinaryCodec.TryEncode<FixtureEncoder, Fixture>(
                in value,
                destination,
                Limits,
                out int length));

        Assert.Equal(11, length);
        Assert.Equal(0x80, destination[8]);
    }

    [Theory]
    [InlineData(new byte[] { 0x08, 0x01, 0x08, 0x02 }, BinaryStatus.DuplicateField)]
    [InlineData(new byte[] { 0x10, 0x01, 0x08, 0x02 }, BinaryStatus.FieldsOutOfOrder)]
    public void ContiguousReaderRequiresStrictlyIncreasingFields(byte[] bytes, BinaryStatus expected)
    {
        var reader = new BinaryReadCursor(bytes, Limits);

        Assert.True(reader.TryReadFieldHeader(out _, out BinaryWireType firstWire));
        Assert.True(reader.TryReadUInt64(firstWire, out _));
        Assert.False(reader.TryReadFieldHeader(out _, out _));
        Assert.Equal(expected, reader.Status);
    }

    [Theory]
    [InlineData(new byte[] { 0x08, 0x01, 0x08, 0x02 }, BinaryStatus.DuplicateField)]
    [InlineData(new byte[] { 0x10, 0x01, 0x08, 0x02 }, BinaryStatus.FieldsOutOfOrder)]
    public void SequenceReaderRequiresStrictlyIncreasingFields(byte[] bytes, BinaryStatus expected)
    {
        ReadOnlySequence<byte> sequence = SegmentEveryByte(bytes);
        var reader = new BinarySequenceReadCursor(in sequence, Limits);

        Assert.True(reader.TryReadFieldHeader(out _, out BinaryWireType firstWire));
        Assert.True(reader.TryReadUInt64(firstWire, out _));
        Assert.False(reader.TryReadFieldHeader(out _, out _));
        Assert.Equal(expected, reader.Status);
    }

    [Fact]
    public void UnsupportedWireTypeIsRejected()
    {
        ReadOnlySpan<byte> bytes = [0x0E];
        var reader = new BinaryReadCursor(bytes, Limits);
        Assert.False(reader.TryReadFieldHeader(out _, out _));
        Assert.Equal(BinaryStatus.UnsupportedWireType, reader.Status);
    }

    [Fact]
    public void NonMinimalVarIntFailsClosedInBothReaders()
    {
        byte[] bytes = [0x08, 0x80, 0x00];
        var contiguous = new BinaryReadCursor(bytes, Limits);
        Assert.True(contiguous.TryReadFieldHeader(out _, out BinaryWireType contiguousWire));
        Assert.False(contiguous.TryReadUInt64(contiguousWire, out _));
        Assert.Equal(BinaryStatus.MalformedVarInt, contiguous.Status);

        ReadOnlySequence<byte> sequence = SegmentEveryByte(bytes);
        var segmented = new BinarySequenceReadCursor(in sequence, Limits);
        Assert.True(segmented.TryReadFieldHeader(out _, out BinaryWireType segmentedWire));
        Assert.False(segmented.TryReadUInt64(segmentedWire, out _));
        Assert.Equal(BinaryStatus.MalformedVarInt, segmented.Status);
    }

    [Fact]
    public void InvalidUtf8FailsInBothReaders()
    {
        byte[] bytes = [0x0A, 0x02, 0xC3, 0x28];
        var contiguous = new BinaryReadCursor(bytes, Limits);
        Assert.True(contiguous.TryReadFieldHeader(out _, out BinaryWireType contiguousWire));
        Assert.False(contiguous.TryReadUtf8(contiguousWire, out _));
        Assert.Equal(BinaryStatus.InvalidUtf8, contiguous.Status);

        ReadOnlySequence<byte> sequence = SegmentEveryByte(bytes);
        var segmented = new BinarySequenceReadCursor(in sequence, Limits);
        Assert.True(segmented.TryReadFieldHeader(out _, out BinaryWireType segmentedWire));
        Assert.False(segmented.TryReadUtf8(segmentedWire, out _));
        Assert.Equal(BinaryStatus.InvalidUtf8, segmented.Status);
    }

    [Fact]
    public void EveryByteSegmentationReadsCrossSegmentValuesWithoutCoalescing()
    {
        var value = new CompleteFixture(300, 0x1122_3344, 0x0102_0304_0506_0708, "跨段", [9, 8, 7]);
        Span<byte> destination = stackalloc byte[128];
        Assert.Equal(
            BinaryStatus.Done,
            BinaryCodec.TryEncode<CompleteFixtureEncoder, CompleteFixture>(
                in value,
                destination,
                Limits,
                out int written));

        ReadOnlySequence<byte> sequence = SegmentEveryByte(destination[..written].ToArray());
        BinaryStatus status = BinaryCodec.TryDecode<CompleteFixtureSequenceDecoder, CompleteFixture>(
            in sequence,
            Limits,
            out CompleteFixture? decoded);

        Assert.Equal(BinaryStatus.Done, status);
        Assert.NotNull(decoded);
        Assert.Equal(value.Count, decoded.Count);
        Assert.Equal(value.Fixed32, decoded.Fixed32);
        Assert.Equal(value.Fixed64, decoded.Fixed64);
        Assert.Equal(value.Text, decoded.Text);
        Assert.Equal(value.Body, decoded.Body);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SequenceReaderReportsTruncationInsideFinalLengthDelimitedField(int removeBytes)
    {
        var value = new CompleteFixture(300, 0x1122_3344, 0x0102_0304_0506_0708, "hello", [9, 8, 7]);
        Span<byte> destination = stackalloc byte[128];
        Assert.Equal(
            BinaryStatus.Done,
            BinaryCodec.TryEncode<CompleteFixtureEncoder, CompleteFixture>(
                in value,
                destination,
                Limits,
                out int written));
        ReadOnlySequence<byte> sequence = SegmentEveryByte(destination[..(written - removeBytes)].ToArray());

        Assert.Equal(
            BinaryStatus.Truncated,
            BinaryCodec.TryDecode<CompleteFixtureSequenceDecoder, CompleteFixture>(
                in sequence,
                Limits,
                out _));
    }

    [Theory]
    [MemberData(nameof(TruncatedSegmentedFields))]
    public void SequenceReaderReportsTruncationForEveryWireKind(
        byte[] payload,
        BinaryWireType expectedWire)
    {
        ReadOnlySequence<byte> sequence = SegmentEveryByte(payload);
        var reader = new BinarySequenceReadCursor(in sequence, Limits);

        Assert.True(reader.TryReadFieldHeader(out _, out BinaryWireType wire));
        Assert.Equal(expectedWire, wire);
        bool read = wire switch
        {
            BinaryWireType.VarInt => reader.TryReadUInt64(wire, out _),
            BinaryWireType.Fixed32 => reader.TryReadFixed32(wire, out _),
            BinaryWireType.Fixed64 => reader.TryReadFixed64(wire, out _),
            BinaryWireType.LengthDelimited => reader.TryReadBytes(wire, out _),
            _ => throw new InvalidOperationException()
        };

        Assert.False(read);
        Assert.Equal(BinaryStatus.Truncated, reader.Status);
    }

    public static TheoryData<byte[], BinaryWireType> TruncatedSegmentedFields => new()
    {
        { [0x08, 0x80], BinaryWireType.VarInt },
        { [0x0D, 1, 2, 3], BinaryWireType.Fixed32 },
        { [0x09, 1, 2, 3, 4, 5, 6, 7], BinaryWireType.Fixed64 },
        { [0x0A, 0x03, 1, 2], BinaryWireType.LengthDelimited }
    };

    [Fact]
    public void SequenceStringReportsTruncationAcrossSegments()
    {
        byte[] payload = [0x0A, 0x03, (byte)'a', (byte)'b'];
        ReadOnlySequence<byte> sequence = SegmentEveryByte(payload);
        var reader = new BinarySequenceReadCursor(in sequence, Limits);

        Assert.True(reader.TryReadFieldHeader(out _, out BinaryWireType wire));
        Assert.False(reader.TryReadUtf8(wire, out _));
        Assert.Equal(BinaryStatus.Truncated, reader.Status);
    }

    [Fact]
    public void SequenceReaderEnforcesTypeAndMessageLimits()
    {
        var limits = new BinaryLimits(12, 8, 3, 2, 8);
        byte[] tooLongString = [0x0A, 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t'];
        ReadOnlySequence<byte> stringSequence = SegmentEveryByte(tooLongString);
        var stringReader = new BinarySequenceReadCursor(in stringSequence, limits);
        Assert.True(stringReader.TryReadFieldHeader(out _, out BinaryWireType stringWire));
        Assert.False(stringReader.TryReadUtf8(stringWire, out _));
        Assert.Equal(BinaryStatus.StringTooLarge, stringReader.Status);

        byte[] tooLongBytes = [0x0A, 0x03, 1, 2, 3];
        ReadOnlySequence<byte> bytesSequence = SegmentEveryByte(tooLongBytes);
        var bytesReader = new BinarySequenceReadCursor(in bytesSequence, limits);
        Assert.True(bytesReader.TryReadFieldHeader(out _, out BinaryWireType bytesWire));
        Assert.False(bytesReader.TryReadBytes(bytesWire, out _));
        Assert.Equal(BinaryStatus.ByteArrayTooLarge, bytesReader.Status);

        byte[] tooLarge = new byte[13];
        ReadOnlySequence<byte> messageSequence = SegmentEveryByte(tooLarge);
        Assert.Equal(
            BinaryStatus.MessageTooLarge,
            BinaryCodec.TryDecode<ChecksumSequenceDecoder, ulong>(in messageSequence, limits, out _));
    }

    [Fact]
    public void WriterRejectsClearlyOversizedStringBeforeUtf8Encoding()
    {
        var limits = new BinaryLimits(32, 8, 4, 4, 8);
        Span<byte> destination = stackalloc byte[32];
        var writer = new BinaryWriteCursor(destination, limits);

        Assert.Equal(BinaryStatus.StringTooLarge, writer.WriteString(1, "12345"));
        Assert.Equal(0, writer.WrittenCount);
    }

    [Fact]
    public void ContiguousReaderKeepsEqualStringAndByteLimitsSemanticallyDistinct()
    {
        var limits = new BinaryLimits(32, 4, 2, 2, 8);
        byte[] payload = [0x0A, 0x03, 1, 2, 3];

        var stringReader = new BinaryReadCursor(payload, limits);
        Assert.True(stringReader.TryReadFieldHeader(out _, out BinaryWireType stringWire));
        Assert.False(stringReader.TryReadUtf8(stringWire, out _));
        Assert.Equal(BinaryStatus.StringTooLarge, stringReader.Status);

        var bytesReader = new BinaryReadCursor(payload, limits);
        Assert.True(bytesReader.TryReadFieldHeader(out _, out BinaryWireType bytesWire));
        Assert.False(bytesReader.TryReadBytes(bytesWire, out _));
        Assert.Equal(BinaryStatus.ByteArrayTooLarge, bytesReader.Status);
    }

    [Fact]
    public void UnknownLengthDelimitedFieldCannotBypassFieldLimit()
    {
        var limits = new BinaryLimits(32, 3, 3, 3, 8);
        byte[] payload = [0x2A, 0x04, 1, 2, 3, 4];
        ReadOnlySequence<byte> sequence = SegmentEveryByte(payload);
        var reader = new BinarySequenceReadCursor(in sequence, limits);

        Assert.True(reader.TryReadFieldHeader(out int field, out BinaryWireType wire));
        Assert.Equal(5, field);
        Assert.False(reader.TrySkip(wire));
        Assert.Equal(BinaryStatus.FieldTooLarge, reader.Status);
    }

    [Theory]
    [MemberData(nameof(Utf8Cases))]
    public void Utf8ValidationMatchesAcrossEveryByteBoundary(
        byte[] utf8,
        BinaryStatus expected)
    {
        byte[] payload = new byte[2 + utf8.Length];
        payload[0] = 0x0A;
        payload[1] = (byte)utf8.Length;
        utf8.CopyTo(payload, 2);

        var contiguous = new BinaryReadCursor(payload, Limits);
        Assert.True(contiguous.TryReadFieldHeader(out _, out BinaryWireType contiguousWire));
        bool contiguousRead = contiguous.TryReadUtf8(contiguousWire, out ReadOnlySpan<byte> contiguousBytes);

        ReadOnlySequence<byte> sequence = SegmentEveryByte(payload);
        var segmented = new BinarySequenceReadCursor(in sequence, Limits);
        Assert.True(segmented.TryReadFieldHeader(out _, out BinaryWireType segmentedWire));
        bool segmentedRead = segmented.TryReadUtf8(
            segmentedWire,
            out ReadOnlySequence<byte> segmentedBytes);

        Assert.Equal(expected == BinaryStatus.Done, contiguousRead);
        Assert.Equal(contiguousRead, segmentedRead);
        Assert.Equal(expected, contiguous.Status);
        Assert.Equal(contiguous.Status, segmented.Status);
        if (expected == BinaryStatus.Done)
        {
            Assert.Equal(utf8, contiguousBytes.ToArray());
            Assert.Equal(utf8, segmentedBytes.ToArray());
        }
    }

    public static TheoryData<byte[], BinaryStatus> Utf8Cases => new()
    {
        { [], BinaryStatus.Done },
        { [(byte)'a'], BinaryStatus.Done },
        { [0xC2, 0xA2], BinaryStatus.Done },
        { [0xE2, 0x82, 0xAC], BinaryStatus.Done },
        { [0xF0, 0x9F, 0x98, 0x80], BinaryStatus.Done },
        { [0xC0, 0x80], BinaryStatus.InvalidUtf8 },
        { [0xE0, 0x80, 0x80], BinaryStatus.InvalidUtf8 },
        { [0xED, 0xA0, 0x80], BinaryStatus.InvalidUtf8 },
        { [0xF4, 0x90, 0x80, 0x80], BinaryStatus.InvalidUtf8 },
        { [0xE2, 0x82], BinaryStatus.InvalidUtf8 },
        { [0x80], BinaryStatus.InvalidUtf8 }
    };

    [Fact]
    public void DefaultLimitsFailFastAtEveryPublicEntryPoint()
    {
        var value = new Fixture(0, 0, 0, []);
        var destination = new byte[32];
        byte[] source = [];
        ReadOnlySequence<byte> sequence = ReadOnlySequence<byte>.Empty;

        Assert.Throws<ArgumentException>(() =>
            BinaryCodec.TryEncode<FixtureEncoder, Fixture>(
                in value,
                destination,
                default,
                out _));
        Assert.Throws<ArgumentException>(() =>
            BinaryCodec.TryDecode<ChecksumDecoder, ulong>(source, default, out _));
        Assert.Throws<ArgumentException>(() =>
            BinaryCodec.TryDecode<ChecksumSequenceDecoder, ulong>(in sequence, default, out _));
        Assert.Throws<ArgumentException>(() =>
        {
            var writer = new BinaryWriteCursor(destination, default);
            _ = writer.Status;
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var reader = new BinaryReadCursor(source, default);
            _ = reader.Status;
        });
        Assert.Throws<ArgumentException>(() =>
        {
            var reader = new BinarySequenceReadCursor(in sequence, default);
            _ = reader.Status;
        });
    }

    [Fact]
    public void ContiguousAndOneByteSegmentedReadersReturnSameStatusForArbitraryInput()
    {
        var random = new Random(0x43_4F_52_45);
        var bytes = new byte[64];

        for (int iteration = 0; iteration < 2_000; iteration++)
        {
            random.NextBytes(bytes);
            int length = random.Next(bytes.Length + 1);
            byte[] payload = bytes.AsSpan(0, length).ToArray();

            BinaryStatus contiguous = BinaryCodec.TryDecode<ChecksumDecoder, ulong>(
                payload,
                Limits,
                out ulong contiguousChecksum);
            ReadOnlySequence<byte> sequence = SegmentEveryByte(payload);
            BinaryStatus segmented = BinaryCodec.TryDecode<ChecksumSequenceDecoder, ulong>(
                in sequence,
                Limits,
                out ulong segmentedChecksum);

            Assert.Equal(contiguous, segmented);
            if (contiguous == BinaryStatus.Done)
            {
                Assert.Equal(contiguousChecksum, segmentedChecksum);
            }
        }
    }

    private static ReadOnlySequence<byte> SegmentEveryByte(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        ByteSegment? first = null;
        ByteSegment? last = null;
        foreach (byte value in bytes)
        {
            var next = new ByteSegment(new byte[] { value });
            if (first is null)
            {
                first = next;
            }
            else
            {
                last!.Append(next);
            }

            last = next;
        }

        return new ReadOnlySequence<byte>(first!, 0, last!, 1);
    }

    private sealed class ByteSegment : ReadOnlySequenceSegment<byte>
    {
        public ByteSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public void Append(ByteSegment next)
        {
            next.RunningIndex = RunningIndex + Memory.Length;
            Next = next;
        }
    }

    private sealed record Fixture(int Id, ulong Count, float Ratio, byte[] Body);

    private readonly struct FixtureEncoder : IBinaryEncoder<FixtureEncoder, Fixture>
    {
        public static BinaryStatus Write(ref BinaryWriteCursor writer, in Fixture value)
        {
            BinaryStatus status = writer.WriteInt32(1, value.Id);
            status = status == BinaryStatus.Done ? writer.WriteUInt64(2, value.Count) : status;
            status = status == BinaryStatus.Done ? writer.WriteSingle(3, value.Ratio) : status;
            return status == BinaryStatus.Done ? writer.WriteBytes(4, value.Body) : status;
        }
    }

    private sealed record CompleteFixture(
        ulong Count,
        uint Fixed32,
        ulong Fixed64,
        string Text,
        byte[] Body);

    private readonly struct CompleteFixtureEncoder : IBinaryEncoder<CompleteFixtureEncoder, CompleteFixture>
    {
        public static BinaryStatus Write(ref BinaryWriteCursor writer, in CompleteFixture value)
        {
            BinaryStatus status = writer.WriteUInt64(1, value.Count);
            status = status == BinaryStatus.Done ? writer.WriteFixed32(2, value.Fixed32) : status;
            status = status == BinaryStatus.Done ? writer.WriteFixed64(3, value.Fixed64) : status;
            status = status == BinaryStatus.Done ? writer.WriteString(4, value.Text) : status;
            return status == BinaryStatus.Done ? writer.WriteBytes(5, value.Body) : status;
        }
    }

    private readonly struct CompleteFixtureSequenceDecoder
        : IBinarySequenceDecoder<CompleteFixtureSequenceDecoder, CompleteFixture>
    {
        public static BinaryStatus Read(
            ref BinarySequenceReadCursor reader,
            out CompleteFixture? value)
        {
            value = default;
            ulong count = 0;
            uint fixed32 = 0;
            ulong fixed64 = 0;
            string text = string.Empty;
            ReadOnlySequence<byte> body = default;
            while (reader.TryReadFieldHeader(out int field, out BinaryWireType wire))
            {
                bool read = field switch
                {
                    1 => reader.TryReadUInt64(wire, out count),
                    2 => reader.TryReadFixed32(wire, out fixed32),
                    3 => reader.TryReadFixed64(wire, out fixed64),
                    4 => TryReadRequiredString(ref reader, wire, out text),
                    5 => reader.TryReadBytes(wire, out body),
                    _ => reader.TrySkip(wire)
                };
                if (!read)
                {
                    return reader.Status;
                }
            }

            if (reader.Status != BinaryStatus.Done)
            {
                return reader.Status;
            }

            value = new CompleteFixture(count, fixed32, fixed64, text, body.ToArray());
            return BinaryStatus.Done;
        }

        private static bool TryReadRequiredString(
            ref BinarySequenceReadCursor reader,
            BinaryWireType wire,
            out string value)
        {
            if (reader.TryReadUtf8(wire, out ReadOnlySequence<byte> decoded))
            {
                value = System.Text.EncodingExtensions.GetString(
                    System.Text.Encoding.UTF8,
                    in decoded);
                return true;
            }

            value = string.Empty;
            return false;
        }
    }

    private readonly struct ChecksumDecoder : IBinaryDecoder<ChecksumDecoder, ulong>
    {
        public static BinaryStatus Read(ref BinaryReadCursor reader, out ulong value)
        {
            value = 17;
            while (reader.TryReadFieldHeader(out int fieldNumber, out BinaryWireType wireType))
            {
                value = unchecked((value * 31) ^ (uint)fieldNumber ^ (byte)wireType);
                if (!reader.TrySkip(wireType))
                {
                    return reader.Status;
                }
            }

            return reader.Status;
        }
    }

    private readonly struct ChecksumSequenceDecoder
        : IBinarySequenceDecoder<ChecksumSequenceDecoder, ulong>
    {
        public static BinaryStatus Read(ref BinarySequenceReadCursor reader, out ulong value)
        {
            value = 17;
            while (reader.TryReadFieldHeader(out int fieldNumber, out BinaryWireType wireType))
            {
                value = unchecked((value * 31) ^ (uint)fieldNumber ^ (byte)wireType);
                if (!reader.TrySkip(wireType))
                {
                    return reader.Status;
                }
            }

            return reader.Status;
        }
    }
}
