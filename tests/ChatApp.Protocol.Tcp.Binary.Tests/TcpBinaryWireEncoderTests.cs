using System.Buffers;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary.Schemas;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

/// <summary>
/// Type-to-schema encode registry tests: dispatch correctness against the decode registry,
/// empty keep-alive frames, and fail-closed outcomes for null/uncovered/over-limit values.
/// </summary>
public sealed class TcpBinaryWireEncoderTests
{
    private static readonly BinaryLimits Limits = BinaryLimits.Default;

    private sealed class UncoveredType
    {
    }

    private static byte[] EncodeThroughRegistry<T>(T value) where T : class
    {
        var buffer = new byte[Limits.MaxMessageBytes];
        var result = TcpBinaryWireEncoder.TryEncode(value, buffer, Limits);
        Assert.Equal(TcpBinaryWireEncodeStatus.Encoded, result.Status);
        Assert.True(result.Written > 0 || value is Heartbeat or HeartbeatAcknowledgement);
        return buffer.AsSpan(0, result.Written).ToArray();
    }

    private static void AssertRoundTrip<T>(PacketCommand command, T value, Func<T, T, bool> verify)
        where T : class
    {
        var payload = EncodeThroughRegistry(value);
        var decoded = TcpBinaryWireCodec.TryDecode(command, payload, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, decoded.Status);
        var actual = Assert.IsType<T>(decoded.Value);
        Assert.True(verify(value, actual), $"{typeof(T).Name} round trip mismatch");

        var segmented = SequenceFactory.Segmented(payload, 1);
        var segmentedDecode = TcpBinaryWireCodec.TryDecode(command, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, segmentedDecode.Status);
    }

    [Fact]
    public void EncodeRegistryFailsClosedForNullAndUncoveredValues()
    {
        var buffer = new byte[64];

        var nullResult = TcpBinaryWireEncoder.TryEncode<ChatMessage>(null, buffer, Limits);
        Assert.Equal(TcpBinaryWireEncodeStatus.EncodeFailure, nullResult.Status);
        Assert.Equal(BinaryStatus.MissingRequiredField, nullResult.EncodeStatus);
        Assert.Equal(0, nullResult.Written);

        var uncoveredResult = TcpBinaryWireEncoder.TryEncode(new UncoveredType(), buffer, Limits);
        Assert.Equal(TcpBinaryWireEncodeStatus.SchemaNotCovered, uncoveredResult.Status);
        Assert.Equal(0, uncoveredResult.Written);

        // Nested entry DTOs are encoded inside their owning command schemas; standalone
        // frame payloads they are not, so the registry must not cover them.
        var nestedResult = TcpBinaryWireEncoder.TryEncode(new TcpConversationListItem(), buffer, Limits);
        Assert.Equal(TcpBinaryWireEncodeStatus.SchemaNotCovered, nestedResult.Status);
    }

    [Fact]
    public void EmptyKeepAliveFramesEncodeAsZeroLengthPayloads()
    {
        AssertRoundTrip<Heartbeat>(PacketCommand.Heartbeat, new Heartbeat(), (_, _) => true);
        AssertRoundTrip<HeartbeatAcknowledgement>(
            PacketCommand.HeartbeatAcknowledgement, new HeartbeatAcknowledgement(), (_, _) => true);

        var buffer = new byte[16];
        var heartbeat = TcpBinaryWireEncoder.TryEncode(new Heartbeat(), buffer, Limits);
        Assert.Equal(TcpBinaryWireEncodeStatus.Encoded, heartbeat.Status);
        Assert.Equal(0, heartbeat.Written);

        var nonEmptyDecode = TcpBinaryWireCodec.TryDecode(
            PacketCommand.Heartbeat, new byte[] { 0x08, 0x01 }, Limits);
        Assert.Equal(TcpBinaryWireStatus.DecodeFailure, nonEmptyDecode.Status);
    }

