using System.Buffers;
using ChatApp.Binary.Core;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class BinaryDecoderDifferentialTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    [Fact]
    public void ContiguousAndOneByteSegmentedPathsHaveIdenticalStatusAndValue()
    {
        BinaryContractFixture valid = BinaryFixtureData.CreateFull();
        byte[] validPayload = BinaryFixtureData.Encode(valid);
        byte[] withUnknown = [.. validPayload, 0x9A, 0x06, 0x01, 0x2A];
        const short unknownEnum = 12_345;
        var enumValue = new BinaryContractFixture { State = (FixtureState)unknownEnum };

        DecodeCase[] cases =
        [
            new("valid", validPayload, Limits, BinaryStatus.Done, BinaryFixtureData.Snapshot(valid)),
            new("unknown", withUnknown, Limits, BinaryStatus.Done, BinaryFixtureData.Snapshot(valid)),
            new(
                "unknown-enum",
                BinaryFixtureData.Encode(enumValue),
                Limits,
                BinaryStatus.Done,
                BinaryFixtureData.Snapshot(enumValue)),
            Failure("invalid-field", [0x00], BinaryStatus.InvalidFieldNumber),
            Failure("unsupported-wire", [0x0B], BinaryStatus.UnsupportedWireType),
            Failure("nonminimal-tag", [0x88, 0x00], BinaryStatus.MalformedVarInt),
            Failure(
                "overflow-varint",
                [0x08, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02],
                BinaryStatus.MalformedVarInt),
            Failure("truncated-varint", [0x08, 0x80], BinaryStatus.Truncated),
            Failure("truncated-fixed32", [0x85, 0x01, 0x01, 0x02, 0x03], BinaryStatus.Truncated),
            Failure("wrong-wire", [0x0A, 0x00], BinaryStatus.WireTypeMismatch),
            Failure("duplicate", [0x08, 0x02, 0x08, 0x04], BinaryStatus.DuplicateField),
            Failure("out-of-order", [0x10, 0x02, 0x08, 0x04], BinaryStatus.FieldsOutOfOrder),
            Failure("invalid-utf8", [0x2A, 0x01, 0xFF], BinaryStatus.InvalidUtf8),
            Failure(
                "message-limit",
                [0x08, 0x00],
                BinaryStatus.MessageTooLarge,
                new BinaryLimits(1, 1, 1, 1, 1)),
            Failure(
                "field-count",
                [0x08, 0x00, 0x10, 0x00],
                BinaryStatus.TooManyFields,
                new BinaryLimits(8, 8, 8, 8, 1)),
            Failure(
                "string-limit",
                [0x2A, 0x05, 0x31, 0x32, 0x33, 0x34, 0x35],
                BinaryStatus.StringTooLarge,
                new BinaryLimits(8, 8, 4, 8, 1)),
            // Unknown field 2 keeps RequiredId absent while forcing a real one-byte-per-segment path.
            Failure("required", [0x10, 0x00], BinaryStatus.MissingRequiredField, required: true)
        ];

        foreach (DecodeCase testCase in cases)
        {
            DecodeResult contiguous = Decode(testCase.Payload, testCase.Limits, segmented: false, testCase.Required);
            DecodeResult segmented = Decode(testCase.Payload, testCase.Limits, segmented: true, testCase.Required);
            Assert.Equal(testCase.ExpectedStatus, contiguous.Status);
            Assert.Equal(testCase.ExpectedValue, contiguous.Value);
            Assert.Equal(contiguous, segmented);
        }
    }

    [Fact]
    public void EveryGoldenPrefixHasIdenticalContiguousAndOneByteSegmentedResult()
    {
        byte[] payload = BinaryFixtureData.Encode(BinaryFixtureData.CreateFull());

        for (var length = 0; length <= payload.Length; length++)
        {
            byte[] prefix = payload.AsSpan(0, length).ToArray();
            DecodeResult contiguous = Decode(prefix, Limits, segmented: false, required: false);
            DecodeResult segmented = Decode(prefix, Limits, segmented: true, required: false);
            Assert.True(
                contiguous == segmented,
                $"prefix {length}: contiguous={contiguous}, segmented={segmented}");
        }
    }

    [Fact]
    public void DeterministicFuzzNeverThrowsAndBothPathsAgree()
    {
        var random = new Random(0x42_49_4E_31);
        var payload = new byte[96];

        for (var iteration = 0; iteration < 10_000; iteration++)
        {
            random.NextBytes(payload);
            int length = random.Next(payload.Length + 1);
            byte[] input = payload.AsSpan(0, length).ToArray();
            DecodeResult contiguous = Decode(input, Limits, segmented: false, required: false);
            DecodeResult segmented = Decode(input, Limits, segmented: true, required: false, segmentSize: 7);
            Assert.True(
                contiguous == segmented,
                $"fuzz {iteration}: contiguous={contiguous}, segmented={segmented}");
        }
    }

    private static DecodeResult Decode(
        byte[] payload,
        BinaryLimits limits,
        bool segmented,
        bool required,
        int segmentSize = 1)
    {
        BinaryStatus status;
        if (required)
        {
            if (segmented)
            {
                ReadOnlySequence<byte> sequence = SequenceFactory.Segmented(payload, segmentSize);
                status = RequiredBinaryContractFixtureDescriptor.TryDecode(
                    in sequence,
                    limits,
                    out RequiredBinaryContractFixture? value);
                return new DecodeResult(status, value is null ? null : value.RequiredId.ToString());
            }

            status = RequiredBinaryContractFixtureDescriptor.TryDecode(
                payload,
                limits,
                out RequiredBinaryContractFixture? contiguousValue);
            return new DecodeResult(status, contiguousValue is null ? null : contiguousValue.RequiredId.ToString());
        }

        if (segmented)
        {
            ReadOnlySequence<byte> sequence = SequenceFactory.Segmented(payload, segmentSize);
            status = BinaryContractFixtureDescriptor.TryDecode(
                in sequence,
                limits,
                out BinaryContractFixture? value);
            return new DecodeResult(
                status,
                status == BinaryStatus.Done ? BinaryFixtureData.Snapshot(value!) : null);
        }

        status = BinaryContractFixtureDescriptor.TryDecode(
            payload,
            limits,
            out BinaryContractFixture? contiguous);
        return new DecodeResult(
            status,
            status == BinaryStatus.Done ? BinaryFixtureData.Snapshot(contiguous!) : null);
    }

    private static DecodeCase Failure(
        string name,
        byte[] payload,
        BinaryStatus status,
        BinaryLimits? limits = null,
        bool required = false) =>
        new(name, payload, limits ?? Limits, status, ExpectedValue: null, required);

    private sealed record DecodeCase(
        string Name,
        byte[] Payload,
        BinaryLimits Limits,
        BinaryStatus ExpectedStatus,
        object? ExpectedValue,
        bool Required = false);

    private sealed record DecodeResult(BinaryStatus Status, object? Value);
}

