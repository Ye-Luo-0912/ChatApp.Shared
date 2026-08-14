using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

internal static class SyncBootstrapResponseBinaryFieldNumbers
{
    internal static class ConversationHistoryCatchUp
    {
        internal const int ConversationId = 1;
        internal const int Items = 2;
        internal const int HasMore = 3;
        internal const int NextCursor = 4;
    }

    internal static class RelationshipChangeLogEntry
    {
        internal const int Operation = 1;
        internal const int ResourceId = 2;
        internal const int UserId = 3;
        internal const int Status = 4;
        internal const int Message = 5;
        internal const int CreatedAtMs = 6;
        internal const int OccurredAtMs = 7;
    }

    internal static class RelationshipCatchUp
    {
        internal const int ListType = 1;
        internal const int Changes = 2;
        internal const int HasMore = 3;
        internal const int NextCursor = 4;
        internal const int NextSequence = 5;
        internal const int ResetRequired = 6;
        internal const int ErrorCode = 7;
        internal const int ErrorMessage = 8;
    }

    internal static class Response
    {
        internal const int RequestId = 1;
        internal const int Succeeded = 2;
        internal const int ErrorCode = 3;
        internal const int ErrorMessage = 4;
        internal const int ServerTimeMs = 5;
        internal const int Conversations = 6;
        internal const int ConversationsNextCursor = 7;
        internal const int ConversationsHasMore = 8;
        internal const int CatchUps = 9;
        internal const int ResetsRequired = 10;
        internal const int RelationshipCatchUps = 11;
    }
}

[TcpBinaryContract(typeof(ConversationHistoryCatchUp))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.ConversationId, nameof(ConversationHistoryCatchUp.ConversationId))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.Items, nameof(ConversationHistoryCatchUp.Items), typeof(MessageHistoryItemBinaryDescriptor))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.HasMore, nameof(ConversationHistoryCatchUp.HasMore))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.NextCursor, nameof(ConversationHistoryCatchUp.NextCursor), typeof(MessageHistoryCursorBinaryDescriptor))]
internal static partial class ConversationHistoryCatchUpBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in ConversationHistoryCatchUp value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationHistoryCatchUpBinaryEncoder, ConversationHistoryCatchUp>(in value, destination, limits, out written);
}

internal readonly struct ConversationHistoryCatchUpBinaryEncoder : IBinaryEncoder<ConversationHistoryCatchUpBinaryEncoder, ConversationHistoryCatchUp>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationHistoryCatchUp value)
    {
        writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.ConversationId, value.ConversationId);
        if (value.Items is { } items)
        {
            foreach (MessageHistoryItem item in items)
            {
                if (!writer.TryAddCollectionElement(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.Items)) return writer.Status;
                writer.WriteNested<MessageHistoryItemBinaryEncoder, MessageHistoryItem>(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        writer.WriteBool(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.HasMore, value.HasMore);
        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteNested<MessageHistoryCursorBinaryEncoder, MessageHistoryCursor>(SyncBootstrapResponseBinaryFieldNumbers.ConversationHistoryCatchUp.NextCursor, in nextCursor);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RelationshipChangeLogEntry))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.Operation, nameof(RelationshipChangeLogEntry.Operation))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.ResourceId, nameof(RelationshipChangeLogEntry.ResourceId))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.UserId, nameof(RelationshipChangeLogEntry.UserId))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.Status, nameof(RelationshipChangeLogEntry.Status))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.Message, nameof(RelationshipChangeLogEntry.Message))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.CreatedAtMs, nameof(RelationshipChangeLogEntry.CreatedAtMs))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.OccurredAtMs, nameof(RelationshipChangeLogEntry.OccurredAtMs))]
internal static partial class RelationshipChangeLogEntryBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in RelationshipChangeLogEntry value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RelationshipChangeLogEntryBinaryEncoder, RelationshipChangeLogEntry>(in value, destination, limits, out written);
}

