using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

/// <summary>
/// Consumer-owned schema declarations for the first real binary-v1 migration slice.
/// The canonical DTOs remain in ChatApp.Protocol.Tcp; this file owns only field numbers and
/// handwritten encoders for the test consumer, so the runtime schema package does not guess
/// business DTOs or acquire a TCP contract dependency.
/// </summary>
internal static class RealTcpBinaryFieldNumbers
{
    internal static class ClientHello
    {
        internal const int ProtocolVersion = 1;
        internal const int FeatureBits = 2;
        internal const int InstallationId = 3;
        internal const int ClientTimeMs = 4;
        internal const int ResumeToken = 5;
        internal const int MaxPayloadBytes = 6;
    }

    internal static class ServerHello
    {
        internal const int ProtocolVersion = 1;
        internal const int FeatureBits = 2;
        internal const int ServerDeviceId = 3;
        internal const int ServerTimeMs = 4;
        internal const int HeartbeatIntervalMs = 5;
        internal const int MaxPayloadBytes = 6;
        internal const int ResumeSupported = 7;
        internal const int PayloadFormat = 8;
    }

    internal static class MessageHistoryRequest
    {
        internal const int RequestId = 1;
        internal const int ConversationId = 2;
        internal const int BeforeReceivedAtMs = 3;
        internal const int BeforeMessageId = 4;
        internal const int AfterReceivedAtMs = 5;
        internal const int AfterMessageId = 6;
        internal const int Limit = 7;
    }

    internal static class MessageHistoryCursor
    {
        internal const int ReceivedAtMs = 1;
        internal const int ChangedAtMs = 2;
        internal const int MessageId = 3;
    }
}

[TcpBinaryContract(typeof(ClientHello))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ClientHello.ProtocolVersion, nameof(ClientHello.ProtocolVersion))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ClientHello.FeatureBits, nameof(ClientHello.FeatureBits))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ClientHello.InstallationId, nameof(ClientHello.InstallationId))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ClientHello.ClientTimeMs, nameof(ClientHello.ClientTimeMs))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ClientHello.ResumeToken, nameof(ClientHello.ResumeToken))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ClientHello.MaxPayloadBytes, nameof(ClientHello.MaxPayloadBytes))]
internal static partial class ClientHelloBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in ClientHello value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ClientHelloBinaryEncoder, ClientHello>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct ClientHelloBinaryEncoder : IBinaryEncoder<ClientHelloBinaryEncoder, ClientHello>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ClientHello value)
    {
        writer.WriteUInt32(
            RealTcpBinaryFieldNumbers.ClientHello.ProtocolVersion,
            value.ProtocolVersion);
        writer.WriteUInt32(
            RealTcpBinaryFieldNumbers.ClientHello.FeatureBits,
            value.FeatureBits);
        if (value.InstallationId is { } installationId)
        {
            writer.WriteString(RealTcpBinaryFieldNumbers.ClientHello.InstallationId, installationId);
        }

        writer.WriteInt64(
            RealTcpBinaryFieldNumbers.ClientHello.ClientTimeMs,
            value.ClientTimeMs);
        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(RealTcpBinaryFieldNumbers.ClientHello.ResumeToken, resumeToken);
        }

        if (value.MaxPayloadBytes is { } maxPayloadBytes)
        {
            writer.WriteInt32(
                RealTcpBinaryFieldNumbers.ClientHello.MaxPayloadBytes,
                maxPayloadBytes);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ServerHello))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.ProtocolVersion, nameof(ServerHello.ProtocolVersion))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.FeatureBits, nameof(ServerHello.FeatureBits))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.ServerDeviceId, nameof(ServerHello.ServerDeviceId))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.ServerTimeMs, nameof(ServerHello.ServerTimeMs))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.HeartbeatIntervalMs, nameof(ServerHello.HeartbeatIntervalMs))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.MaxPayloadBytes, nameof(ServerHello.MaxPayloadBytes))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.ResumeSupported, nameof(ServerHello.ResumeSupported))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.ServerHello.PayloadFormat, nameof(ServerHello.PayloadFormat))]
