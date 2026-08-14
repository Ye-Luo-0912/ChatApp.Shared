using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

[TcpBinaryContract(typeof(TcpAttachmentRef))]
[TcpBinaryField(1, nameof(TcpAttachmentRef.RefVersion))]
[TcpBinaryField(2, nameof(TcpAttachmentRef.AttachmentId))]
[TcpBinaryField(3, nameof(TcpAttachmentRef.FileName))]
[TcpBinaryField(4, nameof(TcpAttachmentRef.ContentType))]
[TcpBinaryField(5, nameof(TcpAttachmentRef.SizeBytes))]
[TcpBinaryField(6, nameof(TcpAttachmentRef.Status))]
[TcpBinaryField(7, nameof(TcpAttachmentRef.DownloadApiHint))]
[TcpBinaryField(8, nameof(TcpAttachmentRef.DownloadToken))]
[TcpBinaryField(9, nameof(TcpAttachmentRef.ThumbnailApiHint))]
[TcpBinaryField(10, nameof(TcpAttachmentRef.IsVoice))]
[TcpBinaryField(11, nameof(TcpAttachmentRef.VoiceCodec))]
[TcpBinaryField(12, nameof(TcpAttachmentRef.VoiceContainer))]
[TcpBinaryField(13, nameof(TcpAttachmentRef.VoiceDurationMs))]
[TcpBinaryField(14, nameof(TcpAttachmentRef.VoiceSampleRateHz))]
[TcpBinaryField(15, nameof(TcpAttachmentRef.VoiceChannels))]
internal static partial class TcpAttachmentRefBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in TcpAttachmentRef value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpAttachmentRefBinaryEncoder, TcpAttachmentRef>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct TcpAttachmentRefBinaryEncoder : IBinaryEncoder<TcpAttachmentRefBinaryEncoder, TcpAttachmentRef>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpAttachmentRef value)
    {
        writer.WriteInt32(1, value.RefVersion);
        writer.WriteString(2, value.AttachmentId);
        if (value.FileName is { } fileName) writer.WriteString(3, fileName);
        writer.WriteString(4, value.ContentType);
        writer.WriteInt64(5, value.SizeBytes);
        writer.WriteInt32(6, value.Status);
        if (value.DownloadApiHint is { } downloadApiHint) writer.WriteString(7, downloadApiHint);
        if (value.DownloadToken is { } downloadToken) writer.WriteString(8, downloadToken);
        if (value.ThumbnailApiHint is { } thumbnailApiHint) writer.WriteString(9, thumbnailApiHint);
        writer.WriteBool(10, value.IsVoice);
        if (value.VoiceCodec is { } voiceCodec) writer.WriteString(11, voiceCodec);
        if (value.VoiceContainer is { } voiceContainer) writer.WriteString(12, voiceContainer);
        if (value.VoiceDurationMs is { } voiceDurationMs) writer.WriteInt64(13, voiceDurationMs);
        if (value.VoiceSampleRateHz is { } voiceSampleRateHz) writer.WriteInt32(14, voiceSampleRateHz);
        if (value.VoiceChannels is { } voiceChannels) writer.WriteInt32(15, voiceChannels);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReactionSummary))]
[TcpBinaryField(1, nameof(MessageReactionSummary.Emoji))]
[TcpBinaryField(2, nameof(MessageReactionSummary.Count))]
[TcpBinaryField(3, nameof(MessageReactionSummary.ReactedByMe))]
internal static partial class MessageReactionSummaryBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in MessageReactionSummary value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReactionSummaryBinaryEncoder, MessageReactionSummary>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct MessageReactionSummaryBinaryEncoder : IBinaryEncoder<MessageReactionSummaryBinaryEncoder, MessageReactionSummary>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReactionSummary value)
    {
        writer.WriteString(1, value.Emoji);
        writer.WriteInt32(2, value.Count);
        writer.WriteBool(3, value.ReactedByMe);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryItem))]
