namespace ChatApp.Shared.Protocol.Tcp.Binary;

public enum TcpBinaryDecodeError : byte
{
    None = 0,
    MessageTooLarge,
    TooManyFields,
    InvalidFieldNumber,
    UnsupportedWireType,
    WireTypeMismatch,
    MalformedVarInt,
    Truncated,
    FieldTooLarge,
    InvalidUtf8,
    ValueOutOfRange,
    DuplicateField,
    MissingRequiredField
}
