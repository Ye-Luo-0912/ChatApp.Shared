using System.Text.Json;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Json;
using Xunit;

namespace ChatApp.Shared.ArchitectureTests;

/// <summary>
/// 群通话阶段一（Mesh ≤4 人）wire 契约测试：TcpCallGrant 多人化（CallKind/Participants）、
/// TcpCallSignal.Event 新 kind、以及 <see cref="TcpCallGrantSignature"/> 多人 HMAC。
/// <para>
/// 兼容性红线：Direct grant / 1:1 signal 的 JSON 与 canonical 载荷必须与 0.5.6 逐字节一致。
/// </para>
/// </summary>
public sealed class TcpGroupCallContractTests
{
    private const string Secret = "group-call-signing-test-secret";
    private static readonly long NowMs = 1_700_000_000_000;

    private static TcpProtocolJsonSerializerContext JsonContext =>
        TcpProtocolJsonSerializerContext.Default;

    private static TcpCallGrant GroupGrant() => new()
    {
        CallId = "call-group-1",
        CallerUserId = 42,
        CalleeUserId = 0,
        ExpiresAtMs = NowMs + 60_000,
        Nonce = "nonce-group-1",
        CallKind = TcpCallKind.Group,
        Participants = [42, 43, 44]
    };

    // ---- JSON goldens / round-trip ----

    [Fact]
    public void LegacyDirectGrantJsonIsUnchanged()
    {
        var value = new TcpCallGrant
        {
            CallId = "call-abc",
            CallerUserId = 42,
            CalleeUserId = 7,
            ExpiresAtMs = 1_735_689_600_000,
            Nonce = "n-1",
            Signature = "sig"
        };

        const string expected =
            """{"callId":"call-abc","callerUserId":42,"calleeUserId":7,"expiresAtMs":1735689600000,"nonce":"n-1","signature":"sig"}""";

        Assert.Equal(expected, JsonSerializer.Serialize(value, JsonContext.TcpCallGrant));
    }

