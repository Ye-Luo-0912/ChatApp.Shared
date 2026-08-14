using System.Security.Cryptography;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class MessageHistoryResponseBinarySchemaTests
{
    [Fact]
    public void MessageHistoryResponseHasStableGoldenAndSegmentedRoundTrip()
    {
        var expected = new MessageHistoryResponse
        {
            RequestId = "req-1",
            ConversationId = "conv-1",
            Succeeded = true,
            ErrorCode = "none",
            Items =
            [
                new MessageHistoryItem
                {
                    MessageId = "m",
                    ClientMessageId = "c",
                    SenderUserId = 1,
                    ReceiverUserId = 2,
                    Content = "x",
                    ReceivedAtMs = 10,
                    EditVersion = 1,
                    ChangedAtMs = 11
                }
            ],
            NextCursor = new MessageHistoryCursor
            {
                ReceivedAtMs = 10,
                ChangedAtMs = 20,
                MessageId = "cur"
            },
            HasMore = true
        };

        byte[] payload = Encode(expected);

        Assert.Equal(
            "0A057265712D311206636F6E762D31180122046E6F6E6532130A016D120163180220043201783814580268163A09081410281A036375724001",
            Convert.ToHexString(payload));
        Assert.Equal(
            "8CBB100CE8111EF1880E190DD133AE0DF688F6448D310EB178F4D3354620A95E",
            Convert.ToHexString(SHA256.HashData(payload)));

        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryResponseBinaryDescriptor.TryDecode(
                SequenceFactory.Segmented(payload, 1),
                BinaryLimits.Default,
                out MessageHistoryResponse? actual));
        Assert.Equal(expected.RequestId, actual!.RequestId);
        Assert.Equal(expected.Items[0].MessageId, actual.Items[0].MessageId);
        Assert.Equal(expected.NextCursor!.MessageId, actual.NextCursor!.MessageId);
        Assert.Equal(expected.HasMore, actual.HasMore);
    }

    [Fact]
    public void MessageHistoryResponseRejectsDuplicateCursorTruncatedNestedAndCollectionOverflow()
    {
        Assert.Equal(
            BinaryStatus.DuplicateField,
            MessageHistoryResponseBinaryDescriptor.TryDecode(
                [0x3A, 0x00, 0x3A, 0x00],
                BinaryLimits.Default,
                out _));

        Assert.Equal(
            BinaryStatus.Truncated,
            MessageHistoryResponseBinaryDescriptor.TryDecode(
                [0x32, 0x02, 0x0A, 0x01],
                BinaryLimits.Default,
                out _));

        var limited = new BinaryLimits(1024, 256, 128, 128, 64, maxCollectionElements: 1);
        Assert.Equal(
            BinaryStatus.CollectionTooLarge,
            MessageHistoryResponseBinaryDescriptor.TryDecode(
                [0x32, 0x00, 0x32, 0x00],
                limited,
                out _));

        var value = new MessageHistoryResponse
        {
            Items = [new MessageHistoryItem(), new MessageHistoryItem()]
        };
        Assert.Equal(
            BinaryStatus.CollectionTooLarge,
            MessageHistoryResponseBinaryDescriptor.TryEncode(
                in value,
                new byte[1024],
                limited,
                out int written));
        Assert.Equal(0, written);
    }

    private static byte[] Encode(MessageHistoryResponse value)
    {
        byte[] destination = new byte[BinaryLimits.Default.MaxMessageBytes];
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryResponseBinaryDescriptor.TryEncode(
                in value,
                destination,
                BinaryLimits.Default,
                out int written));
        return destination[..written].ToArray();
    }
}
