namespace ChatApp.Binary.Core;

/// <summary>Compile-time decoder contract intended for generated static decode adapters.</summary>
public interface IBinaryDecoder<TSelf, T>
    where TSelf : IBinaryDecoder<TSelf, T>
{
    static abstract BinaryStatus Read(ref BinaryReadCursor reader, out T? value);
}