internal readonly struct RelationshipChangeLogEntryBinaryEncoder : IBinaryEncoder<RelationshipChangeLogEntryBinaryEncoder, RelationshipChangeLogEntry>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RelationshipChangeLogEntry value)
    {
        writer.WriteUInt32(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.Operation, (byte)value.Operation);
        writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.ResourceId, value.ResourceId);
        writer.WriteInt64(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.UserId, value.UserId);
        if (value.Status is { } status) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.Status, status);
        if (value.Message is { } message) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.Message, message);
        writer.WriteInt64(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.CreatedAtMs, value.CreatedAtMs);
        writer.WriteInt64(SyncBootstrapResponseBinaryFieldNumbers.RelationshipChangeLogEntry.OccurredAtMs, value.OccurredAtMs);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RelationshipCatchUp))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ListType, nameof(RelationshipCatchUp.ListType))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.Changes, nameof(RelationshipCatchUp.Changes), typeof(RelationshipChangeLogEntryBinaryDescriptor))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.HasMore, nameof(RelationshipCatchUp.HasMore))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.NextCursor, nameof(RelationshipCatchUp.NextCursor))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.NextSequence, nameof(RelationshipCatchUp.NextSequence))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ResetRequired, nameof(RelationshipCatchUp.ResetRequired))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ErrorCode, nameof(RelationshipCatchUp.ErrorCode))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ErrorMessage, nameof(RelationshipCatchUp.ErrorMessage))]
internal static partial class RelationshipCatchUpBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in RelationshipCatchUp value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RelationshipCatchUpBinaryEncoder, RelationshipCatchUp>(in value, destination, limits, out written);
}

internal readonly struct RelationshipCatchUpBinaryEncoder : IBinaryEncoder<RelationshipCatchUpBinaryEncoder, RelationshipCatchUp>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RelationshipCatchUp value)
    {
        writer.WriteUInt32(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ListType, (byte)value.ListType);
        if (value.Changes is { } changes)
        {
            foreach (RelationshipChangeLogEntry change in changes)
            {
                if (!writer.TryAddCollectionElement(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.Changes)) return writer.Status;
                writer.WriteNested<RelationshipChangeLogEntryBinaryEncoder, RelationshipChangeLogEntry>(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.Changes, in change, allowRepeatedFieldNumber: true);
            }
        }

        writer.WriteBool(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.HasMore, value.HasMore);
        if (value.NextCursor is { } nextCursor) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.NextCursor, nextCursor);
        writer.WriteInt64(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.NextSequence, value.NextSequence);
        if (value.ResetRequired is { } resetRequired) writer.WriteBool(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ResetRequired, resetRequired);
        if (value.ErrorCode is { } errorCode) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ErrorCode, errorCode);
        if (value.ErrorMessage is { } errorMessage) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.RelationshipCatchUp.ErrorMessage, errorMessage);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(SyncBootstrapResponse))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.Response.RequestId, nameof(SyncBootstrapResponse.RequestId))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.Response.Succeeded, nameof(SyncBootstrapResponse.Succeeded))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.Response.ErrorCode, nameof(SyncBootstrapResponse.ErrorCode))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.Response.ErrorMessage, nameof(SyncBootstrapResponse.ErrorMessage))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.Response.ServerTimeMs, nameof(SyncBootstrapResponse.ServerTimeMs))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.Response.Conversations, nameof(SyncBootstrapResponse.Conversations), typeof(TcpConversationListItemBinaryDescriptor))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.Response.ConversationsNextCursor, nameof(SyncBootstrapResponse.ConversationsNextCursor), typeof(TcpConversationListCursorBinaryDescriptor))]
