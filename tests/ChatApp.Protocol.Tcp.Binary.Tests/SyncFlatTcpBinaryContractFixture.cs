using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

internal static class SyncFlatTcpBinaryFieldNumbers
{
    internal static class ConversationListItem
    {
        internal const int ConversationId = 1;
        internal const int Type = 2;
        internal const int PeerUserId = 3;
        internal const int Title = 4;
        internal const int LastMessageId = 5;
        internal const int LastMessagePreview = 6;
        internal const int LastMessageAtMs = 7;
        internal const int LastSenderUserId = 8;
        internal const int UnreadCount = 9;
        internal const int LastReadMessageId = 10;
        internal const int LastReadAtMs = 11;
        internal const int IsPinned = 12;
        internal const int PinnedAtMs = 13;
        internal const int IsMuted = 14;
        internal const int MutedUntilMs = 15;
    }

    internal static class ConversationListCursor
    {
        internal const int IsPinned = 1;
        internal const int PinnedAtMs = 2;
        internal const int LastMessageAtMs = 3;
        internal const int ConversationId = 4;
    }

    internal static class ResetRequired
    {
        internal const int ConversationId = 1;
        internal const int Reason = 2;
        internal const int TipMessageId = 3;
        internal const int TipReceivedAtMs = 4;
        internal const int ClientAfterReceivedAtMs = 5;
        internal const int ClientAfterMessageId = 6;
    }
}

[TcpBinaryContract(typeof(TcpConversationListItem))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.ConversationId, nameof(TcpConversationListItem.ConversationId))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.Type, nameof(TcpConversationListItem.Type))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.PeerUserId, nameof(TcpConversationListItem.PeerUserId))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.Title, nameof(TcpConversationListItem.Title))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.LastMessageId, nameof(TcpConversationListItem.LastMessageId))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.LastMessagePreview, nameof(TcpConversationListItem.LastMessagePreview))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.LastMessageAtMs, nameof(TcpConversationListItem.LastMessageAtMs))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.LastSenderUserId, nameof(TcpConversationListItem.LastSenderUserId))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.UnreadCount, nameof(TcpConversationListItem.UnreadCount))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.LastReadMessageId, nameof(TcpConversationListItem.LastReadMessageId))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.LastReadAtMs, nameof(TcpConversationListItem.LastReadAtMs))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.IsPinned, nameof(TcpConversationListItem.IsPinned))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.PinnedAtMs, nameof(TcpConversationListItem.PinnedAtMs))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.IsMuted, nameof(TcpConversationListItem.IsMuted))]
[TcpBinaryField(SyncFlatTcpBinaryFieldNumbers.ConversationListItem.MutedUntilMs, nameof(TcpConversationListItem.MutedUntilMs))]
internal static partial class TcpConversationListItemBinaryDescriptor
{
    public static BinaryStatus TryEncode(in TcpConversationListItem value, Span<byte> destination, BinaryLimits limits, out int written) =>
        BinaryCodec.TryEncode<TcpConversationListItemBinaryEncoder, TcpConversationListItem>(in value, destination, limits, out written);
}

internal readonly struct TcpConversationListItemBinaryEncoder : IBinaryEncoder<TcpConversationListItemBinaryEncoder, TcpConversationListItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpConversationListItem value)
    {
        writer.WriteString(1, value.ConversationId);
        writer.WriteUInt32(2, (byte)value.Type);
        if (value.PeerUserId is { } peerUserId) writer.WriteInt64(3, peerUserId);
        if (value.Title is { } title) writer.WriteString(4, title);
        if (value.LastMessageId is { } lastMessageId) writer.WriteString(5, lastMessageId);
        if (value.LastMessagePreview is { } lastMessagePreview) writer.WriteString(6, lastMessagePreview);
        if (value.LastMessageAtMs is { } lastMessageAtMs) writer.WriteInt64(7, lastMessageAtMs);
        if (value.LastSenderUserId is { } lastSenderUserId) writer.WriteInt64(8, lastSenderUserId);
        writer.WriteInt32(9, value.UnreadCount);
        if (value.LastReadMessageId is { } lastReadMessageId) writer.WriteString(10, lastReadMessageId);
        if (value.LastReadAtMs is { } lastReadAtMs) writer.WriteInt64(11, lastReadAtMs);
        writer.WriteBool(12, value.IsPinned);
        if (value.PinnedAtMs is { } pinnedAtMs) writer.WriteInt64(13, pinnedAtMs);
        writer.WriteBool(14, value.IsMuted);
        if (value.MutedUntilMs is { } mutedUntilMs) writer.WriteInt64(15, mutedUntilMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpConversationListCursor))]
