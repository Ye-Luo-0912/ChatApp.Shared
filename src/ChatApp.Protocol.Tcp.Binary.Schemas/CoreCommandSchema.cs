using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Shared.Protocol.Tcp.Binary.Schemas;

/// <summary>
/// Canonical binary-v1 field numbers for the core negotiated-format command subset. These
/// numbers are frozen by the wire goldens established during BIN-SCHEMA-2 and are authoritative;
/// they must never be renumbered or reused across wire-incompatible changes.
/// </summary>
public static class CoreCommandFieldNumbers
{
    public static class ClientHello
    {
        public const int ProtocolVersion = 1;
        public const int FeatureBits = 2;
        public const int InstallationId = 3;
        public const int ClientTimeMs = 4;
        public const int ResumeToken = 5;
        public const int MaxPayloadBytes = 6;
    }

    public static class ServerHello
    {
        public const int ProtocolVersion = 1;
        public const int FeatureBits = 2;
        public const int ServerDeviceId = 3;
        public const int ServerTimeMs = 4;
        public const int HeartbeatIntervalMs = 5;
        public const int MaxPayloadBytes = 6;
        public const int ResumeSupported = 7;
        public const int PayloadFormat = 8;
    }

    public static class GoAway
    {
        public const int RetryAfterMs = 1;
        public const int Reason = 2;
        public const int ServerHint = 3;
    }

    public static class ResumeResponse
    {
        public const int Success = 1;
        public const int FailureKind = 2;
        public const int ResumeToken = 3;
        public const int UserId = 4;
        public const int SessionId = 5;
        public const int DeviceId = 6;
        public const int LastConversationSequence = 7;
        public const int ErrorMessage = 8;
        public const int RetryAfterMs = 9;
    }

    public static class ProtocolErrorFrame
    {
        public const int Code = 1;
        public const int Fatal = 2;
        public const int RetryAfterMs = 3;
        public const int Message = 4;
        public const int OriginCommand = 5;
    }

    public static class MessageHistoryRequest
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int BeforeReceivedAtMs = 3;
        public const int BeforeMessageId = 4;
        public const int AfterReceivedAtMs = 5;
        public const int AfterMessageId = 6;
        public const int Limit = 7;
    }

    public static class MessageHistoryCursor
    {
        public const int ReceivedAtMs = 1;
        public const int ChangedAtMs = 2;
        public const int MessageId = 3;
    }

    public static class TcpAttachmentRef
    {
        public const int RefVersion = 1;
        public const int AttachmentId = 2;
        public const int FileName = 3;
        public const int ContentType = 4;
        public const int SizeBytes = 5;
        public const int Status = 6;
        public const int DownloadApiHint = 7;
        public const int DownloadToken = 8;
        public const int ThumbnailApiHint = 9;
        public const int IsVoice = 10;
        public const int VoiceCodec = 11;
        public const int VoiceContainer = 12;
        public const int VoiceDurationMs = 13;
        public const int VoiceSampleRateHz = 14;
        public const int VoiceChannels = 15;
    }

    public static class MessageReactionSummary
    {
        public const int Emoji = 1;
        public const int Count = 2;
        public const int ReactedByMe = 3;
    }

    public static class MessageHistoryItem
    {
        public const int MessageId = 1;
        public const int ClientMessageId = 2;
        public const int SenderUserId = 3;
        public const int ReceiverUserId = 4;
        public const int ConversationId = 5;
        public const int Content = 6;
        public const int ReceivedAtMs = 7;
        public const int DeliveredAtMs = 8;
        public const int ReadAtMs = 9;
        public const int RecalledAtMs = 10;
        public const int EditVersion = 11;
        public const int EditedAtMs = 12;
        public const int ChangedAtMs = 13;
        public const int Attachments = 14;
        public const int Reactions = 15;
        public const int ReplyToMessageId = 16;
        public const int ReplyToSenderUserId = 17;
        public const int ReplyToPreview = 18;
        public const int ForwardedFromMessageId = 19;
        public const int ForwardedFromSenderUserId = 20;
        public const int ForwardedFromPreview = 21;
        public const int MentionedUserIds = 22;
        public const int MentionedRoles = 23;
    }

    public static class MessageHistoryResponse
    {
        public const int RequestId = 1;
        public const int ConversationId = 2;
        public const int Succeeded = 3;
        public const int ErrorCode = 4;
        public const int ErrorMessage = 5;
        public const int Items = 6;
        public const int NextCursor = 7;
        public const int HasMore = 8;
    }
}

