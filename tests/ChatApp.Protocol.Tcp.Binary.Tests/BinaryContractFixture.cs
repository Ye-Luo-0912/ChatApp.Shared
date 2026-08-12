using ChatApp.Binary.Core;
using ChatApp.Shared.Protocol.Tcp.Binary;
using ChatApp.Shared.Protocol.Tcp.Binary.Generation;

namespace ChatApp.Protocol.Tcp.Binary.Tests;

internal enum FixtureState : short
{
    Unknown = 0,
    Active = 2
}

internal sealed class BinaryContractFixture
{
    public int Id { get; set; }

    public long Delta { get; set; }

    public uint Count { get; set; }

    public bool Enabled { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Note { get; set; }

    public byte[] Data { get; set; } = [];

    public int? OptionalNumber { get; set; }

    public FixtureState State { get; set; }

    public double Score { get; set; }

    public sbyte SByteValue { get; set; }

    public byte ByteValue { get; set; }

    public short ShortValue { get; set; }

    public ushort UShortValue { get; set; }

    public ulong ULongValue { get; set; }

    public float FloatValue { get; set; }

    public FixtureState? OptionalState { get; set; }
}

[TcpBinaryContract(typeof(BinaryContractFixture))]
[TcpBinaryField(BinaryContractFixtureEncoder.IdField, nameof(BinaryContractFixture.Id))]
[TcpBinaryField(BinaryContractFixtureEncoder.DeltaField, nameof(BinaryContractFixture.Delta))]
[TcpBinaryField(BinaryContractFixtureEncoder.CountField, nameof(BinaryContractFixture.Count))]
[TcpBinaryField(BinaryContractFixtureEncoder.EnabledField, nameof(BinaryContractFixture.Enabled))]
[TcpBinaryField(BinaryContractFixtureEncoder.NameField, nameof(BinaryContractFixture.Name))]
[TcpBinaryField(BinaryContractFixtureEncoder.NoteField, nameof(BinaryContractFixture.Note))]
[TcpBinaryField(BinaryContractFixtureEncoder.DataField, nameof(BinaryContractFixture.Data))]
[TcpBinaryField(BinaryContractFixtureEncoder.OptionalNumberField, nameof(BinaryContractFixture.OptionalNumber))]
[TcpBinaryField(BinaryContractFixtureEncoder.StateField, nameof(BinaryContractFixture.State))]
[TcpBinaryField(BinaryContractFixtureEncoder.ScoreField, nameof(BinaryContractFixture.Score))]
[TcpBinaryField(BinaryContractFixtureEncoder.SByteValueField, nameof(BinaryContractFixture.SByteValue))]
[TcpBinaryField(BinaryContractFixtureEncoder.ByteValueField, nameof(BinaryContractFixture.ByteValue))]
[TcpBinaryField(BinaryContractFixtureEncoder.ShortValueField, nameof(BinaryContractFixture.ShortValue))]
[TcpBinaryField(BinaryContractFixtureEncoder.UShortValueField, nameof(BinaryContractFixture.UShortValue))]
[TcpBinaryField(BinaryContractFixtureEncoder.ULongValueField, nameof(BinaryContractFixture.ULongValue))]
[TcpBinaryField(BinaryContractFixtureEncoder.FloatValueField, nameof(BinaryContractFixture.FloatValue))]
[TcpBinaryField(BinaryContractFixtureEncoder.OptionalStateField, nameof(BinaryContractFixture.OptionalState))]
internal static partial class BinaryContractFixtureDescriptor
{
    public static BinaryStatus TryEncode(
        in BinaryContractFixture value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<BinaryContractFixtureEncoder, BinaryContractFixture>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct BinaryContractFixtureEncoder :
    IBinaryEncoder<BinaryContractFixtureEncoder, BinaryContractFixture>
{
    internal const int IdField = 1;
    internal const int DeltaField = 2;
    internal const int CountField = 3;
    internal const int EnabledField = 4;
    internal const int NameField = 5;
    internal const int NoteField = 6;
    internal const int DataField = 7;
    internal const int OptionalNumberField = 8;
    internal const int StateField = 9;
    internal const int ScoreField = 10;
    internal const int SByteValueField = 11;
    internal const int ByteValueField = 12;
    internal const int ShortValueField = 13;
    internal const int UShortValueField = 14;
    internal const int ULongValueField = 15;
    internal const int FloatValueField = 16;
    internal const int OptionalStateField = 17;

    public static BinaryStatus Write(ref BinaryWriteCursor writer, in BinaryContractFixture value)
    {
        writer.WriteInt32(IdField, value.Id);
        writer.WriteInt64(DeltaField, value.Delta);
        writer.WriteUInt32(CountField, value.Count);
        writer.WriteBool(EnabledField, value.Enabled);
        writer.WriteString(NameField, value.Name);
        if (value.Note is { } note)
        {
            writer.WriteString(NoteField, note);
        }

        ReadOnlySpan<byte> data = value.Data;
        writer.WriteBytes(DataField, data);
        if (value.OptionalNumber is { } optionalNumber)
        {
            writer.WriteInt32(OptionalNumberField, optionalNumber);
        }

        writer.WriteInt32(StateField, (short)value.State);
        writer.WriteDouble(ScoreField, value.Score);
        writer.WriteInt32(SByteValueField, value.SByteValue);
        writer.WriteUInt32(ByteValueField, value.ByteValue);
        writer.WriteInt32(ShortValueField, value.ShortValue);
        writer.WriteUInt32(UShortValueField, value.UShortValue);
        writer.WriteUInt64(ULongValueField, value.ULongValue);
        writer.WriteSingle(FloatValueField, value.FloatValue);
        if (value.OptionalState is { } optionalState)
        {
            writer.WriteInt32(OptionalStateField, (short)optionalState);
        }

        return writer.Status;
    }
}

internal sealed class RequiredBinaryContractFixture
{
    public required int RequiredId { get; set; }
}

[TcpBinaryContract(typeof(RequiredBinaryContractFixture))]
[TcpBinaryField(RequiredBinaryContractFixtureEncoder.RequiredIdField, nameof(RequiredBinaryContractFixture.RequiredId))]
internal static partial class RequiredBinaryContractFixtureDescriptor
{
    public static BinaryStatus TryEncode(
        in RequiredBinaryContractFixture value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written) =>
        BinaryCodec.TryEncode<RequiredBinaryContractFixtureEncoder, RequiredBinaryContractFixture>(
            in value,
            destination,
            limits,
            out written);
}

internal readonly struct RequiredBinaryContractFixtureEncoder :
    IBinaryEncoder<RequiredBinaryContractFixtureEncoder, RequiredBinaryContractFixture>
{
    internal const int RequiredIdField = 1;

    public static BinaryStatus Write(
        ref BinaryWriteCursor writer,
        in RequiredBinaryContractFixture value) =>
        writer.WriteInt32(RequiredIdField, value.RequiredId);
}
