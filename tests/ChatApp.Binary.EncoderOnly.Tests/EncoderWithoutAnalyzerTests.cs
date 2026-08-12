using ChatApp.Binary.Core;
using Xunit;

namespace ChatApp.Binary.EncoderOnly.Tests;

public sealed class EncoderWithoutAnalyzerTests
{
    [Fact]
    public void OrdinarySourceEncoderBuildsAndEmitsGoldenWithoutAnalyzer()
    {
        Span<byte> destination = stackalloc byte[64];
        var value = new EncoderOnlyMessage
        {
            Id = -42,
            Body = [1, 2, 3]
        };

        BinaryStatus status = BinaryCodec.TryEncode<EncoderOnlyMessageEncoder, EncoderOnlyMessage>(
            in value,
            destination,
            BinaryLimits.Default,
            out int written);

        Assert.Equal(BinaryStatus.Done, status);
        Assert.Equal(
            [0x08, 0x53, 0x12, 0x03, 0x01, 0x02, 0x03],
            destination[..written].ToArray());
    }

    private sealed class EncoderOnlyMessage
    {
        public int Id { get; init; }

        public byte[] Body { get; init; } = [];
    }

    private readonly struct EncoderOnlyMessageEncoder :
        IBinaryEncoder<EncoderOnlyMessageEncoder, EncoderOnlyMessage>
    {
        public static BinaryStatus Write(
            ref BinaryWriteCursor writer,
            in EncoderOnlyMessage value)
        {
            BinaryStatus status = writer.WriteInt32(1, value.Id);
            return status == BinaryStatus.Done
                ? writer.WriteBytes(2, value.Body)
                : status;
        }
    }
}
