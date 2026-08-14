using System.Security.Cryptography;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class SyncBootstrapResponseBinarySchemaTests
{
    [Fact]
    public void SyncBootstrapResponseHasStableGoldenAndSegmentedRoundTrip()
    {
        var expected = new SyncBootstrapResponse
        {
            RequestId = "sync-1",
            Succeeded = true,
            ServerTimeMs = 100,
            Conversations =
            [
                new TcpConversationListItem
                {
                    ConversationId = "conv-1",
                    Type = TcpConversationType.Direct,
                    PeerUserId = 42,
                    UnreadCount = 2
                }
            ],
            ConversationsNextCursor = new TcpConversationListCursor
            {
                IsPinned = true,
                LastMessageAtMs = 10,
                ConversationId = "conv-1"
            },
            ConversationsHasMore = true,
            CatchUps =
            [
                new ConversationHistoryCatchUp
                {
                    ConversationId = "conv-1",
                    Items =
                    [
                        new MessageHistoryItem
                        {
                            MessageId = "m-1",
                            ClientMessageId = "c-1",
                            Content = "hello",
                            ReceivedAtMs = 10,
                            ChangedAtMs = 10
                        }
                    ],
                    HasMore = false,
                    NextCursor = new MessageHistoryCursor { ReceivedAtMs = 10, MessageId = "m-1" }
                }
            ],
            ResetsRequired =
            [
                new SyncCursorResetRequired
                {
                    ConversationId = "conv-2",
                    Reason = TcpSyncCursorResetReason.GapTooLarge,
                    TipMessageId = "tip-2"
                }
            ],
            RelationshipCatchUps =
            [
                new RelationshipCatchUp
                {
                    ListType = TcpRelationshipListType.Friends,
                    Changes =
                    [
                        new RelationshipChangeLogEntry
                        {
                            Operation = TcpRelationshipChangeOperation.Upsert,
                            ResourceId = "friend-1",
                            UserId = 42,
                            Status = "Accepted",
                            CreatedAtMs = 1,
                            OccurredAtMs = 2
                        }
                    ],
                    HasMore = false,
                    NextSequence = 2
                }
            ]
        };

        byte[] payload = Encode(expected);
        Assert.Equal(
            "0A0673796E632D31100128C80132120A06636F6E762D31100118544804600070003A0C080118142206636F6E762D3140014A300A06636F6E762D31121B0A036D2D311203632D3118002000320568656C6C6F3814580268141800220708141A036D2D3152110A06636F6E762D3210041A057469702D325A240801121C08001208667269656E642D311854220841636365707465643002380418002804",
            Convert.ToHexString(payload));
        Assert.Equal(
            "63E339A58008B3F008A4135B828A5140A74A591C1B36008A12189904F32AB2A2",
            Convert.ToHexString(SHA256.HashData(payload)));

        Assert.Equal(
            BinaryStatus.Done,
            SyncBootstrapResponseBinaryDescriptor.TryDecode(
                SequenceFactory.Segmented(payload, 1),
                BinaryLimits.Default,
                out SyncBootstrapResponse? actual));
        Assert.Equal(expected.RequestId, actual!.RequestId);
        Assert.Equal(expected.Conversations[0].ConversationId, actual.Conversations[0].ConversationId);
        Assert.Equal(expected.CatchUps[0].Items[0].MessageId, actual.CatchUps[0].Items[0].MessageId);
        Assert.Equal(expected.ResetsRequired[0].Reason, actual.ResetsRequired[0].Reason);
        Assert.Equal(expected.RelationshipCatchUps![0].Changes[0].ResourceId, actual.RelationshipCatchUps![0].Changes[0].ResourceId);
    }

    [Fact]
    public void SyncBootstrapResponseRejectsRepeatedOrdinaryFieldsAndCollectionOverflow()
    {
        Assert.Equal(
            BinaryStatus.DuplicateField,
            SyncBootstrapResponseBinaryDescriptor.TryDecode(
                [0x10, 0x01, 0x10, 0x00],
                BinaryLimits.Default,
                out _));

        var limited = new BinaryLimits(4096, 512, 256, 256, 128, maxCollectionElements: 1);
        Assert.Equal(
            BinaryStatus.CollectionTooLarge,
            SyncBootstrapResponseBinaryDescriptor.TryDecode(
                [0x32, 0x00, 0x32, 0x00],
                limited,
                out _));

        var value = new SyncBootstrapResponse
        {
            Conversations = [new TcpConversationListItem(), new TcpConversationListItem()]
        };
        Assert.Equal(
            BinaryStatus.CollectionTooLarge,
            SyncBootstrapResponseBinaryDescriptor.TryEncode(
                in value,
                new byte[4096],
                limited,
                out int written));
        Assert.Equal(0, written);
    }

    private static byte[] Encode(SyncBootstrapResponse value)
    {
        byte[] destination = new byte[BinaryLimits.Default.MaxMessageBytes];
        Assert.Equal(
            BinaryStatus.Done,
            SyncBootstrapResponseBinaryDescriptor.TryEncode(
                in value,
                destination,
                BinaryLimits.Default,
                out int written));
        return destination[..written].ToArray();
    }
}