internal static class SequenceFactory
{
    public static ReadOnlySequence<byte> Segmented(byte[] payload, int segmentSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentSize);
        if (payload.Length == 0)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        ByteSegment first = new(payload.AsMemory(0, Math.Min(segmentSize, payload.Length)));
        ByteSegment last = first;
        for (var offset = first.Memory.Length; offset < payload.Length; offset += segmentSize)
        {
            int length = Math.Min(segmentSize, payload.Length - offset);
            last = last.Append(new ByteSegment(payload.AsMemory(offset, length)));
        }

        if (ReferenceEquals(first, last))
        {
            last = last.Append(new ByteSegment(ReadOnlyMemory<byte>.Empty));
        }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private sealed class ByteSegment : ReadOnlySequenceSegment<byte>
    {
        public ByteSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public ByteSegment Append(ByteSegment next)
        {
            next.RunningIndex = RunningIndex + Memory.Length;
            Next = next;
            return next;
        }
    }
}

internal static class BinaryFixtureData
{
    public static BinaryContractFixture CreateFull() => new()
    {
        Id = -42,
        Delta = long.MinValue + 17,
        Count = uint.MaxValue,
        Enabled = true,
        Name = "binary-测试",
        Note = "present",
        Data = [0, 1, 2, 0xFE, 0xFF],
        OptionalNumber = 0,
        State = FixtureState.Active,
        Score = BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8_0000_0000_0001UL)),
        SByteValue = sbyte.MinValue,
        ByteValue = byte.MaxValue,
        ShortValue = short.MinValue,
        UShortValue = ushort.MaxValue,
        ULongValue = ulong.MaxValue,
        FloatValue = BitConverter.Int32BitsToSingle(unchecked((int)0xFFC0_0001U)),
        OptionalState = FixtureState.Active
    };

    public static byte[] Encode(BinaryContractFixture value)
    {
        var destination = new byte[256];
        BinaryStatus status = BinaryContractFixtureDescriptor.TryEncode(
            in value,
            destination,
            BinaryLimits.Default,
            out int written);
        if (status != BinaryStatus.Done)
        {
            throw new InvalidOperationException($"Fixture encoding failed: {status}.");
        }

        return destination.AsSpan(0, written).ToArray();
    }

    public static FixtureSnapshot Snapshot(BinaryContractFixture value) => new(
        value.Id,
        value.Delta,
        value.Count,
        value.Enabled,
        value.Name,
        value.Note,
        Convert.ToHexString(value.Data),
        value.OptionalNumber,
        (short)value.State,
        unchecked((ulong)BitConverter.DoubleToInt64Bits(value.Score)),
        value.SByteValue,
        value.ByteValue,
        value.ShortValue,
        value.UShortValue,
        value.ULongValue,
        unchecked((uint)BitConverter.SingleToInt32Bits(value.FloatValue)),
        value.OptionalState is { } state ? (short)state : null);
}

internal sealed record FixtureSnapshot(
    int Id,
    long Delta,
    uint Count,
    bool Enabled,
    string Name,
    string? Note,
    string DataHex,
    int? OptionalNumber,
    short State,
    ulong ScoreBits,
    sbyte SByteValue,
    byte ByteValue,
    short ShortValue,
    ushort UShortValue,
    ulong ULongValue,
    uint FloatBits,
    short? OptionalState);
