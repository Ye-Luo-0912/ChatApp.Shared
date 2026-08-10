using System.Buffers;

namespace ChatApp.Shared.Protocol.Tcp.Binary;

public static class ITcpBinaryCodecExtensions
{
    public static void Encode<T>(
        this ITcpBinaryCodec<T> codec,
        IBufferWriter<byte> destination,
        T value)
        where T : class => codec.Encode(destination, value, TcpBinaryLimits.Default);

    public static bool TryDecode<T>(
        this ITcpBinaryCodec<T> codec,
        in ReadOnlySequence<byte> payload,
        out T? value,
        out TcpBinaryDecodeError error)
        where T : class => codec.TryDecode(
            payload,
            TcpBinaryLimits.Default,
            out value,
            out error);
}