[TcpBinaryContract(typeof(ClientHello))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.ProtocolVersion, nameof(ClientHello.ProtocolVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.FeatureBits, nameof(ClientHello.FeatureBits))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.InstallationId, nameof(ClientHello.InstallationId))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.ClientTimeMs, nameof(ClientHello.ClientTimeMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.ResumeToken, nameof(ClientHello.ResumeToken))]
[TcpBinaryField(CoreCommandFieldNumbers.ClientHello.MaxPayloadBytes, nameof(ClientHello.MaxPayloadBytes))]
public static partial class ClientHelloSchema
{
    public static BinaryStatus TryEncode(
        in ClientHello value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ClientHelloSchemaEncoder, ClientHello>(in value, destination, limits, out written);
}

public readonly struct ClientHelloSchemaEncoder : IBinaryEncoder<ClientHelloSchemaEncoder, ClientHello>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ClientHello value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.ClientHello.ProtocolVersion, value.ProtocolVersion);
        writer.WriteUInt32(CoreCommandFieldNumbers.ClientHello.FeatureBits, value.FeatureBits);
        if (value.InstallationId is { } installationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ClientHello.InstallationId, installationId);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.ClientHello.ClientTimeMs, value.ClientTimeMs);
        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.ClientHello.ResumeToken, resumeToken);
        }

        if (value.MaxPayloadBytes is { } maxPayloadBytes)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.ClientHello.MaxPayloadBytes, maxPayloadBytes);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ServerHello))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ProtocolVersion, nameof(ServerHello.ProtocolVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.FeatureBits, nameof(ServerHello.FeatureBits))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ServerDeviceId, nameof(ServerHello.ServerDeviceId))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ServerTimeMs, nameof(ServerHello.ServerTimeMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.HeartbeatIntervalMs, nameof(ServerHello.HeartbeatIntervalMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.MaxPayloadBytes, nameof(ServerHello.MaxPayloadBytes))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.ResumeSupported, nameof(ServerHello.ResumeSupported))]
[TcpBinaryField(CoreCommandFieldNumbers.ServerHello.PayloadFormat, nameof(ServerHello.PayloadFormat))]
public static partial class ServerHelloSchema
{
    public static BinaryStatus TryEncode(
        in ServerHello value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ServerHelloSchemaEncoder, ServerHello>(in value, destination, limits, out written);
}

public readonly struct ServerHelloSchemaEncoder : IBinaryEncoder<ServerHelloSchemaEncoder, ServerHello>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ServerHello value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.ServerHello.ProtocolVersion, value.ProtocolVersion);
        writer.WriteUInt32(CoreCommandFieldNumbers.ServerHello.FeatureBits, value.FeatureBits);
        writer.WriteString(CoreCommandFieldNumbers.ServerHello.ServerDeviceId, value.ServerDeviceId);
        writer.WriteInt64(CoreCommandFieldNumbers.ServerHello.ServerTimeMs, value.ServerTimeMs);
        writer.WriteInt32(CoreCommandFieldNumbers.ServerHello.HeartbeatIntervalMs, value.HeartbeatIntervalMs);
        writer.WriteInt32(CoreCommandFieldNumbers.ServerHello.MaxPayloadBytes, value.MaxPayloadBytes);
        writer.WriteBool(CoreCommandFieldNumbers.ServerHello.ResumeSupported, value.ResumeSupported);
        writer.WriteString(CoreCommandFieldNumbers.ServerHello.PayloadFormat, value.PayloadFormat);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(GoAway))]
[TcpBinaryField(CoreCommandFieldNumbers.GoAway.RetryAfterMs, nameof(GoAway.RetryAfterMs))]
[TcpBinaryField(CoreCommandFieldNumbers.GoAway.Reason, nameof(GoAway.Reason))]
[TcpBinaryField(CoreCommandFieldNumbers.GoAway.ServerHint, nameof(GoAway.ServerHint))]
public static partial class GoAwaySchema
{
    public static BinaryStatus TryEncode(
        in GoAway value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<GoAwaySchemaEncoder, GoAway>(in value, destination, limits, out written);
}

public readonly struct GoAwaySchemaEncoder : IBinaryEncoder<GoAwaySchemaEncoder, GoAway>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in GoAway value)
    {
        writer.WriteInt32(CoreCommandFieldNumbers.GoAway.RetryAfterMs, value.RetryAfterMs);
        if (value.Reason is { } reason)
        {
            writer.WriteString(CoreCommandFieldNumbers.GoAway.Reason, reason);
        }

        if (value.ServerHint is { } serverHint)
        {
            writer.WriteString(CoreCommandFieldNumbers.GoAway.ServerHint, serverHint);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ResumeResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.Success, nameof(ResumeResponse.Success))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.FailureKind, nameof(ResumeResponse.FailureKind))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.ResumeToken, nameof(ResumeResponse.ResumeToken))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.UserId, nameof(ResumeResponse.UserId))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.SessionId, nameof(ResumeResponse.SessionId))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.DeviceId, nameof(ResumeResponse.DeviceId))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.LastConversationSequence, nameof(ResumeResponse.LastConversationSequence))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.ErrorMessage, nameof(ResumeResponse.ErrorMessage))]
