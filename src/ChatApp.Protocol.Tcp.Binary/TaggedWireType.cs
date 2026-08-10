namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>Wire types supported by the bounded tagged payload format.</summary>
public enum TaggedWireType : byte
{
    VarInt = 0,
    Fixed64 = 1,
    LengthDelimited = 2,
    Fixed32 = 5
}
