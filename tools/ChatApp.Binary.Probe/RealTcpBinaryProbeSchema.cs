using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Binary.Probe;

/// <summary>
/// Probe-owned schema for the first real consumer slice. The canonical DTOs are shared TCP
/// contracts; only field numbers and ordinary-source encoders live here. MessageHistoryItem is
/// intentionally absent because its nested/list layout is not frozen by the binary-v1 document.
/// </summary>
internal static class RealTcpBinaryProbeFields
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
}

[TcpBinaryContract(typeof(ClientHello))]
[TcpBinaryField(RealTcpBinaryProbeFields.ClientHello.ProtocolVersion, nameof(ClientHello.ProtocolVersion))]
[TcpBinaryField(RealTcpBinaryProbeFields.ClientHello.FeatureBits, nameof(ClientHello.FeatureBits))]
[TcpBinaryField(RealTcpBinaryProbeFields.ClientHello.InstallationId, nameof(ClientHello.InstallationId))]
[TcpBinaryField(RealTcpBinaryProbeFields.ClientHello.ClientTimeMs, nameof(ClientHello.ClientTimeMs))]
[TcpBinaryField(RealTcpBinaryProbeFields.ClientHello.ResumeToken, nameof(ClientHello.ResumeToken))]
[TcpBinaryField(RealTcpBinaryProbeFields.ClientHello.MaxPayloadBytes, nameof(ClientHello.MaxPayloadBytes))]
internal static partial class ClientHelloProbeDescriptor
{
    public static BinaryStatus TryEncode(
        in ClientHello value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ClientHelloProbeEncoder, ClientHello>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct ClientHelloProbeEncoder : IBinaryEncoder<ClientHelloProbeEncoder, ClientHello>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ClientHello value)
    {
        writer.WriteUInt32(RealTcpBinaryProbeFields.ClientHello.ProtocolVersion, value.ProtocolVersion);
        writer.WriteUInt32(RealTcpBinaryProbeFields.ClientHello.FeatureBits, value.FeatureBits);
        if (value.InstallationId is { } installationId)
        {
            writer.WriteString(RealTcpBinaryProbeFields.ClientHello.InstallationId, installationId);
        }

        writer.WriteInt64(RealTcpBinaryProbeFields.ClientHello.ClientTimeMs, value.ClientTimeMs);
        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(RealTcpBinaryProbeFields.ClientHello.ResumeToken, resumeToken);
        }

        if (value.MaxPayloadBytes is { } maxPayloadBytes)
        {
            writer.WriteInt32(RealTcpBinaryProbeFields.ClientHello.MaxPayloadBytes, maxPayloadBytes);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(MessageHistoryRequest))]
[TcpBinaryField(RealTcpBinaryProbeFields.MessageHistoryRequest.RequestId, nameof(MessageHistoryRequest.RequestId))]
[TcpBinaryField(RealTcpBinaryProbeFields.MessageHistoryRequest.ConversationId, nameof(MessageHistoryRequest.ConversationId))]
[TcpBinaryField(RealTcpBinaryProbeFields.MessageHistoryRequest.BeforeReceivedAtMs, nameof(MessageHistoryRequest.BeforeReceivedAtMs))]
[TcpBinaryField(RealTcpBinaryProbeFields.MessageHistoryRequest.BeforeMessageId, nameof(MessageHistoryRequest.BeforeMessageId))]
[TcpBinaryField(RealTcpBinaryProbeFields.MessageHistoryRequest.AfterReceivedAtMs, nameof(MessageHistoryRequest.AfterReceivedAtMs))]
[TcpBinaryField(RealTcpBinaryProbeFields.MessageHistoryRequest.AfterMessageId, nameof(MessageHistoryRequest.AfterMessageId))]
[TcpBinaryField(RealTcpBinaryProbeFields.MessageHistoryRequest.Limit, nameof(MessageHistoryRequest.Limit))]
internal static partial class MessageHistoryRequestProbeDescriptor
{
    public static BinaryStatus TryEncode(
        in MessageHistoryRequest value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<MessageHistoryRequestProbeEncoder, MessageHistoryRequest>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct MessageHistoryRequestProbeEncoder :
    IBinaryEncoder<MessageHistoryRequestProbeEncoder, MessageHistoryRequest>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in MessageHistoryRequest value)
    {
        if (value.RequestId is { } requestId)
        {
            writer.WriteString(RealTcpBinaryProbeFields.MessageHistoryRequest.RequestId, requestId);
        }

        if (value.ConversationId is { } conversationId)
        {
            writer.WriteString(RealTcpBinaryProbeFields.MessageHistoryRequest.ConversationId, conversationId);
        }

        if (value.BeforeReceivedAtMs is { } beforeReceivedAtMs)
        {
            writer.WriteInt64(
                RealTcpBinaryProbeFields.MessageHistoryRequest.BeforeReceivedAtMs,
                beforeReceivedAtMs);
        }

        if (value.BeforeMessageId is { } beforeMessageId)
        {
            writer.WriteString(
                RealTcpBinaryProbeFields.MessageHistoryRequest.BeforeMessageId,
                beforeMessageId);
        }

        if (value.AfterReceivedAtMs is { } afterReceivedAtMs)
        {
            writer.WriteInt64(
                RealTcpBinaryProbeFields.MessageHistoryRequest.AfterReceivedAtMs,
                afterReceivedAtMs);
        }

        if (value.AfterMessageId is { } afterMessageId)
        {
            writer.WriteString(
                RealTcpBinaryProbeFields.MessageHistoryRequest.AfterMessageId,
                afterMessageId);
        }

        writer.WriteInt32(RealTcpBinaryProbeFields.MessageHistoryRequest.Limit, value.Limit);
        return writer.Status;
    }
}
