namespace ChatApp.Binary.Core;

/// <summary>
/// Compile-time encoder contract. Implementations are ordinary handwritten source; no reflection,
/// runtime registration, generated encoder, or measurement pass is required.
/// </summary>
/// <typeparam name="TSelf">The implementing static-adapter type.</typeparam>
/// <typeparam name="T">The value type being encoded.</typeparam>
public interface IBinaryEncoder<TSelf, T>
    where TSelf : IBinaryEncoder<TSelf, T>
{
    static abstract BinaryStatus Write(ref BinaryWriteCursor writer, in T value);
}
