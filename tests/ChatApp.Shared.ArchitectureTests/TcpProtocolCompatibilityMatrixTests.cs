using System.Text.Json;
using System.Text.Json.Serialization;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class TcpProtocolCompatibilityMatrixTests
{
    [Fact]
    public void LegacyReaderAcceptsNewHistoryResponseAndIgnoresOptionalFields()
    {
        var current = new MessageHistoryResponse
        {
            RequestId = "history-01",
            ConversationId = "conversation-01",
            Succeeded = true,
            Items =
            [
                new MessageHistoryItem
                {
                    MessageId = "message-01",
                    ClientMessageId = "client-01",
                    ConversationId = "conversation-01",
                    Content = "hello",
                    ReceivedAtMs = 1_735_689_600_100,
                    ChangedAtMs = 1_735_689_600_200
                }
            ],
            NextCursor = new MessageHistoryCursor
            {
                ReceivedAtMs = 1_735_689_600_100,
                ChangedAtMs = 1_735_689_600_200,
                MessageId = "message-01"
            }
        };

        var json = JsonSerializer.Serialize(
            current,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryResponse);
        var legacy = JsonSerializer.Deserialize(
            json,
            LegacyTcpJsonContext.Default.LegacyMessageHistoryResponse);

        Assert.NotNull(legacy);
        Assert.Equal("history-01", legacy.RequestId);
        Assert.Equal("message-01", legacy.Items.Single().MessageId);
        Assert.Equal(1_735_689_600_100, legacy.NextCursor?.ReceivedAtMs);
    }

    [Fact]
    public void CurrentReaderAcceptsLegacyHistoryResponseAndPreservesFallbackState()
    {
        var legacy = new LegacyMessageHistoryResponse
        {
            RequestId = "history-01",
            Succeeded = true,
            Items =
            [
                new LegacyMessageHistoryItem
                {
                    MessageId = "message-01",
                    Content = "hello",
                    ReceivedAtMs = 1_735_689_600_100
                }
            ],
            NextCursor = new LegacyMessageHistoryCursor
            {
                ReceivedAtMs = 1_735_689_600_100,
                MessageId = "message-01"
            }
        };

        var json = JsonSerializer.Serialize(
            legacy,
            LegacyTcpJsonContext.Default.LegacyMessageHistoryResponse);
        var current = JsonSerializer.Deserialize(
            json,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryResponse);

        Assert.NotNull(current);
        Assert.Null(current.ConversationId);
        Assert.Equal(string.Empty, current.Items.Single().ClientMessageId);
        Assert.Equal(0, current.Items.Single().ChangedAtMs);
        Assert.Null(current.NextCursor?.ChangedAtMs);
    }

    [Fact]
    public void CurrentReaderIgnoresUnknownOptionalFields()
    {
        const string json = """
            {"requestId":"history-01","conversationId":"conversation-01","succeeded":true,"items":[],"hasMore":false,"futureOptional":{"enabled":true}}
            """;

        var current = JsonSerializer.Deserialize(
            json,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryResponse);

        Assert.NotNull(current);
        Assert.True(current.Succeeded);
        Assert.Equal("conversation-01", current.ConversationId);
    }

    [Fact]
    public void CurrentReaderPreservesUnknownNumericEnumForForwardCompatibility()
    {
        const string json = """
            {"requestId":"sync-01","succeeded":true,"serverTimeMs":1735689600200,"conversations":[],"conversationsHasMore":false,"catchUps":[],"resetsRequired":[{"conversationId":"conversation-01","reason":255}]}
            """;

        var current = JsonSerializer.Deserialize(
            json,
            TcpProtocolJsonSerializerContext.Default.SyncBootstrapResponse);

        Assert.NotNull(current);
        Assert.Equal((TcpSyncCursorResetReason)255, current.ResetsRequired.Single().Reason);
    }

    [Fact]
    public void CursorDirectionsAndUnixMillisecondUnitsRoundTripExactly()
    {
        var request = new MessageHistoryRequest
        {
            RequestId = "history-01",
            ConversationId = "conversation-01",
            BeforeReceivedAtMs = 1_735_689_600_001,
            BeforeMessageId = "message-before",
            AfterReceivedAtMs = 1_735_689_600_999,
            AfterMessageId = "message-after",
            Limit = 17
        };

        var json = JsonSerializer.Serialize(
            request,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryRequest);
        var roundTrip = JsonSerializer.Deserialize(
            json,
            TcpProtocolJsonSerializerContext.Default.MessageHistoryRequest);

        Assert.NotNull(roundTrip);
        Assert.Equal(request.BeforeReceivedAtMs, roundTrip.BeforeReceivedAtMs);
        Assert.Equal(request.AfterReceivedAtMs, roundTrip.AfterReceivedAtMs);
        Assert.Equal(request.BeforeMessageId, roundTrip.BeforeMessageId);
        Assert.Equal(request.AfterMessageId, roundTrip.AfterMessageId);
    }

    [Fact]
    public void TruncatedPayloadIsRejectedInsteadOfPartiallyMaterialized()
    {
        const string truncated =
            "{\"requestId\":\"sync-01\",\"succeeded\":true,\"catchUps\":[{";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            truncated,
            TcpProtocolJsonSerializerContext.Default.SyncBootstrapResponse));
    }
}

internal sealed class LegacyMessageHistoryCursor
{
    public long ReceivedAtMs { get; set; }
    public string MessageId { get; set; } = string.Empty;
}

internal sealed class LegacyMessageHistoryItem
{
    public string MessageId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long ReceivedAtMs { get; set; }
}

internal sealed class LegacyMessageHistoryResponse
{
    public string? RequestId { get; set; }
    public bool Succeeded { get; set; }
    public IReadOnlyList<LegacyMessageHistoryItem> Items { get; set; } = [];
    public LegacyMessageHistoryCursor? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LegacyMessageHistoryResponse))]
internal sealed partial class LegacyTcpJsonContext : JsonSerializerContext;