    [Fact]
    public void ControlFramesRoundTripThroughTheEncodeRegistry()
    {
        AssertRoundTrip(PacketCommand.ClientHello, new ClientHello
        {
            ProtocolVersion = 1,
            FeatureBits = 1u,
            InstallationId = "inst-1",
            ClientTimeMs = 1_700_000_000_000,
            ResumeToken = null,
            MaxPayloadBytes = 81_920
        }, (expected, actual) =>
            expected.ProtocolVersion == actual.ProtocolVersion &&
            expected.FeatureBits == actual.FeatureBits &&
            expected.InstallationId == actual.InstallationId &&
            expected.ClientTimeMs == actual.ClientTimeMs &&
            actual.ResumeToken is null &&
            expected.MaxPayloadBytes == actual.MaxPayloadBytes);

        AssertRoundTrip(PacketCommand.ServerHello, new ServerHello
        {
            ProtocolVersion = 1,
            FeatureBits = 2047u,
            ServerDeviceId = "server-1",
            ServerTimeMs = 1_700_000_000_500,
            HeartbeatIntervalMs = 25_000,
            MaxPayloadBytes = 81_920,
            ResumeSupported = true,
            PayloadFormat = "json"
        }, (expected, actual) =>
            expected.ProtocolVersion == actual.ProtocolVersion &&
            expected.FeatureBits == actual.FeatureBits &&
            expected.ServerDeviceId == actual.ServerDeviceId &&
            expected.ServerTimeMs == actual.ServerTimeMs &&
            expected.HeartbeatIntervalMs == actual.HeartbeatIntervalMs &&
            expected.MaxPayloadBytes == actual.MaxPayloadBytes &&
            expected.ResumeSupported == actual.ResumeSupported &&
            expected.PayloadFormat == actual.PayloadFormat);

        AssertRoundTrip(PacketCommand.GoAway, new GoAway
        {
            RetryAfterMs = 5_000,
            Reason = "shutdown",
            ServerHint = "gateway-a"
        }, (expected, actual) =>
            expected.RetryAfterMs == actual.RetryAfterMs &&
            expected.Reason == actual.Reason &&
            expected.ServerHint == actual.ServerHint);

        AssertRoundTrip(PacketCommand.Error, new ProtocolErrorFrame
        {
            Code = ProtocolErrorCode.InvalidPayload,
            Fatal = true,
            RetryAfterMs = 1_000,
            Message = "bad frame",
            OriginCommand = (ushort)PacketCommand.ChatMessage
        }, (expected, actual) =>
            expected.Code == actual.Code &&
            expected.Fatal == actual.Fatal &&
            expected.RetryAfterMs == actual.RetryAfterMs &&
            expected.Message == actual.Message &&
            expected.OriginCommand == actual.OriginCommand);
    }

    [Fact]
    public void MessageChannelFramesRoundTripThroughTheEncodeRegistry()
    {
        AssertRoundTrip(PacketCommand.ChatMessage, new ChatMessage
        {
            ClientMessageId = "client-msg-1",
            MessageId = "msg-1",
            ConversationId = "conv-1",
            TargetUserId = 20002,
            SenderUserId = 20001,
            Content = "hello \u2713 世界",
            SentAtMs = 1_700_000_000_500,
            AttachmentIds = new[] { "att-1", "att-2" },
            Attachments = new[]
            {
                new TcpAttachmentRef
                {
                    AttachmentId = "att-1",
                    FileName = "a.txt",
                    ContentType = "text/plain",
                    SizeBytes = 12,
                    Status = 1,
                    IsVoice = true,
                    VoiceCodec = "opus",
                    VoiceDurationMs = 3_500
                }
            },
            ReplyToMessageId = "msg-0",
            MentionedUserIds = new long[] { 20003, 20004 },
            MentionedRoles = new[] { "all" }
        }, (expected, actual) =>
            expected.ClientMessageId == actual.ClientMessageId &&
            expected.MessageId == actual.MessageId &&
            expected.ConversationId == actual.ConversationId &&
            expected.TargetUserId == actual.TargetUserId &&
            expected.SenderUserId == actual.SenderUserId &&
            expected.Content == actual.Content &&
            expected.SentAtMs == actual.SentAtMs &&
            expected.AttachmentIds!.SequenceEqual(actual.AttachmentIds!) &&
            expected.ReplyToMessageId == actual.ReplyToMessageId &&
            expected.MentionedUserIds!.SequenceEqual(actual.MentionedUserIds!) &&
            expected.MentionedRoles!.SequenceEqual(actual.MentionedRoles!) &&
            actual.Attachments is { Count: 1 } &&
            actual.Attachments[0].AttachmentId == "att-1" &&
            actual.Attachments[0].IsVoice == true &&
            actual.Attachments[0].VoiceCodec == "opus" &&
            actual.Attachments[0].VoiceDurationMs == 3_500);

        AssertRoundTrip(PacketCommand.MessageAcknowledgement, new MessageAcknowledgement
        {
            ClientMessageId = "client-msg-1",
            CommandId = "cmd-1",
            Accepted = true,
            AcknowledgedAtMs = 1_700_000_001_000
        }, (expected, actual) =>
            expected.ClientMessageId == actual.ClientMessageId &&
            expected.CommandId == actual.CommandId &&
            expected.Accepted == actual.Accepted &&
            expected.AcknowledgedAtMs == actual.AcknowledgedAtMs);

        AssertRoundTrip(PacketCommand.MessageReceipt, new MessageReceipt
        {
            RequestId = "req-1",
            ConversationId = "conv-1",
            LastReadMessageId = "msg-5",
            LastReadAtMs = 1_700_000_010_000,
            ReaderUserId = 20002,
            ReceiverUserId = 20001
        }, (expected, actual) =>
            expected.RequestId == actual.RequestId &&
            expected.ConversationId == actual.ConversationId &&
            expected.LastReadMessageId == actual.LastReadMessageId &&
            expected.LastReadAtMs == actual.LastReadAtMs &&
            expected.ReaderUserId == actual.ReaderUserId &&
            expected.ReceiverUserId == actual.ReceiverUserId);
    }