[TcpBinaryField(1, nameof(TcpConversationListCursor.IsPinned))]
[TcpBinaryField(2, nameof(TcpConversationListCursor.PinnedAtMs))]
[TcpBinaryField(3, nameof(TcpConversationListCursor.LastMessageAtMs))]
[TcpBinaryField(4, nameof(TcpConversationListCursor.ConversationId))]
internal static partial class TcpConversationListCursorBinaryDescriptor
{
    public static BinaryStatus TryEncode(in TcpConversationListCursor value, Span<byte> destination, BinaryLimits limits, out int written) =>
        BinaryCodec.TryEncode<TcpConversationListCursorBinaryEncoder, TcpConversationListCursor>(in value, destination, limits, out written);
}

internal readonly struct TcpConversationListCursorBinaryEncoder : IBinaryEncoder<TcpConversationListCursorBinaryEncoder, TcpConversationListCursor>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpConversationListCursor value)
    {
        writer.WriteBool(1, value.IsPinned);
        if (value.PinnedAtMs is { } pinnedAtMs) writer.WriteInt64(2, pinnedAtMs);
        if (value.LastMessageAtMs is { } lastMessageAtMs) writer.WriteInt64(3, lastMessageAtMs);
        writer.WriteString(4, value.ConversationId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(SyncCursorResetRequired))]
[TcpBinaryField(1, nameof(SyncCursorResetRequired.ConversationId))]
[TcpBinaryField(2, nameof(SyncCursorResetRequired.Reason))]
[TcpBinaryField(3, nameof(SyncCursorResetRequired.TipMessageId))]
[TcpBinaryField(4, nameof(SyncCursorResetRequired.TipReceivedAtMs))]
[TcpBinaryField(5, nameof(SyncCursorResetRequired.ClientAfterReceivedAtMs))]
[TcpBinaryField(6, nameof(SyncCursorResetRequired.ClientAfterMessageId))]
internal static partial class SyncCursorResetRequiredBinaryDescriptor
{
    public static BinaryStatus TryEncode(in SyncCursorResetRequired value, Span<byte> destination, BinaryLimits limits, out int written) =>
        BinaryCodec.TryEncode<SyncCursorResetRequiredBinaryEncoder, SyncCursorResetRequired>(in value, destination, limits, out written);
}

internal readonly struct SyncCursorResetRequiredBinaryEncoder : IBinaryEncoder<SyncCursorResetRequiredBinaryEncoder, SyncCursorResetRequired>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in SyncCursorResetRequired value)
    {
        writer.WriteString(1, value.ConversationId);
        writer.WriteUInt32(2, (byte)value.Reason);
        if (value.TipMessageId is { } tipMessageId) writer.WriteString(3, tipMessageId);
        if (value.TipReceivedAtMs is { } tipReceivedAtMs) writer.WriteInt64(4, tipReceivedAtMs);
        if (value.ClientAfterReceivedAtMs is { } clientAfterReceivedAtMs) writer.WriteInt64(5, clientAfterReceivedAtMs);
        if (value.ClientAfterMessageId is { } clientAfterMessageId) writer.WriteString(6, clientAfterMessageId);
        return writer.Status;
    }
}
