namespace ChatApp.Shared.Protocol.Tcp;

/// <summary>
/// Payload carried by <see cref="PacketCommand.Error"/>.
/// </summary>
public sealed class ProtocolErrorFrame
{
    public ProtocolErrorCode Code { get; set; }

    public bool Fatal { get; set; }

    public int? RetryAfterMs { get; set; }

    public string? Message { get; set; }

    public ushort? OriginCommand { get; set; }
}
