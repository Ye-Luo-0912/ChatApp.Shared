using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

internal static class MessageHistoryResponseBinaryFieldNumbers
{
    internal const int RequestId = 1;
    internal const int ConversationId = 2;
    internal const int Succeeded = 3;
    internal const int ErrorCode = 4;
    internal const int ErrorMessage = 5;
    internal const int Items = 6;
    internal const int NextCursor = 7;
    internal const int HasMore = 8;
}

[TcpBinaryContract(typeof(MessageHistoryResponse))]
[TcpBinaryField(MessageHistoryResponseBinaryFieldNumbers.RequestId, nameof(MessageHistoryResponse.RequestId))]
[TcpBinaryField(MessageHistoryResponseBinaryFieldNumbers.ConversationId, nameof(MessageHistoryResponse.ConversationId))]
[TcpBinaryField(MessageHistoryResponseBinaryFieldNumbers.Succeeded, nameof(MessageHistoryResponse.Succeeded))]
[TcpBinaryField(MessageHistoryResponseBinaryFieldNumbers.ErrorCode, nameof(MessageHistoryResponse.ErrorCode))]
[TcpBinaryField(MessageHistoryResponseBinaryFieldNumbers.ErrorMessage, nameof(MessageHistoryResponse.ErrorMessage))]
[TcpBinaryNestedField(MessageHistoryResponseBinaryFieldNumbers.Items, nameof(MessageHistoryResponse.Items), typeof(MessageHistoryItemBinaryDescriptor))]
[TcpBinaryNestedField(MessageHistoryResponseBinaryFieldNumbers.NextCursor, nameof(MessageHistoryResponse.NextCursor), typeof(MessageHistoryCursorBinaryDescriptor))]
[TcpBinaryField(MessageHistoryResponseBinaryFieldNumbers.HasMore, nameof(MessageHistoryResponse.HasMore))]
internal static partial class MessageHistoryResponseBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in MessageHistoryResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryResponseBinaryEncoder, MessageHistoryResponse>(in value, destination, limits, out written);
}

internal readonly struct MessageHistoryResponseBinaryEncoder : IBinaryEncoder<MessageHistoryResponseBinaryEncoder, MessageHistoryResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryResponse value)
    {
        if (value.RequestId is { } requestId) writer.WriteString(MessageHistoryResponseBinaryFieldNumbers.RequestId, requestId);
        if (value.ConversationId is { } conversationId) writer.WriteString(MessageHistoryResponseBinaryFieldNumbers.ConversationId, conversationId);
        writer.WriteBool(MessageHistoryResponseBinaryFieldNumbers.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode) writer.WriteString(MessageHistoryResponseBinaryFieldNumbers.ErrorCode, errorCode);
        if (value.ErrorMessage is { } errorMessage) writer.WriteString(MessageHistoryResponseBinaryFieldNumbers.ErrorMessage, errorMessage);

        if (value.Items is { } items)
        {
            foreach (MessageHistoryItem item in items)
            {
                if (!writer.TryAddCollectionElement(MessageHistoryResponseBinaryFieldNumbers.Items)) return writer.Status;
                writer.WriteNested<MessageHistoryItemBinaryEncoder, MessageHistoryItem>(MessageHistoryResponseBinaryFieldNumbers.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteNested<MessageHistoryCursorBinaryEncoder, MessageHistoryCursor>(MessageHistoryResponseBinaryFieldNumbers.NextCursor, in nextCursor);
        }

        writer.WriteBool(MessageHistoryResponseBinaryFieldNumbers.HasMore, value.HasMore);
        return writer.Status;
    }
}
