using System.Buffers;
using System.Runtime.CompilerServices;

namespace ChatApp.Binary.Core;

/// <summary>Single-pass adapters for compile-time encoders and bounded decoders.</summary>
public static class BinaryCodec
{
    /// <summary>
    /// Encodes once into caller-owned contiguous capacity. On failure, <paramref name="written"/>
    /// is zero and destination contents are unspecified.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BinaryStatus TryEncode<TEncoder, T>(
        in T value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written)
        where TEncoder : IBinaryEncoder<TEncoder, T>
    {
        limits.Validate(nameof(limits));
        written = 0;
        if (limits.CurrentNestingDepth >= limits.MaxNestingDepth)
        {
            return BinaryStatus.NestingTooDeep;
        }
        BinaryStatus status = EncodeNative<TEncoder, T>(
            in value,
            destination,
            limits,
            out int emitted);
        if (status == BinaryStatus.Done)
        {
            written = emitted;
        }

        return status;
    }

    /// <summary>Decodes bounded contiguous input using the native cursor fast path.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BinaryStatus TryDecode<TDecoder, T>(
        ReadOnlySpan<byte> source,
        BinaryLimits limits,
        out T? value)
        where TDecoder : IBinaryDecoder<TDecoder, T>
    {
        limits.Validate(nameof(limits));
        value = default;
        if (limits.CurrentNestingDepth >= limits.MaxNestingDepth)
        {
            return BinaryStatus.NestingTooDeep;
        }
        if (source.Length > limits.MaxMessageBytes)
        {
            return BinaryStatus.MessageTooLarge;
        }

        return DecodeNative<TDecoder, T>(source, limits, out value);
    }

    /// <summary>Decodes bounded segmented input without coalescing the payload.</summary>
    public static BinaryStatus TryDecode<TDecoder, T>(
        in ReadOnlySequence<byte> source,
        BinaryLimits limits,
        out T? value)
        where TDecoder : IBinarySequenceDecoder<TDecoder, T>
    {
        limits.Validate(nameof(limits));
        value = default;
        if (limits.CurrentNestingDepth >= limits.MaxNestingDepth)
        {
            return BinaryStatus.NestingTooDeep;
        }
        if (source.Length > limits.MaxMessageBytes)
        {
            return BinaryStatus.MessageTooLarge;
        }

        var reader = new BinarySequenceReadCursor(in source, limits);
        BinaryStatus status = TDecoder.Read(ref reader, out T? decoded);
        if (status != BinaryStatus.Done)
        {
            return status;
        }

        if (reader.Status != BinaryStatus.Done)
        {
            return reader.Status;
        }

        if (!reader.End)
        {
            return BinaryStatus.TrailingData;
        }

        value = decoded;
        return BinaryStatus.Done;
    }

    private static unsafe BinaryStatus EncodeNative<TEncoder, T>(
        in T value,
        Span<byte> destination,
        BinaryLimits limits,
        out int written)
        where TEncoder : IBinaryEncoder<TEncoder, T>
    {
        written = 0;
        fixed (byte* start = destination)
        {
            var writer = new BinaryWriteCursor(destination, start, limits);
            BinaryStatus status = TEncoder.Write(ref writer, in value);
            if (status != BinaryStatus.Done)
            {
                return status;
            }

            if (writer.Status != BinaryStatus.Done)
            {
                return writer.Status;
            }

            written = writer.WrittenCount;
            return BinaryStatus.Done;
        }
    }

    private static unsafe BinaryStatus DecodeNative<TDecoder, T>(
        ReadOnlySpan<byte> source,
        BinaryLimits limits,
        out T? value)
        where TDecoder : IBinaryDecoder<TDecoder, T>
    {
        fixed (byte* start = source)
        {
            var reader = new BinaryReadCursor(source, start, limits);
            BinaryStatus status = TDecoder.Read(ref reader, out value);
            if (status != BinaryStatus.Done)
            {
                value = default;
                return status;
            }

            if (reader.Status != BinaryStatus.Done)
            {
                value = default;
                return reader.Status;
            }

            if (!reader.End)
            {
                value = default;
                return BinaryStatus.TrailingData;
            }

            return BinaryStatus.Done;
        }
    }
}