[TcpBinaryField(CoreCommandFieldNumbers.ResumeResponse.RetryAfterMs, nameof(ResumeResponse.RetryAfterMs))]
public static partial class ResumeResponseSchema
{
    public static BinaryStatus TryEncode(
        in ResumeResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ResumeResponseSchemaEncoder, ResumeResponse>(in value, destination, limits, out written);
}

public readonly struct ResumeResponseSchemaEncoder : IBinaryEncoder<ResumeResponseSchemaEncoder, ResumeResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ResumeResponse value)
    {
        writer.WriteBool(CoreCommandFieldNumbers.ResumeResponse.Success, value.Success);
        writer.WriteUInt32(CoreCommandFieldNumbers.ResumeResponse.FailureKind, (byte)value.FailureKind);
        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.ResumeToken, resumeToken);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.ResumeResponse.UserId, value.UserId);
        if (value.SessionId is { } sessionId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.SessionId, sessionId);
        }

        if (value.DeviceId is { } deviceId)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.DeviceId, deviceId);
        }

        if (value.LastConversationSequence is { } lastConversationSequence)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.ResumeResponse.LastConversationSequence, lastConversationSequence);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.ResumeResponse.ErrorMessage, errorMessage);
        }

        if (value.RetryAfterMs is { } retryAfterMs)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.ResumeResponse.RetryAfterMs, retryAfterMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ProtocolErrorFrame))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.Code, nameof(ProtocolErrorFrame.Code))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.Fatal, nameof(ProtocolErrorFrame.Fatal))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.RetryAfterMs, nameof(ProtocolErrorFrame.RetryAfterMs))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.Message, nameof(ProtocolErrorFrame.Message))]
[TcpBinaryField(CoreCommandFieldNumbers.ProtocolErrorFrame.OriginCommand, nameof(ProtocolErrorFrame.OriginCommand))]
public static partial class ProtocolErrorFrameSchema
{
    public static BinaryStatus TryEncode(
        in ProtocolErrorFrame value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ProtocolErrorFrameSchemaEncoder, ProtocolErrorFrame>(in value, destination, limits, out written);
}

public readonly struct ProtocolErrorFrameSchemaEncoder : IBinaryEncoder<ProtocolErrorFrameSchemaEncoder, ProtocolErrorFrame>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ProtocolErrorFrame value)
    {
        writer.WriteUInt32(CoreCommandFieldNumbers.ProtocolErrorFrame.Code, (ushort)value.Code);
        writer.WriteBool(CoreCommandFieldNumbers.ProtocolErrorFrame.Fatal, value.Fatal);
        if (value.RetryAfterMs is { } retryAfterMs)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.ProtocolErrorFrame.RetryAfterMs, retryAfterMs);
        }

        if (value.Message is { } message)
        {
            writer.WriteString(CoreCommandFieldNumbers.ProtocolErrorFrame.Message, message);
        }

        if (value.OriginCommand is { } originCommand)
        {
            writer.WriteUInt32(CoreCommandFieldNumbers.ProtocolErrorFrame.OriginCommand, originCommand);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryRequest))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.RequestId, nameof(MessageHistoryRequest.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.ConversationId, nameof(MessageHistoryRequest.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeReceivedAtMs, nameof(MessageHistoryRequest.BeforeReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeMessageId, nameof(MessageHistoryRequest.BeforeMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.AfterReceivedAtMs, nameof(MessageHistoryRequest.AfterReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.AfterMessageId, nameof(MessageHistoryRequest.AfterMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryRequest.Limit, nameof(MessageHistoryRequest.Limit))]
public static partial class MessageHistoryRequestSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryRequestSchemaEncoder, MessageHistoryRequest>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryRequestSchemaEncoder : IBinaryEncoder<MessageHistoryRequestSchemaEncoder, MessageHistoryRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.RequestId, requestId);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.ConversationId, conversationId);
        }

        if (value.BeforeReceivedAtMs is { } beforeReceivedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeReceivedAtMs, beforeReceivedAtMs);
        }

        if (value.BeforeMessageId is { } beforeMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.BeforeMessageId, beforeMessageId);
        }

        if (value.AfterReceivedAtMs is { } afterReceivedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryRequest.AfterReceivedAtMs, afterReceivedAtMs);
        }

        if (value.AfterMessageId is { } afterMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryRequest.AfterMessageId, afterMessageId);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.MessageHistoryRequest.Limit, value.Limit);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryCursor))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryCursor.ReceivedAtMs, nameof(MessageHistoryCursor.ReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryCursor.ChangedAtMs, nameof(MessageHistoryCursor.ChangedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryCursor.MessageId, nameof(MessageHistoryCursor.MessageId))]
