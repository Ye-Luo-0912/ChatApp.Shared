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
