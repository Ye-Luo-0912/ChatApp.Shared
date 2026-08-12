namespace ChatApp.Binary.Core;

/// <summary>Supported binary wire kinds.</summary>
public enum BinaryWireType : byte
{
    VarInt = 0,
    Fixed64 = 1,
    LengthDelimited = 2,
    Fixed32 = 5
}
