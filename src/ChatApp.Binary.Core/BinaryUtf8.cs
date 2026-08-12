using System.Buffers;
using System.Runtime.CompilerServices;

namespace ChatApp.Binary.Core;

internal static class BinaryUtf8
{
    public static bool IsValid(ReadOnlySpan<byte> source)
    {
        var state = new ValidationState();
        foreach (byte current in source)
        {
            if (!state.Consume(current))
            {
                return false;
            }
        }

        return state.Complete;
    }

    public static bool IsValid(in ReadOnlySequence<byte> source)
    {
        var state = new ValidationState();
        foreach (ReadOnlyMemory<byte> memory in source)
        {
            foreach (byte current in memory.Span)
            {
                if (!state.Consume(current))
                {
                    return false;
                }
            }
        }

        return state.Complete;
    }

    private struct ValidationState
    {
        private uint _codePoint;
        private uint _minimum;
        private int _remaining;

        public readonly bool Complete => _remaining == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Consume(byte current)
        {
            if (_remaining == 0)
            {
                if (current <= 0x7F)
                {
                    return true;
                }

                if (current is >= 0xC2 and <= 0xDF)
                {
                    Begin(current & 0x1FU, 0x80, 1);
                    return true;
                }

                if (current is >= 0xE0 and <= 0xEF)
                {
                    Begin(current & 0x0FU, 0x800, 2);
                    return true;
                }

                if (current is >= 0xF0 and <= 0xF4)
                {
                    Begin(current & 0x07U, 0x10000, 3);
                    return true;
                }

                return false;
            }

            if ((current & 0xC0) != 0x80)
            {
                return false;
            }

            _codePoint = (_codePoint << 6) | (uint)(current & 0x3F);
            _remaining--;
            if (_remaining != 0)
            {
                return true;
            }

            return _codePoint >= _minimum
                && _codePoint <= 0x10FFFF
                && _codePoint is not (>= 0xD800 and <= 0xDFFF);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Begin(uint prefix, uint minimum, int remaining)
        {
            _codePoint = prefix;
            _minimum = minimum;
            _remaining = remaining;
        }
    }
}