internal static partial class ServerHelloBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in ServerHello value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ServerHelloBinaryEncoder, ServerHello>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct ServerHelloBinaryEncoder : IBinaryEncoder<ServerHelloBinaryEncoder, ServerHello>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ServerHello value)
    {
        writer.WriteUInt32(
            RealTcpBinaryFieldNumbers.ServerHello.ProtocolVersion,
            value.ProtocolVersion);
        writer.WriteUInt32(
            RealTcpBinaryFieldNumbers.ServerHello.FeatureBits,
            value.FeatureBits);
        writer.WriteString(
            RealTcpBinaryFieldNumbers.ServerHello.ServerDeviceId,
            value.ServerDeviceId);
        writer.WriteInt64(
            RealTcpBinaryFieldNumbers.ServerHello.ServerTimeMs,
            value.ServerTimeMs);
        writer.WriteInt32(
            RealTcpBinaryFieldNumbers.ServerHello.HeartbeatIntervalMs,
            value.HeartbeatIntervalMs);
        writer.WriteInt32(
            RealTcpBinaryFieldNumbers.ServerHello.MaxPayloadBytes,
            value.MaxPayloadBytes);
        writer.WriteBool(
            RealTcpBinaryFieldNumbers.ServerHello.ResumeSupported,
            value.ResumeSupported);
        writer.WriteString(
            RealTcpBinaryFieldNumbers.ServerHello.PayloadFormat,
            value.PayloadFormat);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryRequest))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryRequest.RequestId, nameof(MessageHistoryRequest.RequestId))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryRequest.ConversationId, nameof(MessageHistoryRequest.ConversationId))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryRequest.BeforeReceivedAtMs, nameof(MessageHistoryRequest.BeforeReceivedAtMs))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryRequest.BeforeMessageId, nameof(MessageHistoryRequest.BeforeMessageId))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryRequest.AfterReceivedAtMs, nameof(MessageHistoryRequest.AfterReceivedAtMs))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryRequest.AfterMessageId, nameof(MessageHistoryRequest.AfterMessageId))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryRequest.Limit, nameof(MessageHistoryRequest.Limit))]
internal static partial class MessageHistoryRequestBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in MessageHistoryRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryRequestBinaryEncoder, MessageHistoryRequest>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct MessageHistoryRequestBinaryEncoder :
    IBinaryEncoder<MessageHistoryRequestBinaryEncoder, MessageHistoryRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(RealTcpBinaryFieldNumbers.MessageHistoryRequest.RequestId, requestId);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(
                RealTcpBinaryFieldNumbers.MessageHistoryRequest.ConversationId,
                conversationId);
        }

        if (value.BeforeReceivedAtMs is { } beforeReceivedAtMs)
        {
            writer.WriteInt64(
                RealTcpBinaryFieldNumbers.MessageHistoryRequest.BeforeReceivedAtMs,
                beforeReceivedAtMs);
        }

        if (value.BeforeMessageId is { } beforeMessageId)
        {
            writer.WriteString(
                RealTcpBinaryFieldNumbers.MessageHistoryRequest.BeforeMessageId,
                beforeMessageId);
        }

        if (value.AfterReceivedAtMs is { } afterReceivedAtMs)
        {
            writer.WriteInt64(
                RealTcpBinaryFieldNumbers.MessageHistoryRequest.AfterReceivedAtMs,
                afterReceivedAtMs);
        }

        if (value.AfterMessageId is { } afterMessageId)
        {
            writer.WriteString(
                RealTcpBinaryFieldNumbers.MessageHistoryRequest.AfterMessageId,
                afterMessageId);
        }

        writer.WriteInt32(
            RealTcpBinaryFieldNumbers.MessageHistoryRequest.Limit,
            value.Limit);
        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryCursor))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryCursor.ReceivedAtMs, nameof(MessageHistoryCursor.ReceivedAtMs))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryCursor.ChangedAtMs, nameof(MessageHistoryCursor.ChangedAtMs))]
[TcpBinaryField(RealTcpBinaryFieldNumbers.MessageHistoryCursor.MessageId, nameof(MessageHistoryCursor.MessageId))]
internal static partial class MessageHistoryCursorBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in MessageHistoryCursor value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryCursorBinaryEncoder, MessageHistoryCursor>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct MessageHistoryCursorBinaryEncoder :
    IBinaryEncoder<MessageHistoryCursorBinaryEncoder, MessageHistoryCursor>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryCursor value)
    {
        writer.WriteInt64(
            RealTcpBinaryFieldNumbers.MessageHistoryCursor.ReceivedAtMs,
            value.ReceivedAtMs);
        if (value.ChangedAtMs is { } changedAtMs)
        {
            writer.WriteInt64(
                RealTcpBinaryFieldNumbers.MessageHistoryCursor.ChangedAtMs,
                changedAtMs);
        }

        writer.WriteString(
            RealTcpBinaryFieldNumbers.MessageHistoryCursor.MessageId,
            value.MessageId);
        return writer.Status;
    }
}