public static partial class MessageHistoryCursorSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryCursor value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryCursorSchemaEncoder, MessageHistoryCursor>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryCursorSchemaEncoder : IBinaryEncoder<MessageHistoryCursorSchemaEncoder, MessageHistoryCursor>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryCursor value)
    {
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryCursor.ReceivedAtMs, value.ReceivedAtMs);
        if (value.ChangedAtMs is { } changedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryCursor.ChangedAtMs, changedAtMs);
        }

        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryCursor.MessageId, value.MessageId);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(TcpAttachmentRef))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.RefVersion, nameof(TcpAttachmentRef.RefVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.AttachmentId, nameof(TcpAttachmentRef.AttachmentId))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.FileName, nameof(TcpAttachmentRef.FileName))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.ContentType, nameof(TcpAttachmentRef.ContentType))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.SizeBytes, nameof(TcpAttachmentRef.SizeBytes))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.Status, nameof(TcpAttachmentRef.Status))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadApiHint, nameof(TcpAttachmentRef.DownloadApiHint))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadToken, nameof(TcpAttachmentRef.DownloadToken))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.ThumbnailApiHint, nameof(TcpAttachmentRef.ThumbnailApiHint))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.IsVoice, nameof(TcpAttachmentRef.IsVoice))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceCodec, nameof(TcpAttachmentRef.VoiceCodec))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceContainer, nameof(TcpAttachmentRef.VoiceContainer))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceDurationMs, nameof(TcpAttachmentRef.VoiceDurationMs))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceSampleRateHz, nameof(TcpAttachmentRef.VoiceSampleRateHz))]
[TcpBinaryField(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceChannels, nameof(TcpAttachmentRef.VoiceChannels))]
public static partial class TcpAttachmentRefSchema
{
    public static BinaryStatus TryEncode(
        in TcpAttachmentRef value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<TcpAttachmentRefSchemaEncoder, TcpAttachmentRef>(in value, destination, limits, out written);
}

public readonly struct TcpAttachmentRefSchemaEncoder : IBinaryEncoder<TcpAttachmentRefSchemaEncoder, TcpAttachmentRef>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in TcpAttachmentRef value)
    {
        writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.RefVersion, value.RefVersion);
        writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.AttachmentId, value.AttachmentId);
        if (value.FileName is { } fileName)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.FileName, fileName);
        }

        writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.ContentType, value.ContentType);
        writer.WriteInt64(CoreCommandFieldNumbers.TcpAttachmentRef.SizeBytes, value.SizeBytes);
        writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.Status, value.Status);
        if (value.DownloadApiHint is { } downloadApiHint)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadApiHint, downloadApiHint);
        }

        if (value.DownloadToken is { } downloadToken)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.DownloadToken, downloadToken);
        }

        if (value.ThumbnailApiHint is { } thumbnailApiHint)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.ThumbnailApiHint, thumbnailApiHint);
        }

        writer.WriteBool(CoreCommandFieldNumbers.TcpAttachmentRef.IsVoice, value.IsVoice);
        if (value.VoiceCodec is { } voiceCodec)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceCodec, voiceCodec);
        }

        if (value.VoiceContainer is { } voiceContainer)
        {
            writer.WriteString(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceContainer, voiceContainer);
        }

        if (value.VoiceDurationMs is { } voiceDurationMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceDurationMs, voiceDurationMs);
        }

        if (value.VoiceSampleRateHz is { } voiceSampleRateHz)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceSampleRateHz, voiceSampleRateHz);
        }

        if (value.VoiceChannels is { } voiceChannels)
        {
            writer.WriteInt32(CoreCommandFieldNumbers.TcpAttachmentRef.VoiceChannels, voiceChannels);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageReactionSummary))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReactionSummary.Emoji, nameof(MessageReactionSummary.Emoji))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReactionSummary.Count, nameof(MessageReactionSummary.Count))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageReactionSummary.ReactedByMe, nameof(MessageReactionSummary.ReactedByMe))]
