using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ChatApp.Shared.Protocol.Tcp.Binary;

/// <summary>
/// Native-pointer fast path for already-bounded, contiguous fixed-width values. Pointer access is
/// deliberately isolated here; segmented input, length parsing, varints, and object construction
/// remain on the safe reference path.
/// </summary>
internal static unsafe class NativeFixedWidth
{
    // x86/x64 and Arm64 support the unaligned little-endian loads/stores used by the tagged wire.
    // Other architectures keep the portable BinaryPrimitives implementation.
    private static readonly bool UseNativePointer =
        BitConverter.IsLittleEndian
        && RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.X64 or Architecture.Arm64;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(source.Length, sizeof(uint));
        if (!UseNativePointer)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(source);
        }

        fixed (byte* start = source)
        {
            return *(uint*)start;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> source)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(source.Length, sizeof(ulong));
        if (!UseNativePointer)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(source);
        }

        fixed (byte* start = source)
        {
            return *(ulong*)start;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt32LittleEndian(Span<byte> destination, uint value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, sizeof(uint));
        if (!UseNativePointer)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
            return;
        }

        fixed (byte* start = destination)
        {
            *(uint*)start = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUInt64LittleEndian(Span<byte> destination, ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, sizeof(ulong));
        if (!UseNativePointer)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
            return;
        }

        fixed (byte* start = destination)
        {
            *(ulong*)start = value;
        }
    }
}
