namespace ChatApp.Binary.Core;

/// <summary>
/// Stable, allocation-free result codes for untrusted input, configured limits, and destination
/// capacity. Programming errors such as invalid API arguments still throw normally.
/// </summary>
public enum BinaryStatus : byte
{
    Done = 0,
    DestinationTooSmall,
    ArithmeticOverflow,
    MessageTooLarge,
    FieldTooLarge,
    StringTooLarge,
    ByteArrayTooLarge,
    TooManyFields,
    InvalidFieldNumber,
    UnsupportedWireType,
    WireTypeMismatch,
    FieldsOutOfOrder,
    DuplicateField,
    MalformedVarInt,
    Truncated,
    InvalidUtf8,
    ValueOutOfRange,
    TrailingData,
    MissingRequiredField
}
