using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary.Schemas;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class TcpBinaryWireCodecTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    [Fact]
    public void NegotiationSwitchRecognizesOnlyTheExactBinaryV1FormatId()
    {
        Assert.Equal("chatapp-bin-v1", TcpBinaryWireCodec.NegotiatedFormatId);
        Assert.True(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("chatapp-bin-v1"));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("json"));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("pb"));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat(null));
        Assert.False(TcpBinaryWireCodec.IsNegotiatedPayloadFormat("CHATAPP-BIN-V1"));
    }

    [Fact]
    public void ControlFramesRoundTripThroughTheRegistryDispatch()
    {
        var serverHello = new ServerHello
        {
            ProtocolVersion = 1,
            FeatureBits = 0x8000_0001,
            ServerDeviceId = "reg-gateway",
            ServerTimeMs = 1_700_000_000_999,
            HeartbeatIntervalMs = 15_000,
            MaxPayloadBytes = 64 * 1024,
            ResumeSupported = true,
            PayloadFormat = TcpBinaryWireCodec.NegotiatedFormatId
        };
        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ServerHello,
            Encode(in serverHello, ServerHelloSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        Assert.Equal(BinaryStatus.Done, result.DecodeStatus);
        var actual = Assert.IsType<ServerHello>(result.Value);
        Assert.Equal(
            (serverHello.ProtocolVersion, serverHello.FeatureBits, serverHello.ServerDeviceId,
                serverHello.ServerTimeMs, serverHello.HeartbeatIntervalMs, serverHello.MaxPayloadBytes,
                serverHello.ResumeSupported, serverHello.PayloadFormat),
            (actual.ProtocolVersion, actual.FeatureBits, actual.ServerDeviceId,
                actual.ServerTimeMs, actual.HeartbeatIntervalMs, actual.MaxPayloadBytes,
                actual.ResumeSupported, actual.PayloadFormat));
    }

    [Fact]
    public void HistoryAndErrorFramesRoundTripThroughTheRegistryDispatch()
    {
        var request = new MessageHistoryRequest
        {
            RequestId = "reg-history",
            ConversationId = "conv-1",
            BeforeReceivedAtMs = 1_700_000_000_100,
            BeforeMessageId = "before",
            AfterReceivedAtMs = 1_700_000_000_200,
            AfterMessageId = "after",
            Limit = 40
        };
        TcpBinaryWireDecode requestResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageHistoryRequest,
            Encode(in request, MessageHistoryRequestSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, requestResult.Status);
        var requestActual = Assert.IsType<MessageHistoryRequest>(requestResult.Value);
        Assert.Equal(
            (request.RequestId, request.ConversationId, request.BeforeReceivedAtMs,
                request.BeforeMessageId, request.AfterReceivedAtMs, request.AfterMessageId,
                request.Limit),
            (requestActual.RequestId, requestActual.ConversationId, requestActual.BeforeReceivedAtMs,
                requestActual.BeforeMessageId, requestActual.AfterReceivedAtMs, requestActual.AfterMessageId,
                requestActual.Limit));

        var error = new ProtocolErrorFrame
        {
            Code = ProtocolErrorCode.UnsupportedCommand,
            Fatal = true,
            RetryAfterMs = 500,
            Message = "binary not negotiated",
            OriginCommand = (ushort)PacketCommand.GoAway
        };
        TcpBinaryWireDecode errorResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.Error,
            Encode(in error, ProtocolErrorFrameSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, errorResult.Status);
        var errorActual = Assert.IsType<ProtocolErrorFrame>(errorResult.Value);
        Assert.Equal(
            (error.Code, error.Fatal, error.RetryAfterMs, error.Message, error.OriginCommand),
            (errorActual.Code, errorActual.Fatal, errorActual.RetryAfterMs, errorActual.Message, errorActual.OriginCommand));
    }

    [Fact]
    public void SegmentedPayloadDecodesThroughTheRegistryDispatch()
    {
        var goAway = new GoAway
        {
            RetryAfterMs = 5_000,
            Reason = "maintenance",
            ServerHint = "retry-01"
        };
        byte[] payload = Encode(in goAway, GoAwaySchema.TryEncode, Limits);
        ReadOnlySequence<byte> segmented = Segmented(payload, 1);

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(PacketCommand.GoAway, in segmented, Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var actual = Assert.IsType<GoAway>(result.Value);
        Assert.Equal(
            (goAway.RetryAfterMs, goAway.Reason, goAway.ServerHint),
            (actual.RetryAfterMs, actual.Reason, actual.ServerHint));
    }

    [Fact]
    public void HistoryPageRoundTripsThroughTheRegistryDispatch()
    {
        var response = new MessageHistoryResponse
        {
            RequestId = "reg-page",
            ConversationId = "conv-1",
            Succeeded = true,
            ErrorCode = null,
            ErrorMessage = null,
            Items =
            [
                new MessageHistoryItem
                {
                    MessageId = "m-1",
                    ClientMessageId = "cm-1",
                    SenderUserId = 7,
                    ReceiverUserId = 8,
                    ConversationId = "conv-1",
                    Content = "hello",
                    ReceivedAtMs = 1_700_000_000_100,
                    ChangedAtMs = 1_700_000_000_100,
                    EditVersion = 1,
                    MentionedUserIds = [9],
                    MentionedRoles = ["role-a"]
                }
            ],
            NextCursor = new MessageHistoryCursor
            {
                ReceivedAtMs = 1_700_000_000_100,
                ChangedAtMs = 1_700_000_000_100,
                MessageId = "m-1"
            },
            HasMore = true
        };

        TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageHistoryPage,
            Encode(in response, MessageHistoryResponseSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, spanResult.Status);
        var spanActual = Assert.IsType<MessageHistoryResponse>(spanResult.Value);
        Assert.Equal(response.RequestId, spanActual.RequestId);
        Assert.Equal(response.Succeeded, spanActual.Succeeded);
        Assert.Single(spanActual.Items);
        Assert.Equal(response.Items[0].MessageId, spanActual.Items[0].MessageId);
        Assert.Equal(response.Items[0].MentionedUserIds, spanActual.Items[0].MentionedUserIds);
        Assert.NotNull(spanActual.NextCursor);
        Assert.Equal(response.NextCursor.MessageId, spanActual.NextCursor.MessageId);
        Assert.Equal(response.HasMore, spanActual.HasMore);

        ReadOnlySequence<byte> segmented = Segmented(
            Encode(in response, MessageHistoryResponseSchema.TryEncode, Limits),
            1);
        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(PacketCommand.MessageHistoryPage, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, sequenceResult.Status);
        var sequenceActual = Assert.IsType<MessageHistoryResponse>(sequenceResult.Value);
        Assert.Single(sequenceActual.Items);
        Assert.Equal(response.Items[0].Content, sequenceActual.Items[0].Content);
    }

    [Fact]
    public void UncoveredCommandsFailClosed()
    {
        foreach (PacketCommand command in new[]
                 {
                     PacketCommand.Heartbeat,
                     PacketCommand.AuthenticationRequest,
                     PacketCommand.ChatMessage,
                     PacketCommand.RelationshipListRequest,
                     PacketCommand.SyncBootstrapRequest,
                     PacketCommand.CallCommandRequest
                 })
        {
            TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(command, [], Limits);
            Assert.Equal(TcpBinaryWireStatus.SchemaNotCovered, spanResult.Status);
            Assert.Null(spanResult.Value);

            ReadOnlySequence<byte> segmented = Segmented([], 1);
            TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(command, in segmented, Limits);
            Assert.Equal(TcpBinaryWireStatus.SchemaNotCovered, sequenceResult.Status);
            Assert.Null(sequenceResult.Value);
        }
    }

    [Fact]
    public void MalformedCoveredPayloadFailsClosedForBothDecoderPaths()
    {
        // Field 1 (varint) followed by a structurally valid but duplicated field for ClientHello.
        byte[] malformed = [0x08, 0x01, 0x08, 0x01];

        TcpBinaryWireDecode spanResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ClientHello,
            malformed.AsSpan(),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, spanResult.Status);
        Assert.Equal(BinaryStatus.DuplicateField, spanResult.DecodeStatus);
        Assert.Null(spanResult.Value);

        TcpBinaryWireDecode sequenceResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ClientHello,
            Segmented(malformed, 1),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, sequenceResult.Status);
        Assert.Equal(BinaryStatus.DuplicateField, sequenceResult.DecodeStatus);
        Assert.Null(sequenceResult.Value);
    }

    [Fact]
    public void OversizedCoveredPayloadFailsClosed()
    {
        var limited = new BinaryLimits(maxMessageBytes: 64, maxFieldBytes: 8, maxStringBytes: 4, maxByteArrayBytes: 8, maxFields: 8);
        var request = new MessageHistoryRequest { RequestId = "too-long-for-limits" };
        byte[] payload = Encode(in request, MessageHistoryRequestSchema.TryEncode, Limits);

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.MessageHistoryRequest,
            payload.AsSpan(),
            limited);

        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, result.Status);
        Assert.NotEqual(BinaryStatus.Done, result.DecodeStatus);
        Assert.Null(result.Value);
    }

    private delegate BinaryStatus EncodeDelegate<T>(in T value, Span<byte> destination, BinaryLimits limits, out int written);

    private static byte[] Encode<T>(in T value, EncodeDelegate<T> encode, BinaryLimits limits)
    {
        byte[] destination = new byte[limits.MaxMessageBytes];
        Assert.Equal(BinaryStatus.Done, encode(in value, destination, limits, out int written));
        return destination[..written].ToArray();
    }

    private static ReadOnlySequence<byte> Segmented(byte[] payload, int segmentSize) =>
        SequenceFactory.Segmented(payload, segmentSize);
}