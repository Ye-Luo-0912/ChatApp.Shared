using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary.Schemas;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

/// <summary>
/// 群通话阶段一 wire 加性字段（TcpCallGrant.CallKind/Participants、TcpCallSignal.Event/
/// ParticipantUserId）的二进制编解码测试。
/// <para>
/// 兼容性红线：Direct grant / 1:1 signal（新字段为缺省值）的 binary 编码不得写出新字段——
/// 经 registry decode 后新属性保持 null 即为证明。
/// </para>
/// </summary>
public sealed class TcpGroupCallWireTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    private delegate BinaryStatus EncodeDelegate<T>(
        in T value, Span<byte> destination, BinaryLimits limits, out int written);

    [Fact]
    public void GroupGrantRoundTripsThroughRegistryDispatch()
    {
        var request = new TcpCallCommandRequest
        {
            RequestId = "req-group-1",
            CommandId = "cmd-group-1",
            CallId = "call-group-1",
            Type = TcpCallCommandType.Invite,
            ActorUserId = 42,
            Revision = 7,
            Grant = new TcpCallGrant
            {
                CallId = "call-group-1",
                CallerUserId = 42,
                CalleeUserId = 0,
                ExpiresAtMs = 1_700_000_090_000,
                Nonce = "nonce-group-1",
                Signature = "sig-group",
                CallKind = TcpCallKind.Group,
                Participants = [42, 43, 44]
            },
            Sdp = "v=0\r\noffer",
            ClientOccurredAtMs = 1_700_000_040_000
        };

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandRequest,
            Encode(in request, TcpCallCommandRequestSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var grant = Assert.IsType<TcpCallCommandRequest>(result.Value).Grant;
        Assert.NotNull(grant);
        Assert.Equal(TcpCallKind.Group, grant.CallKind);
        Assert.Equal(new long[] { 42, 43, 44 }, grant.Participants);
        Assert.Equal("sig-group", grant.Signature);
        Assert.Equal(0, grant.CalleeUserId);
    }

    [Fact]
    public void GroupGrantSegmentedDecodeRoundTrips()
    {
        var grant = new TcpCallGrant
        {
            CallId = "call-group-2",
            CallerUserId = 10,
            ExpiresAtMs = 1_700_000_090_000,
            Nonce = "n",
            CallKind = TcpCallKind.Group,
            Participants = [10, 20, 30, 40]
        };
        var request = new TcpCallCommandRequest
        {
            CommandId = "c",
            CallId = "call-group-2",
            Type = TcpCallCommandType.Invite,
            ActorUserId = 10,
            Revision = 1,
            Grant = grant
        };
        byte[] payload = Encode(in request, TcpCallCommandRequestSchema.TryEncode, Limits);
        ReadOnlySequence<byte> segmented = SequenceFactory.Segmented(payload, 1);

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandRequest, in segmented, Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var decodedGrant = Assert.IsType<TcpCallCommandRequest>(result.Value).Grant;
        Assert.NotNull(decodedGrant);
        Assert.Equal(TcpCallKind.Group, decodedGrant.CallKind);
        Assert.Equal(new long[] { 10, 20, 30, 40 }, decodedGrant.Participants);
    }

    [Fact]
    public void LegacyGrantEncodesWithoutGroupFields()
    {
        var legacy = new TcpCallGrant
        {
            CallId = "call-legacy",
            CallerUserId = 42,
            CalleeUserId = 43,
            ExpiresAtMs = 1_700_000_090_000,
            Nonce = "nonce-legacy",
            Signature = "sig"
        };
        byte[] legacyPayload = Encode(in legacy, TcpCallGrantSchema.TryEncode, Limits);

        // 显式 Direct 与缺省逐字节一致（Direct == 缺省语义，不写出 field 7）。
        var explicitDirect = new TcpCallGrant
        {
            CallId = "call-legacy",
            CallerUserId = 42,
            CalleeUserId = 43,
            ExpiresAtMs = 1_700_000_090_000,
            Nonce = "nonce-legacy",
            Signature = "sig",
            CallKind = TcpCallKind.Direct
        };
        byte[] explicitPayload = Encode(in explicitDirect, TcpCallGrantSchema.TryEncode, Limits);
        Assert.Equal(legacyPayload, explicitPayload);

        // decode 侧新属性保持 null → 编码端确实未写出 field 7/8（0.5.6 旧解码端零改动）。
        var legacyRequest = new TcpCallCommandRequest
        {
            CommandId = "c",
            CallId = "call-legacy",
            Type = TcpCallCommandType.Invite,
            ActorUserId = 42,
            Revision = 1,
            Grant = legacy
        };
        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandRequest,
            Encode(in legacyRequest, TcpCallCommandRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var decodedGrant = Assert.IsType<TcpCallCommandRequest>(result.Value).Grant;
        Assert.NotNull(decodedGrant);
        Assert.Null(decodedGrant.CallKind);
        Assert.Null(decodedGrant.Participants);
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
            OccurredAtMs = 1_700_000_030_000,
            Event = TcpCallConstants.SignalEventParticipantLeft,
            ParticipantUserId = 43
        };

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallSignal,
            Encode(in signal, TcpCallSignalSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var actual = Assert.IsType<TcpCallSignal>(result.Value);
        Assert.Equal(TcpCallConstants.SignalEventParticipantLeft, actual.Event);
        Assert.Equal(43, actual.ParticipantUserId);
        Assert.Equal(TcpCallCommandType.End, actual.Kind);
    }

    [Fact]
    public void UnknownEventAndCallKindValuesArePreservedForTolerantConsumers()
    {
        // unknown event 值（词表外取值）必须原样往返，由接收端容忍跳过（前向兼容）。
        var unknownEventSignal = new TcpCallSignal
        {
            SignalId = "sig-future",
            CallId = "call-group-1",
            FromUserId = 43,
            ToUserId = 42,
            Kind = TcpCallCommandType.Invite,
            Sdp = string.Empty,
            Revision = 1,
            OccurredAtMs = 1,
            Event = "speaker-changed"
        };
        TcpBinaryWireDecode signalResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallSignal,
            Encode(in unknownEventSignal, TcpCallSignalSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, signalResult.Status);
        Assert.Equal(
            "speaker-changed",
            Assert.IsType<TcpCallSignal>(signalResult.Value).Event);

        // 未知 CallKind 数值保留（消费端按 fail-closed 拒绝）。
        var unknownKindRequest = new TcpCallCommandRequest
        {
            CommandId = "c",
            CallId = "c",
            Type = TcpCallCommandType.Invite,
            ActorUserId = 1,
            Revision = 1,
            Grant = new TcpCallGrant
            {
                CallId = "c",
                CallerUserId = 1,
                ExpiresAtMs = 1,
                Nonce = "n",
                CallKind = (TcpCallKind)99
            }
        };
        TcpBinaryWireDecode grantResult = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandRequest,
            Encode(in unknownKindRequest, TcpCallCommandRequestSchema.TryEncode, Limits),
            Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, grantResult.Status);
        Assert.Equal(
            (TcpCallKind)99,
            Assert.IsType<TcpCallCommandRequest>(grantResult.Value).Grant!.CallKind);
    }

    [Fact]
    public void LegacySignalEncodesWithoutGroupFields()
    {
        var signal = new TcpCallSignal
        {
            SignalId = "sig-1",
            CallId = "call-1",
            FromUserId = 1,
            ToUserId = 2,
            Kind = TcpCallCommandType.Invite,
            Sdp = "v=0",
            Revision = 1,
            OccurredAtMs = 1
        };
        byte[] payload = Encode(in signal, TcpCallSignalSchema.TryEncode, Limits);

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallSignal, payload, Limits);

        // decode 侧 Event/ParticipantUserId 保持 null → 未写出 field 9/10（0.5.6 兼容）。
        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var actual = Assert.IsType<TcpCallSignal>(result.Value);
        Assert.Null(actual.Event);
        Assert.Null(actual.ParticipantUserId);
    }

    // ---- 0.5.8 加性字段：逐成员 invite 目标（command）与 invite 随信令下发 grant（signal） ----

    [Fact]
    public void InviteTargetOnCommandRequestRoundTrips()
    {
        var request = new TcpCallCommandRequest
        {
            RequestId = "req-target-1",
            CommandId = "cmd-target-1",
            CallId = "call-group-1",
            Type = TcpCallCommandType.Invite,
            ActorUserId = 42,
            Revision = 3,
            ParticipantUserId = 44
        };

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandRequest,
            Encode(in request, TcpCallCommandRequestSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        Assert.Equal(44, Assert.IsType<TcpCallCommandRequest>(result.Value).ParticipantUserId);
    }

    [Fact]
    public void CommandRequestWithoutInviteTargetEncodesWithoutNewField()
    {
        // 1:1 / 广播形态（目标 null）与 0.5.7 逐字节一致：decode 侧 ParticipantUserId 保持 null。
        var request = new TcpCallCommandRequest
        {
            RequestId = "req-1",
            CommandId = "cmd-1",
            CallId = "call-1",
            Type = TcpCallCommandType.Accept,
            ActorUserId = 7,
            Revision = 1
        };

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallCommandRequest,
            Encode(in request, TcpCallCommandRequestSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        Assert.Null(Assert.IsType<TcpCallCommandRequest>(result.Value).ParticipantUserId);
    }

    [Fact]
    public void SignalWithGrantRoundTripsThroughRegistryDispatch()
    {
        var signal = new TcpCallSignal
        {
            SignalId = "sig-invite-1",
            CallId = "call-group-1",
            FromUserId = 42,
            ToUserId = 44,
            Kind = TcpCallCommandType.Invite,
            Sdp = "v=0\r\noffer-for-44",
            Revision = 1,
            OccurredAtMs = 1_700_000_030_000,
            ParticipantUserId = 44,
            Grant = new TcpCallGrant
            {
                CallId = "call-group-1",
                CallerUserId = 42,
                CalleeUserId = 0,
                ExpiresAtMs = 1_700_000_090_000,
                Nonce = "nonce-group-1",
                Signature = "sig-group",
                CallKind = TcpCallKind.Group,
                Participants = [42, 43, 44]
            }
        };

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallSignal,
            Encode(in signal, TcpCallSignalSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var actual = Assert.IsType<TcpCallSignal>(result.Value);
        Assert.Equal(44, actual.ParticipantUserId);
        Assert.NotNull(actual.Grant);
        Assert.Equal(TcpCallKind.Group, actual.Grant.CallKind);
        Assert.Equal(new long[] { 42, 43, 44 }, actual.Grant.Participants);
        Assert.Equal("sig-group", actual.Grant.Signature);
    }

    [Fact]
    public void SignalWithoutGrantEncodesWithoutNewField()
    {
        // participant-left 事件信令（0.5.7 形态）不写出 field 11：decode 侧 Grant 保持 null。
        var signal = new TcpCallSignal
        {
            SignalId = "sig-left-1",
            CallId = "call-group-1",
            FromUserId = 43,
            ToUserId = 42,
            Kind = TcpCallCommandType.End,
            Sdp = string.Empty,
            Revision = 2,
            OccurredAtMs = 1_700_000_030_000,
            Event = TcpCallConstants.SignalEventParticipantLeft,
            ParticipantUserId = 43
        };

        TcpBinaryWireDecode result = TcpBinaryWireCodec.TryDecode(
            PacketCommand.CallSignal,
            Encode(in signal, TcpCallSignalSchema.TryEncode, Limits),
            Limits);

        Assert.Equal(TcpBinaryWireStatus.Decoded, result.Status);
        var actual = Assert.IsType<TcpCallSignal>(result.Value);
        Assert.Equal(TcpCallConstants.SignalEventParticipantLeft, actual.Event);
        Assert.Null(actual.Grant);
    }

    private static byte[] Encode<T>(
        in T value,
        EncodeDelegate<T> encode,
        BinaryLimits limits)
    {
        byte[] destination = new byte[limits.MaxMessageBytes];
        Assert.Equal(BinaryStatus.Done, encode(in value, destination, limits, out int written));
        return destination[..written].ToArray();
    }
}
