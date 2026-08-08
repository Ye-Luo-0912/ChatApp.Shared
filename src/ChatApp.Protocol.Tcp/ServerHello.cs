namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Server-to-client handshake response carried by <see cref="PacketCommand.ServerHello"/>.
/// </summary>
public sealed class ServerHello
{
    public ushort ProtocolVersion { get; set; } = 1;

    public uint FeatureBits { get; set; }

    public string ServerDeviceId { get; set; } = string.Empty;

    public long ServerTimeMs { get; set; }

    public int HeartbeatIntervalMs { get; set; }

    public int MaxPayloadBytes { get; set; }

    public bool ResumeSupported { get; set; }

    public string PayloadFormat { get; set; } = ProtocolPayloadFormat.Json;
}
