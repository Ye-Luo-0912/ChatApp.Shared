using System.Buffers;
using ChatApp.Shared.Protocol.Tcp.Binary;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class GeneratedCodecTests
{
    [Fact]
    public void SourceGeneratedCodecRoundTripsSupportedTypes()
    {
        var expected = new BinaryContractFixture
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
            Score = Math.PI,
            SByteValue = sbyte.MinValue,
            ByteValue = byte.MaxValue,
            ShortValue = short.MinValue,
            UShortValue = ushort.MaxValue,
            ULongValue = ulong.MaxValue,
            FloatValue = 1.25F,
            OptionalState = FixtureState.Active
        };

        byte[] payload = Encode(expected);

        Assert.True(BinaryContractFixtureDescriptor.Codec.TryDecode(
            new ReadOnlySequence<byte>(payload),
            out BinaryContractFixture? actual,
            out TcpBinaryDecodeError error));
        Assert.Equal(TcpBinaryDecodeError.None, error);
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Delta, actual.Delta);
        Assert.Equal(expected.Count, actual.Count);
        Assert.Equal(expected.Enabled, actual.Enabled);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Note, actual.Note);
        Assert.Equal(expected.Data, actual.Data);
        Assert.Equal(expected.OptionalNumber, actual.OptionalNumber);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.Score, actual.Score);
        Assert.Equal(expected.SByteValue, actual.SByteValue);
        Assert.Equal(expected.ByteValue, actual.ByteValue);
        Assert.Equal(expected.ShortValue, actual.ShortValue);
        Assert.Equal(expected.UShortValue, actual.UShortValue);
        Assert.Equal(expected.ULongValue, actual.ULongValue);
        Assert.Equal(expected.FloatValue, actual.FloatValue);
        Assert.Equal(expected.OptionalState, actual.OptionalState);
    }

    [Fact]
    public void NullableNullAndPresentDefaultRemainDistinct()
    {
        var missing = new BinaryContractFixture { Name = "missing" };
        var presentDefault = new BinaryContractFixture
        {
            Name = "present",
            Note = string.Empty,
            OptionalNumber = 0
        };

        byte[] missingPayload = Encode(missing);
        byte[] presentPayload = Encode(presentDefault);

        Assert.True(BinaryContractFixtureDescriptor.Codec.TryDecode(
            new ReadOnlySequence<byte>(missingPayload),
            out BinaryContractFixture? missingResult,
            out _));
        Assert.True(BinaryContractFixtureDescriptor.Codec.TryDecode(
            new ReadOnlySequence<byte>(presentPayload),
            out BinaryContractFixture? presentResult,
            out _));

        Assert.Null(missingResult!.Note);
        Assert.Null(missingResult.OptionalNumber);
        Assert.Equal(string.Empty, presentResult!.Note);
        Assert.Equal(0, presentResult.OptionalNumber);
        Assert.True(presentPayload.Length > missingPayload.Length);
    }

    [Fact]
    public void DecoderReadsEveryByteAcrossSequenceSegments()
    {
        var expected = new BinaryContractFixture
        {
            Id = 123,
            Name = "segmented",
            Note = "跨段",
            Data = Enumerable.Range(0, 128).Select(static value => (byte)value).ToArray()
        };
        ReadOnlySequence<byte> segmented = SequenceFactory.OneByteSegments(Encode(expected));

        Assert.True(BinaryContractFixtureDescriptor.Codec.TryDecode(
            segmented,
            out BinaryContractFixture? actual,
            out TcpBinaryDecodeError error));
        Assert.Equal(TcpBinaryDecodeError.None, error);
        Assert.Equal(expected.Name, actual!.Name);
        Assert.Equal(expected.Note, actual.Note);
        Assert.Equal(expected.Data, actual.Data);
    }

    [Fact]
    public void DecoderSkipsUnknownFieldsIncludingAcrossSegments()
    {
        var destination = new ArrayBufferWriter<byte>();
        BinaryContractFixtureDescriptor.Codec.Encode(
            destination,
            new BinaryContractFixture { Id = 17, Name = "known" });
        var writer = new TaggedBinaryWriter(destination, TcpBinaryLimits.Default);
        writer.WriteString(99, "future-field");

        Assert.True(BinaryContractFixtureDescriptor.Codec.TryDecode(
            SequenceFactory.OneByteSegments(destination.WrittenMemory.ToArray()),
            out BinaryContractFixture? actual,
            out TcpBinaryDecodeError error));
        Assert.Equal(TcpBinaryDecodeError.None, error);
        Assert.Equal(17, actual!.Id);
        Assert.Equal("known", actual.Name);
    }

    [Fact]
    public void EncodingIntoReusedBufferDoesNotAllocatePerOperation()
    {
        var destination = new ArrayBufferWriter<byte>(256);
        var value = new BinaryContractFixture
        {
            Id = 7,
            Delta = 8,
            Count = 9,
            Enabled = true,
            State = FixtureState.Active,
            Score = 1.25
        };

        for (var index = 0; index < 128; index++)
        {
            destination.Clear();
            BinaryContractFixtureDescriptor.Codec.Encode(destination, value);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            destination.Clear();
            BinaryContractFixtureDescriptor.Codec.Encode(destination, value);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allocated <= 1_024, $"Expected a reusable writer path, but encoded loop allocated {allocated} bytes.");
    }

    private static byte[] Encode(BinaryContractFixture value)
    {
        var destination = new ArrayBufferWriter<byte>();
        BinaryContractFixtureDescriptor.Codec.Encode(destination, value);
        return destination.WrittenMemory.ToArray();
    }
}

internal static class SequenceFactory
{
    public static ReadOnlySequence<byte> OneByteSegments(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        ByteSegment? first = null;
        ByteSegment? last = null;
        foreach (byte value in bytes)
        {
            var next = new ByteSegment(new[] { value });
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

        return new ReadOnlySequence<byte>(first!, 0, last!, last!.Memory.Length);
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
}
