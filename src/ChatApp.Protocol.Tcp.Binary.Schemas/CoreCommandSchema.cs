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