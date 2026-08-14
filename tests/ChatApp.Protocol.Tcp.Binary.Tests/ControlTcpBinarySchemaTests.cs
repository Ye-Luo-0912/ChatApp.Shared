using System.Buffers;
using System.Security.Cryptography;
using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp;
using Xunit;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

public sealed class ControlTcpBinarySchemaTests
{
    [Fact]
    public void ControlPayloadsHaveStableGoldenAndSegmentedRoundTrip()
    {
        var goAway = new GoAway
        {
            RetryAfterMs = 1500,
            Reason = "shutdown",
            ServerHint = "node-a"
        };
        var resume = new ResumeResponse
        {
            Success = false,
            FailureKind = ResumeFailureKind.InvalidToken,
            ResumeToken = "new-token",
            UserId = 42,
            SessionId = "session-1",
            DeviceId = "device-1",
            LastConversationSequence = 99,
            ErrorMessage = "expired",
            RetryAfterMs = 3000
        };
        var error = new ProtocolErrorFrame
        {
            Code = ProtocolErrorCode.PayloadTooLarge,
            Fatal = true,
            RetryAfterMs = 2000,
            Message = "payload too large",
            OriginCommand = (ushort)PacketCommand.ChatMessage
        };

        byte[] goAwayPayload = EncodeGoAway(goAway);
        byte[] resumePayload = EncodeResume(resume);
        byte[] errorPayload = EncodeError(error);

        Assert.Equal("08B817120873687574646F776E1A066E6F64652D61", Convert.ToHexString(goAwayPayload));
        Assert.Equal("952A61DC40FAE1BD9EB1BBE45CB74AE87A7F495E8F831A988923F19AE1C9C37D", Convert.ToHexString(SHA256.HashData(goAwayPayload)));
        Assert.Equal("080010011A096E65772D746F6B656E20542A0973657373696F6E2D3132086465766963652D3138C60142076578706972656448F02E", Convert.ToHexString(resumePayload));
        Assert.Equal("6AEBD4381C0C65432D6D294208D6D1DFFD7924AEA37E7F73572CA28A8F7B729A", Convert.ToHexString(SHA256.HashData(resumePayload)));
        Assert.Equal("0815100118A01F22117061796C6F616420746F6F206C617267652865", Convert.ToHexString(errorPayload));
        Assert.Equal("0797A88759F00833C0556707939F39DE122E7568BBA107B82A8841BBA8CAA4E7", Convert.ToHexString(SHA256.HashData(errorPayload)));

        AssertGoAway(goAway, DecodeGoAway(goAwayPayload));
        AssertResume(resume, DecodeResume(resumePayload));
        AssertError(error, DecodeError(errorPayload));
    }

    [Fact]
    public void ControlPayloadsFailClosedForMalformedAndLimitedInput()
    {
        Assert.Equal(BinaryStatus.FieldsOutOfOrder, GoAwayBinaryDescriptor.TryDecode([0x12, 0x00, 0x08, 0x00], BinaryLimits.Default, out _));
        Assert.Equal(BinaryStatus.InvalidUtf8, GoAwayBinaryDescriptor.TryDecode([0x08, 0x00, 0x12, 0x01, 0xFF], BinaryLimits.Default, out _));
        Assert.Equal(BinaryStatus.DuplicateField, GoAwayBinaryDescriptor.TryDecode([0x08, 0x00, 0x08, 0x01], BinaryLimits.Default, out _));

        var limited = new BinaryLimits(256, 32, 4, 16, 16);
        var value = new GoAway { RetryAfterMs = 1, Reason = "12345" };
        Assert.Equal(BinaryStatus.StringTooLarge, GoAwayBinaryDescriptor.TryEncode(in value, new byte[256], limited, out int written));
        Assert.Equal(0, written);

        var origin = new ProtocolErrorFrame { Code = ProtocolErrorCode.InvalidPayload, Fatal = true, OriginCommand = ushort.MaxValue };
        Assert.Equal(BinaryStatus.Done, ProtocolErrorFrameBinaryDescriptor.TryDecode(
            SequenceFactory.Segmented(EncodeError(origin), 1),
            BinaryLimits.Default,
            out ProtocolErrorFrame? decoded));
        Assert.Equal(ushort.MaxValue, decoded!.OriginCommand);
    }

    private static byte[] EncodeGoAway(GoAway value) => Encode(value, GoAwayBinaryDescriptor.TryEncode);

    private static byte[] EncodeResume(ResumeResponse value) => Encode(value, ResumeResponseBinaryDescriptor.TryEncode);

    private static byte[] EncodeError(ProtocolErrorFrame value) => Encode(value, ProtocolErrorFrameBinaryDescriptor.TryEncode);

    private static byte[] Encode<T>(T value, EncodeDelegate<T> encode)
    {
        byte[] destination = new byte[BinaryLimits.Default.MaxMessageBytes];
        Assert.Equal(BinaryStatus.Done, encode(in value, destination, BinaryLimits.Default, out int written));
        return destination[..written].ToArray();
    }

    private static GoAway DecodeGoAway(byte[] payload)
    {
        Assert.Equal(BinaryStatus.Done, GoAwayBinaryDescriptor.TryDecode(SequenceFactory.Segmented(payload, 1), BinaryLimits.Default, out GoAway? value));
        return value!;
    }

    private static ResumeResponse DecodeResume(byte[] payload)
    {
        Assert.Equal(BinaryStatus.Done, ResumeResponseBinaryDescriptor.TryDecode(SequenceFactory.Segmented(payload, 1), BinaryLimits.Default, out ResumeResponse? value));
        return value!;
    }

    private static ProtocolErrorFrame DecodeError(byte[] payload)
    {
        Assert.Equal(BinaryStatus.Done, ProtocolErrorFrameBinaryDescriptor.TryDecode(SequenceFactory.Segmented(payload, 1), BinaryLimits.Default, out ProtocolErrorFrame? value));
        return value!;
    }

    private static void AssertGoAway(GoAway expected, GoAway actual) =>
        Assert.Equal((expected.RetryAfterMs, expected.Reason, expected.ServerHint), (actual.RetryAfterMs, actual.Reason, actual.ServerHint));

    private static void AssertResume(ResumeResponse expected, ResumeResponse actual) =>
        Assert.Equal(
            (expected.Success, expected.FailureKind, expected.ResumeToken, expected.UserId, expected.SessionId,
                expected.DeviceId, expected.LastConversationSequence, expected.ErrorMessage, expected.RetryAfterMs),
            (actual.Success, actual.FailureKind, actual.ResumeToken, actual.UserId, actual.SessionId,
                actual.DeviceId, actual.LastConversationSequence, actual.ErrorMessage, actual.RetryAfterMs));

    private static void AssertError(ProtocolErrorFrame expected, ProtocolErrorFrame actual) =>
        Assert.Equal((expected.Code, expected.Fatal, expected.RetryAfterMs, expected.Message, expected.OriginCommand),
            (actual.Code, actual.Fatal, actual.RetryAfterMs, actual.Message, actual.OriginCommand));

    private delegate BinaryStatus EncodeDelegate<T>(in T value, Span<byte> destination, BinaryLimits limits, out int written);
}
