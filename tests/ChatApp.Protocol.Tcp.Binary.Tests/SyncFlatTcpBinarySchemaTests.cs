using System.Security.Cryptography;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class SyncFlatTcpBinarySchemaTests
{
    [Fact]
    public void SyncFlatPayloadsHaveStableGoldenAndRoundTrip()
    {
        var item = new TcpConversationListItem
        {
            ConversationId = "conv-1",
            Type = TcpConversationType.Group,
            PeerUserId = 42,
            Title = "team",
            LastMessageId = "msg-1",
            LastMessagePreview = "hello",
            LastMessageAtMs = 1_700_000_000_100,
            LastSenderUserId = 43,
            UnreadCount = 2,
            LastReadMessageId = "msg-0",
            LastReadAtMs = 1_700_000_000_000,
            IsPinned = true,
            PinnedAtMs = 1_700_000_000_010,
            IsMuted = true,
            MutedUntilMs = 1_700_000_100_000
        };
        var cursor = new TcpConversationListCursor
        {
            IsPinned = true,
            PinnedAtMs = 10,
            LastMessageAtMs = 20,
            ConversationId = "conv-1"
        };
        var reset = new SyncCursorResetRequired
        {
            ConversationId = "conv-1",
            Reason = TcpSyncCursorResetReason.GapTooLarge,
            TipMessageId = "tip-1",
            TipReceivedAtMs = 30,
            ClientAfterReceivedAtMs = 20,
            ClientAfterMessageId = "client-1"
        };

        byte[] itemPayload = Encode(item, TcpConversationListItemBinaryDescriptor.TryEncode);
        byte[] cursorPayload = Encode(cursor, TcpConversationListCursorBinaryDescriptor.TryEncode);
        byte[] resetPayload = Encode(reset, SyncCursorResetRequiredBinaryDescriptor.TryEncode);

        Assert.Equal("0A06636F6E762D311002185422047465616D2A056D73672D31320568656C6C6F38C8A1ABFEF9624056480452056D73672D305880A0ABFEF96260016894A0ABFEF962700178C0BAB7FEF962", Convert.ToHexString(itemPayload));
        Assert.Equal("45041FAF55679FFCC6F00941E0A7A413A00A0AC094664B9A626AFE51CFA1F7C4", Convert.ToHexString(SHA256.HashData(itemPayload)));
        Assert.Equal("0801101418282206636F6E762D31", Convert.ToHexString(cursorPayload));
        Assert.Equal("8D9B78C75F7A43DAA6A0449A309F91BFDCC7B17DC4B063AC69A6206FD55AD0DD", Convert.ToHexString(SHA256.HashData(cursorPayload)));
        Assert.Equal("0A06636F6E762D3110041A057469702D31203C28283208636C69656E742D31", Convert.ToHexString(resetPayload));
        Assert.Equal("F4226F00547C6AF7601E87BA92215E3450B04EA1EA52E4883DF113FA93836784", Convert.ToHexString(SHA256.HashData(resetPayload)));

        Assert.Equal(BinaryStatus.Done, TcpConversationListItemBinaryDescriptor.TryDecode(SequenceFactory.Segmented(itemPayload, 1), BinaryLimits.Default, out TcpConversationListItem? itemActual));
        Assert.Equal(BinaryStatus.Done, TcpConversationListCursorBinaryDescriptor.TryDecode(SequenceFactory.Segmented(cursorPayload, 1), BinaryLimits.Default, out TcpConversationListCursor? cursorActual));
        Assert.Equal(BinaryStatus.Done, SyncCursorResetRequiredBinaryDescriptor.TryDecode(SequenceFactory.Segmented(resetPayload, 1), BinaryLimits.Default, out SyncCursorResetRequired? resetActual));
        Assert.Equal(item.ConversationId, itemActual!.ConversationId);
        Assert.Equal(item.UnreadCount, itemActual.UnreadCount);
        Assert.Equal(cursor.ConversationId, cursorActual!.ConversationId);
        Assert.Equal(reset.Reason, resetActual!.Reason);
        Assert.Equal(reset.ClientAfterMessageId, resetActual.ClientAfterMessageId);
    }

    [Fact]
    public void SyncFlatPayloadsRejectMalformedValuesAndOversizedStrings()
    {
        Assert.Equal(BinaryStatus.InvalidUtf8, TcpConversationListItemBinaryDescriptor.TryDecode([0x0A, 0x01, 0xFF], BinaryLimits.Default, out _));
        Assert.Equal(BinaryStatus.FieldsOutOfOrder, TcpConversationListCursorBinaryDescriptor.TryDecode([0x22, 0x00, 0x08, 0x00], BinaryLimits.Default, out _));
        Assert.Equal(BinaryStatus.Truncated, SyncCursorResetRequiredBinaryDescriptor.TryDecode([0x0A, 0x01, 0x78, 0x10], BinaryLimits.Default, out _));

        var limited = new BinaryLimits(256, 32, 4, 16, 32);
        var value = new TcpConversationListItem { ConversationId = "12345" };
        Assert.Equal(BinaryStatus.StringTooLarge, TcpConversationListItemBinaryDescriptor.TryEncode(in value, new byte[256], limited, out int written));
        Assert.Equal(0, written);
    }

    private static byte[] Encode<T>(T value, EncodeDelegate<T> encode)
    {
        byte[] destination = new byte[BinaryLimits.Default.MaxMessageBytes];
        Assert.Equal(BinaryStatus.Done, encode(in value, destination, BinaryLimits.Default, out int written));
        return destination[..written].ToArray();
    }

    private delegate BinaryStatus EncodeDelegate<T>(in T value, Span<byte> destination, BinaryLimits limits, out int written);
}
