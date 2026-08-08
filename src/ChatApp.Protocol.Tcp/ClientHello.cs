namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Client-to-server handshake request carried by <see cref="PacketCommand.ClientHello"/>.
/// </summary>
public sealed class ClientHello
{
    public ushort ProtocolVersion { get; set; } = 1;

    public uint FeatureBits { get; set; }

    public string? InstallationId { get; set; }

    public long ClientTimeMs { get; set; }

    public string? ResumeToken { get; set; }

    public int? MaxPayloadBytes { get; set; }
}
