using System.Buffers;

namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>
/// Stateless, reflection-free codec contract. Implementations must be thread-safe and must not retain
/// the destination writer, source sequence, decoded object, or per-call mutable state.
/// </summary>
public interface ITcpBinaryCodec<T>
    where T : class
{
    /// <summary>
    /// Encodes into a caller-owned writer. If encoding fails after a prior field was emitted, the
    /// caller must discard or reset that payload writer; codecs never publish or retain partial data.
    /// </summary>
    void Encode(IBufferWriter<byte> destination, T value, TcpBinaryLimits limits);

    bool TryDecode(
        in ReadOnlySequence<byte> payload,
        TcpBinaryLimits limits,
        out T? value,
        out TcpBinaryDecodeError error);
}
