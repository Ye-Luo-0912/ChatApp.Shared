namespace ChatApp.Binary.Core;

/// <summary>Compile-time decoder contract for segmented input without payload coalescing.</summary>
public interface IBinarySequenceDecoder<TSelf, T>
    where TSelf : IBinarySequenceDecoder<TSelf, T>
{
    static abstract BinaryStatus Read(ref BinarySequenceReadCursor reader, out T? value);
}