    [Fact]
    public void RealtimeStatusAndGroupUpdateFramesRoundTripThroughTheEncodeRegistry()
    {
        AssertRoundTrip(PacketCommand.TypingNotify, new TcpTypingNotify
        {
            TargetUserId = 20002,
            ConversationId = "conv-1",
            IsTyping = true
        }, (expected, actual) =>
            expected.TargetUserId == actual.TargetUserId &&
            expected.ConversationId == actual.ConversationId &&
            expected.IsTyping == actual.IsTyping);

        AssertRoundTrip(PacketCommand.TypingUpdate, new TcpTypingUpdate
        {
            SenderUserId = 20001,
            ConversationId = null,
            IsTyping = false
        }, (expected, actual) =>
            expected.SenderUserId == actual.SenderUserId &&
            actual.ConversationId is null &&
            expected.IsTyping == actual.IsTyping);

        AssertRoundTrip(PacketCommand.RoleChanged, new TcpRoleChangedUpdate
        {
            ConversationId = "conv-group-1",
            UserId = 20003,
            NewRole = TcpGroupMemberRole.Admin,
            PreviousRole = TcpGroupMemberRole.Member,
            ActorUserId = 20001,
            OccurredAtMs = 1_700_000_020_000
        }, (expected, actual) =>
            expected.ConversationId == actual.ConversationId &&
            expected.UserId == actual.UserId &&
            expected.NewRole == actual.NewRole &&
            expected.PreviousRole == actual.PreviousRole &&
            expected.ActorUserId == actual.ActorUserId &&
            expected.OccurredAtMs == actual.OccurredAtMs);

        AssertRoundTrip(PacketCommand.MembersAddedUpdate, new TcpMembersAddedUpdate
        {
            ConversationId = "conv-group-1",
            AddedUserIds = new long[] { 20003, 20004, 20005 },
            ActorUserId = 20001,
            Title = "project",
            OccurredAtMs = 1_700_000_021_000
        }, (expected, actual) =>
            expected.ConversationId == actual.ConversationId &&
            expected.AddedUserIds!.SequenceEqual(actual.AddedUserIds!) &&
            expected.ActorUserId == actual.ActorUserId &&
            expected.Title == actual.Title &&
            expected.OccurredAtMs == actual.OccurredAtMs);
    }

