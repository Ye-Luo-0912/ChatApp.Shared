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
[TcpBinaryField(1, nameof(BinaryContractFixture.Id))]
[TcpBinaryField(2, nameof(BinaryContractFixture.Delta))]
[TcpBinaryField(3, nameof(BinaryContractFixture.Count))]
[TcpBinaryField(4, nameof(BinaryContractFixture.Enabled))]
[TcpBinaryField(5, nameof(BinaryContractFixture.Name))]
[TcpBinaryField(6, nameof(BinaryContractFixture.Note))]
[TcpBinaryField(7, nameof(BinaryContractFixture.Data))]
[TcpBinaryField(8, nameof(BinaryContractFixture.OptionalNumber))]
[TcpBinaryField(9, nameof(BinaryContractFixture.State))]
[TcpBinaryField(10, nameof(BinaryContractFixture.Score))]
[TcpBinaryField(11, nameof(BinaryContractFixture.SByteValue))]
[TcpBinaryField(12, nameof(BinaryContractFixture.ByteValue))]
[TcpBinaryField(13, nameof(BinaryContractFixture.ShortValue))]
[TcpBinaryField(14, nameof(BinaryContractFixture.UShortValue))]
[TcpBinaryField(15, nameof(BinaryContractFixture.ULongValue))]
[TcpBinaryField(16, nameof(BinaryContractFixture.FloatValue))]
[TcpBinaryField(17, nameof(BinaryContractFixture.OptionalState))]
internal static partial class BinaryContractFixtureDescriptor;

internal sealed class RequiredBinaryContractFixture
{
    public required int RequiredId { get; set; }
}

[TcpBinaryContract(typeof(RequiredBinaryContractFixture))]
[TcpBinaryField(1, nameof(RequiredBinaryContractFixture.RequiredId))]
internal static partial class RequiredBinaryContractFixtureDescriptor;
