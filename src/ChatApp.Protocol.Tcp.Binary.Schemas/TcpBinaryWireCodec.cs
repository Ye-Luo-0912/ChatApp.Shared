using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;

namespace ChatApp.Shared.Protocol.Tcp.Binary.Schemas;

/// <summary>
/// Stable outcome of a wire-codec dispatch. When <see cref="Status"/> is
/// <see cref="TcpBinaryWireStatus.Decoded"/>, <see cref="Value"/> holds the schema-authored
/// DTO for the covered command; otherwise the caller must fail closed and must not use
/// <see cref="Value"/>.
/// </summary>
public enum TcpBinaryWireStatus : byte
{
    /// <summary>The command is part of the negotiated subset and its payload decoded.</summary>
    Decoded = 0,

    /// <summary>The command has no binary-v1 schema; the connection must fail closed.</summary>
    SchemaNotCovered,

    /// <summary>A covered command's payload was malformed or over-limit; see <see cref="TcpBinaryWireDecode.DecodeStatus"/>.</summary>
    DecodeFailure,
}

/// <summary>Result of dispatching a frame to the command-to-schema registry.</summary>
public readonly struct TcpBinaryWireDecode
{
    public static TcpBinaryWireDecode NotCovered { get; } =
        new(TcpBinaryWireStatus.SchemaNotCovered, BinaryStatus.Done, null);

    public static TcpBinaryWireDecode Failure(BinaryStatus decodeStatus) =>
        new(TcpBinaryWireStatus.DecodeFailure, decodeStatus, null);

    public static TcpBinaryWireDecode Success(object value) =>
        new(TcpBinaryWireStatus.Decoded, BinaryStatus.Done, value);

    private TcpBinaryWireDecode(TcpBinaryWireStatus status, BinaryStatus decodeStatus, object? value)
    {
        Status = status;
        DecodeStatus = decodeStatus;
        Value = value;
    }

    public TcpBinaryWireStatus Status { get; }

    /// <summary>Populated only when <see cref="Status"/> is <see cref="TcpBinaryWireStatus.DecodeFailure"/>.</summary>
    public BinaryStatus DecodeStatus { get; }

    public object? Value { get; }
}

/// <summary>
/// The command-to-schema registry for the negotiated <c>chatapp-bin-v1</c> subset. It owns the
/// canonical mapping from <see cref="PacketCommand"/> to a schema descriptor and the fail-closed
/// contract: any command without a schema, any malformed or over-limit payload for a covered
/// command, and any format that is not the negotiated binary id returns a stable non-success
/// outcome. Handshake frames remain JSON and are never decoded over the binary path in production;
/// the registry still recognises them so the subset can be validated and goldened offline.
/// </summary>
public static class TcpBinaryWireCodec
{
    /// <summary>The exact negotiated format id that this registry recognises.</summary>
    public const string NegotiatedFormatId = BinaryPayloadFormat.Id;

    /// <summary>Negotiation identification: true only for the exact binary-v1 format id.</summary>
    public static bool IsNegotiatedPayloadFormat(string? payloadFormat) =>
        string.Equals(payloadFormat, BinaryPayloadFormat.Id, StringComparison.Ordinal);

    /// <summary>
    /// Dispatches a contiguous frame payload to the schema for <paramref name="command"/>.
    /// Uncovered commands and malformed covered payloads fail closed.
    /// </summary>
    public static TcpBinaryWireDecode TryDecode(
        PacketCommand command,
        ReadOnlySpan<byte> payload,
        BinaryLimits limits)
    {
        return command switch
        {
            PacketCommand.ClientHello =>
                FromStatus(ClientHelloSchema.TryDecode(payload, limits, out ClientHello? clientHello), clientHello),
            PacketCommand.ServerHello =>
                FromStatus(ServerHelloSchema.TryDecode(payload, limits, out ServerHello? serverHello), serverHello),
            PacketCommand.GoAway =>
                FromStatus(GoAwaySchema.TryDecode(payload, limits, out GoAway? goAway), goAway),
            PacketCommand.ResumeResponse =>
                FromStatus(ResumeResponseSchema.TryDecode(payload, limits, out ResumeResponse? resumeResponse), resumeResponse),
            PacketCommand.Error =>
                FromStatus(ProtocolErrorFrameSchema.TryDecode(payload, limits, out ProtocolErrorFrame? frame), frame),
            PacketCommand.MessageHistoryRequest =>
                FromStatus(MessageHistoryRequestSchema.TryDecode(payload, limits, out MessageHistoryRequest? request), request),
            _ => TcpBinaryWireDecode.NotCovered
        };
    }

    /// <summary>
    /// Dispatches a segmented frame payload without coalescing it. Uncovered commands and
    /// malformed covered payloads fail closed.
    /// </summary>
    public static TcpBinaryWireDecode TryDecode(
        PacketCommand command,
        in ReadOnlySequence<byte> payload,
        BinaryLimits limits)
    {
        return command switch
        {
            PacketCommand.ClientHello =>
                FromStatus(ClientHelloSchema.TryDecode(in payload, limits, out ClientHello? clientHello), clientHello),
            PacketCommand.ServerHello =>
                FromStatus(ServerHelloSchema.TryDecode(in payload, limits, out ServerHello? serverHello), serverHello),
            PacketCommand.GoAway =>
                FromStatus(GoAwaySchema.TryDecode(in payload, limits, out GoAway? goAway), goAway),
            PacketCommand.ResumeResponse =>
                FromStatus(ResumeResponseSchema.TryDecode(in payload, limits, out ResumeResponse? resumeResponse), resumeResponse),
            PacketCommand.Error =>
                FromStatus(ProtocolErrorFrameSchema.TryDecode(in payload, limits, out ProtocolErrorFrame? frame), frame),
            PacketCommand.MessageHistoryRequest =>
                FromStatus(MessageHistoryRequestSchema.TryDecode(in payload, limits, out MessageHistoryRequest? request), request),
            _ => TcpBinaryWireDecode.NotCovered
        };
    }

    private static TcpBinaryWireDecode FromStatus(BinaryStatus status, object? value)
    {
        if (status == BinaryStatus.Done && value is not null)
        {
            return TcpBinaryWireDecode.Success(value);
        }

        return TcpBinaryWireDecode.Failure(status);
    }
}