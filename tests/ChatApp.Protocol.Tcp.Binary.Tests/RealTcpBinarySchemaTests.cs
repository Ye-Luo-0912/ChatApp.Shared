using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class RealTcpBinarySchemaTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    [Fact]
    public void ClientHelloGoldenIsStableAndRoundTripsThroughSegmentedDecoder()
    {
        var expected = new ClientHello
        {
            ProtocolVersion = 1,
            FeatureBits = 0xA5,
            InstallationId = "install-01",
            ClientTimeMs = 1_700_000_000_123,
            ResumeToken = "resume-token",
            MaxPayloadBytes = 80 * 1024
        };

        byte[] payload = EncodeClientHello(expected);

        Assert.Equal(
            "080110A5011A0A696E7374616C6C2D303120F6A1ABFEF9622A0C726573756D652D746F6B656E3080800A",
            Convert.ToHexString(payload));
        Assert.Equal(
            "AA2A83D7D5F61D3522FAEACF3091773D348325D816D4FBF03EC9E2EBD386B2AC",
            Convert.ToHexString(SHA256.HashData(payload)));
        Assert.Equal(
            BinaryStatus.Done,
            ClientHelloBinaryDescriptor.TryDecode(
                payload.AsSpan(),
                Limits,
                out ClientHello? contiguous));
        Assert.Equal(
            BinaryStatus.Done,
            ClientHelloBinaryDescriptor.TryDecode(
                SequenceFactory.Segmented(payload, 1),
                Limits,
                out ClientHello? segmented));

        AssertClientHelloEqual(expected, contiguous!);
        AssertClientHelloEqual(expected, segmented!);
    }

    [Fact]
    public void ServerHelloGoldenPreservesHandshakeDefaultsAndRawBinaryFields()
    {
        var expected = new ServerHello
        {
            ProtocolVersion = 1,
            FeatureBits = 0x8000_0001,
            ServerDeviceId = "gateway-device",
            ServerTimeMs = 1_700_000_000_999,
            HeartbeatIntervalMs = 15_000,
            MaxPayloadBytes = 80 * 1024,
            ResumeSupported = true,
            PayloadFormat = ProtocolPayloadFormat.Json
        };

        byte[] payload = EncodeServerHello(expected);

        Assert.Equal(
            "08011081808080081A0E676174657761792D64657669636520CEAFABFEF96228B0EA013080800A380142046A736F6E",
            Convert.ToHexString(payload));
        Assert.Equal(
            "AAF091766CF69388C22762F0F44C4A1BE34BAC3D3260E0F8BBA807E3B258C964",
            Convert.ToHexString(SHA256.HashData(payload)));
        Assert.Equal(
            BinaryStatus.Done,
            ServerHelloBinaryDescriptor.TryDecode(
                SequenceFactory.Segmented(payload, 1),
                Limits,
                out ServerHello? actual));
        AssertServerHelloEqual(expected, actual!);
    }

    [Fact]
    public void HistoryRequestAndCursorKeepUnixMillisecondsAndNullByAbsence()
    {
        var expectedRequest = new MessageHistoryRequest
        {
            RequestId = "history-1",
            ConversationId = "conversation-1",
            BeforeReceivedAtMs = 1_700_000_000_100,
            BeforeMessageId = "msg-before",
            AfterReceivedAtMs = 1_700_000_000_200,
            AfterMessageId = "msg-after",
            Limit = 50
        };
        var expectedCursor = new MessageHistoryCursor
        {
            ReceivedAtMs = 1_700_000_000_300,
            ChangedAtMs = 1_700_000_000_400,
            MessageId = "msg-cursor"
        };

        byte[] requestPayload = EncodeMessageHistoryRequest(expectedRequest);
        byte[] cursorPayload = EncodeMessageHistoryCursor(expectedCursor);

        Assert.Equal(
            "0A09686973746F72792D31120E636F6E766572736174696F6E2D3118C8A1ABFEF962220A6D73672D6265666F72652890A3ABFEF96232096D73672D61667465723864",
            Convert.ToHexString(requestPayload));
        Assert.Equal(
            "08D8A4ABFEF96210A0A6ABFEF9621A0A6D73672D637572736F72",
            Convert.ToHexString(cursorPayload));
        Assert.Equal(
            "BCF897F4F71D3912D6159395FDBE0F1D783BB3DC4B03EA6482F00D86C4163AD0",
            Convert.ToHexString(SHA256.HashData(requestPayload)));
        Assert.Equal(
            "5EC8AEA5B577F1B394099D93D12D7FA497192E0D146ACF5248BE131DB3DEE0AD",
            Convert.ToHexString(SHA256.HashData(cursorPayload)));

        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryRequestBinaryDescriptor.TryDecode(
                SequenceFactory.Segmented(requestPayload, 1),
                Limits,
                out MessageHistoryRequest? request));
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryCursorBinaryDescriptor.TryDecode(
                cursorPayload.AsSpan(),
                Limits,
                out MessageHistoryCursor? cursor));
        AssertMessageHistoryRequestEqual(expectedRequest, request!);
        AssertMessageHistoryCursorEqual(expectedCursor, cursor!);

        var nullDefaults = new MessageHistoryRequest { Limit = 0 };
        byte[] nullDefaultsPayload = EncodeMessageHistoryRequest(nullDefaults);
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryRequestBinaryDescriptor.TryDecode(
                nullDefaultsPayload.AsSpan(),
                Limits,
                out MessageHistoryRequest? decodedDefaults));
        Assert.Null(decodedDefaults!.RequestId);
        Assert.Null(decodedDefaults.ConversationId);
        Assert.Null(decodedDefaults.BeforeReceivedAtMs);
        Assert.Null(decodedDefaults.BeforeMessageId);
        Assert.Null(decodedDefaults.AfterReceivedAtMs);
        Assert.Null(decodedDefaults.AfterMessageId);
        Assert.Equal(0, decodedDefaults.Limit);
    }

    [Fact]
    public void BinarySchemaDoesNotChangeJsonFallbackContract()
    {
        var expected = new MessageHistoryRequest
        {
            RequestId = "json-fallback",
            ConversationId = "conversation-json",
            AfterReceivedAtMs = 1_700_000_000_500,
            AfterMessageId = "message-json",
            Limit = 20
        };

        string json = JsonSerializer.Serialize(
            expected,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryRequest);
        MessageHistoryRequest? roundTrip = JsonSerializer.Deserialize(
            json,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryRequest);

        Assert.NotNull(roundTrip);
        AssertMessageHistoryRequestEqual(expected, roundTrip!);
        Assert.False(ProtocolPayloadFormat.IsValid(BinaryPayloadFormat.Id));
    }

    [Fact]
    public void RealSchemaRejectsOutOfOrderAndOversizedUntrustedInput()
    {
        Assert.Equal(
            BinaryStatus.FieldsOutOfOrder,
            ClientHelloBinaryDescriptor.TryDecode(
                [0x10, 0x02, 0x08, 0x02],
                Limits,
                out _));

        var limited = new BinaryLimits(64, 8, 4, 8, 32);
        var value = new ClientHello { InstallationId = "12345" };
        Span<byte> destination = stackalloc byte[128];
        Assert.Equal(
            BinaryStatus.StringTooLarge,
            ClientHelloBinaryDescriptor.TryEncode(
                in value,
                destination,
                limited,
                out int written));
        Assert.Equal(0, written);
    }

    [Fact]
    public void RealSchemaCorpusCoversDefaultsStringHeavyAndMaximumLegalValues()
    {
        var clientDefaults = new ClientHello();
        byte[] clientDefaultsPayload = EncodeClientHello(clientDefaults);
        Assert.Equal("080110002000", Convert.ToHexString(clientDefaultsPayload));
        Assert.Equal(
            BinaryStatus.Done,
            ClientHelloBinaryDescriptor.TryDecode(
                clientDefaultsPayload,
                Limits,
                out ClientHello? decodedClientDefaults));
        AssertClientHelloEqual(clientDefaults, decodedClientDefaults!);

        var historyDefaults = new MessageHistoryRequest();
        byte[] historyDefaultsPayload = EncodeMessageHistoryRequest(historyDefaults);
        Assert.Equal("3864", Convert.ToHexString(historyDefaultsPayload));
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryRequestBinaryDescriptor.TryDecode(
                historyDefaultsPayload,
                Limits,
                out MessageHistoryRequest? decodedHistoryDefaults));
        AssertMessageHistoryRequestEqual(historyDefaults, decodedHistoryDefaults!);

        var corpusLimits = new BinaryLimits(
            maxMessageBytes: 8 * 1024,
            maxFieldBytes: 2 * 1024,
            maxStringBytes: 1024,
            maxByteArrayBytes: 2 * 1024,
            maxFields: 16);
        string unicodeAtStringLimit = new('界', 341); // 1,023 UTF-8 bytes.
        var maxClient = new ClientHello
        {
            ProtocolVersion = 1,
            FeatureBits = uint.MaxValue,
            InstallationId = unicodeAtStringLimit,
            ClientTimeMs = long.MaxValue,
            ResumeToken = new string('x', 1024),
            MaxPayloadBytes = int.MaxValue
        };
        byte[] maxClientPayload = EncodeClientHello(maxClient, corpusLimits);
        Assert.True(maxClientPayload.Length <= corpusLimits.MaxMessageBytes);
        Assert.Equal(
            BinaryStatus.Done,
            ClientHelloBinaryDescriptor.TryDecode(
                SequenceFactory.Segmented(maxClientPayload, 1),
                corpusLimits,
                out ClientHello? decodedMaxClient));
        AssertClientHelloEqual(maxClient, decodedMaxClient!);

        var maxHistory = new MessageHistoryRequest
        {
            RequestId = unicodeAtStringLimit,
            ConversationId = new string('x', 1024),
            BeforeReceivedAtMs = long.MinValue,
            BeforeMessageId = unicodeAtStringLimit,
            AfterReceivedAtMs = long.MaxValue,
            AfterMessageId = new string('y', 1024),
            Limit = int.MaxValue
        };
        byte[] maxHistoryPayload = EncodeMessageHistoryRequest(maxHistory, corpusLimits);
        Assert.True(maxHistoryPayload.Length <= corpusLimits.MaxMessageBytes);
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryRequestBinaryDescriptor.TryDecode(
                maxHistoryPayload,
                corpusLimits,
                out MessageHistoryRequest? decodedMaxHistory));
        AssertMessageHistoryRequestEqual(maxHistory, decodedMaxHistory!);
    }

    [Fact]
    public void RealSchemaMalformedCorpusFailsClosedForBothDecoderPaths()
    {
        RealDecodeCase[] clientCases =
        [
            new("invalid-utf8", [0x1A, 0x01, 0xFF], BinaryStatus.InvalidUtf8),
            new("truncated-string", [0x1A, 0x02, 0x61], BinaryStatus.Truncated),
            new("wrong-wire", [0x0A, 0x00], BinaryStatus.WireTypeMismatch),
            new("duplicate", [0x08, 0x01, 0x08, 0x01], BinaryStatus.DuplicateField),
            new("out-of-order", [0x10, 0x01, 0x08, 0x01], BinaryStatus.FieldsOutOfOrder),
            new(
                "string-limit",
                [0x1A, 0x05, 0x31, 0x32, 0x33, 0x34, 0x35],
                BinaryStatus.StringTooLarge,
                new BinaryLimits(64, 8, 4, 8, 8))
        ];

        foreach (RealDecodeCase testCase in clientCases)
        {
            Assert.Equal(
                testCase.Status,
                ClientHelloBinaryDescriptor.TryDecode(
                    testCase.Payload,
                    testCase.Limits,
                    out ClientHello? contiguous));
            ReadOnlySequence<byte> sequence = SequenceFactory.Segmented(testCase.Payload, 1);
            Assert.Equal(
                testCase.Status,
                ClientHelloBinaryDescriptor.TryDecode(
                    in sequence,
                    testCase.Limits,
                    out ClientHello? segmented));
            Assert.Null(contiguous);
            Assert.Null(segmented);
        }

        RealDecodeCase[] historyCases =
        [
            new("invalid-utf8", [0x0A, 0x01, 0xFF], BinaryStatus.InvalidUtf8),
            new("truncated-string", [0x0A, 0x02, 0x61], BinaryStatus.Truncated),
            new("wrong-wire", [0x3A, 0x00], BinaryStatus.WireTypeMismatch),
            new("out-of-order", [0x12, 0x00, 0x0A, 0x00], BinaryStatus.FieldsOutOfOrder),
            new(
                "string-limit",
                [0x0A, 0x05, 0x31, 0x32, 0x33, 0x34, 0x35],
                BinaryStatus.StringTooLarge,
                new BinaryLimits(64, 8, 4, 8, 8))
        ];

        foreach (RealDecodeCase testCase in historyCases)
        {
            Assert.Equal(
                testCase.Status,
                MessageHistoryRequestBinaryDescriptor.TryDecode(
                    testCase.Payload,
                    testCase.Limits,
                    out MessageHistoryRequest? contiguous));
            ReadOnlySequence<byte> sequence = SequenceFactory.Segmented(testCase.Payload, 1);
            Assert.Equal(
                testCase.Status,
                MessageHistoryRequestBinaryDescriptor.TryDecode(
                    in sequence,
                    testCase.Limits,
                    out MessageHistoryRequest? segmented));
            Assert.Null(contiguous);
            Assert.Null(segmented);
        }
    }

    private static byte[] EncodeClientHello(ClientHello value)
    {
        return EncodeClientHello(value, Limits);
    }

    private static byte[] EncodeClientHello(ClientHello value, BinaryLimits limits)
    {
        byte[] destination = new byte[limits.MaxMessageBytes];
        Assert.Equal(
            BinaryStatus.Done,
            ClientHelloBinaryDescriptor.TryEncode(
                in value,
                destination,
                limits,
                out int written));
        return destination[..written].ToArray();
    }

    private static byte[] EncodeServerHello(ServerHello value)
    {
        Span<byte> destination = stackalloc byte[512];
        Assert.Equal(
            BinaryStatus.Done,
            ServerHelloBinaryDescriptor.TryEncode(
                in value,
                destination,
                Limits,
                out int written));
        return destination[..written].ToArray();
    }

    private static byte[] EncodeMessageHistoryRequest(MessageHistoryRequest value)
    {
        return EncodeMessageHistoryRequest(value, Limits);
    }

    private static byte[] EncodeMessageHistoryRequest(
        MessageHistoryRequest value,
        BinaryLimits limits)
    {
        byte[] destination = new byte[limits.MaxMessageBytes];
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryRequestBinaryDescriptor.TryEncode(
                in value,
                destination,
                limits,
                out int written));
        return destination[..written].ToArray();
    }

    private static byte[] EncodeMessageHistoryCursor(MessageHistoryCursor value)
    {
        Span<byte> destination = stackalloc byte[256];
        Assert.Equal(
            BinaryStatus.Done,
            MessageHistoryCursorBinaryDescriptor.TryEncode(
                in value,
                destination,
                Limits,
                out int written));
        return destination[..written].ToArray();
    }

    private static void AssertClientHelloEqual(ClientHello expected, ClientHello actual) =>
        Assert.Equal(
            (expected.ProtocolVersion, expected.FeatureBits, expected.InstallationId,
                expected.ClientTimeMs, expected.ResumeToken, expected.MaxPayloadBytes),
            (actual.ProtocolVersion, actual.FeatureBits, actual.InstallationId,
                actual.ClientTimeMs, actual.ResumeToken, actual.MaxPayloadBytes));

    private static void AssertServerHelloEqual(ServerHello expected, ServerHello actual) =>
        Assert.Equal(
            (expected.ProtocolVersion, expected.FeatureBits, expected.ServerDeviceId,
                expected.ServerTimeMs, expected.HeartbeatIntervalMs, expected.MaxPayloadBytes,
                expected.ResumeSupported, expected.PayloadFormat),
            (actual.ProtocolVersion, actual.FeatureBits, actual.ServerDeviceId,
                actual.ServerTimeMs, actual.HeartbeatIntervalMs, actual.MaxPayloadBytes,
                actual.ResumeSupported, actual.PayloadFormat));

    private static void AssertMessageHistoryRequestEqual(
        MessageHistoryRequest expected,
        MessageHistoryRequest actual) =>
        Assert.Equal(
            (expected.RequestId, expected.ConversationId, expected.BeforeReceivedAtMs,
                expected.BeforeMessageId, expected.AfterReceivedAtMs, expected.AfterMessageId,
                expected.Limit),
            (actual.RequestId, actual.ConversationId, actual.BeforeReceivedAtMs,
                actual.BeforeMessageId, actual.AfterReceivedAtMs, actual.AfterMessageId,
                actual.Limit));

    private static void AssertMessageHistoryCursorEqual(
        MessageHistoryCursor expected,
        MessageHistoryCursor actual) =>
        Assert.Equal(
            (expected.ReceivedAtMs, expected.ChangedAtMs, expected.MessageId),
            (actual.ReceivedAtMs, actual.ChangedAtMs, actual.MessageId));

    private sealed record RealDecodeCase(
        string Name,
        byte[] Payload,
        BinaryStatus Status,
        BinaryLimits? ConfiguredLimits = null)
    {
        public BinaryLimits Limits => ConfiguredLimits ?? RealTcpBinarySchemaTests.Limits;
    }
}