[TcpBinaryField(1, nameof(MessageHistoryItem.MessageId))]
[TcpBinaryField(2, nameof(MessageHistoryItem.ClientMessageId))]
[TcpBinaryField(3, nameof(MessageHistoryItem.SenderUserId))]
[TcpBinaryField(4, nameof(MessageHistoryItem.ReceiverUserId))]
[TcpBinaryField(5, nameof(MessageHistoryItem.ConversationId))]
[TcpBinaryField(6, nameof(MessageHistoryItem.Content))]
[TcpBinaryField(7, nameof(MessageHistoryItem.ReceivedAtMs))]
[TcpBinaryField(8, nameof(MessageHistoryItem.DeliveredAtMs))]
[TcpBinaryField(9, nameof(MessageHistoryItem.ReadAtMs))]
[TcpBinaryField(10, nameof(MessageHistoryItem.RecalledAtMs))]
[TcpBinaryField(11, nameof(MessageHistoryItem.EditVersion))]
[TcpBinaryField(12, nameof(MessageHistoryItem.EditedAtMs))]
[TcpBinaryField(13, nameof(MessageHistoryItem.ChangedAtMs))]
[TcpBinaryNestedField(14, nameof(MessageHistoryItem.Attachments), typeof(TcpAttachmentRefBinaryDescriptor))]
[TcpBinaryNestedField(15, nameof(MessageHistoryItem.Reactions), typeof(MessageReactionSummaryBinaryDescriptor))]
[TcpBinaryField(16, nameof(MessageHistoryItem.ReplyToMessageId))]
[TcpBinaryField(17, nameof(MessageHistoryItem.ReplyToSenderUserId))]
[TcpBinaryField(18, nameof(MessageHistoryItem.ReplyToPreview))]
[TcpBinaryField(19, nameof(MessageHistoryItem.ForwardedFromMessageId))]
[TcpBinaryField(20, nameof(MessageHistoryItem.ForwardedFromSenderUserId))]
[TcpBinaryField(21, nameof(MessageHistoryItem.ForwardedFromPreview))]
[TcpBinaryRepeatedField(22, nameof(MessageHistoryItem.MentionedUserIds))]
[TcpBinaryRepeatedField(23, nameof(MessageHistoryItem.MentionedRoles))]
internal static partial class MessageHistoryItemBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in MessageHistoryItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryItemBinaryEncoder, MessageHistoryItem>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct MessageHistoryItemBinaryEncoder : IBinaryEncoder<MessageHistoryItemBinaryEncoder, MessageHistoryItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryItem value)
    {
        writer.WriteString(1, value.MessageId);
        writer.WriteString(2, value.ClientMessageId);
        writer.WriteInt64(3, value.SenderUserId);
        writer.WriteInt64(4, value.ReceiverUserId);
        if (value.ConversationId is { } conversationId) writer.WriteString(5, conversationId);
        writer.WriteString(6, value.Content);
        writer.WriteInt64(7, value.ReceivedAtMs);
        if (value.DeliveredAtMs is { } deliveredAtMs) writer.WriteInt64(8, deliveredAtMs);
        if (value.ReadAtMs is { } readAtMs) writer.WriteInt64(9, readAtMs);
        if (value.RecalledAtMs is { } recalledAtMs) writer.WriteInt64(10, recalledAtMs);
        writer.WriteInt32(11, value.EditVersion);
        if (value.EditedAtMs is { } editedAtMs) writer.WriteInt64(12, editedAtMs);
        writer.WriteInt64(13, value.ChangedAtMs);

        if (value.Attachments is { } attachments)
        {
            foreach (TcpAttachmentRef attachment in attachments)
            {
                if (!writer.TryAddCollectionElement(14)) return writer.Status;
                writer.WriteNested<TcpAttachmentRefBinaryEncoder, TcpAttachmentRef>(14, in attachment, allowRepeatedFieldNumber: true);
            }
        }

        if (value.Reactions is { } reactions)
        {
            foreach (MessageReactionSummary reaction in reactions)
            {
                if (!writer.TryAddCollectionElement(15)) return writer.Status;
                writer.WriteNested<MessageReactionSummaryBinaryEncoder, MessageReactionSummary>(15, in reaction, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ReplyToMessageId is { } replyToMessageId) writer.WriteString(16, replyToMessageId);
        if (value.ReplyToSenderUserId is { } replyToSenderUserId) writer.WriteInt64(17, replyToSenderUserId);
        if (value.ReplyToPreview is { } replyToPreview) writer.WriteString(18, replyToPreview);
        if (value.ForwardedFromMessageId is { } forwardedFromMessageId) writer.WriteString(19, forwardedFromMessageId);
        if (value.ForwardedFromSenderUserId is { } forwardedFromSenderUserId) writer.WriteInt64(20, forwardedFromSenderUserId);
        if (value.ForwardedFromPreview is { } forwardedFromPreview) writer.WriteString(21, forwardedFromPreview);

        if (value.MentionedUserIds is { } mentionedUserIds)
        {
            foreach (long mentionedUserId in mentionedUserIds)
            {
                if (!writer.TryAddCollectionElement(22)) return writer.Status;
                writer.WriteRepeatedInt64(22, mentionedUserId);
            }
        }

        if (value.MentionedRoles is { } mentionedRoles)
        {
            foreach (string mentionedRole in mentionedRoles)
            {
                if (!writer.TryAddCollectionElement(23)) return writer.Status;
                writer.WriteRepeatedString(23, mentionedRole);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ConversationSyncWatermark))]
[TcpBinaryField(1, nameof(ConversationSyncWatermark.ConversationId))]
[TcpBinaryField(2, nameof(ConversationSyncWatermark.AfterReceivedAtMs))]
[TcpBinaryField(3, nameof(ConversationSyncWatermark.AfterMessageId))]
internal static partial class ConversationSyncWatermarkBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in ConversationSyncWatermark value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ConversationSyncWatermarkBinaryEncoder, ConversationSyncWatermark>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct ConversationSyncWatermarkBinaryEncoder : IBinaryEncoder<ConversationSyncWatermarkBinaryEncoder, ConversationSyncWatermark>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ConversationSyncWatermark value)
    {
        writer.WriteString(1, value.ConversationId);
        writer.WriteInt64(2, value.AfterReceivedAtMs);
        writer.WriteString(3, value.AfterMessageId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(RelationshipSyncWatermark))]
[TcpBinaryField(1, nameof(RelationshipSyncWatermark.ListType))]
[TcpBinaryField(2, nameof(RelationshipSyncWatermark.AfterSequence))]
internal static partial class RelationshipSyncWatermarkBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in RelationshipSyncWatermark value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RelationshipSyncWatermarkBinaryEncoder, RelationshipSyncWatermark>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct RelationshipSyncWatermarkBinaryEncoder : IBinaryEncoder<RelationshipSyncWatermarkBinaryEncoder, RelationshipSyncWatermark>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in RelationshipSyncWatermark value)
    {
        writer.WriteInt32(1, (byte)value.ListType);
        writer.WriteInt64(2, value.AfterSequence);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(SyncBootstrapRequest))]
