using System.Buffers;
using System.Security.Cryptography;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp.Binary;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class BinaryProtocolTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    [Fact]
    public void NewFormatIdentityIsStable()
    {
        Assert.Equal((byte)1, BinaryPayloadFormat.Version);
        Assert.Equal("chatapp-bin-v1", BinaryPayloadFormat.Id);
    }

    [Fact]
    public void GeneratedStaticDecoderRoundTripsAllSeventeenFields()
    {
        BinaryContractFixture expected = BinaryFixtureData.CreateFull();
        byte[] payload = BinaryFixtureData.Encode(expected);

        Assert.Equal(
            BinaryStatus.Done,
            BinaryContractFixtureDescriptor.TryDecode(payload, Limits, out BinaryContractFixture? contiguous));

        ReadOnlySequence<byte> segmented = SequenceFactory.Segmented(payload, 1);
        Assert.Equal(
            BinaryStatus.Done,
            BinaryContractFixtureDescriptor.TryDecode(in segmented, Limits, out BinaryContractFixture? sequence));

        Assert.Equal(BinaryFixtureData.Snapshot(expected), BinaryFixtureData.Snapshot(contiguous!));
        Assert.Equal(BinaryFixtureData.Snapshot(expected), BinaryFixtureData.Snapshot(sequence!));
    }

    [Fact]
    public void FullFixtureHasFrozenBytesAndHash()
    {
        byte[] payload = BinaryFixtureData.Encode(BinaryFixtureData.CreateFull());

        Assert.Equal(
            "085310DDFFFFFFFFFFFFFFFF0118FFFFFFFF0F20012A0D62696E6172792DE6B58BE8AF95320770726573656E743A05000102FEFF4000480451010000000000F8FF58FF0160FF0168FFFF0370FFFF0378FFFFFFFFFFFFFFFFFF0185010100C0FF880104",
            Convert.ToHexString(payload));
        Assert.Equal(
            "9CAF417B6B6CE9D79DE722D94C8609C23AD2AFA5CF8DF5058F09081C6CFC1DB4",
            Convert.ToHexString(SHA256.HashData(payload)));
    }

    [Fact]
    public void NullableAbsenceAndPresentDefaultRemainDistinct()
    {
        var missing = new BinaryContractFixture { Name = "missing" };
        var present = new BinaryContractFixture
        {
            Name = "present",
            Note = string.Empty,
            OptionalNumber = 0,
            OptionalState = FixtureState.Unknown
        };

        byte[] missingPayload = BinaryFixtureData.Encode(missing);
        byte[] presentPayload = BinaryFixtureData.Encode(present);
        Assert.Equal(
            BinaryStatus.Done,
            BinaryContractFixtureDescriptor.TryDecode(missingPayload, Limits, out BinaryContractFixture? missingValue));
        Assert.Equal(
            BinaryStatus.Done,
            BinaryContractFixtureDescriptor.TryDecode(presentPayload, Limits, out BinaryContractFixture? presentValue));

        Assert.Null(missingValue!.Note);
        Assert.Null(missingValue.OptionalNumber);
        Assert.Null(missingValue.OptionalState);
        Assert.Equal(string.Empty, presentValue!.Note);
        Assert.Equal(0, presentValue.OptionalNumber);
        Assert.Equal(FixtureState.Unknown, presentValue.OptionalState);
        Assert.True(presentPayload.Length > missingPayload.Length);
    }

    [Fact]
    public void UnknownAscendingFieldIsSkipped()
    {
        BinaryContractFixture expected = BinaryFixtureData.CreateFull();
        byte[] known = BinaryFixtureData.Encode(expected);
        byte[] payload = new byte[known.Length + 6];
        known.CopyTo(payload, 0);
        // field 99, length-delimited, three-byte body: tag 794 => 0x9A 0x06.
        ReadOnlySpan<byte> unknown = [0x9A, 0x06, 0x03, 0x6E, 0x65, 0x77];
        unknown.CopyTo(payload.AsSpan(known.Length));

        Assert.Equal(
            BinaryStatus.Done,
            BinaryContractFixtureDescriptor.TryDecode(payload, Limits, out BinaryContractFixture? actual));
        Assert.Equal(BinaryFixtureData.Snapshot(expected), BinaryFixtureData.Snapshot(actual!));
    }

    [Fact]
    public void UnknownEnumValuesArePreserved()
    {
        const short state = 12_345;
        const short optionalState = -12_345;
        var expected = new BinaryContractFixture
        {
            State = (FixtureState)state,
            OptionalState = (FixtureState)optionalState
        };
        byte[] payload = BinaryFixtureData.Encode(expected);

        Assert.Equal(
            BinaryStatus.Done,
            BinaryContractFixtureDescriptor.TryDecode(payload, Limits, out BinaryContractFixture? actual));
        Assert.Equal(state, (short)actual!.State);
        Assert.Equal(optionalState, (short)actual.OptionalState!.Value);
    }

    [Fact]
    public void RequiredFieldMustBePresentEvenWhenItsValueWouldBeDefault()
    {
        Assert.Equal(
            BinaryStatus.MissingRequiredField,
            RequiredBinaryContractFixtureDescriptor.TryDecode(
                ReadOnlySpan<byte>.Empty,
                Limits,
                out RequiredBinaryContractFixture? missing));
        Assert.Null(missing);

        var value = new RequiredBinaryContractFixture { RequiredId = 0 };
        Span<byte> payload = stackalloc byte[16];
        Assert.Equal(
            BinaryStatus.Done,
            RequiredBinaryContractFixtureDescriptor.TryEncode(in value, payload, Limits, out int written));
        Assert.Equal(
            BinaryStatus.Done,
            RequiredBinaryContractFixtureDescriptor.TryDecode(
                payload[..written],
                Limits,
                out RequiredBinaryContractFixture? decoded));
        Assert.Equal(0, decoded!.RequiredId);
    }

    [Fact]
    public void EncoderAndDecoderRequireStrictlyIncreasingFields()
    {
        var value = new BinaryContractFixture();
        Span<byte> payload = stackalloc byte[32];

        Assert.Equal(
            BinaryStatus.FieldsOutOfOrder,
            BinaryCodec.TryEncode<OutOfOrderEncoder, BinaryContractFixture>(
                in value,
                payload,
                Limits,
                out int outOfOrderWritten));
        Assert.Equal(0, outOfOrderWritten);
        Assert.Equal(
            BinaryStatus.DuplicateField,
            BinaryCodec.TryEncode<DuplicateEncoder, BinaryContractFixture>(
                in value,
                payload,
                Limits,
                out int duplicateWritten));
        Assert.Equal(0, duplicateWritten);

        Assert.Equal(
            BinaryStatus.FieldsOutOfOrder,
            BinaryContractFixtureDescriptor.TryDecode(
                [0x10, 0x02, 0x08, 0x04],
                Limits,
                out _));
        Assert.Equal(
            BinaryStatus.DuplicateField,
            BinaryContractFixtureDescriptor.TryDecode(
                [0x08, 0x02, 0x08, 0x04],
                Limits,
                out _));
    }

    [Fact]
    public void RawFloatingPointBitsSurviveRoundTrip()
    {
        const uint singleBits = 0xFFC0_0001;
        const ulong doubleBits = 0xFFF8_0000_0000_0001;
        var expected = new BinaryContractFixture
        {
            Score = BitConverter.Int64BitsToDouble(unchecked((long)doubleBits)),
            FloatValue = BitConverter.Int32BitsToSingle(unchecked((int)singleBits))
        };

        byte[] payload = BinaryFixtureData.Encode(expected);
        Assert.Equal(
            BinaryStatus.Done,
            BinaryContractFixtureDescriptor.TryDecode(payload, Limits, out BinaryContractFixture? actual));
        Assert.Equal(doubleBits, unchecked((ulong)BitConverter.DoubleToInt64Bits(actual!.Score)));
        Assert.Equal(singleBits, unchecked((uint)BitConverter.SingleToInt32Bits(actual.FloatValue)));
    }

    [Fact]
    public void EncodeAndDecodeLimitsFailClosed()
    {
        var value = new BinaryContractFixture { Name = "12345", Data = [1, 2, 3, 4, 5] };
        Span<byte> destination = stackalloc byte[256];
        var stringLimited = new BinaryLimits(64, 8, 4, 8, 32);
        var bytesLimited = new BinaryLimits(64, 8, 8, 4, 32);
        var fieldCountLimited = new BinaryLimits(64, 16, 16, 16, 1);

        Assert.Equal(
            BinaryStatus.StringTooLarge,
            BinaryContractFixtureDescriptor.TryEncode(in value, destination, stringLimited, out _));
        value.Name = string.Empty;
        Assert.Equal(
            BinaryStatus.ByteArrayTooLarge,
            BinaryContractFixtureDescriptor.TryEncode(in value, destination, bytesLimited, out _));
        Assert.Equal(
            BinaryStatus.TooManyFields,
            BinaryContractFixtureDescriptor.TryEncode(in value, destination, fieldCountLimited, out _));

        byte[] payload = BinaryFixtureData.Encode(BinaryFixtureData.CreateFull());
        var messageLimited = new BinaryLimits(8, 8, 8, 8, 8);
        Assert.Equal(
            BinaryStatus.MessageTooLarge,
            BinaryContractFixtureDescriptor.TryDecode(payload, messageLimited, out _));
        Assert.Equal(
            BinaryStatus.DestinationTooSmall,
            BinaryContractFixtureDescriptor.TryEncode(
                in value,
                destination[..1],
                Limits,
                out int written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void ReusedSpanEncodeAllocatesZeroBytesPerOperation()
    {
        BinaryContractFixture value = BinaryFixtureData.CreateFull();
        var destination = new byte[256];
        for (var iteration = 0; iteration < 1_024; iteration++)
        {
            _ = BinaryContractFixtureDescriptor.TryEncode(
                in value,
                destination,
                Limits,
                out _);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 10_000; iteration++)
        {
            BinaryStatus status = BinaryContractFixtureDescriptor.TryEncode(
                in value,
                destination,
                Limits,
                out int written);
            if (status != BinaryStatus.Done || written == 0)
            {
                throw new InvalidOperationException("The warmed encoder failed.");
            }
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }

    private readonly struct OutOfOrderEncoder :
        IBinaryEncoder<OutOfOrderEncoder, BinaryContractFixture>
    {
        public static BinaryStatus Write(
            ref BinaryWriteCursor writer,
            in BinaryContractFixture value)
        {
            writer.WriteInt32(2, value.Id);
            return writer.WriteInt32(1, value.Id);
        }
    }

    private readonly struct DuplicateEncoder :
        IBinaryEncoder<DuplicateEncoder, BinaryContractFixture>
    {
        public static BinaryStatus Write(
            ref BinaryWriteCursor writer,
            in BinaryContractFixture value)
        {
            writer.WriteInt32(1, value.Id);
            return writer.WriteInt32(1, value.Id);
        }
    }
}
