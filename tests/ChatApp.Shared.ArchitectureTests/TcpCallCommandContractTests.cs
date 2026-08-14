using System.Text;
using System.Text.Json;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

public sealed class TcpCallCommandContractTests
{
    private static TcpProtocolJsonSerializerContext JsonContext =>
        TcpProtocolJsonSerializerContext.Default;

    // ---- golden ----

    [Fact]
    public void CallCommandRequestInviteMatchesGoldenJson()
    {
        var value = new TcpCallCommandRequest
        {
            RequestId = "call-01",
            CommandId = "cmd-1",
            CallId = "call-abc",
            Type = TcpCallCommandType.Invite,
            ActorUserId = 42,
            Revision = 1,
            Grant = new TcpCallGrant
            {
                CallId = "call-abc",
                CallerUserId = 42,
                CalleeUserId = 7,
                ExpiresAtMs = 1_735_689_600_000,
                Nonce = "n-1",
                Signature = "sig"
            },
            Sdp = "v=0",
            ClientOccurredAtMs = 1_735_689_600_000
        };

        const string expected =
            """{"requestId":"call-01","commandId":"cmd-1","callId":"call-abc","type":1,"actorUserId":42,"revision":1,"grant":{"callId":"call-abc","callerUserId":42,"calleeUserId":7,"expiresAtMs":1735689600000,"nonce":"n-1","signature":"sig"},"sdp":"v=0","clientOccurredAtMs":1735689600000}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpCallCommandRequest));
    }

    [Fact]
    public void CallCommandRequestOmitsNullOptionalFields()
    {
        var value = new TcpCallCommandRequest
        {
            RequestId = "call-01",
            CommandId = "cmd-1",
            CallId = "call-abc",
            Type = TcpCallCommandType.Reject,
            ActorUserId = 7,
            Revision = 2
        };

        const string expected =
            """{"requestId":"call-01","commandId":"cmd-1","callId":"call-abc","type":4,"actorUserId":7,"revision":2,"clientOccurredAtMs":0}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpCallCommandRequest));
    }

    [Fact]
    public void CallCommandResponseSuccessMatchesGoldenJson()
    {
        var value = new TcpCallCommandResponse
        {
            RequestId = "call-01",
            CallId = "call-abc",
            Succeeded = true,
            State = TcpCallState.Active,
            Revision = 3,
            SignalToForward = new TcpCallSignal
            {
                SignalId = "sig-1",
                CallId = "call-abc",
                FromUserId = 7,
                ToUserId = 42,
                Kind = TcpCallCommandType.Accept,
                Sdp = "v=0",
                Revision = 3,
                OccurredAtMs = 1_735_689_600_000
            }
        };

        const string expected =
            """{"requestId":"call-01","callId":"call-abc","succeeded":true,"state":2,"endReason":0,"revision":3,"replayed":false,"signalToForward":{"signalId":"sig-1","callId":"call-abc","fromUserId":7,"toUserId":42,"kind":3,"sdp":"v=0","revision":3,"occurredAtMs":1735689600000}}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpCallCommandResponse));
    }

    [Fact]
    public void CallCommandResponseFailureOmitsNullStateAndSignal()
    {
        var value = new TcpCallCommandResponse
        {
            RequestId = "call-01",
            CallId = "call-abc",
            Succeeded = false,
            ErrorCode = TcpCallErrorCode.GrantExpired,
            ErrorMessage = "grant expired"
        };

        const string expected =
            """{"requestId":"call-01","callId":"call-abc","succeeded":false,"errorCode":"call_grant_expired","errorMessage":"grant expired","state":0,"endReason":0,"revision":0,"replayed":false}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpCallCommandResponse));
    }

    // ---- round-trip ----

    [Fact]
    public void CallCommandRequestRoundTrips()
    {
        var value = new TcpCallCommandRequest
        {
            RequestId = "call-01",
            CommandId = "cmd-9",
            CallId = "call-xyz",
            Type = TcpCallCommandType.Reconnect,
            ActorUserId = 42,
            Revision = 5,
            Grant = new TcpCallGrant
            {
                CallId = "call-xyz",
                CallerUserId = 42,
                CalleeUserId = 7,
                ExpiresAtMs = 1_735_689_600_000,
                Nonce = "n-9",
                Signature = "sig9"
            },
            Sdp = "v=0\r\n"
        };

        var json = JsonSerializer.Serialize(value, JsonContext.TcpCallCommandRequest);
        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.TcpCallCommandRequest);

        Assert.NotNull(roundTrip);
        Assert.Equal("cmd-9", roundTrip.CommandId);
        Assert.Equal(TcpCallCommandType.Reconnect, roundTrip.Type);
        Assert.Equal("call-xyz", roundTrip.Grant!.CallId);
        Assert.Equal("sig9", roundTrip.Grant.Signature);
    }

    // ---- null / unknown handling ----

    [Fact]
    public void CurrentReaderPreservesUnknownCommandTypeEnumValue()
    {
        const string json =
            """{"requestId":"call-01","commandId":"cmd-1","callId":"call-abc","type":255,"actorUserId":42,"revision":1}""";

        var current = JsonSerializer.Deserialize(json, JsonContext.TcpCallCommandRequest);

        Assert.NotNull(current);
        Assert.Equal((TcpCallCommandType)255, current.Type);
    }

    [Fact]
    public void CurrentReaderIgnoresUnknownOptionalFields()
    {
        const string json =
            """{"requestId":"call-01","commandId":"cmd-1","callId":"call-abc","type":1,"actorUserId":42,"revision":1,"futureOptional":{"enabled":true}}""";

        var current = JsonSerializer.Deserialize(json, JsonContext.TcpCallCommandRequest);

        Assert.NotNull(current);
        Assert.Equal("cmd-1", current.CommandId);
    }

    // ---- budget ----

    [Fact]
    public void CallFieldBudgetsAreStable()
    {
        Assert.Equal(64, TcpCallConstants.MaxCallIdBytes);
        Assert.Equal(64, TcpCallConstants.MaxCommandIdBytes);
        Assert.Equal(16 * 1024, TcpCallConstants.MaxSdpBytes);
        Assert.Equal(32 * 1024, TcpCallConstants.MaxResponseBytes);
    }

    [Fact]
    public void MaxSdpRequestRoundTripsWithinResponseBudget()
    {
        var value = new TcpCallCommandRequest
        {
            RequestId = "call-big",
            CommandId = new string('c', TcpCallConstants.MaxCommandIdBytes),
            CallId = new string('a', TcpCallConstants.MaxCallIdBytes),
            Type = TcpCallCommandType.Invite,
            ActorUserId = 1,
            Revision = 1,
            Sdp = new string('s', TcpCallConstants.MaxSdpBytes)
        };

        var json = JsonSerializer.Serialize(value, JsonContext.TcpCallCommandRequest);
        var bytes = Encoding.UTF8.GetByteCount(json);

        Assert.True(bytes <= TcpCallConstants.MaxResponseBytes,
            $"max-field command wire payload {bytes} exceeds budget {TcpCallConstants.MaxResponseBytes}");

        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.TcpCallCommandRequest);
        Assert.NotNull(roundTrip);
        Assert.Equal(TcpCallConstants.MaxSdpBytes, roundTrip.Sdp!.Length);
    }

    // ---- truncated / malformed codec ----

    [Fact]
    public void TruncatedCallPayloadIsRejectedInsteadOfPartiallyMaterialized()
    {
        const string truncated = """{"requestId":"call-01","commandId":"cmd-1","callId":"call-abc","type":1,""";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            truncated,
            JsonContext.TcpCallCommandRequest));
    }
}