[TcpBinaryField(1, nameof(SyncBootstrapRequest.RequestId))]
[TcpBinaryField(2, nameof(SyncBootstrapRequest.ListLimit))]
[TcpBinaryField(3, nameof(SyncBootstrapRequest.HistoryLimitPerConversation))]
[TcpBinaryField(4, nameof(SyncBootstrapRequest.MaxConversationsWithHistory))]
[TcpBinaryNestedField(5, nameof(SyncBootstrapRequest.Watermarks), typeof(ConversationSyncWatermarkBinaryDescriptor))]
[TcpBinaryNestedField(6, nameof(SyncBootstrapRequest.RelationshipWatermarks), typeof(RelationshipSyncWatermarkBinaryDescriptor))]
[TcpBinaryField(7, nameof(SyncBootstrapRequest.RelationshipListLimit))]
internal static partial class SyncBootstrapRequestBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in SyncBootstrapRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<SyncBootstrapRequestBinaryEncoder, SyncBootstrapRequest>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct SyncBootstrapRequestBinaryEncoder : IBinaryEncoder<SyncBootstrapRequestBinaryEncoder, SyncBootstrapRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in SyncBootstrapRequest value)
    {
        if (value.RequestId is { } requestId) writer.WriteString(1, requestId);
        writer.WriteInt32(2, value.ListLimit);
        writer.WriteInt32(3, value.HistoryLimitPerConversation);
        writer.WriteInt32(4, value.MaxConversationsWithHistory);

        if (value.Watermarks is { } watermarks)
        {
            foreach (ConversationSyncWatermark watermark in watermarks)
            {
                if (!writer.TryAddCollectionElement(5)) return writer.Status;
                writer.WriteNested<ConversationSyncWatermarkBinaryEncoder, ConversationSyncWatermark>(5, in watermark, allowRepeatedFieldNumber: true);
            }
        }

        if (value.RelationshipWatermarks is { } relationshipWatermarks)
        {
            foreach (RelationshipSyncWatermark watermark in relationshipWatermarks)
            {
                if (!writer.TryAddCollectionElement(6)) return writer.Status;
                writer.WriteNested<RelationshipSyncWatermarkBinaryEncoder, RelationshipSyncWatermark>(6, in watermark, allowRepeatedFieldNumber: true);
            }
        }

        if (value.RelationshipListLimit is { } relationshipListLimit)
        {
            writer.WriteInt32(7, relationshipListLimit);
        }

        return writer.Status;
    }
}
