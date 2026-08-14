using System.Buffers;
using System.Security.Cryptography;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class NestedTcpBinarySchemaTests
{
    [Fact]
    public void MessageHistoryItemNestedAndRepeatedSchemaHasStableGoldenAndSegmentedRoundTrip()
    {
        var expected = new MessageHistoryItem
        {
            MessageId = "msg-1",
            ClientMessageId = "client-1",
            SenderUserId = 11,
            ReceiverUserId = 22,
            ConversationId = "conv-1",
            Content = "hello",
            ReceivedAtMs = 1_700_000_000_001,
            DeliveredAtMs = 1_700_000_000_002,
            ReadAtMs = 1_700_000_000_003,
            RecalledAtMs = null,
            EditVersion = 2,
            EditedAtMs = 1_700_000_000_004,
            ChangedAtMs = 1_700_000_000_005,
            Attachments =
            [
                new TcpAttachmentRef
                {
                    AttachmentId = "att-1",
                    ContentType = "image/png",
                    SizeBytes = 123,
                    Status = 2,
                    FileName = "a.png"
                }
            ],
            Reactions = [new MessageReactionSummary { Emoji = "👍", Count = 3, ReactedByMe = true }],
            ReplyToMessageId = "reply-1",
            ReplyToSenderUserId = 33,
            ReplyToPreview = "quoted",
            ForwardedFromMessageId = "forward-1",
            ForwardedFromSenderUserId = 44,
            ForwardedFromPreview = "forwarded",
            MentionedUserIds = [55, 66],
            MentionedRoles = ["admin", "owner"]
        };

        byte[] payload = EncodeMessage(expected);
        Assert.Equal(
            "0A056D73672D311208636C69656E742D311816202C2A06636F6E762D31320568656C6C6F3882A0ABFEF9624084A0ABFEF9624886A0ABFEF96258046088A0ABFEF962688AA0ABFEF9627222080212056174742D311A05612E706E672209696D6167652F706E6728F601300450007A0A0A04F09F918D100618018201077265706C792D3188014292010671756F7465649A0109666F72776172642D31A00158AA0109666F72776172646564B0016EB0018401BA010561646D696EBA01056F776E6572",
            Convert.ToHexString(payload));
        Assert.Equal(
            "7C5AA5B1D493D6EDCA41B5E5A64093FC987A48F49127E55B8B8EC7B90BAE31E2",
            Convert.ToHexString(SHA256.HashData(payload)));

        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryItemBinaryDescriptor.TryDecode(payload, BinaryLimits.Default, out MessageHistoryItem? contiguous));
        ReadOnlySequence<byte> segmented = SequenceFactory.Segmented(payload, 1);
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryItemBinaryDescriptor.TryDecode(in segmented, BinaryLimits.Default, out MessageHistoryItem? segmentedValue));

        AssertMessageEqual(expected, contiguous!);
        AssertMessageEqual(expected, segmentedValue!);
    }

    [Fact]
    public void SyncBootstrapRequestNestedWatermarksRoundTripAndCollectionLimitFailsClosed()
    {
        var expected = new SyncBootstrapRequest
        {
            RequestId = "sync-1",
            ListLimit = 50,
            HistoryLimitPerConversation = 20,
            MaxConversationsWithHistory = 10,
            Watermarks = [new ConversationSyncWatermark { ConversationId = "conv-1", AfterReceivedAtMs = 10, AfterMessageId = "msg-1" }],
            RelationshipWatermarks = [new RelationshipSyncWatermark { ListType = TcpRelationshipListType.Friends, AfterSequence = 12 }],
            RelationshipListLimit = 20
        };

        byte[] payload = EncodeSync(expected);
        Assert.Equal(
            BinaryStatus.Done,
            SyncBootstrapRequestBinaryDescriptor.TryDecode(
                SequenceFactory.Segmented(payload, 1),
                BinaryLimits.Default,
                out SyncBootstrapRequest? actual));
        Assert.Equal(expected.RequestId, actual!.RequestId);
        Assert.Equal(expected.Watermarks![0].ConversationId, actual.Watermarks![0].ConversationId);
        Assert.Equal(expected.RelationshipWatermarks![0].AfterSequence, actual.RelationshipWatermarks![0].AfterSequence);

        var limited = new BinaryLimits(1024, 256, 128, 128, 64, maxCollectionElements: 1);
        var tooMany = new MessageHistoryItem
        {
            MessageId = "m",
            ClientMessageId = "c",
            Content = "x",
            Attachments = [new TcpAttachmentRef(), new TcpAttachmentRef()]
        };
        Assert.Equal(
            BinaryStatus.CollectionTooLarge,
            MessageHistoryItemBinaryDescriptor.TryEncode(in tooMany, new byte[1024], limited, out int written));
        Assert.Equal(0, written);
        Assert.Equal(
            BinaryStatus.CollectionTooLarge,
            MessageHistoryItemBinaryDescriptor.TryDecode(
                [0x0A, 0x01, 0x6D, 0x12, 0x01, 0x63, 0x2A, 0x01, 0x78, 0x72, 0x00, 0x72, 0x00],
                limited,
                out _));

        var materializedLimited = new BinaryLimits(
            1024,
            256,
            128,
            128,
            64,
            maxMaterializedBytes: 5);
        Assert.Equal(
            BinaryStatus.MaterializedBytesTooLarge,
            MessageHistoryItemBinaryDescriptor.TryDecode(
                [0x0A, 0x06, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66],
                materializedLimited,
                out _));

        var depthLimited = new BinaryLimits(
            1024,
            256,
            128,
            128,
            64,
            maxNestingDepth: 1);
        Assert.Equal(
            BinaryStatus.NestingTooDeep,
            MessageHistoryItemBinaryDescriptor.TryDecode(
                [0x72, 0x00],
                depthLimited,
                out _));
    }

    private static byte[] EncodeMessage(MessageHistoryItem value)
    {
        byte[] destination = new byte[BinaryLimits.Default.MaxMessageBytes];
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryItemBinaryDescriptor.TryEncode(in value, destination, BinaryLimits.Default, out int written));
        return destination[..written].ToArray();
    }

    private static byte[] EncodeSync(SyncBootstrapRequest value)
    {
        byte[] destination = new byte[BinaryLimits.Default.MaxMessageBytes];
        Assert.Equal(
            BinaryStatus.Done,
            SyncBootstrapRequestBinaryDescriptor.TryEncode(in value, destination, BinaryLimits.Default, out int written));
        return destination[..written].ToArray();
    }

    private static void AssertMessageEqual(MessageHistoryItem expected, MessageHistoryItem actual)
    {
        Assert.Equal(expected.MessageId, actual.MessageId);
        Assert.Equal(expected.ClientMessageId, actual.ClientMessageId);
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.ReceivedAtMs, actual.ReceivedAtMs);
        Assert.Equal(expected.Attachments?.Select(item => item.AttachmentId), actual.Attachments?.Select(item => item.AttachmentId));
        Assert.Equal(expected.Reactions?.Select(item => (item.Emoji, item.Count, item.ReactedByMe)), actual.Reactions?.Select(item => (item.Emoji, item.Count, item.ReactedByMe)));
        Assert.Equal(expected.MentionedUserIds, actual.MentionedUserIds);
        Assert.Equal(expected.MentionedRoles, actual.MentionedRoles);
    }
}