public static partial class MessageReactionSummarySchema
{
    public static BinaryStatus TryEncode(
        in MessageReactionSummary value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageReactionSummarySchemaEncoder, MessageReactionSummary>(in value, destination, limits, out written);
}

public readonly struct MessageReactionSummarySchemaEncoder : IBinaryEncoder<MessageReactionSummarySchemaEncoder, MessageReactionSummary>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageReactionSummary value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageReactionSummary.Emoji, value.Emoji);
        writer.WriteInt32(CoreCommandFieldNumbers.MessageReactionSummary.Count, value.Count);
        writer.WriteBool(CoreCommandFieldNumbers.MessageReactionSummary.ReactedByMe, value.ReactedByMe);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryItem))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.MessageId, nameof(MessageHistoryItem.MessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ClientMessageId, nameof(MessageHistoryItem.ClientMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.SenderUserId, nameof(MessageHistoryItem.SenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReceiverUserId, nameof(MessageHistoryItem.ReceiverUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ConversationId, nameof(MessageHistoryItem.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.Content, nameof(MessageHistoryItem.Content))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReceivedAtMs, nameof(MessageHistoryItem.ReceivedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.DeliveredAtMs, nameof(MessageHistoryItem.DeliveredAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReadAtMs, nameof(MessageHistoryItem.ReadAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.RecalledAtMs, nameof(MessageHistoryItem.RecalledAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.EditVersion, nameof(MessageHistoryItem.EditVersion))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.EditedAtMs, nameof(MessageHistoryItem.EditedAtMs))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ChangedAtMs, nameof(MessageHistoryItem.ChangedAtMs))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryItem.Attachments, nameof(MessageHistoryItem.Attachments), typeof(TcpAttachmentRefSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryItem.Reactions, nameof(MessageHistoryItem.Reactions), typeof(MessageReactionSummarySchema))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToMessageId, nameof(MessageHistoryItem.ReplyToMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToSenderUserId, nameof(MessageHistoryItem.ReplyToSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToPreview, nameof(MessageHistoryItem.ReplyToPreview))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromMessageId, nameof(MessageHistoryItem.ForwardedFromMessageId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromSenderUserId, nameof(MessageHistoryItem.ForwardedFromSenderUserId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromPreview, nameof(MessageHistoryItem.ForwardedFromPreview))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.MessageHistoryItem.MentionedUserIds, nameof(MessageHistoryItem.MentionedUserIds))]
[TcpBinaryRepeatedField(CoreCommandFieldNumbers.MessageHistoryItem.MentionedRoles, nameof(MessageHistoryItem.MentionedRoles))]
public static partial class MessageHistoryItemSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryItem value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryItemSchemaEncoder, MessageHistoryItem>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryItemSchemaEncoder : IBinaryEncoder<MessageHistoryItemSchemaEncoder, MessageHistoryItem>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryItem value)
    {
        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.MessageId, value.MessageId);
        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ClientMessageId, value.ClientMessageId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.SenderUserId, value.SenderUserId);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReceiverUserId, value.ReceiverUserId);
        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ConversationId, conversationId);
        }

        writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.Content, value.Content);
        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReceivedAtMs, value.ReceivedAtMs);
        if (value.DeliveredAtMs is { } deliveredAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.DeliveredAtMs, deliveredAtMs);
        }

        if (value.ReadAtMs is { } readAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReadAtMs, readAtMs);
        }

        if (value.RecalledAtMs is { } recalledAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.RecalledAtMs, recalledAtMs);
        }

        writer.WriteInt32(CoreCommandFieldNumbers.MessageHistoryItem.EditVersion, value.EditVersion);
        if (value.EditedAtMs is { } editedAtMs)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.EditedAtMs, editedAtMs);
        }

        writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ChangedAtMs, value.ChangedAtMs);

        if (value.Attachments is { } attachments)
        {
            foreach (TcpAttachmentRef attachment in attachments)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.Attachments)) return writer.Status;
                writer.WriteNested<TcpAttachmentRefSchemaEncoder, TcpAttachmentRef>(CoreCommandFieldNumbers.MessageHistoryItem.Attachments, in attachment, allowRepeatedFieldNumber: true);
            }
        }

        if (value.Reactions is { } reactions)
        {
            foreach (MessageReactionSummary reaction in reactions)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.Reactions)) return writer.Status;
                writer.WriteNested<MessageReactionSummarySchemaEncoder, MessageReactionSummary>(CoreCommandFieldNumbers.MessageHistoryItem.Reactions, in reaction, allowRepeatedFieldNumber: true);
            }
        }

        if (value.ReplyToMessageId is { } replyToMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToMessageId, replyToMessageId);
        }

        if (value.ReplyToSenderUserId is { } replyToSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToSenderUserId, replyToSenderUserId);
        }

        if (value.ReplyToPreview is { } replyToPreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ReplyToPreview, replyToPreview);
        }

        if (value.ForwardedFromMessageId is { } forwardedFromMessageId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromMessageId, forwardedFromMessageId);
        }

        if (value.ForwardedFromSenderUserId is { } forwardedFromSenderUserId)
        {
            writer.WriteInt64(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromSenderUserId, forwardedFromSenderUserId);
        }

        if (value.ForwardedFromPreview is { } forwardedFromPreview)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryItem.ForwardedFromPreview, forwardedFromPreview);
        }

        if (value.MentionedUserIds is { } mentionedUserIds)
        {
            foreach (long mentionedUserId in mentionedUserIds)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.MentionedUserIds)) return writer.Status;
                writer.WriteRepeatedInt64(CoreCommandFieldNumbers.MessageHistoryItem.MentionedUserIds, mentionedUserId);
            }
        }

        if (value.MentionedRoles is { } mentionedRoles)
        {
            foreach (string mentionedRole in mentionedRoles)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryItem.MentionedRoles)) return writer.Status;
                writer.WriteRepeatedString(CoreCommandFieldNumbers.MessageHistoryItem.MentionedRoles, mentionedRole);
            }
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryResponse))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.RequestId, nameof(MessageHistoryResponse.RequestId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.ConversationId, nameof(MessageHistoryResponse.ConversationId))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.Succeeded, nameof(MessageHistoryResponse.Succeeded))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorCode, nameof(MessageHistoryResponse.ErrorCode))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorMessage, nameof(MessageHistoryResponse.ErrorMessage))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryResponse.Items, nameof(MessageHistoryResponse.Items), typeof(MessageHistoryItemSchema))]
[TcpBinaryNestedField(CoreCommandFieldNumbers.MessageHistoryResponse.NextCursor, nameof(MessageHistoryResponse.NextCursor), typeof(MessageHistoryCursorSchema))]
[TcpBinaryField(CoreCommandFieldNumbers.MessageHistoryResponse.HasMore, nameof(MessageHistoryResponse.HasMore))]
public static partial class MessageHistoryResponseSchema
{
    public static BinaryStatus TryEncode(
        in MessageHistoryResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryResponseSchemaEncoder, MessageHistoryResponse>(in value, destination, limits, out written);
}

public readonly struct MessageHistoryResponseSchemaEncoder : IBinaryEncoder<MessageHistoryResponseSchemaEncoder, MessageHistoryResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryResponse value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.RequestId, requestId);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.ConversationId, conversationId);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageHistoryResponse.Succeeded, value.Succeeded);
        if (value.ErrorCode is { } errorCode)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorCode, errorCode);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(CoreCommandFieldNumbers.MessageHistoryResponse.ErrorMessage, errorMessage);
        }

        if (value.Items is { } items)
        {
            foreach (MessageHistoryItem item in items)
            {
                if (!writer.TryAddCollectionElement(CoreCommandFieldNumbers.MessageHistoryResponse.Items)) return writer.Status;
                writer.WriteNested<MessageHistoryItemSchemaEncoder, MessageHistoryItem>(CoreCommandFieldNumbers.MessageHistoryResponse.Items, in item, allowRepeatedFieldNumber: true);
            }
        }

        if (value.NextCursor is { } nextCursor)
        {
            writer.WriteNested<MessageHistoryCursorSchemaEncoder, MessageHistoryCursor>(CoreCommandFieldNumbers.MessageHistoryResponse.NextCursor, in nextCursor);
        }

        writer.WriteBool(CoreCommandFieldNumbers.MessageHistoryResponse.HasMore, value.HasMore);
        return writer.Status;
    }
}