using System.Runtime.CompilerServices;

namespace ChatApp.Binary.Core;

internal static class BinaryEncoding
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong CreateTag(int fieldNumber, BinaryWireType wireType) =>
        ((ulong)(uint)fieldNumber << 3) | (byte)wireType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ZigZagEncode(int value) =>
        unchecked((uint)((value << 1) ^ (value >> 31)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ZigZagEncode(long value) =>
        unchecked((ulong)((value << 1) ^ (value >> 63)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ZigZagDecode(uint value) =>
        unchecked((int)((value >> 1) ^ (uint)-(int)(value & 1)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ZigZagDecode(ulong value) =>
        unchecked((long)((value >> 1) ^ (ulong)-(long)(value & 1)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int VarIntSize(ulong value)
    {
        var size = 1;
        while (value >= 0x80)
        {
            value >>= 7;
            size++;
        }

        return size;
    }
}
