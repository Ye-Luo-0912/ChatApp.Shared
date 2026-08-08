namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Stable constants that define the ChatApp TCP wire header and protocol version.
/// Header integer fields are encoded in little-endian byte order.
/// </summary>
/// <remarks>
/// Payload-size limits are intentionally excluded because they are endpoint policy,
/// not part of the fixed frame layout.
/// </remarks>
public static class TcpFrameConstants
{
    /// <summary>The four-byte value at the start of every TCP frame.</summary>
    public const uint MagicNumber = 0x1A2B3C4D;

    /// <summary>Byte offset of the four-byte magic number.</summary>
    public const int MagicOffset = 0;

    /// <summary>Byte offset of the two-byte <see cref="PacketCommand"/> value.</summary>
    public const int CommandOffset = MagicOffset + sizeof(uint);

    /// <summary>Byte offset of the four-byte signed payload length.</summary>
    public const int LengthOffset = CommandOffset + sizeof(ushort);

    /// <summary>Total frame-header size: magic, command, and payload length.</summary>
    public const int HeaderSize = LengthOffset + sizeof(int);

    /// <summary>The latest protocol version understood by this package.</summary>
    public const ushort CurrentProtocolVersion = 1;
}