    [Fact]
    public void VoiceWaveformPeaks_RoundTripsThroughChatMessage_AndAbsentStaysNull()
    {
        var withWaveform = new ChatMessage
        {
            MessageId = "msg-wf-1",
            TargetUserId = 20002,
            SenderUserId = 20001,
            Content = "voice",
            SentAtMs = 1_700_000_000_500,
            AttachmentIds = new[] { "att-wf" },
            Attachments = new[]
            {
                new TcpAttachmentRef
                {
                    AttachmentId = "att-wf",
                    ContentType = "audio/wav",
                    SizeBytes = 112_044,
                    Status = 1,
                    IsVoice = true,
                    VoiceCodec = "pcm",
                    VoiceContainer = "wav",
                    VoiceDurationMs = 3_500,
                    VoiceSampleRateHz = 16_000,
                    VoiceChannels = 1,
                    VoiceWaveformPeaks = [0, 64, 128, 255, 128, 64]
                }
            }
        };
        AssertRoundTrip(PacketCommand.ChatMessage, withWaveform, (expected, actual) =>
        {
            var expectedPeaks = expected.Attachments![0].VoiceWaveformPeaks!;
            var actualPeaks = actual.Attachments![0].VoiceWaveformPeaks;
            return actualPeaks is not null && expectedPeaks.SequenceEqual(actualPeaks);
        });

        // 无 waveform 的语音附件：解码后保持 null（可选字段缺省语义）。
        var withoutWaveform = new ChatMessage
        {
            MessageId = "msg-wf-2",
            TargetUserId = 20002,
            SenderUserId = 20001,
            Content = "voice",
            SentAtMs = 1_700_000_000_500,
            AttachmentIds = new[] { "att-nowf" },
            Attachments = new[]
            {
                new TcpAttachmentRef
                {
                    AttachmentId = "att-nowf",
                    ContentType = "audio/wav",
                    Status = 1,
                    IsVoice = true
                }
            }
        };
        var payload = EncodeThroughRegistry(withoutWaveform);
        var decoded = TcpBinaryWireCodec.TryDecode(
            PacketCommand.ChatMessage, payload, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, decoded.Status);
        var actual = Assert.IsType<ChatMessage>(decoded.Value);
        Assert.Null(actual.Attachments![0].VoiceWaveformPeaks);
    }

    [Fact]
    public void EncodeRegistryFailsClosedWhenDestinationIsTooSmall()
    {
        var message = new ChatMessage
        {
            ClientMessageId = "client-msg-1",
            MessageId = "msg-1",
            ConversationId = "conv-1",
            TargetUserId = 20002,
            SenderUserId = 20001,
            Content = "hello",
            SentAtMs = 1_700_000_000_500
        };

        var full = EncodeThroughRegistry(message);

        var smallBuffer = new byte[8];
        var smallResult = TcpBinaryWireEncoder.TryEncode(message, smallBuffer, Limits);
        Assert.Equal(TcpBinaryWireEncodeStatus.EncodeFailure, smallResult.Status);
        Assert.Equal(BinaryStatus.DestinationTooSmall, smallResult.EncodeStatus);
        Assert.Equal(0, smallResult.Written);

        var retryBuffer = new byte[full.Length];
        var retryResult = TcpBinaryWireEncoder.TryEncode(message, retryBuffer, Limits);
        Assert.Equal(TcpBinaryWireEncodeStatus.Encoded, retryResult.Status);
        Assert.Equal(full.Length, retryResult.Written);
        Assert.True(full.AsSpan().SequenceEqual(retryBuffer));

        var decoded = TcpBinaryWireCodec.TryDecode(PacketCommand.ChatMessage, retryBuffer, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, decoded.Status);
        var actual = Assert.IsType<ChatMessage>(decoded.Value);
        Assert.Equal(message.Content, actual.Content);
    }

    [Fact]
    public void EncodeRegistryPayloadsDecodeAcrossSegmentedInput()
    {
        var payload = EncodeThroughRegistry(new TcpTypingNotify
        {
            TargetUserId = 20002,
            ConversationId = "conv-1",
            IsTyping = true
        });

        var segmented = SequenceFactory.Segmented(payload, 1);
        var decoded = TcpBinaryWireCodec.TryDecode(PacketCommand.TypingNotify, in segmented, Limits);
        Assert.Equal(TcpBinaryWireStatus.Decoded, decoded.Status);
        var actual = Assert.IsType<TcpTypingNotify>(decoded.Value);
        Assert.Equal((20002L, "conv-1", true), (actual.TargetUserId, actual.ConversationId, actual.IsTyping));
    }
}
