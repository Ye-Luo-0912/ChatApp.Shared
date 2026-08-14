using System.Text.Json;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class TcpProtocolJsonGoldenTests
{
    private static TcpProtocolJsonSerializerContext JsonContext =>
        TcpProtocolJsonSerializerContext.Default;

    [Fact]
    public void ClientHelloMatchesGoldenJson()
    {
        var value = new ClientHello
        {
            ProtocolVersion = 1,
            FeatureBits = (uint)(GatewayFeature.CommandCapabilities | GatewayFeature.SessionResume),
            InstallationId = "0123456789abcdef0123456789abcdef",
            ClientTimeMs = 1_735_689_600_123,
            ResumeToken = "resume-token",
            MaxPayloadBytes = 1_048_576
        };

        const string expected = """
            {"protocolVersion":1,"featureBits":24,"installationId":"0123456789abcdef0123456789abcdef","clientTimeMs":1735689600123,"resumeToken":"resume-token","maxPayloadBytes":1048576}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.ClientHello));
    }

    [Fact]
    public void ServerHelloMatchesGoldenJson()
    {
        var value = new ServerHello
        {
            ProtocolVersion = 1,
            FeatureBits = (uint)(GatewayFeature.CommandCapabilities | GatewayFeature.SessionResume),
            ServerDeviceId = "fedcba9876543210fedcba9876543210",
            ServerTimeMs = 1_735_689_600_456,
            HeartbeatIntervalMs = 15_000,
            MaxPayloadBytes = 4_194_304,
            ResumeSupported = true,
            PayloadFormat = ProtocolPayloadFormat.Json
        };

        const string expected = """
            {"protocolVersion":1,"featureBits":24,"serverDeviceId":"fedcba9876543210fedcba9876543210","serverTimeMs":1735689600456,"heartbeatIntervalMs":15000,"maxPayloadBytes":4194304,"resumeSupported":true,"payloadFormat":"json"}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.ServerHello));
    }

    [Fact]
    public void GoAwayMatchesGoldenJson()
    {
        var value = new GoAway
        {
            RetryAfterMs = 5_000,
            Reason = "upgrade",
            ServerHint = "gateway-02.example:5000"
        };

        const string expected = """
            {"retryAfterMs":5000,"reason":"upgrade","serverHint":"gateway-02.example:5000"}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.GoAway));
    }

    [Fact]
    public void ResumeResponseMatchesGoldenJsonAndOmitsNulls()
    {
        var value = new ResumeResponse
        {
            Success = true,
            FailureKind = ResumeFailureKind.None,
            ResumeToken = "rotated-resume-token",
            UserId = 42,
            SessionId = "session-01",
            DeviceId = "device-01",
            LastConversationSequence = 987_654,
            ErrorMessage = null,
            RetryAfterMs = null
        };

        const string expected = """
            {"success":true,"failureKind":0,"resumeToken":"rotated-resume-token","userId":42,"sessionId":"session-01","deviceId":"device-01","lastConversationSequence":987654}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.ResumeResponse));
    }

    [Fact]
    public void ProtocolErrorFrameMatchesGoldenJson()
    {
        var value = new ProtocolErrorFrame
        {
            Code = ProtocolErrorCode.RateLimited,
            Fatal = false,
            RetryAfterMs = 2_500,
            Message = "rate limited",
            OriginCommand = (ushort)PacketCommand.ChatMessage
        };

        const string expected = """
            {"code":20,"fatal":false,"retryAfterMs":2500,"message":"rate limited","originCommand":101}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.ProtocolErrorFrame));
    }

    [Fact]
    public void MessageHistoryRequestMatchesGoldenJson()
    {
        var value = new MessageHistoryRequest
        {
            RequestId = "request-01",
            ConversationId = "conversation-01",
            AfterReceivedAtMs = 1_735_689_600_123,
            AfterMessageId = "message-09",
            Limit = 50
        };

        const string expected = """
            {"requestId":"request-01","conversationId":"conversation-01","afterReceivedAtMs":1735689600123,"afterMessageId":"message-09","limit":50}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.MessageHistoryRequest));
    }

    [Fact]
    public void MessageHistoryResponseMatchesGoldenJson()
    {
        var value = new MessageHistoryResponse
        {
            RequestId = "request-01",
            ConversationId = "conversation-01",
            Succeeded = true,
            Items =
            [
                new MessageHistoryItem
                {
                    MessageId = "message-10",
                    ClientMessageId = "client-message-10",
                    SenderUserId = 7,
                    ReceiverUserId = 8,
                    ConversationId = "conversation-01",
                    Content = "hello",
                    ReceivedAtMs = 1_735_689_600_000,
                    EditVersion = 1,
                    ChangedAtMs = 1_735_689_600_100,
                    Attachments =
                    [
                        new TcpAttachmentRef
                        {
                            AttachmentId = "attachment-01",
                            FileName = "voice.opus",
                            ContentType = "audio/opus",
                            SizeBytes = 1234,
                            Status = 1
                        }
                    ],
                    Reactions =
                    [
                        new MessageReactionSummary { Emoji = "👍", Count = 2, ReactedByMe = true }
                    ],
                    MentionedUserIds = [8],
                    MentionedRoles = ["admin"]
                }
            ],
            NextCursor = new MessageHistoryCursor
            {
                ReceivedAtMs = 1_735_689_600_000,
                ChangedAtMs = 1_735_689_600_100,
                MessageId = "message-10"
            },
            HasMore = true
        };

        const string expected = """
            {"requestId":"request-01","conversationId":"conversation-01","succeeded":true,"items":[{"messageId":"message-10","clientMessageId":"client-message-10","senderUserId":7,"receiverUserId":8,"conversationId":"conversation-01","content":"hello","receivedAtMs":1735689600000,"editVersion":1,"changedAtMs":1735689600100,"attachments":[{"refVersion":1,"attachmentId":"attachment-01","fileName":"voice.opus","contentType":"audio/opus","sizeBytes":1234,"status":1,"isVoice":false}],"reactions":[{"emoji":"\uD83D\uDC4D","count":2,"reactedByMe":true}],"mentionedUserIds":[8],"mentionedRoles":["admin"]}],"nextCursor":{"receivedAtMs":1735689600000,"changedAtMs":1735689600100,"messageId":"message-10"},"hasMore":true}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.MessageHistoryResponse));
    }

    [Fact]
    public void MessageHistoryResponseReadsLegacyPayloadWithoutConversationIdOrChangedAt()
    {
        const string json = """
            {"requestId":"request-01","succeeded":true,"items":[],"nextCursor":{"receivedAtMs":1735689600000,"messageId":"message-10"},"hasMore":true}
            """;

        MessageHistoryResponse? value = JsonSerializer.Deserialize(
            json,
            JsonContext.MessageHistoryResponse);

        Assert.NotNull(value);
        Assert.Null(value.ConversationId);
        Assert.Null(value.NextCursor?.ChangedAtMs);
    }

    [Fact]
    public void TcpAttachmentRefVoiceMetadataMatchesGoldenJson()
    {
        var value = new TcpAttachmentRef
        {
            AttachmentId = "voice-01",
            FileName = "voice.opus",
            ContentType = "audio/opus",
            SizeBytes = 1234,
            Status = 1,
            IsVoice = true,
            VoiceCodec = "opus",
            VoiceContainer = "ogg",
            VoiceDurationMs = 4_500,
            VoiceSampleRateHz = 48_000,
            VoiceChannels = 1
        };

        const string expected = """
            {"refVersion":1,"attachmentId":"voice-01","fileName":"voice.opus","contentType":"audio/opus","sizeBytes":1234,"status":1,"isVoice":true,"voiceCodec":"opus","voiceContainer":"ogg","voiceDurationMs":4500,"voiceSampleRateHz":48000,"voiceChannels":1}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpAttachmentRef));
    }

    [Fact]
    public void TcpAttachmentRefReadsLegacyPayloadWithoutVoiceFields()
    {
        // 旧客户端（或旧 Server）不写语音字段：非语音附件仍可正常反序列化，语音字段为 null/默认。
        const string json = """
            {"refVersion":1,"attachmentId":"plain-01","fileName":"doc.pdf","contentType":"application/pdf","sizeBytes":2048,"status":1}
            """;

        TcpAttachmentRef? value = JsonSerializer.Deserialize(json, JsonContext.TcpAttachmentRef);

        Assert.NotNull(value);
        Assert.Equal("plain-01", value.AttachmentId);
        Assert.False(value.IsVoice);
        Assert.Null(value.VoiceCodec);
        Assert.Null(value.VoiceContainer);
        Assert.Null(value.VoiceDurationMs);
        Assert.Null(value.VoiceSampleRateHz);
        Assert.Null(value.VoiceChannels);
    }

    [Fact]
    public void TcpAttachmentRefIgnoresUnknownFieldsAndRoundTripsVoice()
    {
        // 新客户端发送的语音字段 + 未来未知字段：反序列化忽略未知字段并保留已知语音元数据往返。
        const string json = """
            {"refVersion":1,"attachmentId":"voice-02","fileName":"m.m4a","contentType":"audio/mp4","sizeBytes":5678,"status":1,"isVoice":true,"voiceCodec":"aac","voiceContainer":"m4a","voiceDurationMs":9800,"voiceSampleRateHz":44100,"voiceChannels":2,"futureUnknown":123}
            """;

        TcpAttachmentRef? value = JsonSerializer.Deserialize(json, JsonContext.TcpAttachmentRef);

        Assert.NotNull(value);
        Assert.True(value.IsVoice);
        Assert.Equal("aac", value.VoiceCodec);
        Assert.Equal("m4a", value.VoiceContainer);
        Assert.Equal(9_800, value.VoiceDurationMs);
        Assert.Equal(44_100, value.VoiceSampleRateHz);
        Assert.Equal((short)2, value.VoiceChannels);

        // 往返序列化后语音字段保持不变。
        var reencoded = JsonSerializer.Serialize(value, JsonContext.TcpAttachmentRef);
        TcpAttachmentRef? roundTrip = JsonSerializer.Deserialize(reencoded, JsonContext.TcpAttachmentRef);
        Assert.NotNull(roundTrip);
        Assert.True(roundTrip.IsVoice);
        Assert.Equal("aac", roundTrip.VoiceCodec);
        Assert.Equal(9_800, roundTrip.VoiceDurationMs);
    }

    [Fact]
    public void SyncBootstrapRequestMatchesGoldenJson()
    {
        var value = new SyncBootstrapRequest
        {
            RequestId = "sync-01",
            ListLimit = 50,
            HistoryLimitPerConversation = 20,
            MaxConversationsWithHistory = 10,
            Watermarks =
            [
                new ConversationSyncWatermark
                {
                    ConversationId = "conversation-01",
                    AfterReceivedAtMs = 1_735_689_600_100,
                    AfterMessageId = "message-10"
                }
            ]
        };

        const string expected = """
            {"requestId":"sync-01","listLimit":50,"historyLimitPerConversation":20,"maxConversationsWithHistory":10,"watermarks":[{"conversationId":"conversation-01","afterReceivedAtMs":1735689600100,"afterMessageId":"message-10"}]}
            """;

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.SyncBootstrapRequest));
    }

    [Fact]
    public void SyncBootstrapResponseReadsLegacyPayloadAndPreservesResetSemantics()
    {
        const string json = """
            {"requestId":"sync-01","succeeded":true,"serverTimeMs":1735689600200,"conversations":[],"conversationsHasMore":false,"catchUps":[{"conversationId":"conversation-01","items":[],"hasMore":true,"nextCursor":{"receivedAtMs":1735689600100,"messageId":"message-10"}}],"resetsRequired":[{"conversationId":"conversation-02","reason":5,"tipMessageId":"message-20","tipReceivedAtMs":1735689600200,"clientAfterReceivedAtMs":1735689500000,"clientAfterMessageId":"message-01"}]}
            """;

        SyncBootstrapResponse? value = JsonSerializer.Deserialize(
            json,
            JsonContext.SyncBootstrapResponse);

        Assert.NotNull(value);
        Assert.Null(value.CatchUps[0].NextCursor?.ChangedAtMs);
        Assert.Equal(TcpSyncCursorResetReason.BeyondRetention, value.ResetsRequired[0].Reason);
        Assert.Equal(1_735_689_600_200, value.ResetsRequired[0].TipReceivedAtMs);
    }

    [Fact]
    public void PropertyNamesAreCaseSensitive()
    {
        const string json = """
            {"ProtocolVersion":9,"featureBits":16,"clientTimeMs":123}
            """;

        ClientHello? value = JsonSerializer.Deserialize(json, JsonContext.ClientHello);

        Assert.NotNull(value);
        Assert.Equal((ushort)1, value.ProtocolVersion);
        Assert.Equal(16u, value.FeatureBits);
        Assert.Equal(123, value.ClientTimeMs);
    }
}