    [Fact]
    public void GroupGrantSerializesCallKindAndParticipantsAndRoundTrips()
    {
        var json = JsonSerializer.Serialize(GroupGrant(), JsonContext.TcpCallGrant);

        Assert.Contains("\"callKind\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"participants\":[42,43,44]", json, StringComparison.Ordinal);

        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.TcpCallGrant);
        Assert.NotNull(roundTrip);
        Assert.Equal(TcpCallKind.Group, roundTrip.CallKind);
        Assert.Equal(new long[] { 42, 43, 44 }, roundTrip.Participants);
        Assert.Equal(0, roundTrip.CalleeUserId);
    }

    [Fact]
    public void GrantWithoutCallKindDeserializesAsUnset_TreatedAsDirect()
    {
        const string legacyJson =
            """{"callId":"call-abc","callerUserId":42,"calleeUserId":7,"expiresAtMs":1735689600000,"nonce":"n-1","signature":"sig"}""";

        var grant = JsonSerializer.Deserialize(legacyJson, JsonContext.TcpCallGrant);

        Assert.NotNull(grant);
        Assert.Null(grant.CallKind);
        Assert.Null(grant.Participants);
        // 消费端约定：null（wire 缺省）与 Direct 同等对待 → 旧客户端/旧服务端零改动。
        Assert.True(grant.CallKind is null or TcpCallKind.Direct);
    }

    [Fact]
    public void SignalWithParticipantEventRoundTrips()
    {
        var signal = new TcpCallSignal
        {
            SignalId = "sig-left-1",
            CallId = "call-group-1",
            FromUserId = 43,
            ToUserId = 42,
            Kind = TcpCallCommandType.End,
            Sdp = string.Empty,
            Revision = 2,
            OccurredAtMs = NowMs,
            Event = TcpCallConstants.SignalEventParticipantLeft,
            ParticipantUserId = 43
        };

        var json = JsonSerializer.Serialize(signal, JsonContext.TcpCallSignal);
        Assert.Contains("\"event\":\"participant-left\"", json, StringComparison.Ordinal);
        Assert.Contains("\"participantUserId\":43", json, StringComparison.Ordinal);

        var roundTrip = JsonSerializer.Deserialize(json, JsonContext.TcpCallSignal);
        Assert.NotNull(roundTrip);
        Assert.Equal(TcpCallConstants.SignalEventParticipantLeft, roundTrip.Event);
        Assert.Equal(43, roundTrip.ParticipantUserId);
        Assert.Equal(TcpCallCommandType.End, roundTrip.Kind);
    }

    [Fact]
    public void LegacySignalJsonIsUnchanged()
    {
        var signal = new TcpCallSignal
        {
            SignalId = "sig-1",
            CallId = "call-abc",
            FromUserId = 7,
            ToUserId = 42,
            Kind = TcpCallCommandType.Accept,
            Sdp = "v=0",
            Revision = 3,
            OccurredAtMs = 1_735_689_600_000
        };

        const string expected =
            """{"signalId":"sig-1","callId":"call-abc","fromUserId":7,"toUserId":42,"kind":3,"sdp":"v=0","revision":3,"occurredAtMs":1735689600000}""";

        Assert.Equal(expected, JsonSerializer.Serialize(signal, JsonContext.TcpCallSignal));
    }

    [Fact]
    public void SignalReaderToleratesUnknownEventKind()
    {
        const string json =
            """{"signalId":"sig-9","callId":"call-x","fromUserId":1,"toUserId":2,"kind":1,"sdp":"","revision":1,"occurredAtMs":5,"event":"speaker-changed","participantUserId":9}""";

        var signal = JsonSerializer.Deserialize(json, JsonContext.TcpCallSignal);

        // 前向兼容：unknown kind 值被容忍并原样保留，接收端按约定跳过（不断链、不报协议错误）。
        Assert.NotNull(signal);
        Assert.Equal("speaker-changed", signal.Event);
        Assert.Equal(9, signal.ParticipantUserId);
    }

    // ---- canonical payload / HMAC ----

    [Fact]
    public void DirectCanonicalPayloadMatchesLegacyFormatByteForByte()
    {
        var grant = new TcpCallGrant
        {
            CallId = "call-abc",
            CallerUserId = 1001,
            CalleeUserId = 1002,
            ExpiresAtMs = 1_700_000_060_000,
            Nonce = "nonce-test"
        };

        Assert.Equal(
            "call-abc|1001|1002|1700000060000|nonce-test",
            TcpCallGrantSignature.BuildCanonicalPayload(grant));
    }

    [Fact]
    public void GroupCanonicalPayloadCoversAllParticipantsInOrder()
    {
        Assert.Equal(
            "call-group-1|42|0|1700000060000|nonce-group-1|G|42,43,44",
            TcpCallGrantSignature.BuildCanonicalPayload(GroupGrant()));
    }

    [Fact]
    public void SignThenVerifyRoundTrips_ForDirectAndGroup()
    {
        var direct = new TcpCallGrant
        {
            CallId = "call-d",
            CallerUserId = 42,
            CalleeUserId = 43,
            ExpiresAtMs = NowMs + 60_000,
            Nonce = "n-d"
        };
        direct.Signature = TcpCallGrantSignature.Sign(Secret, direct);
        Assert.True(TcpCallGrantSignature.TryVerify(direct, Secret, NowMs, out var directError));
        Assert.Null(directError);

        var group = GroupGrant();
        group.Signature = TcpCallGrantSignature.Sign(Secret, group);
        Assert.True(TcpCallGrantSignature.TryVerify(group, Secret, NowMs, out var groupError));
        Assert.Null(groupError);
    }

    [Fact]
    public void TamperedParticipantListIsRejected()
    {
        var group = GroupGrant();
        group.Signature = TcpCallGrantSignature.Sign(Secret, group);

        // 替换成员。
        var replaced = GroupGrant();
        replaced.Signature = group.Signature;
        replaced.Participants = [42, 43, 45];
        Assert.False(TcpCallGrantSignature.TryVerify(replaced, Secret, NowMs, out _));

        // 增删成员。
        var dropped = GroupGrant();
        dropped.Signature = group.Signature;
        dropped.Participants = [42, 43];
        Assert.False(TcpCallGrantSignature.TryVerify(dropped, Secret, NowMs, out _));

        // 重排（防规范化形态绕过）。
        var reordered = GroupGrant();
        reordered.Signature = group.Signature;
        reordered.Participants = [44, 42, 43];
        Assert.False(TcpCallGrantSignature.TryVerify(reordered, Secret, NowMs, out _));
    }

    [Fact]
    public void CallKindDowngradeOrUpgradeIsRejected()
    {
        var group = GroupGrant();
        group.Signature = TcpCallGrantSignature.Sign(Secret, group);

        // 群组 grant 冒充 Direct（载荷无群组段 → 签名不匹配）。
        var asDirect = GroupGrant();
        asDirect.Signature = group.Signature;
        asDirect.CallKind = TcpCallKind.Direct;
        Assert.False(TcpCallGrantSignature.TryVerify(asDirect, Secret, NowMs, out _));

        // Direct grant 冒充 Group（载荷缺群组段 → 签名不匹配）。
        var direct = new TcpCallGrant
        {
            CallId = "call-d",
            CallerUserId = 42,
            CalleeUserId = 43,
            ExpiresAtMs = NowMs + 60_000,
            Nonce = "n-d"
        };
        direct.Signature = TcpCallGrantSignature.Sign(Secret, direct);
        var asGroup = new TcpCallGrant
        {
            CallId = "call-d",
            CallerUserId = 42,
            CalleeUserId = 43,
            ExpiresAtMs = NowMs + 60_000,
            Nonce = "n-d",
            CallKind = TcpCallKind.Group,
            Participants = [42, 43],
            Signature = direct.Signature
        };
        Assert.False(TcpCallGrantSignature.TryVerify(asGroup, Secret, NowMs, out _));
    }

    [Fact]
    public void MalformedGroupGrantsAreRejected()
    {
        // 缺主叫。
        var missingCaller = GroupGrant();
        missingCaller.Signature = TcpCallGrantSignature.Sign(Secret, missingCaller);
        missingCaller.Participants = [43, 44];
        Assert.False(TcpCallGrantSignature.TryVerify(missingCaller, Secret, NowMs, out _));

        // 超过 Mesh 上限（4 人）。
        var tooMany = GroupGrant();
        tooMany.Participants = [1, 2, 3, 42, 43];
        Assert.False(TcpCallGrantSignature.TryVerify(tooMany, Secret, NowMs, out _));

        // 单人（<2 人）。
        var solo = GroupGrant();
        solo.Participants = [42];
        Assert.False(TcpCallGrantSignature.TryVerify(solo, Secret, NowMs, out _));

        // 未排序 / 重复。
        var unsorted = GroupGrant();
        unsorted.Participants = [42, 44, 43];
        Assert.False(TcpCallGrantSignature.TryVerify(unsorted, Secret, NowMs, out _));

        // 非法 CalleeUserId（群组必须为 0）。
        var withCallee = GroupGrant();
        withCallee.CalleeUserId = 43;
        Assert.False(TcpCallGrantSignature.TryVerify(withCallee, Secret, NowMs, out _));

        // 未知 CallKind。
        var unknownKind = GroupGrant();
        unknownKind.CallKind = (TcpCallKind)99;
        Assert.False(TcpCallGrantSignature.TryVerify(unknownKind, Secret, NowMs, out _));

        // Direct grant 携带参与者名单 → 拒绝。
        var directWithParticipants = new TcpCallGrant
        {
            CallId = "call-d",
            CallerUserId = 42,
            CalleeUserId = 43,
            ExpiresAtMs = NowMs + 60_000,
            Nonce = "n-d",
            Participants = [42, 43]
        };
        Assert.False(TcpCallGrantSignature.TryVerify(directWithParticipants, Secret, NowMs, out _));
    }

    [Fact]
    public void ExpiredGrantReturnsStableExpiredCode()
    {
        var group = GroupGrant();
        group.Signature = TcpCallGrantSignature.Sign(Secret, group);

        Assert.False(TcpCallGrantSignature.TryVerify(
            group, Secret, group.ExpiresAtMs + 501, out var errorCode));
        Assert.Equal(TcpCallErrorCode.GrantExpired, errorCode);

        // 宽限期内仍有效。
        Assert.True(TcpCallGrantSignature.TryVerify(
            group, Secret, group.ExpiresAtMs + 500, out _));
    }

    [Fact]
    public void MissingSecretOrSignatureFailsClosed()
    {
        var group = GroupGrant();

        Assert.False(TcpCallGrantSignature.TryVerify(group, null, NowMs, out var nullSecretCode));
        Assert.Equal(TcpCallErrorCode.GrantInvalid, nullSecretCode);
        Assert.False(TcpCallGrantSignature.TryVerify(group, "  ", NowMs, out _));

        Assert.False(TcpCallGrantSignature.TryVerify(group, Secret, NowMs, out var noSignatureCode));
        Assert.Equal(TcpCallErrorCode.GrantInvalid, noSignatureCode);

        // 错误密钥。
        group.Signature = TcpCallGrantSignature.Sign(Secret, group);
        Assert.False(TcpCallGrantSignature.TryVerify(group, "other-secret", NowMs, out _));
    }

    [Fact]
    public void WireBudgetConstantsIncludeGroupCap()
    {
        Assert.Equal(4, TcpCallConstants.MaxGroupCallParticipants);
        Assert.Equal("participant-joined", TcpCallConstants.SignalEventParticipantJoined);
        Assert.Equal("participant-left", TcpCallConstants.SignalEventParticipantLeft);
    }
}