[TcpBinaryField(SyncBootstrapResponseBinaryFieldNumbers.Response.ConversationsHasMore, nameof(SyncBootstrapResponse.ConversationsHasMore))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.Response.CatchUps, nameof(SyncBootstrapResponse.CatchUps), typeof(ConversationHistoryCatchUpBinaryDescriptor))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.Response.ResetsRequired, nameof(SyncBootstrapResponse.ResetsRequired), typeof(SyncCursorResetRequiredBinaryDescriptor))]
[TcpBinaryNestedField(SyncBootstrapResponseBinaryFieldNumbers.Response.RelationshipCatchUps, nameof(SyncBootstrapResponse.RelationshipCatchUps), typeof(RelationshipCatchUpBinaryDescriptor))]
internal static partial class SyncBootstrapResponseBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in SyncBootstrapResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<SyncBootstrapResponseBinaryEncoder, SyncBootstrapResponse>(in value, destination, limits, out written);
}

internal readonly struct SyncBootstrapResponseBinaryEncoder : IBinaryEncoder<SyncBootstrapResponseBinaryEncoder, SyncBootstrapResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in SyncBootstrapResponse value)
    {
        if (value.RequestId is { } requestId) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.Response.RequestId, requestId);
        writer.WriteBool(SyncBootstrapResponseBinaryFieldNumbers.Response.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.Response.ErrorCode, errorCode);
        if (value.ErrorMessage is { } errorMessage) writer.WriteString(SyncBootstrapResponseBinaryFieldNumbers.Response.ErrorMessage, errorMessage);
        writer.WriteInt64(SyncBootstrapResponseBinaryFieldNumbers.Response.ServerTimeMs, value.ServerTimeMs);

        if (value.Conversations is { } conversations)
        {
            foreach (TcpConversationListItem conversation in conversations)
            {
                if (!writer.TryAddCollectionElement(SyncBootstrapResponseBinaryFieldNumbers.Response.Conversations)) return writer.Status;
                writer.WriteNested<TcpConversationListItemBinaryEncoder, TcpConversationListItem>(SyncBootstrapResponseBinaryFieldNumbers.Response.Conversations, in conversation, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ConversationsNextCursor is { } conversationsNextCursor)
        {
            writer.WriteNested<TcpConversationListCursorBinaryEncoder, TcpConversationListCursor>(SyncBootstrapResponseBinaryFieldNumbers.Response.ConversationsNextCursor, in conversationsNextCursor);
        }

        writer.WriteBool(SyncBootstrapResponseBinaryFieldNumbers.Response.ConversationsHasMore, value.ConversationsHasMore);

        if (value.CatchUps is { } catchUps)
        {
            foreach (ConversationHistoryCatchUp catchUp in catchUps)
            {
                if (!writer.TryAddCollectionElement(SyncBootstrapResponseBinaryFieldNumbers.Response.CatchUps)) return writer.Status;
                writer.WriteNested<ConversationHistoryCatchUpBinaryEncoder, ConversationHistoryCatchUp>(SyncBootstrapResponseBinaryFieldNumbers.Response.CatchUps, in catchUp, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ResetsRequired is { } resets)
        {
            foreach (SyncCursorResetRequired reset in resets)
            {
                if (!writer.TryAddCollectionElement(SyncBootstrapResponseBinaryFieldNumbers.Response.ResetsRequired)) return writer.Status;
                writer.WriteNested<SyncCursorResetRequiredBinaryEncoder, SyncCursorResetRequired>(SyncBootstrapResponseBinaryFieldNumbers.Response.ResetsRequired, in reset, allowRepeatedFieldNumber: true);
            }
        }

        if (value.RelationshipCatchUps is { } relationshipCatchUps)
        {
            foreach (RelationshipCatchUp catchUp in relationshipCatchUps)
            {
                if (!writer.TryAddCollectionElement(SyncBootstrapResponseBinaryFieldNumbers.Response.RelationshipCatchUps)) return writer.Status;
                writer.WriteNested<RelationshipCatchUpBinaryEncoder, RelationshipCatchUp>(SyncBootstrapResponseBinaryFieldNumbers.Response.RelationshipCatchUps, in catchUp, allowRepeatedFieldNumber: true);
            }
        }

        return writer.Status;
    }
}
