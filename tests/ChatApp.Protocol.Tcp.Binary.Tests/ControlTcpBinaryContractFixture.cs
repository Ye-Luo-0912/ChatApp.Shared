using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

internal static class ControlTcpBinaryFieldNumbers
{
    internal static class GoAway
    {
        internal const int RetryAfterMs = 1;
        internal const int Reason = 2;
        internal const int ServerHint = 3;
    }

    internal static class ResumeResponse
    {
        internal const int Success = 1;
        internal const int FailureKind = 2;
        internal const int ResumeToken = 3;
        internal const int UserId = 4;
        internal const int SessionId = 5;
        internal const int DeviceId = 6;
        internal const int LastConversationSequence = 7;
        internal const int ErrorMessage = 8;
        internal const int RetryAfterMs = 9;
    }

    internal static class ProtocolErrorFrame
    {
        internal const int Code = 1;
        internal const int Fatal = 2;
        internal const int RetryAfterMs = 3;
        internal const int Message = 4;
        internal const int OriginCommand = 5;
    }
}

[TcpBinaryContract(typeof(GoAway))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.GoAway.RetryAfterMs, nameof(GoAway.RetryAfterMs))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.GoAway.Reason, nameof(GoAway.Reason))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.GoAway.ServerHint, nameof(GoAway.ServerHint))]
internal static partial class GoAwayBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in GoAway value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<GoAwayBinaryEncoder, GoAway>(in value, destination, limits, out written);
}

internal readonly struct GoAwayBinaryEncoder : IBinaryEncoder<GoAwayBinaryEncoder, GoAway>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in GoAway value)
    {
        writer.WriteInt32(ControlTcpBinaryFieldNumbers.GoAway.RetryAfterMs, value.RetryAfterMs);
        if (value.Reason is { } reason)
        {
            writer.WriteString(ControlTcpBinaryFieldNumbers.GoAway.Reason, reason);
        }

        if (value.ServerHint is { } serverHint)
        {
            writer.WriteString(ControlTcpBinaryFieldNumbers.GoAway.ServerHint, serverHint);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ResumeResponse))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.Success, nameof(ResumeResponse.Success))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.FailureKind, nameof(ResumeResponse.FailureKind))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.ResumeToken, nameof(ResumeResponse.ResumeToken))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.UserId, nameof(ResumeResponse.UserId))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.SessionId, nameof(ResumeResponse.SessionId))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.DeviceId, nameof(ResumeResponse.DeviceId))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.LastConversationSequence, nameof(ResumeResponse.LastConversationSequence))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.ErrorMessage, nameof(ResumeResponse.ErrorMessage))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ResumeResponse.RetryAfterMs, nameof(ResumeResponse.RetryAfterMs))]
internal static partial class ResumeResponseBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in ResumeResponse value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ResumeResponseBinaryEncoder, ResumeResponse>(in value, destination, limits, out written);
}

internal readonly struct ResumeResponseBinaryEncoder : IBinaryEncoder<ResumeResponseBinaryEncoder, ResumeResponse>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ResumeResponse value)
    {
        writer.WriteBool(ControlTcpBinaryFieldNumbers.ResumeResponse.Success, value.Success);
        writer.WriteUInt32(ControlTcpBinaryFieldNumbers.ResumeResponse.FailureKind, (byte)value.FailureKind);
        if (value.ResumeToken is { } resumeToken)
        {
            writer.WriteString(ControlTcpBinaryFieldNumbers.ResumeResponse.ResumeToken, resumeToken);
        }

        writer.WriteInt64(ControlTcpBinaryFieldNumbers.ResumeResponse.UserId, value.UserId);
        if (value.SessionId is { } sessionId)
        {
            writer.WriteString(ControlTcpBinaryFieldNumbers.ResumeResponse.SessionId, sessionId);
        }

        if (value.DeviceId is { } deviceId)
        {
            writer.WriteString(ControlTcpBinaryFieldNumbers.ResumeResponse.DeviceId, deviceId);
        }

        if (value.LastConversationSequence is { } lastConversationSequence)
        {
            writer.WriteInt64(ControlTcpBinaryFieldNumbers.ResumeResponse.LastConversationSequence, lastConversationSequence);
        }

        if (value.ErrorMessage is { } errorMessage)
        {
            writer.WriteString(ControlTcpBinaryFieldNumbers.ResumeResponse.ErrorMessage, errorMessage);
        }

        if (value.RetryAfterMs is { } retryAfterMs)
        {
            writer.WriteInt32(ControlTcpBinaryFieldNumbers.ResumeResponse.RetryAfterMs, retryAfterMs);
        }

        return writer.Status;
    }
}

[TcpBinaryContract(typeof(ProtocolErrorFrame))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.Code, nameof(ProtocolErrorFrame.Code))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.Fatal, nameof(ProtocolErrorFrame.Fatal))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.RetryAfterMs, nameof(ProtocolErrorFrame.RetryAfterMs))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.Message, nameof(ProtocolErrorFrame.Message))]
[TcpBinaryField(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.OriginCommand, nameof(ProtocolErrorFrame.OriginCommand))]
internal static partial class ProtocolErrorFrameBinaryDescriptor
{
    public static BinaryStatus TryEncode(
        in ProtocolErrorFrame value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<ProtocolErrorFrameBinaryEncoder, ProtocolErrorFrame>(in value, destination, limits, out written);
}

internal readonly struct ProtocolErrorFrameBinaryEncoder : IBinaryEncoder<ProtocolErrorFrameBinaryEncoder, ProtocolErrorFrame>
{
    public static BinaryStatus Write(ref BinaryWriteCursor writer, in ProtocolErrorFrame value)
    {
        writer.WriteUInt32(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.Code, (ushort)value.Code);
        writer.WriteBool(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.Fatal, value.Fatal);
        if (value.RetryAfterMs is { } retryAfterMs)
        {
            writer.WriteInt32(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.RetryAfterMs, retryAfterMs);
        }

        if (value.Message is { } message)
        {
            writer.WriteString(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.Message, message);
        }

        if (value.OriginCommand is { } originCommand)
        {
            writer.WriteUInt32(ControlTcpBinaryFieldNumbers.ProtocolErrorFrame.OriginCommand, originCommand);
        }

        return writer.Status;
    }
}
